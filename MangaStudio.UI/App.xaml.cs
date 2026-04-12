using Microsoft.Extensions.DependencyInjection;
using MangaStudio.Core.Enums;
using MangaStudio.Core.Interfaces;
using MangaStudio.Imaging;
using MangaStudio.IO;
using Serilog;
using System.Windows;

namespace MangaStudio.UI;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File("logs/mangastudio-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7)
            .CreateLogger();

        Log.Information("MangaStudio starting up");

        var services = new ServiceCollection();

        // Logging
        services.AddSingleton<ILogger>(Log.Logger);

        // IO services
        services.AddSingleton<IFolderScanner, FolderScanner>();
        services.AddSingleton<IFileSorter, FileSorter>();
        services.AddSingleton<IChapterRenamer, ChapterRenamer>();
        services.AddSingleton<OutputManager>();

        // Imaging
        services.AddSingleton<ImageServiceFactory>();

        // Default backend is libvips. Phase 5 (Settings UI) will let the user
        // change this at runtime. For now it is wired here.
        services.AddSingleton<IImageService>(provider =>
        {
            var factory = provider.GetRequiredService<ImageServiceFactory>();
            return factory.Create(ImagingBackend.Vips);
        });

        Services = services.BuildServiceProvider();

        var mainWindow = new MainWindow();
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("MangaStudio shutting down");
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}