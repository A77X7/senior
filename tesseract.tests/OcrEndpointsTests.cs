using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using tesseract.Endpoints;
using tesseract.Services;

namespace tesseract.tests;

public sealed class OcrEndpointsTests
{
    [Fact]
    public async Task Ping_ReturnsPong()
    {
        await using var app = await CreateAppAsync();
        using var client = app.GetTestClient();

        var response = await client.GetAsync("/ping");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("\"pong\"", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Ocr_UsesDefaultLanguage_WhenLangMissing()
    {
        var fake = new FakeOcrService();
        await using var app = await CreateAppAsync(fake);
        using var client = app.GetTestClient();

        var response = await client.PostAsync("/ocr", new ByteArrayContent(Array.Empty<byte>()));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("eng+rus", fake.Language);
    }

    [Fact]
    public async Task Ocr_UsesProvidedLanguage()
    {
        var fake = new FakeOcrService();
        await using var app = await CreateAppAsync(fake);
        using var client = app.GetTestClient();

        var response = await client.PostAsync("/ocr?lang=deu", new ByteArrayContent(Array.Empty<byte>()));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("deu", fake.Language);
    }

    [Fact]
    public async Task Ocr_ReturnsRecognizedText()
    {
        var fake = new FakeOcrService { Result = "hello world" };
        await using var app = await CreateAppAsync(fake);
        using var client = app.GetTestClient();

        var response = await client.PostAsync("/ocr", new ByteArrayContent(Array.Empty<byte>()));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("hello world", body);
    }

    [Fact]
    public async Task Ocr_Returns500_OnInvalidOperationException()
    {
        var fake = new FakeOcrService { ExceptionToThrow = new InvalidOperationException("tesseract failed") };
        await using var app = await CreateAppAsync(fake);
        using var client = app.GetTestClient();

        var response = await client.PostAsync("/ocr", new ByteArrayContent(Array.Empty<byte>()));

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    private static async Task<WebApplication> CreateAppAsync(IOcrService? ocrService = null)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        if (ocrService is not null)
        {
            builder.Services.AddSingleton(ocrService);
        }

        var app = builder.Build();
        app.MapOcrEndpoints();
        await app.StartAsync();

        return app;
    }

    private sealed class FakeOcrService : IOcrService
    {
        public string? Language { get; private set; }

        public string Result { get; set; } = "recognized text";

        public Exception? ExceptionToThrow { get; set; }

        public Task<string> RecognizeAsync(Stream stream, string? contentType, string language, CancellationToken ct = default)
        {
            Language = language;

            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }

            return Task.FromResult(Result);
        }
    }
}
