using System.Diagnostics.Metrics;

namespace tesseract.Metrics;

public class TesseractMetrics
{
    public const string MeterName = "TesseractMetrics";

    private readonly Counter<long> _fileSizeCounter;
    private readonly Counter<long> _fileTimeCounter;

    public TesseractMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        _fileSizeCounter = meter.CreateCounter<long>("FileSizeB");
        _fileTimeCounter = meter.CreateCounter<long>("FileTimeMS");
    }

    public void RecordFileThroughput(long fileSizeB, long fileProcessingTimeMS)
    {
        _fileSizeCounter.Add(fileSizeB);
        _fileTimeCounter.Add(fileProcessingTimeMS);
    }
}
