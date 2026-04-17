using Microsoft.Extensions.DependencyInjection;
using MangaStudio.Core.Enums;
using MangaStudio.Core.Interfaces;
using MangaStudio.Core.Processing;
using MangaStudio.Core.Services;
using MangaStudio.Imaging;
using MangaStudio.IO;
using MangaStudio.UI.Logging;
using MangaStudio.UI.Services;
using MangaStudio.UI.ViewModels;
using Serilog;
using System.Windows;

namespace MangaStudio.UI;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // ── Step 1: Create LogViewModel first so UiLogSink can reference it ──
        var logViewModel = new LogViewModel();

        // ── Step 2: Configure Serilog with the UI sink ────────────────────────
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console(outputTemplate:
                "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File("logs/mangastudio-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7)
            .WriteTo.Sink(new UiLogSink(logViewModel))
            .CreateLogger();

        Log.Information("MangaStudio starting up");

        // ── Step 3: Load settings to know which backend to start with ─────────
        var settingsService = new SettingsService();
        var appSettings = settingsService.Load();

        // ── Step 4: Build DI container ────────────────────────────────────────
        var services = new ServiceCollection();

        // Infrastructure
        services.AddSingleton<ILogger>(Log.Logger);
        services.AddSingleton(settingsService);

        // IO
        services.AddSingleton<IFolderScanner, FolderScanner>();
        services.AddSingleton<IFileSorter, FileSorter>();
        services.AddSingleton<IChapterRenamer, ChapterRenamer>();
        services.AddSingleton<IOutputManager, OutputManager>();

        // Imaging
        services.AddSingleton<ImageServiceFactory>();
        services.AddSingleton<IImageService>(provider =>
            provider.GetRequiredService<ImageServiceFactory>().Create(appSettings.Backend));

        // Core pipeline
        services.AddSingleton<IMetadataReader, MetadataReader>();
        services.AddSingleton<IChapterMetadataBuilder, ChapterMetadataBuilder>();
        services.AddSingleton<IImageEnumerator, ImageEnumerator>();
        services.AddSingleton<IWidthNormalizer, WidthNormalizer>();
        services.AddSingleton<IStitchPlanner, StitchPlanner>();
        services.AddSingleton<IStitchEngine, StitchEngine>();
        services.AddSingleton<PipelineOrchestrator>();

        // ViewModels — LogViewModel is pre-created so we register the instance
        services.AddSingleton(logViewModel);
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<HomeViewModel>();
        services.AddSingleton<MainViewModel>();

        Services = services.BuildServiceProvider();

        Log.Information("DI container built — backend: {Backend}", appSettings.Backend);

        new MainWindow().Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("MangaStudio shutting down");
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}