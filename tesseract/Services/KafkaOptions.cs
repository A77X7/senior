namespace tesseract.Services;

public sealed class KafkaOptions
{
    public string BootstrapServers { get; set; } = "localhost:9092";

    public string RequestTopic { get; set; } = "ocr-requests";

    public string ResultTopic { get; set; } = "ocr-results";

    public string GroupId { get; set; } = "tesseract";
}
