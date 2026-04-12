using MangaStudio.Core.DTOs;

namespace MangaStudio.Core.Interfaces;

public interface IWidthNormalizer
{
    // Loads the image at imagePath and resizes it to targetWidth if needed.
    // Returns an IImage the caller owns and must dispose.
    // If loading or resizing fails, logs the error and returns null
    // so the pipeline can skip this image and continue.
    IImage? NormalizeWidth(string imagePath, int targetWidth);
}