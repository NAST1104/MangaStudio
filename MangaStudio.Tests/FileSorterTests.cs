using MangaStudio.IO;
using Xunit;

namespace MangaStudio.Tests;

public class FileSorterTests
{
    private readonly FileSorter _sorter = new();

    [Fact]
    public void Sort_NaturalOrder_TenAfterTwo()
    {
        // Classic natural sort problem: lexicographic puts "10" before "2"
        var input = new[] { "img10.jpg", "img2.jpg", "img1.jpg", "img20.jpg" };
        var result = _sorter.Sort(input).Select(Path.GetFileName).ToList();

        Assert.Equal(new[] { "img1.jpg", "img2.jpg", "img10.jpg", "img20.jpg" }, result);
    }

    [Fact]
    public void Sort_LeadingZeros_SortsCorrectly()
    {
        var input = new[] { "003.jpg", "010.jpg", "001.jpg", "002.jpg" };
        var result = _sorter.Sort(input).Select(Path.GetFileName).ToList();

        Assert.Equal(new[] { "001.jpg", "002.jpg", "003.jpg", "010.jpg" }, result);
    }

    [Fact]
    public void Sort_MixedPrefixAndNumber_SortsCorrectly()
    {
        var input = new[] { "manga_10.jpg", "manga_2.jpg", "manga_1.jpg" };
        var result = _sorter.Sort(input).Select(Path.GetFileName).ToList();

        Assert.Equal(new[] { "manga_1.jpg", "manga_2.jpg", "manga_10.jpg" }, result);
    }

    [Fact]
    public void Sort_SingleItem_ReturnsItUnchanged()
    {
        var input = new[] { "001.jpg" };
        var result = _sorter.Sort(input).ToList();

        Assert.Single(result);
    }

    [Fact]
    public void Sort_EmptyInput_ReturnsEmpty()
    {
        var result = _sorter.Sort(Enumerable.Empty<string>()).ToList();
        Assert.Empty(result);
    }
}