namespace folder_watcher.Services;

public sealed class WatcherOptions
{
    public string WatchFolder { get; set; } = string.Empty;

    public string ResultFolder { get; set; } = string.Empty;

    public string TesseractBaseUrl { get; set; } = "http://localhost:8081";

    public string Transport { get; set; } = "Http";

    public int PollingIntervalSeconds { get; set; } = 10;

    public KafkaOptions Kafka { get; set; } = new();
}

public sealed class KafkaOptions
{
    public string BootstrapServers { get; set; } = "localhost:9092";

    public string RequestTopic { get; set; } = "ocr-requests";

    public string ResultTopic { get; set; } = "ocr-results";

    public string GroupId { get; set; } = "folder-watcher";
}
