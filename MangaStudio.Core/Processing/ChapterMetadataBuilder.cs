using MangaStudio.Core.DTOs;
using MangaStudio.Core.Interfaces;
using Serilog;

namespace MangaStudio.Core.Processing;

public sealed class ChapterMetadataBuilder : IChapterMetadataBuilder
{
    private readonly IMetadataReader _metadataReader;
    private readonly ILogger _logger;

    // Minimum width we will ever use as a target, even if all images are narrower.
    private const int MinimumTargetWidth = 900;

    public ChapterMetadataBuilder(IMetadataReader metadataReader, ILogger logger)
    {
        _metadataReader = metadataReader;
        _logger = logger.ForContext<ChapterMetadataBuilder>();
    }

    public ChapterMetadata Build(string chapterPath, IEnumerable<string> sortedImagePaths)
    {
        var paths = sortedImagePaths.ToList();

        _logger.Information("Building metadata for chapter: {Path} ({Count} images)",
            chapterPath, paths.Count);

        // Read all headers in one pass — no pixel data loaded
        var infoList = _metadataReader.ReadAll(paths).ToList();

        var widths = infoList.Select(i => i.Width).ToList();
        var heights = infoList.Select(i => i.Height).ToList();

        var targetWidth = ResolveTargetWidth(widths);

        _logger.Information("Target width resolved to {W}px for {Path}", targetWidth, chapterPath);

        return new ChapterMetadata
        {
            OriginalPath = chapterPath,
            NormalizedName = Path.GetFileName(chapterPath),
            ImagePaths = paths,
            TargetWidth = targetWidth,
            ImageWidths = widths,
            ImageHeights = heights
        };
    }

    // Selects the best target width from a list of image widths.
    //
    // Rule:
    //   1. Find the mode (most frequently occurring width).
    //   2. If a clear mode exists (appears more than once), use it.
    //   3. If all widths are unique (no clear mode), use the maximum width.
    //   4. Always clamp to at least MinimumTargetWidth (900px).
    //
    // Why mode instead of min or max?
    // Manhua chapters typically have one dominant width with a few
    // outlier images. Mode gives us the width that fits the majority
    // of pages without unnecessary upscaling of the rest.
    internal static int ResolveTargetWidth(List<int> widths)
    {
        if (widths.Count == 0)
            return MinimumTargetWidth;

        var grouped = widths
            .GroupBy(w => w)
            .OrderByDescending(g => g.Count())
            .ThenByDescending(g => g.Key) // tie-break: prefer wider
            .First();

        // "Clear mode" = appears more than once
        int candidate = grouped.Count() > 1
            ? grouped.Key          // mode
            : widths.Max();        // fallback: max width

        return Math.Max(candidate, MinimumTargetWidth);
    }
}