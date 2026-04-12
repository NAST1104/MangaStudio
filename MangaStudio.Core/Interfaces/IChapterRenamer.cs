namespace MangaStudio.Core.Interfaces;

public interface IChapterRenamer
{
    // Converts any chapter folder name into the CH0001 format (in memory only)
    string NormalizeChapterName(string folderName);

    // Renames the folder on disk and returns the new full path
    bool TryRenameOnDisk(string currentPath, out string newPath);
}