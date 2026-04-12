using MangaStudio.IO;
using Serilog;
using Xunit;

namespace MangaStudio.Tests;

// IDisposable lets xUnit clean up the temp folder automatically after each test
public class FolderScannerTests : IDisposable
{
    private readonly FolderScanner _scanner;
    private readonly string _tempRoot;

    public FolderScannerTests()
    {
        var logger = new LoggerConfiguration().CreateLogger();
        _scanner = new FolderScanner(logger);

        // Create a unique temp folder so tests never interfere with each other
        _tempRoot = Path.Combine(Path.GetTempPath(), "MangaStudioTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_tempRoot);
    }

    [Fact]
    public void ScanForChapters_FindsFoldersContainingImages()
    {
        // Arrange: one chapter folder with images, one empty folder
        var chapterDir = Path.Combine(_tempRoot, "CH0001");
        Directory.CreateDirectory(chapterDir);
        File.WriteAllText(Path.Combine(chapterDir, "001.jpg"), "fake image data");
        File.WriteAllText(Path.Combine(chapterDir, "002.jpg"), "fake image data");

        var emptyDir = Path.Combine(_tempRoot, "empty-folder");
        Directory.CreateDirectory(emptyDir);

        // Act
        var chapters = _scanner.ScanForChapters(_tempRoot).ToList();

        // Assert: only the folder with images is returned
        Assert.Single(chapters);
        Assert.Contains(chapterDir, chapters);
        Assert.DoesNotContain(emptyDir, chapters);
    }

    [Fact]
    public void ScanForChapters_ReturnsEmpty_WhenRootDoesNotExist()
    {
        var result = _scanner.ScanForChapters(@"C:\this\path\does\not\exist").ToList();
        Assert.Empty(result);
    }

    [Fact]
    public void GetImagePaths_ReturnsOnlySupportedExtensions()
    {
        // Arrange: mix of supported and unsupported files
        var chapterDir = Path.Combine(_tempRoot, "CH0001");
        Directory.CreateDirectory(chapterDir);
        File.WriteAllText(Path.Combine(chapterDir, "001.jpg"),  "fake");
        File.WriteAllText(Path.Combine(chapterDir, "002.jpeg"), "fake");
        File.WriteAllText(Path.Combine(chapterDir, "003.png"),  "fake");
        File.WriteAllText(Path.Combine(chapterDir, "004.webp"), "fake");
        File.WriteAllText(Path.Combine(chapterDir, "readme.txt"), "not an image");
        File.WriteAllText(Path.Combine(chapterDir, "thumb.gif"),  "not supported");

        // Act
        var images = _scanner.GetImagePaths(chapterDir).ToList();

        // Assert: exactly 4 supported files, no txt or gif
        Assert.Equal(4, images.Count);
        Assert.All(images, p =>
            Assert.True(p.EndsWith(".jpg") || p.EndsWith(".jpeg") ||
                        p.EndsWith(".png") || p.EndsWith(".webp")));
    }

    [Fact]
    public void GetImagePaths_ReturnsEmpty_WhenFolderDoesNotExist()
    {
        var result = _scanner.GetImagePaths(@"C:\nonexistent\chapter").ToList();
        Assert.Empty(result);
    }

    // xUnit calls this after every test — cleans up the temp folder
    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
            Directory.Delete(_tempRoot, recursive: true);
    }
}