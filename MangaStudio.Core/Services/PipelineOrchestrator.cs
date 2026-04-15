using MangaStudio.Core.DTOs;
using MangaStudio.Core.Interfaces;
using Serilog;

namespace MangaStudio.Core.Services;

// The single entry point the UI calls.
// Given a manga root folder and export options, it:
//   1. Scans for chapter folders
//   2. Sorts and builds metadata for each chapter
//   3. Runs the stitch engine on each chapter
//   4. Collects and returns all results
public sealed class PipelineOrchestrator
{
    private readonly IFolderScanner _folderScanner;
    private readonly IFileSorter _fileSorter;
    private readonly IChapterRenamer _chapterRenamer;
    private readonly IChapterMetadataBuilder _metadataBuilder;
    private readonly IStitchEngine _stitchEngine;
    private readonly ILogger _logger;

    public PipelineOrchestrator(
        IFolderScanner folderScanner,
        IFileSorter fileSorter,
        IChapterRenamer chapterRenamer,
        IChapterMetadataBuilder metadataBuilder,
        IStitchEngine stitchEngine,
        ILogger logger)
    {
        _folderScanner = folderScanner;
        _fileSorter = fileSorter;
        _chapterRenamer = chapterRenamer;
        _metadataBuilder = metadataBuilder;
        _stitchEngine = stitchEngine;
        _logger = logger.ForContext<PipelineOrchestrator>();
    }

    // Processes all chapters found under mangaRootPath.
    // outputRootPath is the top-level folder where processed chapters are written.
    // progress reports (chaptersCompleted, totalChapters).
    public async Task<List<ProcessingResult>> ProcessAllAsync(
        string mangaRootPath,
        string outputRootPath,
        ExportOptions options,
        IProgress<(int current, int total, string currentChapter)>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var results = new List<ProcessingResult>();

        // ── Step 1: Discover chapters ────────────────────────────────────────
        var chapterPaths = _folderScanner
            .ScanForChapters(mangaRootPath)
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (chapterPaths.Count == 0)
        {
            _logger.Warning("No chapter folders found under {Path}", mangaRootPath);
            return results;
        }

        _logger.Information("Found {N} chapter(s) under {Path}", chapterPaths.Count, mangaRootPath);

        // ── Step 2: Process each chapter ─────────────────────────────────────
        for (int i = 0; i < chapterPaths.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var chapterPath = chapterPaths[i];
            var folderName = Path.GetFileName(chapterPath);

            _logger.Information("── Chapter {I}/{N}: {Name} ──",
                i + 1, chapterPaths.Count, folderName);

            progress?.Report((i, chapterPaths.Count, folderName));

            // Rename chapter folder on disk to normalized name
            string normalizedPath = chapterPath;
            if (_chapterRenamer.TryRenameOnDisk(chapterPath, out var renamed))
                normalizedPath = renamed;

            // Get sorted image paths
            var imagePaths = _fileSorter.Sort(
                _folderScanner.GetImagePaths(normalizedPath)).ToList();

            if (imagePaths.Count == 0)
            {
                _logger.Warning("No images found in {Path} — skipping", normalizedPath);
                results.Add(new ProcessingResult
                {
                    Success = false,
                    ChapterName = folderName,
                    ErrorMessage = "No images found in chapter folder."
                });
                continue;
            }

            // Build metadata (reads headers, resolves targetWidth, plans chunks)
            ChapterMetadata metadata;
            try
            {
                metadata = _metadataBuilder.Build(normalizedPath, imagePaths);

                // Attach chunk plan now so StitchEngine doesn't need to re-plan
                metadata.ChunkPlan = new MangaStudio.Core.Processing.StitchPlanner(
                    Serilog.Log.Logger).Plan(metadata.ImageHeights, options.MaxStitchHeight);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Metadata build failed for {Path}", normalizedPath);
                results.Add(new ProcessingResult
                {
                    Success = false,
                    ChapterName = folderName,
                    ErrorMessage = $"Metadata error: {ex.Message}"
                });
                continue;
            }

            // Run the stitch engine
            var chapterProgress = new Progress<(int, int)>(p =>
                progress?.Report((i, chapterPaths.Count,
                    $"{folderName} chunk {p.Item1}/{p.Item2}")));

            var result = await _stitchEngine.ProcessChapterAsync(
                metadata,
                outputRootPath,
                options,
                chapterProgress,
                cancellationToken);

            results.Add(result);
        }

        progress?.Report((chapterPaths.Count, chapterPaths.Count, "Done"));

        var passed = results.Count(r => r.Success);
        var failed = results.Count(r => !r.Success);
        _logger.Information("Pipeline complete — {P} succeeded, {F} failed", passed, failed);

        return results;
    }
}