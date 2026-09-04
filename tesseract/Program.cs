using tesseract.Endpoints;
using tesseract.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IOcrService, OcrService>();

var app = builder.Build();

app.MapOcrEndpoints();

app.Run();
