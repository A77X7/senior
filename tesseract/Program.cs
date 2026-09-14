using tesseract.Endpoints;
using tesseract.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<KafkaOptions>(builder.Configuration.GetSection("Kafka"));

builder.Services.AddSingleton<IOcrService, OcrService>();
builder.Services.AddSingleton<KafkaResultPublisher>();
builder.Services.AddHostedService<KafkaOcrConsumerService>();

var app = builder.Build();

app.MapOcrEndpoints();

app.Run();
