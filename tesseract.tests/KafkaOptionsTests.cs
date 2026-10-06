using tesseract.Services;

namespace tesseract.tests;

public sealed class KafkaOptionsTests
{
    [Fact]
    public void Defaults_BootstrapServers_IsLocalhost9092()
    {
        var options = new KafkaOptions();

        Assert.Equal("localhost:9092", options.BootstrapServers);
    }

    [Fact]
    public void Defaults_RequestTopic_IsOcrRequests()
    {
        var options = new KafkaOptions();

        Assert.Equal("ocr-requests", options.RequestTopic);
    }

    [Fact]
    public void Defaults_ResultTopic_IsOcrResults()
    {
        var options = new KafkaOptions();

        Assert.Equal("ocr-results", options.ResultTopic);
    }

    [Fact]
    public void Defaults_GroupId_IsTesseract()
    {
        var options = new KafkaOptions();

        Assert.Equal("tesseract", options.GroupId);
    }
}
