namespace MangaStudio.Core.Interfaces;

public interface IOutputManager
{
    string EnsureOutputDirectory(string outputRoot, string chapterName);
    string GetOutputFilePath(string outputDir, string chapterName, int fileIndex, string extension);
    void DeleteOriginalChapter(string chapterPath);
}