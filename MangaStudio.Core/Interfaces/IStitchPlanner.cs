using MangaStudio.Core.DTOs;

namespace MangaStudio.Core.Interfaces;

public interface IStitchPlanner
{
    // Given all image heights in a chapter and the max stitch height per chunk,
    // returns a list of chunk plans — each plan is a list of image indices
    // that belong to that chunk.
    // Example: 5 images [100,200,550,650,500] with maxHeight=1000 →
    //   chunk 0: [0,1,2]    (100+200+550 = 850 ≤ 1000)
    //   chunk 1: [3]        (650 alone — adding 850 would exceed 1000)
    //   chunk 2: [4]        (500)
    List<List<int>> Plan(List<int> imageHeights, int maxStitchHeight);
}