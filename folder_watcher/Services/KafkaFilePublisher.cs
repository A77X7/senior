using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace folder_watcher.Services;

public sealed class KafkaFilePublisher : IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly KafkaOptions _options;
    private readonly ILogger<KafkaFilePublisher> _logger;

    public KafkaFilePublisher(IOptions<WatcherOptions> options, ILogger<KafkaFilePublisher> logger)
    {
        _options = options.Value.Kafka;
        _logger = logger;

        var config = new ProducerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            Acks = Acks.All,
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishAsync(string filePath, CancellationToken ct)
    {
        var message = new Message<string, string>
        {
            Key = Path.GetFileName(filePath),
            Value = filePath,
        };

        await _producer.ProduceAsync(_options.RequestTopic, message, ct);
        _logger.LogInformation("Файл {File} поставлен в очередь Kafka", filePath);
    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
    }
}
