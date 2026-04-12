using MangaStudio.Core.Interfaces;
using Serilog;
using System.Text.RegularExpressions;

namespace MangaStudio.IO;

public class ChapterRenamer : IChapterRenamer
{
    private readonly ILogger _logger;

    public ChapterRenamer(ILogger logger)
    {
        _logger = logger.ForContext<ChapterRenamer>();
    }

    public string NormalizeChapterName(string folderName)
    {
        var name = folderName.Trim();

        // Rule 1: Already normalized — nothing to do
        // Matches: CH0001, ch0042, CH0100
        if (Regex.IsMatch(name, @"^CH\d{4}$", RegexOptions.IgnoreCase))
            return name.ToUpperInvariant();

        // Rule 2: Season + Chapter pattern
        // Matches: Season-01_Chapter-02, S01_CH02, Season-2-Chapter-15, S1C5
        var seasonChapter = Regex.Match(name,
            @"[Ss](?:eason)?[-_\s]*(\d+)[-_\s]*(?:[Cc]h(?:apter|ap)?|[Ee]p(?:isode)?)[-_\s]*(\d+)",
            RegexOptions.IgnoreCase);

        if (seasonChapter.Success)
        {
            int season  = int.Parse(seasonChapter.Groups[1].Value);
            int chapter = int.Parse(seasonChapter.Groups[2].Value);
            var result  = $"CH{season:D2}{chapter:D2}";
            _logger.Debug("Normalized '{Input}' → '{Output}' via season+chapter rule", folderName, result);
            return result;
        }

        // Rule 3: Common chapter prefix
        // Matches: Chapter-01, Chap_99, Ch.05, ch01, Cap-01, cap_10,
        //          capitulo-03, episode-07, ep-12, MangaX_Chap_01
        var chapterPrefix = Regex.Match(name,
            @"(?:chapter|chap|ch|cap|capitulo|episode|ep)[-_.\s]*(\d+)",
            RegexOptions.IgnoreCase);

        if (chapterPrefix.Success)
        {
            int num    = int.Parse(chapterPrefix.Groups[1].Value);
            var result = $"CH{num:D4}";
            _logger.Debug("Normalized '{Input}' → '{Output}' via chapter prefix rule", folderName, result);
            return result;
        }

        // Rule 4: Trailing number fallback
        // Matches: 001, 01, 1, 100, manga_042, MangaName_012
        var trailingNumber = Regex.Match(name, @"0*(\d+)\s*$");

        if (trailingNumber.Success)
        {
            int num    = int.Parse(trailingNumber.Groups[1].Value);
            var result = $"CH{num:D4}";
            _logger.Debug("Normalized '{Input}' → '{Output}' via trailing number rule", folderName, result);
            return result;
        }

        // Last resort: sanitize the original name so it is at least safe to use on disk
        var fallback = "CH_" + Regex.Replace(name, @"[^\w]", "_").ToUpperInvariant();
        _logger.Warning("Could not extract chapter number from '{Input}', fallback: '{Output}'", folderName, fallback);
        return fallback;
    }

    public bool TryRenameOnDisk(string currentPath, out string newPath)
    {
        newPath = currentPath;

        var parentDir  = Path.GetDirectoryName(currentPath) ?? string.Empty;
        var folderName = Path.GetFileName(currentPath);
        var normalized = NormalizeChapterName(folderName);

        // No rename needed if already correct
        if (normalized == folderName)
        {
            _logger.Debug("No rename needed for '{Path}'", currentPath);
            return true;
        }

        newPath = Path.Combine(parentDir, normalized);

        // Do not overwrite an existing folder
        if (Directory.Exists(newPath))
        {
            _logger.Warning("Cannot rename '{From}' → '{To}': destination already exists", currentPath, newPath);
            return false;
        }

        try
        {
            Directory.Move(currentPath, newPath);
            _logger.Information("Renamed '{From}' → '{To}'", currentPath, newPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to rename '{From}' → '{To}'", currentPath, newPath);
            newPath = currentPath;
            return false;
        }
    }
}