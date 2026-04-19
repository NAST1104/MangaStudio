using MangaStudio.Core.DTOs;
using MangaStudio.Core.Interfaces;
using Serilog;
using System.Runtime.CompilerServices;

namespace MangaStudio.Core.Processing;

public sealed class ImageEnumerator : IImageEnumerator
{
    private readonly ILogger _logger;

    public ImageEnumerator(ILogger logger)
    {
        _logger = logger.ForContext<ImageEnumerator>();
    }

    public async IAsyncEnumerable<string> EnumeratePathsAsync(
        ChapterMetadata chapter,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        int total = chapter.ImagePaths.Count;

        for (int i = 0; i < total; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var path = chapter.ImagePaths[i];

            if (!File.Exists(path))
            {
                _logger.Warning("Image file not found, skipping: {Path}", path);
                continue;
            }

            _logger.Debug("Enumerating image {Index}/{Total}: {Path}", i + 1, total, path);

            yield return path;

            // Yield every 10 images to keep the thread pool scheduler happy.
            // Since we now run inside Task.Run, this yields the thread briefly
            // so other queued work items can run — not the UI thread.
            if (i % 10 == 0)
                await Task.Yield();
        }
    }
}