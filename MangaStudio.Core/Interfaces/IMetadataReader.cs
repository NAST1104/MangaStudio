using MangaStudio.Core.DTOs;

namespace MangaStudio.Core.Interfaces;

public interface IMetadataReader
{
    // Reads width and height from the file header only.
    // Never loads pixel data. Safe to call on thousands of files with no RAM spike.
    ImageInfo ReadInfo(string imagePath);

    // Reads metadata for every image in the list.
    // Returns only successful reads — failures are logged and skipped.
    IEnumerable<ImageInfo> ReadAll(IEnumerable<string> imagePaths);
}