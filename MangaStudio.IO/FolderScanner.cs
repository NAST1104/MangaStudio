using MangaStudio.Core.Interfaces;
using Serilog;

namespace MangaStudio.IO;

public class FolderScanner : IFolderScanner
{
    // Only these extensions are treated as manga pages
    private static readonly HashSet<string> SupportedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };

    private readonly ILogger _logger;

    public FolderScanner(ILogger logger)
    {
        _logger = logger.ForContext<FolderScanner>();
    }

    public IEnumerable<string> ScanForChapters(string mangaRootPath)
    {
        if (!Directory.Exists(mangaRootPath))
        {
            _logger.Warning("Path does not exist: {Path}", mangaRootPath);
            yield break;
        }

        // Walk every subdirectory recursively.
        // A folder qualifies as a chapter only if it directly contains at least one image.
        foreach (var dir in Directory.EnumerateDirectories(mangaRootPath, "*", SearchOption.AllDirectories))
        {
            if (GetImagePaths(dir).Any())
            {
                _logger.Debug("Found chapter folder: {Dir}", dir);
                yield return dir;
            }
        }
    }

    public IEnumerable<string> GetImagePaths(string chapterPath)
    {
        if (!Directory.Exists(chapterPath))
            yield break;

        // EnumerateFiles lists only files in this folder, not subdirectories
        foreach (var file in Directory.EnumerateFiles(chapterPath))
        {
            if (SupportedExtensions.Contains(Path.GetExtension(file)))
                yield return file;
        }
    }
}