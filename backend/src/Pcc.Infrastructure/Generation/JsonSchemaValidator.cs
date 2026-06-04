using System.Text.Json;
using NJsonSchema;

namespace Pcc.Infrastructure.Generation;

public interface IJsonSchemaValidator
{
    Task<SchemaValidationResult> ValidateAsync(string json, string schemaJson, CancellationToken ct);
}

public sealed record SchemaValidationResult(bool Valid, string? Error);

public sealed class JsonSchemaValidator : IJsonSchemaValidator
{
    public async Task<SchemaValidationResult> ValidateAsync(string json, string schemaJson, CancellationToken ct)
    {
        try
        {
            using var _ = JsonDocument.Parse(json);
            if (string.IsNullOrWhiteSpace(schemaJson))
            {
                return new SchemaValidationResult(true, null);
            }

            var schema = await JsonSchema.FromJsonAsync(schemaJson, ct);
            var errors = schema.Validate(json);
            return errors.Count == 0
                ? new SchemaValidationResult(true, null)
                : new SchemaValidationResult(false, string.Join("; ", errors.Select(error => error.ToString())));
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException)
        {
            return new SchemaValidationResult(false, ex.Message);
        }
    }
}
