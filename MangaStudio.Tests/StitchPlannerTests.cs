using MangaStudio.Core.Processing;
using Serilog;
using Xunit;

namespace MangaStudio.Tests;

public class StitchPlannerTests
{
    private readonly StitchPlanner _planner;

    public StitchPlannerTests()
    {
        var logger = new LoggerConfiguration().CreateLogger();
        _planner = new StitchPlanner(logger);
    }

    [Fact]
    public void Plan_ExampleFromSpec_ProducesCorrectChunks()
    {
        // Exact example from the project plan:
        // heights=[100,200,550,650,500], maxHeight=1000
        // Expected: [[0,1,2],[3],[4]]
        var heights = new List<int> { 100, 200, 550, 650, 500 };

        var chunks = _planner.Plan(heights, maxStitchHeight: 1000);

        Assert.Equal(3, chunks.Count);
        Assert.Equal(new[] { 0, 1, 2 }, chunks[0]);
        Assert.Equal(new[] { 3 }, chunks[1]);
        Assert.Equal(new[] { 4 }, chunks[2]);
    }

    [Fact]
    public void Plan_AllFitInOneChunk_ReturnsSingleChunk()
    {
        var heights = new List<int> { 100, 200, 300 };

        var chunks = _planner.Plan(heights, maxStitchHeight: 1000);

        Assert.Single(chunks);
        Assert.Equal(new[] { 0, 1, 2 }, chunks[0]);
    }

    [Fact]
    public void Plan_SingleImageTallerThanMax_GetsOwnChunk()
    {
        // A single image taller than maxStitchHeight must not be split.
        var heights = new List<int> { 500, 2000, 500 };

        var chunks = _planner.Plan(heights, maxStitchHeight: 1000);

        Assert.Equal(3, chunks.Count);
        Assert.Equal(new[] { 0 }, chunks[0]);
        Assert.Equal(new[] { 1 }, chunks[1]);
        Assert.Equal(new[] { 2 }, chunks[2]);
    }

    [Fact]
    public void Plan_EachImageExactlyAtLimit_OneChunkPerImage()
    {
        var heights = new List<int> { 1000, 1000, 1000 };

        var chunks = _planner.Plan(heights, maxStitchHeight: 1000);

        // Each image is exactly 1000px — adding any two would exceed 1000
        Assert.Equal(3, chunks.Count);
    }

    [Fact]
    public void Plan_EmptyInput_ReturnsEmptyPlan()
    {
        var chunks = _planner.Plan(new List<int>(), maxStitchHeight: 1000);
        Assert.Empty(chunks);
    }

    [Fact]
    public void Plan_NoChunkExceedsMaxHeight()
    {
        // Property-based style: verify the constraint holds for any valid plan
        var heights = new List<int> { 300, 400, 250, 600, 100, 800, 150, 700 };
        int maxH = 1000;

        var chunks = _planner.Plan(heights, maxH);

        foreach (var chunk in chunks)
        {
            int chunkHeight = chunk.Sum(idx => heights[idx]);

            // A chunk may exceed maxH only if it is a single image
            if (chunk.Count > 1)
                Assert.True(chunkHeight <= maxH,
                    $"Chunk {string.Join(",", chunk)} has height {chunkHeight} > {maxH}");
        }
    }

    [Fact]
    public void Plan_AllImagesAccountedFor()
    {
        var heights = new List<int> { 200, 300, 400, 100, 600 };

        var chunks = _planner.Plan(heights, maxStitchHeight: 800);

        // Every image index must appear exactly once across all chunks
        var allIndices = chunks.SelectMany(c => c).OrderBy(i => i).ToList();
        var expected = Enumerable.Range(0, heights.Count).ToList();

        Assert.Equal(expected, allIndices);
    }
}