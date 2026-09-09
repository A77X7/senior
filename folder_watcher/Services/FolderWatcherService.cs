using System.Collections.Concurrent;
using Microsoft.Extensions.Options;

namespace folder_watcher.Services;

public sealed class FolderWatcherService : BackgroundService
{
    private readonly WatcherOptions _options;
    private readonly TesseractClient _tesseractClient;
    private readonly ILogger<FolderWatcherService> _logger;
    private readonly ConcurrentDictionary<string, byte> _processing = new(StringComparer.OrdinalIgnoreCase);

    public FolderWatcherService(
        IOptions<WatcherOptions> options,
        TesseractClient tesseractClient,
        ILogger<FolderWatcherService> logger)
    {
        _options = options.Value;
        _tesseractClient = tesseractClient;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var watchFolder = Path.GetFullPath(_options.WatchFolder);
        var resultFolder = Path.GetFullPath(_options.ResultFolder);

        Directory.CreateDirectory(watchFolder);
        Directory.CreateDirectory(resultFolder);

        await ProcessExistingFilesAsync(watchFolder, resultFolder, stoppingToken);

        using var watcher = new FileSystemWatcher(watchFolder)
        {
            IncludeSubdirectories = false,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
            EnableRaisingEvents = true,
        };

        watcher.Created += (_, e) => _ = ProcessAsync(e.FullPath, resultFolder, stoppingToken);
        watcher.Changed += (_, e) => _ = ProcessAsync(e.FullPath, resultFolder, stoppingToken);
        watcher.Renamed += (_, e) => _ = ProcessAsync(e.FullPath, resultFolder, stoppingToken);

        _logger.LogInformation("Наблюдение за папкой {WatchFolder} запущено", watchFolder);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task ProcessExistingFilesAsync(string watchFolder, string resultFolder, CancellationToken ct)
    {
        foreach (var path in Directory.EnumerateFiles(watchFolder))
        {
            await ProcessFileAsync(path, resultFolder, ct);
        }
    }

    private Task ProcessAsync(string path, string resultFolder, CancellationToken ct)
    {
        if (!File.Exists(path) || !_processing.TryAdd(path, 0))
        {
            return Task.CompletedTask;
        }

        return ProcessFileAsync(path, resultFolder, ct);
    }

    private async Task ProcessFileAsync(string path, string resultFolder, CancellationToken ct)
    {
        try
        {
            var outputPath = Path.Combine(resultFolder, Path.GetFileName(path) + ".txt");
            var text = await RecognizeFileAsync(path, ct);

            await File.WriteAllTextAsync(outputPath, text, ct);

            _logger.LogInformation("Файл {File} распознан в {Output}", path, outputPath);

            try
            {
                File.Delete(path);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Не удалось удалить исходный файл {File}", path);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не удалось обработать файл {File}", path);
        }
        finally
        {
            _processing.TryRemove(path, out _);
        }
    }

    private async Task<string> RecognizeFileAsync(string path, CancellationToken ct)
    {
        var contentType = ResolveContentType(path);

        const int maxAttempts = 10;
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                return await _tesseractClient.RecognizeAsync(stream, contentType, ct);
            }
            catch (IOException) when (!ct.IsCancellationRequested)
            {
                if (attempt == maxAttempts - 1)
                {
                    throw;
                }

                await Task.Delay(500, ct);
            }
        }

        throw new IOException($"Не удалось открыть файл {path} для чтения");
    }

    private static string ResolveContentType(string path) =>
        Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".tif" or ".tiff" => "image/tiff",
            ".bmp" => "image/bmp",
            ".webp" => "image/webp",
            ".pdf" => "application/pdf",
            _ => "application/octet-stream",
        };
}
