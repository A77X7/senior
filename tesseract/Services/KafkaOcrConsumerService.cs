using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace tesseract.Services;

public sealed class KafkaOcrConsumerService : BackgroundService
{
    private readonly KafkaOptions _options;
    private readonly IOcrService _ocrService;
    private readonly KafkaResultPublisher _resultPublisher;
    private readonly ILogger<KafkaOcrConsumerService> _logger;

    public KafkaOcrConsumerService(
        IOptions<KafkaOptions> options,
        IOcrService ocrService,
        KafkaResultPublisher resultPublisher,
        ILogger<KafkaOcrConsumerService> logger)
    {
        _options = options.Value;
        _ocrService = ocrService;
        _resultPublisher = resultPublisher;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            GroupId = _options.GroupId,
            EnableAutoCommit = false,
            AutoOffsetReset = AutoOffsetReset.Earliest,
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(_options.RequestTopic);

        _logger.LogInformation(
            "Слушатель Kafka запущен: тема {Topic}, группа {Group}",
            _options.RequestTopic,
            _options.GroupId);

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
                _logger.LogError(ex, "Ошибка чтения из Kafka");
                continue;
            }

            var path = result.Message.Value;

            try
            {
                var text = await ProcessFileAsync(path, stoppingToken);
                await _resultPublisher.PublishAsync(path, text, stoppingToken);

                try
                {
                    consumer.Commit(result);
                }
                catch (KafkaException ex)
                {
                    _logger.LogWarning(ex, "Не удалось зафиксировать смещение для {File}", path);
                }

                _logger.LogInformation("Файл {File} распознан, результат отправлен в Kafka", path);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Не удалось обработать файл {File}", path);
            }
        }
    }

    private async Task<string> ProcessFileAsync(string path, CancellationToken ct)
    {
        var contentType = ResolveContentType(path);

        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        var text = await _ocrService.RecognizeAsync(stream, contentType, "eng+rus", ct);

        return text;
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
