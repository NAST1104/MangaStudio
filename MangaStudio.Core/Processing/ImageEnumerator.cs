using MangaStudio.Core.DTOs;
using MangaStudio.Core.Interfaces;
using Serilog;
using System.Runtime.CompilerServices;

namespace MangaStudio.Core.Processing;

// Yields image file paths one at a time.
// The consumer (StitchEngine) is responsible for loading and disposing each image.
// This keeps the enumerator itself allocation-free and testable without imaging deps.
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

            // Yield the path — the caller loads, uses, and disposes the image
            yield return path;

            // Small yield to keep the UI responsive when processing large chapters
            await Task.Yield();
        }
    }
}