namespace MangaStudio.UI.ViewModels;

public class MainViewModel : BaseViewModel
{
    public HomeViewModel Home { get; }
    public SettingsViewModel Settings { get; }
    public LogViewModel Logs { get; }

    public MainViewModel(
        HomeViewModel home,
        SettingsViewModel settings,
        LogViewModel logs)
    {
        Home = home;
        Settings = settings;
        Logs = logs;
    }
}