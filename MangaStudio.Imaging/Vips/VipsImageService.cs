using MangaStudio.Core.DTOs;
using MangaStudio.Core.Interfaces;
using NetVips;
using Serilog;

namespace MangaStudio.Imaging.Vips;

public sealed class VipsImageService : IImageService
{
    private readonly ILogger _logger;

    public VipsImageService(ILogger logger)
    {
        _logger = logger.ForContext<VipsImageService>();

        // Silence libvips's own internal logging — we handle errors ourselves.
        NetVips.NetVips.Leak = false;
    }

    // ── ReadInfo ──────────────────────────────────────────────────────────────
    // libvips reads only the image header here — no pixel data enters RAM.
    // This is the correct method to use in ChapterMetadataBuilder.
    public ImageInfo ReadInfo(string path)
    {
        try
        {
            using var image = Image.NewFromFile(path, access: Enums.Access.Sequential);
            return new ImageInfo
            {
                FilePath = path,
                Width = image.Width,
                Height = image.Height,
                FileSizeBytes = new FileInfo(path).Length
            };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "ReadInfo failed for {Path}", path);
            throw;
        }
    }

    // ── Load ──────────────────────────────────────────────────────────────────
    // Sequential access tells libvips to stream the file top-to-bottom,
    // keeping only one strip in RAM at a time. Best for tall manga images.
    public IImage Load(string path)
    {
        try
        {
            var image = Image.NewFromFile(path, access: Enums.Access.Sequential);
            _logger.Debug("Loaded {Path} ({W}x{H})", path, image.Width, image.Height);
            return new VipsImageWrapper(image, path);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Load failed for {Path}", path);
            throw;
        }
    }

    // ── Resize ────────────────────────────────────────────────────────────────
    // libvips Resize uses a high-quality Lanczos3 kernel by default.
    // scale = targetWidth / currentWidth  (e.g. 0.9 shrinks, 1.1 enlarges)
    public IImage Resize(IImage input, int targetWidth)
    {
        GuardType<VipsImageWrapper>(input, nameof(input));
        var wrapper = (VipsImageWrapper)input;

        double scale = (double)targetWidth / wrapper.Width;

        // scale == 1.0 means no size change, but we still return a fresh handle
        // so the caller always owns a distinct object it can safely dispose.
        var resized = wrapper.Inner.Resize(scale);

        _logger.Debug("Resized {W}→{TW}px ({Path})", wrapper.Width, targetWidth, input.SourcePath);
        return new VipsImageWrapper(resized, input.SourcePath);
    }

    // ── Crop ──────────────────────────────────────────────────────────────────
    public IImage Crop(IImage input, int x, int y, int width, int height)
    {
        GuardType<VipsImageWrapper>(input, nameof(input));
        var wrapper = (VipsImageWrapper)input;

        var cropped = wrapper.Inner.Crop(x, y, width, height);
        return new VipsImageWrapper(cropped, input.SourcePath);
    }

    // ── Canvas: Create ────────────────────────────────────────────────────────
    public IStitchCanvas CreateCanvas(int width)
    {
        return new VipsStitchCanvas { Width = width };
    }

    // ── Canvas: Append ────────────────────────────────────────────────────────
    // We call .Copy() to get a stable lazy reference to the current state of
    // the image pipeline. This is a libvips no-op (no pixels copied) — it just
    // increments a reference counter so the handle stays valid after the caller
    // disposes their original IImage.
    public IStitchCanvas AppendToCanvas(IStitchCanvas canvas, IImage image)
    {
        GuardType<VipsStitchCanvas>(canvas, nameof(canvas));
        GuardType<VipsImageWrapper>(image, nameof(image));

        var vipsCanvas = (VipsStitchCanvas)canvas;
        var wrapper    = (VipsImageWrapper)image;

        vipsCanvas.Layers.Add(wrapper.Inner.Copy());
        vipsCanvas.CurrentHeight += image.Height;

        _logger.Debug("Appended image ({W}x{H}), canvas now {Total}px tall",
            image.Width, image.Height, vipsCanvas.CurrentHeight);

        return vipsCanvas;
    }

    // ── Canvas: Flush ─────────────────────────────────────────────────────────
    // Arrayjoin stitches the lazy VipsImage list into one tall image and then
    // the encoder writes it to disk in a single streaming pass — never loading
    // the full pixel data into RAM all at once.
    // After saving, the canvas is cleared and reusable immediately.
    public void FlushCanvas(IStitchCanvas canvas, string outputPath, ExportOptions options)
    {
        GuardType<VipsStitchCanvas>(canvas, nameof(canvas));
        var vipsCanvas = (VipsStitchCanvas)canvas;

        if (vipsCanvas.IsEmpty)
        {
            _logger.Warning("FlushCanvas called on an empty canvas — nothing written");
            return;
        }

        _logger.Information("Flushing canvas ({Count} images, {H}px) → {Path}",
            vipsCanvas.Layers.Count, vipsCanvas.CurrentHeight, outputPath);

        // across: 1  means one column — all images stacked vertically
        using var joined = Image.Arrayjoin(vipsCanvas.Layers.ToArray(), across: 1);
        WriteToFile(joined, outputPath, options);

        // Dispose the layer handles and reset height so the canvas can be reused
        foreach (var img in vipsCanvas.Layers)
            img.Dispose();

        vipsCanvas.Layers.Clear();
        vipsCanvas.CurrentHeight = 0;
        // Width is NOT reset — it remains constant for the whole chapter
    }

    // ── Save (single image) ───────────────────────────────────────────────────
    public void Save(IImage input, string path, ExportOptions options)
    {
        GuardType<VipsImageWrapper>(input, nameof(input));
        var wrapper = (VipsImageWrapper)input;
        WriteToFile(wrapper.Inner, path, options);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private void WriteToFile(Image image, string path, ExportOptions options)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        switch (options.Format)
        {
            case ExportFormat.WebP:
                // lossless: false  →  lossy WebP at the given quality
                image.Webpsave(path, q: options.Quality, lossless: false);
                break;

            case ExportFormat.Jpg:
                // stripAll removes EXIF/metadata to keep file size down
                image.Jpegsave(path, q: options.Quality);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(options.Format),
                    $"Unsupported format: {options.Format}");
        }

        _logger.Debug("Written {Path} ({Format} q={Q})", path, options.Format, options.Quality);
    }

    // Guard method: gives a clear error when the wrong wrapper type is passed.
    // Without this you'd get a confusing InvalidCastException with no context.
    private static void GuardType<TExpected>(object obj, string paramName)
    {
        if (obj is not TExpected)
            throw new ArgumentException(
                $"Expected {typeof(TExpected).Name} but received {obj.GetType().Name}. " +
                $"Do not mix IImage/IStitchCanvas objects from different IImageService implementations.",
                paramName);
    }
}