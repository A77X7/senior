using tesseract.Services;

namespace tesseract.tests;

public sealed class OcrServiceTests
{
    [Theory]
    [InlineData("image/jpeg", ".jpg")]
    [InlineData("image/jpg", ".jpg")]
    [InlineData("image/png", ".png")]
    [InlineData("image/tiff", ".tiff")]
    [InlineData("image/bmp", ".bmp")]
    [InlineData("image/webp", ".webp")]
    [InlineData("image/gif", ".gif")]
    [InlineData("application/pdf", ".pdf")]
    public void ResolveExtension_MapsKnownContentType(string contentType, string expected)
    {
        var result = OcrService.ResolveExtension(contentType);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("text/plain")]
    [InlineData("application/octet-stream")]
    public void ResolveExtension_UnknownOrMissingContentType_ReturnsBin(string? contentType)
    {
        var result = OcrService.ResolveExtension(contentType);

        Assert.Equal(".bin", result);
    }

    [Theory]
    [InlineData("IMAGE/JPEG", ".jpg")]
    [InlineData("Image/Png", ".png")]
    [InlineData("APPLICATION/PDF", ".pdf")]
    public void ResolveExtension_IsCaseInsensitive(string contentType, string expected)
    {
        var result = OcrService.ResolveExtension(contentType);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("image/jpeg; charset=utf-8", ".jpg")]
    [InlineData("image/png ; boundary=abc", ".png")]
    public void ResolveExtension_StripsParameters(string contentType, string expected)
    {
        var result = OcrService.ResolveExtension(contentType);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("prefix-0001.png", 1)]
    [InlineData("page-0012.png", 12)]
    [InlineData("page-00042.png", 42)]
    [InlineData("page-0.png", 0)]
    [InlineData("page-123.png", 123)]
    public void ExtractPageNumber_ReturnsTrailingNumber(string fileName, int expected)
    {
        var path = Path.Combine("C:", "temp", fileName);

        var result = OcrService.ExtractPageNumber(path);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ExtractPageNumber_NoDigits_ReturnsZero()
    {
        var result = OcrService.ExtractPageNumber(Path.Combine("C:", "temp", "page.png"));

        Assert.Equal(0, result);
    }
}
