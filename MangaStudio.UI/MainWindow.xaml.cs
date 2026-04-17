using Microsoft.Extensions.DependencyInjection;
using MangaStudio.UI.ViewModels;
using System.Windows;

namespace MangaStudio.UI;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<MainViewModel>();
    }
}