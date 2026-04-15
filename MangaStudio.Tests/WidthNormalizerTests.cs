using MangaStudio.Core.DTOs;
using MangaStudio.Imaging.Sharp;
using MangaStudio.Core.Processing;
using MangaStudio.Tests.Helpers;
using Serilog;
using Xunit;

namespace MangaStudio.Tests;

public class WidthNormalizerTests : IDisposable
{
    private readonly WidthNormalizer _normalizer;
    private readonly string _tempDir;

    public WidthNormalizerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "MangaStudioNormTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_tempDir);

        var logger = new LoggerConfiguration().CreateLogger();
        var service = new ImageSharpImageService(logger);
        _normalizer = new WidthNormalizer(service, logger);
    }

    [Fact]
    public void NormalizeWidth_ImageAlreadyCorrectWidth_ReturnsImageUnchanged()
    {
        var path = TestImageFactory.CreatePng(_tempDir, "correct.png", width: 900, height: 200);

        using var result = _normalizer.NormalizeWidth(path, targetWidth: 900);

        Assert.NotNull(result);
        Assert.Equal(900, result!.Width);
    }

    [Fact]
    public void NormalizeWidth_ImageTooWide_ResizesDown()
    {
        var path = TestImageFactory.CreatePng(_tempDir, "wide.png", width: 1200, height: 400);

        using var result = _normalizer.NormalizeWidth(path, targetWidth: 900);

        Assert.NotNull(result);
        Assert.Equal(900, result!.Width);
        // Height must be proportional: 400 * (900/1200) = 300
        Assert.Equal(300, result!.Height);
    }

    [Fact]
    public void NormalizeWidth_ImageTooNarrow_ResizesUp()
    {
        var path = TestImageFactory.CreatePng(_tempDir, "narrow.png", width: 600, height: 300);

        using var result = _normalizer.NormalizeWidth(path, targetWidth: 900);

        Assert.NotNull(result);
        Assert.Equal(900, result!.Width);
    }

    [Fact]
    public void NormalizeWidth_NonExistentFile_ReturnsNull()
    {
        // Must not throw — returns null so the pipeline can skip
        var result = _normalizer.NormalizeWidth(
            @"C:\does\not\exist\fake.png", targetWidth: 900);

        Assert.Null(result);
    }

    [Fact]
    public void NormalizeWidth_WithinTolerance_DoesNotResize()
    {
        // 901px is within the 2px tolerance of 900px — no resize should happen
        var path = TestImageFactory.CreatePng(_tempDir, "close.png", width: 901, height: 200);

        using var result = _normalizer.NormalizeWidth(path, targetWidth: 900);

        Assert.NotNull(result);
        // Width should be 901 (original), not 900 (resized)
        Assert.Equal(901, result!.Width);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }
}