using MangaStudio.Core.DTOs;
using MangaStudio.Core.Interfaces;
using Serilog;

namespace MangaStudio.Core.Processing;

public sealed class StitchEngine : IStitchEngine
{
    private readonly IImageService _imageService;
    private readonly IWidthNormalizer _widthNormalizer;
    private readonly IStitchPlanner _stitchPlanner;
    private readonly IImageEnumerator _imageEnumerator;
    private readonly IOutputManager _outputManager;
    private readonly ILogger _logger;

    public StitchEngine(
        IImageService imageService,
        IWidthNormalizer widthNormalizer,
        IStitchPlanner stitchPlanner,
        IImageEnumerator imageEnumerator,
        IOutputManager outputManager,
        ILogger logger)
    {
        _imageService = imageService;
        _widthNormalizer = widthNormalizer;
        _stitchPlanner = stitchPlanner;
        _imageEnumerator = imageEnumerator;
        _outputManager = outputManager;
        _logger = logger.ForContext<StitchEngine>();
    }

    public async Task<ProcessingResult> ProcessChapterAsync(
        ChapterMetadata chapter,
        string outputDirectory,
        ExportOptions options,
        IProgress<(int current, int total)>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var warnings = new List<string>();
        int outputCount = 0;

        _logger.Information("Processing chapter: {Name} ({Count} images, targetWidth={W}px)",
            chapter.NormalizedName, chapter.ImagePaths.Count, chapter.TargetWidth);

        try
        {
            // ── Step 1: Run the stitch planner ──────────────────────────────
            // We need image heights to plan chunks. If ChunkPlan was already set
            // by the caller (e.g. from ChapterMetadataBuilder) we reuse it.
            // Otherwise we plan now from the heights in the metadata.
            if (chapter.ChunkPlan.Count == 0)
            {
                chapter.ChunkPlan = _stitchPlanner.Plan(
                    chapter.ImageHeights,
                    options.MaxStitchHeight);
            }

            int totalChunks = chapter.ChunkPlan.Count;
            _logger.Information("Chapter split into {N} chunks", totalChunks);

            // Ensure the output folder exists
            var outputDir = _outputManager.EnsureOutputDirectory(
                outputDirectory, chapter.NormalizedName);

            // Build a lookup: imageIndex → chunkIndex
            // This lets us know — as we stream images — which chunk each one belongs to.
            var imageToChunk = BuildImageToChunkMap(chapter.ChunkPlan);

            // ── Step 2: Stream images and stitch ────────────────────────────
            // We create ONE canvas and reuse it across all chunks.
            // FlushCanvas resets it without deallocating the wrapper object.
            using var canvas = _imageService.CreateCanvas(chapter.TargetWidth);

            int currentChunkIndex = -1; // -1 = no chunk started yet
            int imageStreamIndex = 0;  // tracks position in the enumerated stream

            // We enumerate paths; the planner told us which chunk each index belongs to.
            // imageToChunk maps the original chapter image index (position in ImagePaths)
            // to a chunk number. As we stream we match them up.
            int chapterImageIndex = 0;

            await foreach (var imagePath in _imageEnumerator.EnumeratePathsAsync(
                chapter, cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Which chunk does this image belong to?
                if (!imageToChunk.TryGetValue(chapterImageIndex, out int targetChunk))
                {
                    // Image was skipped by MetadataReader (e.g. unreadable header)
                    _logger.Warning("No chunk assignment for image index {I} — skipping", chapterImageIndex);
                    chapterImageIndex++;
                    continue;
                }

                // ── Chunk boundary: flush previous chunk and start new one ──
                if (targetChunk != currentChunkIndex)
                {
                    if (currentChunkIndex >= 0 && !canvas.IsEmpty)
                    {
                        // Write the completed chunk to disk
                        var chunkPath = _outputManager.GetOutputFilePath(
                            outputDir,
                            chapter.NormalizedName,
                            currentChunkIndex + 1,
                            options.Format == ExportFormat.WebP ? "webp" : "jpg");

                        _imageService.FlushCanvas(canvas, chunkPath, options);
                        outputCount++;

                        progress?.Report((currentChunkIndex + 1, totalChunks));
                        _logger.Information("Flushed chunk {C}/{T} → {Path}",
                            currentChunkIndex + 1, totalChunks, chunkPath);
                    }

                    currentChunkIndex = targetChunk;
                }

                // ── Load and normalize this image ────────────────────────────
                var normalizedImage = _widthNormalizer.NormalizeWidth(imagePath, chapter.TargetWidth);

                if (normalizedImage is null)
                {
                    var msg = $"Skipped image (normalize failed): {Path.GetFileName(imagePath)}";
                    warnings.Add(msg);
                    _logger.Warning(msg);
                    chapterImageIndex++;
                    continue;
                }

                // ── Append to canvas ─────────────────────────────────────────
                using (normalizedImage)
                {
                    _imageService.AppendToCanvas(canvas, normalizedImage);
                }
                // normalizedImage is disposed here — only the canvas holds a copy

                chapterImageIndex++;
                imageStreamIndex++;
            }

            // ── Step 3: Flush the final chunk ────────────────────────────────
            if (!canvas.IsEmpty)
            {
                var finalChunkPath = _outputManager.GetOutputFilePath(
                    outputDir,
                    chapter.NormalizedName,
                    currentChunkIndex + 1,
                    options.Format == ExportFormat.WebP ? "webp" : "jpg");

                _imageService.FlushCanvas(canvas, finalChunkPath, options);
                outputCount++;

                progress?.Report((totalChunks, totalChunks));
                _logger.Information("Flushed final chunk {C}/{T} → {Path}",
                    currentChunkIndex + 1, totalChunks, finalChunkPath);
            }

            // ── Step 4: Optionally delete original chapter folder ────────────
            if (options.DeleteOriginals)
                _outputManager.DeleteOriginalChapter(chapter.OriginalPath);

            _logger.Information("Chapter {Name} done — {N} output file(s), {W} warning(s)",
                chapter.NormalizedName, outputCount, warnings.Count);

            return new ProcessingResult
            {
                Success = true,
                ChapterName = chapter.NormalizedName,
                OutputFileCount = outputCount,
                Warnings = warnings
            };
        }
        catch (OperationCanceledException)
        {
            _logger.Warning("Processing cancelled for chapter: {Name}", chapter.NormalizedName);
            return new ProcessingResult
            {
                Success = false,
                ChapterName = chapter.NormalizedName,
                ErrorMessage = "Processing was cancelled by the user.",
                Warnings = warnings
            };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Unexpected error processing chapter: {Name}", chapter.NormalizedName);
            return new ProcessingResult
            {
                Success = false,
                ChapterName = chapter.NormalizedName,
                ErrorMessage = ex.Message,
                Warnings = warnings
            };
        }
    }

    // Builds a flat dictionary: imageIndex → chunkIndex
    // from the nested chunk plan list.
    // Example: [[0,1,2],[3],[4]] →
    //   { 0→0, 1→0, 2→0, 3→1, 4→2 }
    private static Dictionary<int, int> BuildImageToChunkMap(List<List<int>> chunkPlan)
    {
        var map = new Dictionary<int, int>();

        for (int chunkIndex = 0; chunkIndex < chunkPlan.Count; chunkIndex++)
            foreach (var imageIndex in chunkPlan[chunkIndex])
                map[imageIndex] = chunkIndex;

        return map;
    }
}