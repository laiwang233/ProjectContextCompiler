using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Pcc.Application.Generation;
using Pcc.Infrastructure.Json;

namespace Pcc.Infrastructure.Generation;

public sealed class LocalCodexCliOptions
{
    public string ExecutablePath { get; set; } = "codex";
    public string WorkingRoot { get; set; } = Path.Combine("storage", "model-runs");
    public int TimeoutSeconds { get; set; } = 300;
}

public sealed class LocalCodexCliProvider(IOptions<LocalCodexCliOptions> options) : IStructuredGenerationProvider
{
    private readonly LocalCodexCliOptions _options = options.Value;

    public async Task<StructuredGenerationResult> GenerateAsync(
        StructuredGenerationRequest request,
        CancellationToken ct)
    {
        var runId = Guid.NewGuid();
        var workingDirectory = Path.GetFullPath(Path.Combine(_options.WorkingRoot, runId.ToString("N")));
        Directory.CreateDirectory(workingDirectory);
        Directory.CreateDirectory(Path.Combine(workingDirectory, "files"));

        await File.WriteAllTextAsync(
            Path.Combine(workingDirectory, "instructions.md"),
            BuildInstructions(request),
            ct);
        await File.WriteAllTextAsync(
            Path.Combine(workingDirectory, "request.json"),
            request.InputJson,
            ct);
        await File.WriteAllTextAsync(
            Path.Combine(workingDirectory, "output.schema.json"),
            request.OutputSchemaJson,
            ct);

        foreach (var file in request.Files)
        {
            if (!File.Exists(file.LocalPath))
            {
                continue;
            }

            var targetPath = Path.Combine(workingDirectory, "files", Path.GetFileName(file.LocalPath));
            File.Copy(file.LocalPath, targetPath, overwrite: true);
        }

        var watch = Stopwatch.StartNew();
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(request.Options.TimeoutSeconds > 0
            ? request.Options.TimeoutSeconds
            : _options.TimeoutSeconds));

        var prompt = "请读取当前目录中的 instructions.md、request.json、output.schema.json。严格按照 instructions.md 执行。最终只把 JSON 写入 output.json。不要输出解释。不要修改 request.json。不要修改 output.schema.json。不要访问当前目录之外的文件。";
        var startInfo = new ProcessStartInfo
        {
            FileName = _options.ExecutablePath,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add(prompt);

        try
        {
            using var process = Process.Start(startInfo)
                ?? throw new InvalidOperationException("Unable to start Codex CLI process.");

            var stdoutTask = process.StandardOutput.ReadToEndAsync(timeoutCts.Token);
            var stderrTask = process.StandardError.ReadToEndAsync(timeoutCts.Token);
            await process.WaitForExitAsync(timeoutCts.Token);

            var stdout = await stdoutTask;
            var stderr = await stderrTask;
            watch.Stop();

            var outputPath = Path.Combine(workingDirectory, "output.json");
            if (!File.Exists(outputPath))
            {
                return Failure("LocalCodexCliProvider", workingDirectory, watch.Elapsed, stdout, $"output.json not found. {stderr}");
            }

            var outputJson = await File.ReadAllTextAsync(outputPath, ct);
            using var _ = JsonDocument.Parse(outputJson);
            return new StructuredGenerationResult
            {
                OutputJson = outputJson,
                ProviderName = "LocalCodexCliProvider",
                ModelName = "codex-cli",
                SchemaValid = true,
                RawOutput = stdout,
                Duration = watch.Elapsed,
                WorkingDirectory = workingDirectory
            };
        }
        catch (OperationCanceledException)
        {
            watch.Stop();
            return Failure("LocalCodexCliProvider", workingDirectory, watch.Elapsed, null, "Codex CLI timed out.");
        }
        catch (Exception ex) when (ex is InvalidOperationException or JsonException)
        {
            watch.Stop();
            return Failure("LocalCodexCliProvider", workingDirectory, watch.Elapsed, null, ex.Message);
        }
    }

    private static StructuredGenerationResult Failure(
        string providerName,
        string workingDirectory,
        TimeSpan duration,
        string? rawOutput,
        string error)
    {
        return new StructuredGenerationResult
        {
            ProviderName = providerName,
            ModelName = "codex-cli",
            SchemaValid = false,
            RawOutput = rawOutput,
            Error = error,
            Duration = duration,
            WorkingDirectory = workingDirectory
        };
    }

    private static string BuildInstructions(StructuredGenerationRequest request)
    {
        return $"""
            # Purpose
            {request.Purpose}

            # System Instruction
            {request.SystemInstruction}

            # User Instruction
            {request.UserInstruction}

            # Output
            Write only valid JSON to output.json.
            """;
    }
}
