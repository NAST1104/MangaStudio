using MangaStudio.Core.Interfaces;
using Serilog;

namespace MangaStudio.Core.Processing;

public sealed class WidthNormalizer : IWidthNormalizer
{
    private readonly IImageService _imageService;
    private readonly ILogger _logger;

    // Tolerance in pixels — widths within this range are considered equal.
    // Avoids unnecessary resize operations for trivial differences (e.g. 899 vs 900).
    private const int WidthTolerance = 2;

    public WidthNormalizer(IImageService imageService, ILogger logger)
    {
        _imageService = imageService;
        _logger = logger.ForContext<WidthNormalizer>();
    }

    // Returns an IImage the caller owns and must dispose.
    // Returns null if the image cannot be loaded or resized — the pipeline
    // logs this and skips the image rather than aborting the chapter.
    public IImage? NormalizeWidth(string imagePath, int targetWidth)
    {
        IImage? loaded = null;

        try
        {
            loaded = _imageService.Load(imagePath);

            // Check if resize is actually needed
            if (Math.Abs(loaded.Width - targetWidth) <= WidthTolerance)
            {
                _logger.Debug("Width OK ({W}px), no resize needed: {Path}", loaded.Width, imagePath);
                return loaded; // caller owns this
            }

            _logger.Debug("Resizing {W}→{T}px: {Path}", loaded.Width, targetWidth, imagePath);

            var resized = _imageService.Resize(loaded, targetWidth);

            // The original loaded image is no longer needed — dispose it now
            // to free memory before the resized version is used downstream.
            loaded.Dispose();
            loaded = null;

            return resized; // caller owns this
        }
        catch (Exception ex)
        {
            // Dispose the loaded image if resize failed partway through
            loaded?.Dispose();

            _logger.Warning(ex,
                "Failed to normalize width for {Path} — image will be skipped", imagePath);

            return null;
        }
    }
}