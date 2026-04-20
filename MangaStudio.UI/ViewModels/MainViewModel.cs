namespace MangaStudio.UI.ViewModels;

public class MainViewModel : BaseViewModel
{
    public HomeViewModel Home { get; }
    public SettingsViewModel Settings { get; }
    public HistoryViewModel History { get; }
    public LogViewModel Logs { get; }

    public MainViewModel(
        HomeViewModel home,
        SettingsViewModel settings,
        HistoryViewModel history,
        LogViewModel logs)
    {
        Home = home;
        Settings = settings;
        History = history;
        Logs = logs;
    }
}