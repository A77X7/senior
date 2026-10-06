using tesseract.Services;

namespace tesseract.tests;

public sealed class KafkaOcrConsumerServiceTests
{
    [Theory]
    [InlineData(".jpg", "image/jpeg")]
    [InlineData(".jpeg", "image/jpeg")]
    [InlineData(".png", "image/png")]
    [InlineData(".tif", "image/tiff")]
    [InlineData(".tiff", "image/tiff")]
    [InlineData(".bmp", "image/bmp")]
    [InlineData(".webp", "image/webp")]
    [InlineData(".pdf", "application/pdf")]
    public void ResolveContentType_MapsKnownExtension(string extension, string expected)
    {
        var result = KafkaOcrConsumerService.ResolveContentType("file" + extension);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(".txt")]
    [InlineData(".docx")]
    [InlineData("")]
    public void ResolveContentType_UnknownExtension_ReturnsOctetStream(string extension)
    {
        var result = KafkaOcrConsumerService.ResolveContentType("file" + extension);

        Assert.Equal("application/octet-stream", result);
    }

    [Theory]
    [InlineData(".JPG", "image/jpeg")]
    [InlineData(".Png", "image/png")]
    [InlineData(".PDF", "application/pdf")]
    public void ResolveContentType_IsCaseInsensitive(string extension, string expected)
    {
        var result = KafkaOcrConsumerService.ResolveContentType("file" + extension);

        Assert.Equal(expected, result);
    }
}
