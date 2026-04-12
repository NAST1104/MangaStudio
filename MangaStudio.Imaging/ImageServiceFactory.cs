using MangaStudio.Core.Enums;
using MangaStudio.Core.Interfaces;
using MangaStudio.Imaging.Sharp;
using MangaStudio.Imaging.Vips;
using Serilog;

namespace MangaStudio.Imaging;

// Decides which IImageService implementation to hand out.
// Registered as a singleton in DI — the UI settings page calls
// ChangeBackend() when the user switches between libvips and ImageSharp.
public sealed class ImageServiceFactory
{
    private readonly ILogger _logger;

    public ImageServiceFactory(ILogger logger)
    {
        _logger = logger.ForContext<ImageServiceFactory>();
    }

    public IImageService Create(ImagingBackend backend)
    {
        _logger.Information("Creating imaging service: {Backend}", backend);

        return backend switch
        {
            ImagingBackend.Vips       => CreateVipsSafe(),
            ImagingBackend.ImageSharp => new ImageSharpImageService(_logger),
            _ => throw new ArgumentOutOfRangeException(nameof(backend), backend, null)
        };
    }

    // Tries to create VipsImageService. If libvips native DLLs are missing or
    // fail to initialize, falls back to ImageSharp automatically with a warning.
    private IImageService CreateVipsSafe()
    {
        try
        {
            // This forces NetVips to load the native libvips DLLs.
            // If they are missing, VipsException is thrown here, not later.
            var vipsVersion = NetVips.NetVips.Version(0);
            _logger.Information("libvips {Version} loaded successfully", vipsVersion);
            return new VipsImageService(_logger);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex,
                "libvips failed to load — falling back to ImageSharp. " +
                "Check that NetVips.Native.win-x64 is installed.");
            return new ImageSharpImageService(_logger);
        }
    }
}