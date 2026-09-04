using Microsoft.Extensions.Options;
using ocr.Endpoints;
using ocr.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<TesseractOptions>(builder.Configuration.GetSection("Tesseract"));

builder.Services.AddAntiforgery();

builder.Services.AddHttpClient<TesseractClient>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<TesseractOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
});

var app = builder.Build();

app.UseAntiforgery();

app.MapOcrEndpoints();

app.Run();
