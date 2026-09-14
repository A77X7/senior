using Microsoft.Extensions.Options;

namespace folder_watcher.Services;

public sealed class FolderWatcherKafkaService : BackgroundService
{
    private readonly WatcherOptions _options;
    private readonly KafkaFilePublisher _publisher;
    private readonly PublishedFiles _publishedFiles;
    private readonly ILogger<FolderWatcherKafkaService> _logger;

    public FolderWatcherKafkaService(
        IOptions<WatcherOptions> options,
        KafkaFilePublisher publisher,
        PublishedFiles publishedFiles,
        ILogger<FolderWatcherKafkaService> logger)
    {
        _options = options.Value;
        _publisher = publisher;
        _publishedFiles = publishedFiles;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var watchFolder = Path.GetFullPath(_options.WatchFolder);
        Directory.CreateDirectory(watchFolder);

        await PublishExistingFilesAsync(watchFolder, stoppingToken);

        using var watcher = new FileSystemWatcher(watchFolder)
        {
            IncludeSubdirectories = false,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
            EnableRaisingEvents = true,
        };

        watcher.Created += (_, e) => _ = PublishAsync(e.FullPath, stoppingToken);
        watcher.Changed += (_, e) => _ = PublishAsync(e.FullPath, stoppingToken);
        watcher.Renamed += (_, e) => _ = PublishAsync(e.FullPath, stoppingToken);

        _logger.LogInformation("Наблюдение за папкой {WatchFolder} запущено (Kafka)", watchFolder);

        using var pollTimer = new PeriodicTimer(TimeSpan.FromSeconds(_options.PollingIntervalSeconds));

        try
        {
            while (await pollTimer.WaitForNextTickAsync(stoppingToken))
            {
                await PublishExistingFilesAsync(watchFolder, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task PublishExistingFilesAsync(string watchFolder, CancellationToken ct)
    {
        foreach (var path in Directory.EnumerateFiles(watchFolder))
        {
            await PublishAsync(path, ct);
        }
    }

    private async Task PublishAsync(string path, CancellationToken ct)
    {
        if (!File.Exists(path) || !_publishedFiles.TryAdd(path))
        {
            return;
        }

        try
        {
            await _publisher.PublishAsync(path, ct);
        }
        catch (OperationCanceledException)
        {
            _publishedFiles.Remove(path);
        }
        catch (Exception ex)
        {
            _publishedFiles.Remove(path);
            _logger.LogError(ex, "Не удалось поставить файл {File} в очередь Kafka", path);
        }
    }
}
