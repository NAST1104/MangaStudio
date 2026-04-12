using MangaStudio.Core.DTOs;

namespace MangaStudio.Core.Interfaces;

public interface IStitchEngine
{
    // Processes one chapter end-to-end:
    //   enumerate → normalize → stitch → flush → export
    // Reports progress via the callback (chunkIndex, totalChunks).
    // Returns a ProcessingResult describing what happened.
    Task<ProcessingResult> ProcessChapterAsync(
        ChapterMetadata chapter,
        string outputDirectory,
        ExportOptions options,
        IProgress<(int current, int total)>? progress = null,
        CancellationToken cancellationToken = default);
}