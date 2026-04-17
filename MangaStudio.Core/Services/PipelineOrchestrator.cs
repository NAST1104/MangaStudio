using MangaStudio.Core.DTOs;
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

        for (int i = 0; i < chapterPaths.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var chapterPath = chapterPaths[i];
            var folderName = Path.GetFileName(chapterPath);

            _logger.Information("── Chapter {I}/{N}: {Name} ──", i + 1, chapterPaths.Count, folderName);
            progress?.Report((i, chapterPaths.Count, folderName));

            string normalizedPath = chapterPath;
            if (_chapterRenamer.TryRenameOnDisk(chapterPath, out var renamed))
                normalizedPath = renamed;

            var imagePaths = _fileSorter
                .Sort(_folderScanner.GetImagePaths(normalizedPath))
                .ToList();

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
                    ChapterName = folderName,
                    ErrorMessage = $"Metadata error: {ex.Message}"
                });
                continue;
            }

            var chapterProgress = new Progress<(int, int)>(p =>
                progress?.Report((i, chapterPaths.Count,
                    $"{folderName} — chunk {p.Item1}/{p.Item2}")));

            var result = await _stitchEngine.ProcessChapterAsync(
                metadata, outputRootPath, options, chapterProgress, cancellationToken);

            results.Add(result);
        }

        progress?.Report((chapterPaths.Count, chapterPaths.Count, "Done"));

        _logger.Information("Pipeline complete — {P} succeeded, {F} failed",
            results.Count(r => r.Success), results.Count(r => !r.Success));

        return results;
    }
}