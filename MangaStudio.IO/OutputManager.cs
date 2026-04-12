using Serilog;

namespace MangaStudio.IO;

public class OutputManager
{
    private readonly ILogger _logger;

    public OutputManager(ILogger logger)
    {
        _logger = logger.ForContext<OutputManager>();
    }

    // Creates the output directory for a chapter if it doesn't exist yet
    public string EnsureOutputDirectory(string outputRoot, string chapterName)
    {
        var path = Path.Combine(outputRoot, chapterName);

        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            _logger.Debug("Created output directory: {Path}", path);
        }

        return path;
    }

    // Builds the output file name for each stitched chunk
    // Example: CH0001, index 3, webp → CH0001-003.webp
    public string GetOutputFilePath(string outputDir, string chapterName, int fileIndex, string extension)
    {
        var ext      = extension.TrimStart('.');
        var fileName = $"{chapterName}-{fileIndex:D3}.{ext}";
        return Path.Combine(outputDir, fileName);
    }

    // Deletes the original chapter folder after successful processing
    public void DeleteOriginalChapter(string chapterPath)
    {
        try
        {
            Directory.Delete(chapterPath, recursive: true);
            _logger.Information("Deleted original chapter folder: {Path}", chapterPath);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to delete original chapter folder: {Path}", chapterPath);
        }
    }
}