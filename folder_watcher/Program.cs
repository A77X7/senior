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

builder.Services.AddHostedService<FolderWatcherService>();

var app = builder.Build();

app.Run();
