namespace MangaStudio.UI.Models;

public class LogEntry
{
    public DateTime Timestamp { get; init; }
    public string Level { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}