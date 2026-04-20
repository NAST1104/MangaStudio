using MangaStudio.Core.DTOs;
using MangaStudio.Core.Enums;
using MangaStudio.Core.Interfaces;
using Serilog;

namespace MangaStudio.Core.Services;

public sealed class PipelineOrchestrator
{
    private readonly IFolderScanner _folderScanner;
    private readonly IFileSorter _fileSorter;
    private readonly IChapterRenamer _chapterRenamer;
    private readonly IChapterMetadataBuilder _metadataBuilder;
    private readonly IStitchPlanner _stitchPlanner;
    private readonly IStitchEngine _stitchEngine;
    private readonly ILogger _logger;

    public PipelineOrchestrator(
        IFolderScanner folderScanner,
        IFileSorter fileSorter,
        IChapterRenamer chapterRenamer,
        IChapterMetadataBuilder metadataBuilder,
        IStitchPlanner stitchPlanner,
        IStitchEngine stitchEngine,
        ILogger logger)
    {
        _folderScanner = folderScanner;
        _fileSorter = fileSorter;
        _chapterRenamer = chapterRenamer;
        _metadataBuilder = metadataBuilder;
        _stitchPlanner = stitchPlanner;
        _stitchEngine = stitchEngine;
        _logger = logger.ForContext<PipelineOrchestrator>();
    }

    public async Task<List<ProcessingResult>> ProcessAllAsync(
        string mangaRootPath,
        string outputRootPath,
        ExportOptions options,
        IProgress<(int current, int total, string currentChapter)>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var results = new List<ProcessingResult>();

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

        var mangaFolderName = Path.GetFileName(
            mangaRootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        var mangaOutputPath = Path.Combine(outputRootPath, mangaFolderName);

        _logger.Information("Output path for this manga: {Path}", mangaOutputPath);

        for (int i = 0; i < chapterPaths.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var chapterPath = chapterPaths[i];
            var folderName = Path.GetFileName(chapterPath);

            _logger.Information("── Chapter {I}/{N}: {Name} ──", i + 1, chapterPaths.Count, folderName);
            progress?.Report((i, chapterPaths.Count, folderName));

            // Rename on disk first so the normalized name drives the output path
            string normalizedPath = chapterPath;
            if (_chapterRenamer.TryRenameOnDisk(chapterPath, out var renamed))
                normalizedPath = renamed;

            var normalizedName = Path.GetFileName(normalizedPath);
            var chapterOutputDir = Path.Combine(mangaOutputPath, normalizedName);

            // ── Duplicate detection ──────────────────────────────────────────
            if (Directory.Exists(chapterOutputDir) &&
                Directory.GetFiles(chapterOutputDir).Length > 0)
            {
                if (options.DuplicateAction == DuplicateAction.Skip)
                {
                    _logger.Information(
                        "Skipping {Name} — output already exists at {Dir}",
                        normalizedName, chapterOutputDir);

                    results.Add(new ProcessingResult
                    {
                        Success = true,
                        IsSkipped = true,
                        ChapterName = normalizedName,
                        OutputFileCount = Directory.GetFiles(chapterOutputDir).Length
                    });

                    progress?.Report((i + 1, chapterPaths.Count, $"Skipped: {normalizedName}"));
                    continue;
                }
                else // Overwrite
                {
                    _logger.Warning(
                        "Overwriting {Name} — deleting existing output at {Dir}",
                        normalizedName, chapterOutputDir);

                    try { Directory.Delete(chapterOutputDir, recursive: true); }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "Could not delete existing output for {Name}", normalizedName);
                        results.Add(new ProcessingResult
                        {
                            Success = false,
                            ChapterName = normalizedName,
                            ErrorMessage = $"Could not delete existing output: {ex.Message}"
                        });
                        continue;
                    }
                }
            }

            // ── Scan images ──────────────────────────────────────────────────
            var imagePaths = _fileSorter
                .Sort(_folderScanner.GetImagePaths(normalizedPath))
                .ToList();

            if (imagePaths.Count == 0)
            {
                _logger.Warning("No images found in {Path} — skipping", normalizedPath);
                results.Add(new ProcessingResult
                {
                    Success = false,
                    ChapterName = normalizedName,
                    ErrorMessage = "No images found in chapter folder."
                });
                continue;
            }

            // ── Build metadata ───────────────────────────────────────────────
            ChapterMetadata metadata;
            try
            {
                metadata = _metadataBuilder.Build(normalizedPath, imagePaths);
                metadata.ChunkPlan = _stitchPlanner.Plan(
                    metadata.ImageHeights, options.MaxStitchHeight);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Metadata build failed for {Path}", normalizedPath);
                results.Add(new ProcessingResult
                {
                    Success = false,
                    ChapterName = normalizedName,
                    ErrorMessage = $"Metadata error: {ex.Message}"
                });
                continue;
            }

            // ── Stitch ───────────────────────────────────────────────────────
            var chapterProgress = new Progress<(int, int)>(p =>
                progress?.Report((i, chapterPaths.Count,
                    $"{normalizedName} — chunk {p.Item1}/{p.Item2}")));

            ProcessingResult result;
            try
            {
                result = await _stitchEngine.ProcessChapterAsync(
                    metadata, mangaOutputPath, options, chapterProgress, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Unhandled error in stitch engine for {Name}", normalizedName);
                result = new ProcessingResult
                {
                    Success = false,
                    ChapterName = normalizedName,
                    ErrorMessage = $"{ex.GetType().Name}: {ex.Message}"
                };
            }

            results.Add(result);
        }

        progress?.Report((chapterPaths.Count, chapterPaths.Count, "Done"));

        _logger.Information("Pipeline complete — {P} succeeded, {S} skipped, {F} failed",
            results.Count(r => r.Success && !r.IsSkipped),
            results.Count(r => r.IsSkipped),
            results.Count(r => !r.Success));

        return results;
    }
}