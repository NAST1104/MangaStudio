using MangaStudio.Core.DTOs;

namespace MangaStudio.Core.Interfaces;

public interface IImageEnumerator
{
    // Yields one image at a time from the chapter.
    // Each image is loaded, passed to the consumer via the callback,
    // then disposed before the next one is loaded.
    // This guarantees at most one image is in RAM at any moment.
    IAsyncEnumerable<string> EnumeratePathsAsync(
        ChapterMetadata chapter,
        CancellationToken cancellationToken = default);
}