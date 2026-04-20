using System.Collections.ObjectModel;
using System.Windows.Input;
using MangaStudio.UI.Commands;
using MangaStudio.UI.Models;
using MangaStudio.UI.Services;

namespace MangaStudio.UI.ViewModels;

public class HistoryViewModel : BaseViewModel
{
    private readonly HistoryService _historyService;

    public ObservableCollection<HistoryEntry> Entries { get; } = new();

    private HistoryEntry? _selected;
    public HistoryEntry? Selected
    {
        get => _selected;
        set => SetField(ref _selected, value);
    }

    public ICommand ClearAllCommand { get; }
    public ICommand RemoveCommand { get; }

    public HistoryViewModel(HistoryService historyService)
    {
        _historyService = historyService;

        ClearAllCommand = new RelayCommand(ClearAll, () => Entries.Count > 0);
        RemoveCommand = new RelayCommand(RemoveSelected, () => Selected is not null);

        Reload();
    }

    public void Reload()
    {
        Entries.Clear();
        foreach (var e in _historyService.GetAll())
            Entries.Add(e);
    }

    public void AddEntry(HistoryEntry entry)
    {
        _historyService.AddEntry(entry);
        Entries.Insert(0, entry); // newest first
    }

    private void ClearAll()
    {
        _historyService.Clear();
        Entries.Clear();
    }

    private void RemoveSelected()
    {
        if (Selected is null) return;
        _historyService.Remove(Selected.Id);
        Entries.Remove(Selected);
        Selected = null;
    }
}