using folder_watcher.Services;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<WatcherOptions>(builder.Configuration.GetSection("Watcher"));

builder.Services.AddHttpClient<TesseractClient>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<WatcherOptions>>().Value;
    client.BaseAddress = new Uri(options.TesseractBaseUrl);
})
.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
{
    UseProxy = false,
});

var transport = builder.Configuration["Watcher:Transport"] ?? "Http";

if (transport.Equals("Kafka", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddSingleton<PublishedFiles>();
    builder.Services.AddSingleton<KafkaFilePublisher>();
    builder.Services.AddHostedService<FolderWatcherKafkaService>();
    builder.Services.AddHostedService<KafkaResultConsumerService>();
}
else
{
    builder.Services.AddHostedService<FolderWatcherService>();
}

var app = builder.Build();

app.Run();
