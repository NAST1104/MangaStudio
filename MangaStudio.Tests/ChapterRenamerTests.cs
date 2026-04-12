using MangaStudio.IO;
using Serilog;
using Xunit;

namespace MangaStudio.Tests;

public class ChapterRenamerTests
{
    private readonly ChapterRenamer _renamer;

    public ChapterRenamerTests()
    {
        // Silent logger for tests — we don't want test output flooded with logs
        var logger = new LoggerConfiguration().CreateLogger();
        _renamer = new ChapterRenamer(logger);
    }

    [Theory]
    // Already normalized
    [InlineData("CH0001",                  "CH0001")]
    [InlineData("ch0042",                  "CH0042")]
    // Chapter prefix variants
    [InlineData("chapter-01",              "CH0001")]
    [InlineData("Chapter-01",              "CH0001")]
    [InlineData("CHAPTER-01",              "CH0001")]
    [InlineData("chap-01",                 "CH0001")]
    [InlineData("chap_99",                 "CH0099")]
    [InlineData("ch-01",                   "CH0001")]
    [InlineData("ch.05",                   "CH0005")]
    [InlineData("cap-01",                  "CH0001")]
    [InlineData("ep-07",                   "CH0007")]
    [InlineData("episode-12",              "CH0012")]
    [InlineData("MangaX_Chap_01",          "CH0001")]
    [InlineData("MangaX_Chapter_99",       "CH0099")]
    // Season + chapter variants
    [InlineData("Season-01_Chapter-02",    "CH0102")]
    [InlineData("S01_CH02",                "CH0102")]
    [InlineData("Season-2-Chapter-15",     "CH0215")]
    // Pure number / trailing number fallback
    [InlineData("001",                     "CH0001")]
    [InlineData("01",                      "CH0001")]
    [InlineData("1",                       "CH0001")]
    [InlineData("100",                     "CH0100")]
    [InlineData("MangaName_042",           "CH0042")]
    public void NormalizeChapterName_ReturnsExpectedFormat(string input, string expected)
    {
        var result = _renamer.NormalizeChapterName(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void NormalizeChapterName_AlreadyNormalized_ReturnsSameValue()
    {
        // Verifies no unnecessary transformation happens
        Assert.Equal("CH0001", _renamer.NormalizeChapterName("CH0001"));
    }

    [Fact]
    public void NormalizeChapterName_UnrecognizedInput_ReturnsNonEmptyFallback()
    {
        // Should not throw or return empty — produces a CH_ prefixed fallback
        var result = _renamer.NormalizeChapterName("totally-random-folder-no-number");
        Assert.False(string.IsNullOrWhiteSpace(result));
        Assert.StartsWith("CH_", result);
    }
}