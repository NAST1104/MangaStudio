using System.Windows;
using System.Windows.Controls;
using MangaStudio.Core.DTOs;
using MangaStudio.UI.ViewModels;

namespace MangaStudio.UI.Views;

public partial class HomeView : UserControl
{
    public HomeView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender,
        DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is HomeViewModel oldVm)
            oldVm.ProcessingCompleted -= OnProcessingCompleted;

        if (e.NewValue is HomeViewModel newVm)
            newVm.ProcessingCompleted += OnProcessingCompleted;
    }

    private void OnProcessingCompleted(List<ProcessingResult> results, string outputPath)
    {
        var window = new SummaryWindow(results, outputPath)
        {
            Owner = Window.GetWindow(this)
        };
        window.ShowDialog();
    }

    private void FolderList_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void FolderList_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

        var paths = (string[])e.Data.GetData(DataFormats.FileDrop);
        if (DataContext is HomeViewModel vm)
            vm.AddFolders(paths);
    }
}