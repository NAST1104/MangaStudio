namespace MangaStudio.Core.Interfaces;

public interface IFolderScanner
{
    // Returns paths of all subdirectories that contain at least one image
    IEnumerable<string> ScanForChapters(string mangaRootPath);

    // Returns sorted image file paths inside a chapter folder
    IEnumerable<string> GetImagePaths(string chapterPath);
}