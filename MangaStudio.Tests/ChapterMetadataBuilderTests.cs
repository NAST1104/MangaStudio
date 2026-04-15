using MangaStudio.Core.Processing;
using Xunit;

namespace MangaStudio.Tests;

public class ChapterMetadataBuilderTests
{
    // We test ResolveTargetWidth directly — it is internal but accessible
    // because the test project references Core.

    [Theory]
    [InlineData(new[] { 900, 900, 900 }, 900)]  // clear mode = 900, already at min
    [InlineData(new[] { 1200, 1200, 800 }, 1200)] // clear mode = 1200
    [InlineData(new[] { 800, 900, 1000 }, 1000)] // no mode → max = 1000
    [InlineData(new[] { 700, 700, 700 }, 900)] // mode = 700, clamped to 900 min
    [InlineData(new[] { 1200, 900, 1200, 900 }, 1200)] // tie → prefer wider (1200)
    [InlineData(new int[] { }, 900)] // empty → minimum
    public void ResolveTargetWidth_ReturnsExpectedWidth(int[] widths, int expected)
    {
        var result = ChapterMetadataBuilder.ResolveTargetWidth(widths.ToList());
        Assert.Equal(expected, result);
    }
}