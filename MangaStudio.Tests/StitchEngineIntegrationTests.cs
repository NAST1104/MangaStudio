using MangaStudio.Core.DTOs;
using MangaStudio.Core.Interfaces;
using MangaStudio.Core.Processing;
using MangaStudio.Imaging.Sharp;
using MangaStudio.IO;
using MangaStudio.Tests.Helpers;
using Serilog;
using Xunit;
using Xunit.Abstractions;

namespace MangaStudio.Tests;

// End-to-end test: creates real image files on disk, runs the full pipeline,
// and verifies the output files exist with reasonable sizes.
public class StitchEngineIntegrationTests : IDisposable
{
    private readonly StitchEngine _engine;
    private readonly string _tempDir;
    private readonly ITestOutputHelper _output;

    public StitchEngineIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
        _tempDir = Path.Combine(Path.GetTempPath(), "MangaStudioEngineTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_tempDir);

        var logger = new LoggerConfiguration().CreateLogger();
        var service = new ImageSharpImageService(logger);

        IOutputManager outputManager = new OutputManager(logger);

        _engine = new StitchEngine(
            imageService: service,
            widthNormalizer: new WidthNormalizer(service, logger),
            stitchPlanner: new StitchPlanner(logger),
            imageEnumerator: new ImageEnumerator(logger),
            outputManager: outputManager,
            logger: logger);
    }

    [Fact]
    public async Task ProcessChapter_FiveImages_ProducesCorrectChunks()
    {
        // Arrange
        var chapterDir = Path.Combine(_tempDir, "CH0001");
        Directory.CreateDirectory(chapterDir);

        // Create 5 images matching the spec example:
        // heights [100,200,550,650,500], all 900px wide
        var imagePaths = new List<string>
        {
            TestImageFactory.CreatePng(chapterDir, "001.png", 900, 100),
            TestImageFactory.CreatePng(chapterDir, "002.png", 900, 200),
            TestImageFactory.CreatePng(chapterDir, "003.png", 900, 550),
            TestImageFactory.CreatePng(chapterDir, "004.png", 900, 650),
            TestImageFactory.CreatePng(chapterDir, "005.png", 900, 500)
        };

        var chapter = new ChapterMetadata
        {
            OriginalPath = chapterDir,
            NormalizedName = "CH0001",
            ImagePaths = imagePaths,
            TargetWidth = 900,
            ImageWidths = new List<int> { 900, 900, 900, 900, 900 },
            ImageHeights = new List<int> { 100, 200, 550, 650, 500 }
        };

        var options = new ExportOptions
        {
            Format = ExportFormat.WebP,
            Quality = 80,
            MaxStitchHeight = 1000,
            DeleteOriginals = false
        };

        var outputRoot = Path.Combine(_tempDir, "output");

        // Act
        var result = await _engine.ProcessChapterAsync(chapter, outputRoot, options);

        // Assert
        Assert.True(result.Success, result.ErrorMessage ?? "Processing failed");
        Assert.Equal("CH0001", result.ChapterName);

        // Expected: 3 chunks → [[0,1,2],[3],[4]]
        Assert.Equal(3, result.OutputFileCount);

        var chunkDir = Path.Combine(outputRoot, "CH0001");
        Assert.True(Directory.Exists(chunkDir));

        var outputFiles = Directory.GetFiles(chunkDir, "*.webp").OrderBy(f => f).ToList();
        Assert.Equal(3, outputFiles.Count);

        foreach (var file in outputFiles)
        {
            var size = new FileInfo(file).Length;
            _output.WriteLine($"{Path.GetFileName(file)} — {size:N0} bytes");
            Assert.True(size > 0, $"Output file is empty: {file}");
        }
    }

    [Fact]
    public async Task ProcessChapter_MixedWidths_NormalizesAll()
    {
        // Arrange: 3 images with different widths — all should be normalized to 900
        var chapterDir = Path.Combine(_tempDir, "CH0002");
        Directory.CreateDirectory(chapterDir);

        var imagePaths = new List<string>
        {
            TestImageFactory.CreatePng(chapterDir, "001.png", 800,  200),
            TestImageFactory.CreatePng(chapterDir, "002.png", 900,  200),
            TestImageFactory.CreatePng(chapterDir, "003.png", 1000, 200)
        };

        var chapter = new ChapterMetadata
        {
            OriginalPath = chapterDir,
            NormalizedName = "CH0002",
            ImagePaths = imagePaths,
            TargetWidth = 900,
            ImageWidths = new List<int> { 800, 900, 1000 },
            ImageHeights = new List<int> { 200, 200, 200 }
        };

        var options = new ExportOptions
        {
            Format = ExportFormat.WebP,
            Quality = 80,
            MaxStitchHeight = 10000,
            DeleteOriginals = false
        };

        // Act
        var result = await _engine.ProcessChapterAsync(
            chapter, Path.Combine(_tempDir, "output2"), options);

        // Assert
        Assert.True(result.Success, result.ErrorMessage ?? "Processing failed");
        Assert.Equal(1, result.OutputFileCount); // all fit in one chunk
    }

    [Fact]
    public async Task ProcessChapter_Cancellation_ReturnsCancelledResult()
    {
        var chapterDir = Path.Combine(_tempDir, "CH0003");
        Directory.CreateDirectory(chapterDir);
        TestImageFactory.CreatePngs(chapterDir, count: 10, width: 900, height: 300);

        var chapter = new ChapterMetadata
        {
            OriginalPath = chapterDir,
            NormalizedName = "CH0003",
            ImagePaths = Directory.GetFiles(chapterDir).ToList(),
            TargetWidth = 900,
            ImageWidths = Enumerable.Repeat(900, 10).ToList(),
            ImageHeights = Enumerable.Repeat(300, 10).ToList()
        };

        var options = new ExportOptions
        {
            Format = ExportFormat.WebP,
            Quality = 80,
            MaxStitchHeight = 10000
        };

        // Cancel immediately
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await _engine.ProcessChapterAsync(
            chapter,
            Path.Combine(_tempDir, "output3"),
            options,
            cancellationToken: cts.Token);

        Assert.False(result.Success);
        Assert.Contains("cancel", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }
}