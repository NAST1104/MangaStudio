namespace MangaStudio.Core.Interfaces;

// A backend-agnostic handle to an image.
// Always dispose this when you are done — the underlying library resource
// (VipsImage or ImageSharp Image<T>) is freed on Dispose.
public interface IImage : IDisposable
{
    int Width { get; }
    int Height { get; }

    // Null when the image was created in-memory (e.g. after resize)
    string? SourcePath { get; }
}