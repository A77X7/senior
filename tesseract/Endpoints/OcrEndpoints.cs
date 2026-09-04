using tesseract.Services;

namespace tesseract.Endpoints;

public static class OcrEndpoints
{
    public static IEndpointRouteBuilder MapOcrEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/ocr", async (HttpContext context, IOcrService ocrService) =>
        {
            var language = context.Request.Query["lang"].ToString();
            if (string.IsNullOrWhiteSpace(language))
            {
                language = "eng+rus";
            }

            try
            {
                var text = await ocrService.RecognizeAsync(
                    context.Request.Body,
                    context.Request.ContentType,
                    language,
                    context.RequestAborted);

                return Results.Ok(new { text });
            }
            catch (InvalidOperationException ex)
            {
                return Results.Problem(ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        });

        return app;
    }
}
