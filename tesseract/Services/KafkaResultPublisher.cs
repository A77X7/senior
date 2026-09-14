using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace tesseract.Services;

public sealed class KafkaResultPublisher : IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly KafkaOptions _options;

    public KafkaResultPublisher(IOptions<KafkaOptions> options)
    {
        _options = options.Value;

        var config = new ProducerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            Acks = Acks.All,
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishAsync(string filePath, string text, CancellationToken ct)
    {
        var payload = JsonSerializer.Serialize(new OcrResultMessage(filePath, text));

        var message = new Message<string, string>
        {
            Key = Path.GetFileName(filePath),
            Value = payload,
        };

        await _producer.ProduceAsync(_options.ResultTopic, message, ct);
    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
    }

    private sealed record OcrResultMessage(string Path, string Text);
}
