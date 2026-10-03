using OpenTelemetry.Metrics;
using Serilog;
using tesseract.Endpoints;
using tesseract.Metrics;
using tesseract.Services;

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .Build())
    .CreateLogger();

try
{
    Log.Information("Starting up the application");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, configuration) =>
    {
        configuration.ReadFrom.Configuration(context.Configuration);
    });

    builder.Services.Configure<KafkaOptions>(builder.Configuration.GetSection("Kafka"));

    builder.Services.AddSingleton<IOcrService, OcrService>();
    builder.Services.AddSingleton<TesseractMetrics>();
    builder.Services.AddSingleton<KafkaResultPublisher>();
    builder.Services.AddHostedService<KafkaOcrConsumerService>();

    builder.Services.AddOpenTelemetry()
        .WithMetrics(metricsProvider =>
        {
            metricsProvider
                .AddAspNetCoreInstrumentation() // Стандартные метрики ASP.NET Core
                .AddRuntimeInstrumentation()
                .AddMeter(TesseractMetrics.MeterName) // Говорим OTel слушать наш кастомный Meter
                .AddPrometheusExporter(); // Включаем поддержку формата Prometheus
        });

    var app = builder.Build();

    app.UseSerilogRequestLogging();

    app.UseOpenTelemetryPrometheusScrapingEndpoint();

    app.MapOcrEndpoints();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application start-up failed");
}
finally
{
    Log.CloseAndFlush();
}