using CoreImageInfo = MangaStudio.Core.DTOs.ImageInfo;
using MangaStudio.Core.DTOs;
using MangaStudio.Core.Interfaces;
using Serilog;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SharpImageInfo = SixLabors.ImageSharp.ImageInfo;

namespace MangaStudio.Imaging.Sharp;

public sealed class ImageSharpImageService : IImageService
{
    private readonly ILogger _logger;

    public ImageSharpImageService(ILogger logger)
    {
        _logger = logger.ForContext<ImageSharpImageService>();
    }

    // ── ReadInfo ──────────────────────────────────────────────────────────────
    // Image.Identify reads only the file header — far cheaper than a full Load.
    public CoreImageInfo ReadInfo(string path)
    {
        try
        {
            SharpImageInfo info = Image.Identify(path);
            return new CoreImageInfo
            {
                FilePath = path,
                Width = info.Width,
                Height = info.Height,
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
    // ImageSharp decodes the entire image into RAM on Load.
    // That is acceptable for the fallback backend.
    public IImage Load(string path)
    {
        try
        {
            var image = Image.Load<Rgba32>(path);
            _logger.Debug("Loaded {Path} ({W}x{H})", path, image.Width, image.Height);
            return new ImageSharpWrapper(image, path);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Load failed for {Path}", path);
            throw;
        }
    }

    // ── Resize ────────────────────────────────────────────────────────────────
    public IImage Resize(IImage input, int targetWidth)
    {
        GuardType<ImageSharpWrapper>(input, nameof(input));
        var wrapper = (ImageSharpWrapper)input;

        // Calculate the proportional height so we don't distort the image
        int targetHeight = (int)Math.Round((double)wrapper.Height * targetWidth / wrapper.Width);

        // Clone creates a new image — the original is not modified
        var resized = wrapper.Inner.Clone(ctx =>
            ctx.Resize(targetWidth, targetHeight, KnownResamplers.Lanczos3));

        _logger.Debug("Resized {W}→{TW}px ({Path})", wrapper.Width, targetWidth, input.SourcePath);
        return new ImageSharpWrapper(resized, input.SourcePath);
    }

    // ── Crop ──────────────────────────────────────────────────────────────────
    public IImage Crop(IImage input, int x, int y, int width, int height)
    {
        GuardType<ImageSharpWrapper>(input, nameof(input));
        var wrapper = (ImageSharpWrapper)input;

        var rect    = new Rectangle(x, y, width, height);
        var cropped = wrapper.Inner.Clone(ctx => ctx.Crop(rect));
        return new ImageSharpWrapper(cropped, input.SourcePath);
    }

    // ── Canvas: Create ────────────────────────────────────────────────────────
    public IStitchCanvas CreateCanvas(int width)
    {
        return new ImageSharpStitchCanvas { Width = width };
    }

    // ── Canvas: Append ───────────────────────────────────────────────────────
    // We clone the image before storing it so the caller can safely
    // dispose their copy without affecting the canvas layer.
    public IStitchCanvas AppendToCanvas(IStitchCanvas canvas, IImage image)
    {
        GuardType<ImageSharpStitchCanvas>(canvas, nameof(canvas));
        GuardType<ImageSharpWrapper>(image, nameof(image));

        var sharpCanvas = (ImageSharpStitchCanvas)canvas;
        var wrapper     = (ImageSharpWrapper)image;

        sharpCanvas.Layers.Add(wrapper.Inner.Clone());
        sharpCanvas.CurrentHeight += image.Height;

        _logger.Debug("Appended image ({W}x{H}), canvas now {Total}px tall",
            image.Width, image.Height, sharpCanvas.CurrentHeight);

        return sharpCanvas;
    }

    // ── Canvas: Flush ─────────────────────────────────────────────────────────
    // Composites all layers onto a single white canvas, saves it, then resets.
    public void FlushCanvas(IStitchCanvas canvas, string outputPath, ExportOptions options)
    {
        GuardType<ImageSharpStitchCanvas>(canvas, nameof(canvas));
        var sharpCanvas = (ImageSharpStitchCanvas)canvas;

        if (sharpCanvas.IsEmpty)
        {
            _logger.Warning("FlushCanvas called on an empty canvas — nothing written");
            return;
        }

        _logger.Information("Flushing canvas ({Count} images, {H}px) → {Path}",
            sharpCanvas.Layers.Count, sharpCanvas.CurrentHeight, outputPath);

        // Allocate one output image tall enough to hold all layers
        using var result = new Image<Rgba32>(sharpCanvas.Width, sharpCanvas.CurrentHeight, Color.White);

        int yOffset = 0;
        foreach (var layer in sharpCanvas.Layers)
        {
            // DrawImage composites 'layer' onto 'result' at the given point
            var point = new SixLabors.ImageSharp.Point(0, yOffset);
            result.Mutate(ctx => ctx.DrawImage(layer, point, opacity: 1f));
            yOffset += layer.Height;
        }

        WriteToFile(result, outputPath, options);

        // Dispose layers and reset the canvas for reuse
        foreach (var img in sharpCanvas.Layers)
            img.Dispose();

        sharpCanvas.Layers.Clear();
        sharpCanvas.CurrentHeight = 0;
    }

    // ── Save (single image) ───────────────────────────────────────────────────
    public void Save(IImage input, string path, ExportOptions options)
    {
        GuardType<ImageSharpWrapper>(input, nameof(input));
        var wrapper = (ImageSharpWrapper)input;
        WriteToFile(wrapper.Inner, path, options);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private void WriteToFile(Image<Rgba32> image, string path, ExportOptions options)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        switch (options.Format)
        {
            case ExportFormat.WebP:
                image.Save(path, new WebpEncoder { Quality = options.Quality });
                break;

            case ExportFormat.Jpg:
                image.Save(path, new JpegEncoder { Quality = options.Quality });
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(options.Format),
                    $"Unsupported format: {options.Format}");
        }

        _logger.Debug("Written {Path} ({Format} q={Q})", path, options.Format, options.Quality);
    }

    private static void GuardType<TExpected>(object obj, string paramName)
    {
        if (obj is not TExpected)
            throw new ArgumentException(
                $"Expected {typeof(TExpected).Name} but received {obj.GetType().Name}. " +
                $"Do not mix IImage/IStitchCanvas objects from different IImageService implementations.",
                paramName);
    }
}