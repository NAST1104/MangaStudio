using MangaStudio.Core.DTOs;
using MangaStudio.Imaging.Vips;
using MangaStudio.Tests.Helpers;
using Serilog;
using Xunit;
using Xunit.Abstractions;

namespace MangaStudio.Tests;

public class VipsImageServiceTests : IDisposable
{
    private readonly VipsImageService? _service;
    private readonly string _tempDir;
    private readonly bool _vipsAvailable;
    private readonly ITestOutputHelper _output;

    public VipsImageServiceTests(ITestOutputHelper output)
    {
        _output = output;
        _tempDir = Path.Combine(Path.GetTempPath(), "MangaStudioVipsTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_tempDir);

        var logger = new LoggerConfiguration().CreateLogger();

        try
        {
            _ = NetVips.NetVips.Version(0);
            _service = new VipsImageService(logger);
            _vipsAvailable = true;
        }
        catch
        {
            _vipsAvailable = false;
            _output.WriteLine("libvips not available — Vips tests are skipped.");
        }
    }

    [Fact]
    public void ReadInfo_ReturnsCorrectDimensions()
    {
        if (!_vipsAvailable) { _output.WriteLine("Skipped: libvips not available"); return; }

        var path = TestImageFactory.CreatePng(_tempDir, "test.png", width: 100, height: 200);

        var info = _service!.ReadInfo(path);

        Assert.Equal(100, info.Width);
        Assert.Equal(200, info.Height);
        Assert.Equal(path, info.FilePath);
        Assert.True(info.FileSizeBytes > 0);
    }

    [Fact]
    public void Load_ReturnsImageWithCorrectDimensions()
    {
        if (!_vipsAvailable) { _output.WriteLine("Skipped: libvips not available"); return; }

        var path = TestImageFactory.CreatePng(_tempDir, "test.png", width: 80, height: 150);

        using var image = _service!.Load(path);

        Assert.Equal(80, image.Width);
        Assert.Equal(150, image.Height);
        Assert.Equal(path, image.SourcePath);
    }

    [Fact]
    public void Resize_ScalesWidthCorrectly()
    {
        if (!_vipsAvailable) { _output.WriteLine("Skipped: libvips not available"); return; }

        var path = TestImageFactory.CreatePng(_tempDir, "wide.png", width: 200, height: 100);

        using var original = _service!.Load(path);
        using var resized = _service!.Resize(original, targetWidth: 100);

        Assert.Equal(100, resized.Width);
        Assert.Equal(50, resized.Height);
    }

    [Fact]
    public void AppendToCanvas_AccumulatesHeight()
    {
        if (!_vipsAvailable) { _output.WriteLine("Skipped: libvips not available"); return; }

        var paths = TestImageFactory.CreatePngs(_tempDir, count: 3, width: 100, height: 100);

        using var canvas = _service!.CreateCanvas(100);

        foreach (var path in paths)
        {
            using var img = _service!.Load(path);
            _service!.AppendToCanvas(canvas, img);
        }

        Assert.Equal(300, canvas.CurrentHeight);
        Assert.Equal(100, canvas.Width);
        Assert.False(canvas.IsEmpty);
    }

    [Fact]
    public void FlushCanvas_WritesFileAndResetsCanvas()
    {
        if (!_vipsAvailable) { _output.WriteLine("Skipped: libvips not available"); return; }

        var paths = TestImageFactory.CreatePngs(_tempDir, count: 2, width: 100, height: 100);
        var outputPath = Path.Combine(_tempDir, "output-001.webp");
        var options = new ExportOptions { Format = ExportFormat.WebP, Quality = 80 };

        using var canvas = _service!.CreateCanvas(100);

        foreach (var path in paths)
        {
            using var img = _service!.Load(path);
            _service!.AppendToCanvas(canvas, img);
        }

        _service!.FlushCanvas(canvas, outputPath, options);

        Assert.True(File.Exists(outputPath));
        Assert.True(new FileInfo(outputPath).Length > 0);
        Assert.True(canvas.IsEmpty);
        Assert.Equal(0, canvas.CurrentHeight);
    }

    [Fact]
    public void FlushCanvas_CanBeReusedAfterFlush()
    {
        if (!_vipsAvailable) { _output.WriteLine("Skipped: libvips not available"); return; }

        var options = new ExportOptions { Format = ExportFormat.WebP, Quality = 80 };
        using var canvas = _service!.CreateCanvas(100);

        // First flush
        var paths1 = TestImageFactory.CreatePngs(_tempDir, count: 2, width: 100, height: 100);
        foreach (var p in paths1)
        {
            using var img = _service!.Load(p);
            _service!.AppendToCanvas(canvas, img);
        }
        _service!.FlushCanvas(canvas, Path.Combine(_tempDir, "chunk-001.webp"), options);

        // Second flush — canvas was reset
        var paths2 = TestImageFactory.CreatePngs(_tempDir, count: 2, width: 100, height: 100);
        foreach (var p in paths2)
        {
            using var img = _service!.Load(p);
            _service!.AppendToCanvas(canvas, img);
        }
        _service!.FlushCanvas(canvas, Path.Combine(_tempDir, "chunk-002.webp"), options);

        Assert.True(File.Exists(Path.Combine(_tempDir, "chunk-001.webp")));
        Assert.True(File.Exists(Path.Combine(_tempDir, "chunk-002.webp")));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }
}