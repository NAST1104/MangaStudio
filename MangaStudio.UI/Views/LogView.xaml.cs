using System.Collections.Specialized;
using System.Windows.Controls;
using MangaStudio.UI.ViewModels;

namespace MangaStudio.UI.Views;

public partial class LogView : UserControl
{
    private LogViewModel? _vm;

    public LogView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender,
        System.Windows.DependencyPropertyChangedEventArgs e)
    {
        if (_vm is not null)
            _vm.Entries.CollectionChanged -= OnEntriesChanged;

        _vm = DataContext as LogViewModel;

        if (_vm is not null)
            _vm.Entries.CollectionChanged += OnEntriesChanged;
    }

    // Auto-scroll to the newest entry whenever the collection grows
    private void OnEntriesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add
            && LogListView.Items.Count > 0)
        {
            LogListView.ScrollIntoView(
                LogListView.Items[LogListView.Items.Count - 1]);
        }
    }
}