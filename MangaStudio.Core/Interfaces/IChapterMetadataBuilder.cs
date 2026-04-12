using MangaStudio.Core.DTOs;

namespace MangaStudio.Core.Interfaces;

public interface IChapterMetadataBuilder
{
    // Given a chapter folder path and its sorted image paths,
    // reads all metadata and produces a complete ChapterMetadata object
    // including the resolved targetWidth.
    ChapterMetadata Build(string chapterPath, IEnumerable<string> sortedImagePaths);
}