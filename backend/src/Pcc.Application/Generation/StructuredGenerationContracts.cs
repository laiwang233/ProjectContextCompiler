namespace Pcc.Application.Generation;

public interface IStructuredGenerationProvider
{
    Task<StructuredGenerationResult> GenerateAsync(
        StructuredGenerationRequest request,
        CancellationToken ct);
}

public sealed class StructuredGenerationRequest
{
    public string Purpose { get; init; } = "";
    public string SystemInstruction { get; init; } = "";
    public string UserInstruction { get; init; } = "";
    public string InputJson { get; init; } = "";
    public string OutputSchemaJson { get; init; } = "";
    public IReadOnlyList<ModelInputFile> Files { get; init; } = [];
    public GenerationOptions Options { get; init; } = new();
}

public sealed class StructuredGenerationResult
{
    public string OutputJson { get; init; } = "";
    public string ProviderName { get; init; } = "";
    public string ModelName { get; init; } = "";
    public bool SchemaValid { get; init; }
    public string? RawOutput { get; init; }
    public string? Error { get; init; }
    public TimeSpan Duration { get; init; }
    public string? WorkingDirectory { get; init; }
}

public sealed class ModelInputFile
{
    public string FileName { get; init; } = "";
    public string MimeType { get; init; } = "";
    public string LocalPath { get; init; } = "";
    public string? TextFallback { get; init; }
}

public sealed class GenerationOptions
{
    public decimal Temperature { get; init; } = 0;
    public int TimeoutSeconds { get; init; } = 300;
    public bool RequireValidJson { get; init; } = true;
    public bool AllowFileAccess { get; init; }
}
