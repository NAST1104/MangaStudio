namespace MangaStudio.Core.Interfaces;

public interface IFileSorter
{
    // Sorts file paths using natural order so img2 comes before img10
    IEnumerable<string> Sort(IEnumerable<string> paths);
}