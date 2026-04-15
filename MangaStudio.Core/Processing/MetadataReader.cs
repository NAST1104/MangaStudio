using MangaStudio.Core.DTOs;
using MangaStudio.Core.Interfaces;
using Serilog;

namespace MangaStudio.Core.Processing;

// MetadataReader lives in Core because its output drives pipeline decisions
// (targetWidth calculation, chunk planning).
// It delegates the actual file header read to IImageService — so it works
// with both libvips and ImageSharp without any imaging-specific code here.
public sealed class MetadataReader : IMetadataReader
{
    private readonly IImageService _imageService;
    private readonly ILogger _logger;

    public MetadataReader(IImageService imageService, ILogger logger)
    {
        _imageService = imageService;
        _logger = logger.ForContext<MetadataReader>();
    }

    public ImageInfo ReadInfo(string imagePath)
    {
        return _imageService.ReadInfo(imagePath);
    }

    public IEnumerable<ImageInfo> ReadAll(IEnumerable<string> imagePaths)
    {
        foreach (var path in imagePaths)
        {
            ImageInfo info;
            try
            {
                info = _imageService.ReadInfo(path);
            }
            catch (Exception ex)
            {
                // One unreadable file must not abort the whole chapter.
                // Log it and move on — the caller will notice the gap in indices.
                _logger.Warning(ex, "Could not read metadata for {Path} — skipping", path);
                continue;
            }

            _logger.Debug("Metadata: {Path} → {W}x{H}", path, info.Width, info.Height);
            yield return info;
        }
    }
}