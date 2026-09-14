using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace folder_watcher.Services;

public sealed class KafkaResultConsumerService : BackgroundService
{
    private readonly WatcherOptions _options;
    private readonly PublishedFiles _publishedFiles;
    private readonly ILogger<KafkaResultConsumerService> _logger;

    public KafkaResultConsumerService(
        IOptions<WatcherOptions> options,
        PublishedFiles publishedFiles,
        ILogger<KafkaResultConsumerService> logger)
    {
        _options = options.Value;
        _publishedFiles = publishedFiles;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var kafka = _options.Kafka;
        var resultFolder = Path.GetFullPath(_options.ResultFolder);
        Directory.CreateDirectory(resultFolder);

        var config = new ConsumerConfig
        {
            BootstrapServers = kafka.BootstrapServers,
            GroupId = kafka.GroupId,
            EnableAutoCommit = false,
            AutoOffsetReset = AutoOffsetReset.Earliest,
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(kafka.ResultTopic);

        _logger.LogInformation(
            "Слушатель результатов Kafka запущен: тема {Topic}, группа {Group}",
            kafka.ResultTopic,
            kafka.GroupId);

        while (!stoppingToken.IsCancellationRequested)
        {
            ConsumeResult<string, string> result;
            try
            {
                result = consumer.Consume(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ConsumeException ex)
            {
                _logger.LogError(ex, "Ошибка чтения результатов из Kafka");
                continue;
            }

            try
            {
                var message = JsonSerializer.Deserialize<OcrResultMessage>(result.Message.Value);

                if (message is null)
                {
                    _logger.LogWarning("Получено пустое сообщение результата, пропуск");
                }
                else
                {
                    await SaveResultAsync(message.Path, message.Text, resultFolder, stoppingToken);
                }

                try
                {
                    consumer.Commit(result);
                }
                catch (KafkaException ex)
                {
                    _logger.LogWarning(ex, "Не удалось зафиксировать смещение результата {File}", result.Message.Value);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Не удалось обработать результат из Kafka");
            }
        }
    }

    private async Task SaveResultAsync(string path, string text, string resultFolder, CancellationToken ct)
    {
        var outputPath = Path.Combine(resultFolder, Path.GetFileName(path) + ".txt");
        await File.WriteAllTextAsync(outputPath, text, ct);

        _logger.LogInformation("Результат для {File} сохранён в {Output}", path, outputPath);

        try
        {
            File.Delete(path);
            _publishedFiles.Remove(path);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Не удалось удалить исходный файл {File}", path);
        }
    }

    private sealed record OcrResultMessage(string Path, string Text);
}
