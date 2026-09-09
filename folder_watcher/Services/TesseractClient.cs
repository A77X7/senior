using System.Net.Http.Headers;
using System.Text.Json.Serialization;

namespace folder_watcher.Services;

public sealed class TesseractClient
{
    private readonly HttpClient _httpClient;

    public TesseractClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> RecognizeAsync(Stream content, string contentType, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "ocr")
        {
            Content = new StreamContent(content),
        };
        request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);

        using var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OcrResponse>(ct);
        return result?.Text ?? string.Empty;
    }

    private sealed record OcrResponse([property: JsonPropertyName("text")] string Text);
}
