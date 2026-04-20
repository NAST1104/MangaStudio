using Microsoft.Extensions.DependencyInjection;
using MangaStudio.UI.ViewModels;
using MangaStudio.UI.Views;
using System.Windows;

namespace MangaStudio.UI;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<MainViewModel>();
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        new AboutWindow { Owner = this }.ShowDialog();
    }
}