using Microsoft.Extensions.DependencyInjection;
using MangaStudio.Core.Enums;
using MangaStudio.Core.Interfaces;
using MangaStudio.Core.Processing;
using MangaStudio.Core.Services;
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
            .WriteTo.Console(outputTemplate:
                "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File("logs/mangastudio-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7)
            .CreateLogger();

        Log.Information("MangaStudio starting up");

        var services = new ServiceCollection();

        // Logging
        services.AddSingleton<ILogger>(Log.Logger);

        // IO
        services.AddSingleton<IFolderScanner, FolderScanner>();
        services.AddSingleton<IFileSorter, FileSorter>();
        services.AddSingleton<IChapterRenamer, ChapterRenamer>();
        services.AddSingleton<IOutputManager, OutputManager>();

        // Imaging
        services.AddSingleton<ImageServiceFactory>();
        services.AddSingleton<IImageService>(provider =>
            provider.GetRequiredService<ImageServiceFactory>().Create(ImagingBackend.Vips));

        // Core pipeline
        services.AddSingleton<IMetadataReader, MetadataReader>();
        services.AddSingleton<IChapterMetadataBuilder, ChapterMetadataBuilder>();
        services.AddSingleton<IImageEnumerator, ImageEnumerator>();
        services.AddSingleton<IWidthNormalizer, WidthNormalizer>();
        services.AddSingleton<IStitchPlanner, StitchPlanner>();
        services.AddSingleton<IStitchEngine, StitchEngine>();
        services.AddSingleton<PipelineOrchestrator>();

        Services = services.BuildServiceProvider();

        new MainWindow().Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("MangaStudio shutting down");
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}