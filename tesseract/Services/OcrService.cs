using System.Diagnostics;
using tesseract.Metrics;

namespace tesseract.Services;

public interface IOcrService
{
    Task<string> RecognizeAsync(Stream stream, string? contentType, string language, CancellationToken ct = default);
}

public sealed class OcrService : IOcrService
{
    private static readonly Dictionary<string, string> ExtensionByContentType = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/jpg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/tiff"] = ".tiff",
        ["image/bmp"] = ".bmp",
        ["image/webp"] = ".webp",
        ["image/gif"] = ".gif",
        ["application/pdf"] = ".pdf",
    };
    private readonly TesseractMetrics tesseractMetrics;
    private readonly ILogger<OcrService> logger;

    public OcrService(TesseractMetrics tesseractMetrics, ILogger<OcrService> logger)
    {
        this.tesseractMetrics = tesseractMetrics;
        this.logger = logger;
    }

    public async Task<string> RecognizeAsync(Stream stream, string? contentType, string language, CancellationToken ct = default)
    {
        logger.LogInformation("Starting OCR recognition. Type: {ContentType}. Size: {FileSize}", contentType, stream.Length);

        var sw = Stopwatch.StartNew();

        var extension = ResolveExtension(contentType);

        var tempDir = Path.Combine(Path.GetTempPath(), "tesseract-ocr");
        Directory.CreateDirectory(tempDir);

        var inputPath = Path.Combine(tempDir, $"{Guid.NewGuid():N}{extension}");

        try
        {
            await using (var fs = new FileStream(inputPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true))
            {
                await stream.CopyToAsync(fs, ct);
            }

            return string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase)
                ? await RecognizePdfAsync(inputPath, language, tempDir, ct)
                : await RunTesseractAsync(inputPath, language, ct);
        }
        finally
        {
            sw.Stop();
            tesseractMetrics.RecordFileThroughput(stream.Length, sw.ElapsedMilliseconds);
            File.Delete(inputPath);
            logger.LogInformation("Finished OCR recognition. Type: {ContentType}. Size: {FileSize}. Time: {ElapsedMilliseconds} ms", contentType, stream.Length, sw.ElapsedMilliseconds);
        }
    }

    private async Task<string> RecognizePdfAsync(string inputPath, string language, string tempDir, CancellationToken ct)
    {
        var prefix = Path.Combine(tempDir, Guid.NewGuid().ToString("N"));
        var pages = new List<string>();

        try
        {
            await RunProcessAsync("pdftoppm", new[] { "-png", "-r", "300", inputPath, prefix }, ct);

            var pageFiles = Directory.GetFiles(tempDir, Path.GetFileName(prefix) + "*.png")
                .OrderBy(ExtractPageNumber)
                .ToArray();

            foreach (var page in pageFiles)
            {
                var text = await RunTesseractAsync(page, language, ct);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    pages.Add(text.Trim());
                }
            }

            return string.Join(Environment.NewLine + Environment.NewLine, pages);
        }
        finally
        {
            foreach (var file in Directory.GetFiles(tempDir, Path.GetFileName(prefix) + "*.png"))
            {
                File.Delete(file);
            }
        }
    }

    private static async Task<string> RunTesseractAsync(string inputPath, string language, CancellationToken ct)
    {
        var (exitCode, stdout, stderr) = await RunProcessAsync("tesseract", new[] { inputPath, "stdout", "-l", language }, ct);

        if (exitCode != 0)
        {
            throw new InvalidOperationException($"tesseract exited with code {exitCode}: {stderr}");
        }

        return stdout;
    }

    private static string ResolveExtension(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return ".bin";
        }

        var mediaType = contentType.Split(';')[0].Trim();
        return ExtensionByContentType.TryGetValue(mediaType, out var extension) ? extension : ".bin";
    }

    private static int ExtractPageNumber(string path)
    {
        var name = Path.GetFileNameWithoutExtension(path);
        var index = name.Length;
        while (index > 0 && char.IsDigit(name[index - 1]))
        {
            index--;
        }

        return int.TryParse(name.AsSpan(index), out var number) ? number : 0;
    }

    private static async Task<(int ExitCode, string Stdout, string Stderr)> RunProcessAsync(
        string fileName, IReadOnlyList<string> arguments, CancellationToken ct)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        try
        {
            await process.WaitForExitAsync(ct);
        }
        catch
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }

            throw;
        }

        return (process.ExitCode, await stdoutTask, await stderrTask);
    }
}
