namespace folder_watcher.Services;

public sealed class WatcherOptions
{
    public string WatchFolder { get; set; } = string.Empty;

    public string ResultFolder { get; set; } = string.Empty;

    public string TesseractBaseUrl { get; set; } = "http://localhost:8081";
}
