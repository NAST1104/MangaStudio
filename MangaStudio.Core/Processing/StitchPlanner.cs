using MangaStudio.Core.DTOs;
using MangaStudio.Core.Interfaces;
using Serilog;

namespace MangaStudio.Core.Processing;

public sealed class StitchPlanner : IStitchPlanner
{
    private readonly ILogger _logger;

    public StitchPlanner(ILogger logger)
    {
        _logger = logger.ForContext<StitchPlanner>();
    }

    // Splits a list of image heights into chunks where no chunk exceeds maxStitchHeight.
    //
    // Algorithm:
    //   Walk the list, accumulating height into the current chunk.
    //   When adding the next image would exceed the limit, close the current
    //   chunk and start a new one.
    //   If a single image is taller than maxStitchHeight it gets its own chunk —
    //   we never split a single image.
    //
    // Example: heights=[100,200,550,650,500], maxStitchHeight=1000
    //   i=0: current=100  (100+200≤1000, continue)
    //   i=1: current=300  (300+550≤1000, continue)
    //   i=2: current=850  (850+650>1000, close chunk [0,1,2], start new)
    //   i=3: current=650  (650+500>1000, close chunk [3], start new)
    //   i=4: current=500  (end of list, close chunk [4])
    //   Result: [[0,1,2],[3],[4]]
    public List<List<int>> Plan(List<int> imageHeights, int maxStitchHeight)
    {
        // Use the caller-supplied maxStitchHeight as-is.
        // The caller (PipelineOrchestrator) already gets this value from
        // ExportOptions.MaxStitchHeight which is clamped per-format at build time.
        int effectiveMax = maxStitchHeight;

        _logger.Debug("StitchPlanner: planning {ImageCount} images with effectiveMax={Max}px",
            imageHeights.Count, effectiveMax);

        var chunks = new List<List<int>>();
        var current = new List<int>();
        int runningHeight = 0;

        for (int i = 0; i < imageHeights.Count; i++)
        {
            int h = imageHeights[i];

            if (h > effectiveMax)
                _logger.Warning(
                    "Image index {I} height {H}px exceeds max {Max}px — placed alone in its own chunk",
                    i, h, effectiveMax);

            bool wouldExceed = runningHeight + h > effectiveMax;
            bool currentHasImages = current.Count > 0;

            if (wouldExceed && currentHasImages)
            {
                chunks.Add(new List<int>(current));
                current.Clear();
                runningHeight = 0;
            }

            current.Add(i);
            runningHeight += h;
        }

        if (current.Count > 0)
            chunks.Add(current);

        _logger.Debug("StitchPlanner: {ImageCount} images → {ChunkCount} chunks (effectiveMax={Max}px)",
            imageHeights.Count, chunks.Count, effectiveMax);

        for (int c = 0; c < chunks.Count; c++)
            _logger.Debug("  Chunk {C}: images [{Indices}], height={H}px",
                c + 1,
                string.Join(",", chunks[c]),
                chunks[c].Sum(idx => imageHeights[idx]));

        return chunks;
    }
}