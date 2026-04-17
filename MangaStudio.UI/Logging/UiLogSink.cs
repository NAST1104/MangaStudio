using MangaStudio.UI.Models;
using MangaStudio.UI.ViewModels;
using Serilog.Core;
using Serilog.Events;
using System.Windows;

namespace MangaStudio.UI.Logging;

// Serilog sink that feeds log events into the LogViewModel in real time.
// Emit() can be called from any thread — we always marshal to the UI thread.
public sealed class UiLogSink : ILogEventSink
{
    private readonly LogViewModel _logViewModel;

    public UiLogSink(LogViewModel logViewModel)
    {
        _logViewModel = logViewModel;
    }

    public void Emit(LogEvent logEvent)
    {
        var entry = new LogEntry
        {
            Timestamp = logEvent.Timestamp.DateTime,
            Level = logEvent.Level.ToString(),
            Message = logEvent.RenderMessage()
        };

        Application.Current?.Dispatcher.InvokeAsync(() => _logViewModel.AddEntry(entry));
    }
}