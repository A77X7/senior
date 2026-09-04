using Microsoft.AspNetCore.Antiforgery;
using ocr.Services;

namespace ocr.Endpoints;

public static class OcrEndpoints
{
    public static IEndpointRouteBuilder MapOcrEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/");

        group.MapGet("/test", (HttpContext context, IAntiforgery antiforgery) =>
        {
            var tokens = antiforgery.GetAndStoreTokens(context);

            return Results.Content(
                $"""
                <!DOCTYPE html>
                <html lang="ru">
                <head>
                    <meta charset="utf-8" />
                    <title>OCR</title>
                </head>
                <body>
                    <form method="post" action="/test" enctype="multipart/form-data">
                        <input type="hidden" name="{tokens.FormFieldName}" value="{tokens.RequestToken}" />
                        <input type="file" name="file" accept="image/*,.pdf" />
                        <button type="submit">Распознать</button>
                    </form>
                </body>
                </html>
                """,
                "text/html");
        });

        group.MapPost("/test", async (IFormFile file, TesseractClient client, CancellationToken ct) =>
        {
            await using var stream = file.OpenReadStream();
            var contentType = ResolveContentType(file);

            try
            {
                var text = await client.RecognizeAsync(stream, contentType, ct);
                return Results.Content(text, "text/plain; charset=utf-8");
            }
            catch (HttpRequestException ex)
            {
                return Results.Problem(ex.Message, statusCode: StatusCodes.Status502BadGateway);
            }
        });

        return app;
    }

    private static string ResolveContentType(IFormFile file)
    {
        if (!string.IsNullOrWhiteSpace(file.ContentType))
        {
            return file.ContentType;
        }

        return Path.GetExtension(file.FileName).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".tif" or ".tiff" => "image/tiff",
            ".bmp" => "image/bmp",
            ".webp" => "image/webp",
            ".pdf" => "application/pdf",
            _ => "application/octet-stream",
        };
    }
}
