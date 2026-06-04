namespace Pcc.Infrastructure.Generation;

public static class GenerationSchemas
{
    public const string ContentBlocks = """
        {
          "type": "object",
          "required": ["contentBlocks"],
          "properties": {
            "contentBlocks": {
              "type": "array",
              "items": {
                "type": "object",
                "required": ["type", "text", "orderIndex", "confidence"],
                "properties": {
                  "type": { "type": "string" },
                  "text": { "type": "string" },
                  "locationLabel": { "type": ["string", "null"] },
                  "orderIndex": { "type": "integer" },
                  "confidence": { "type": "number" },
                  "metadata": { "type": "object" }
                }
              }
            }
          }
        }
        """;

    public const string Claims = """
        {
          "type": "object",
          "required": ["claims"],
          "properties": {
            "claims": {
              "type": "array",
              "items": {
                "type": "object",
                "required": ["type", "subject", "text", "confidence", "contentBlockIds"],
                "properties": {
                  "type": { "type": "string" },
                  "subject": { "type": "string" },
                  "text": { "type": "string" },
                  "confidence": { "type": "number" },
                  "ambiguityScore": { "type": "number" },
                  "freshnessScore": { "type": "number" },
                  "sourceAuthorityScore": { "type": "number" },
                  "contentBlockIds": { "type": "array", "items": { "type": "string" } }
                }
              }
            }
          }
        }
        """;

    public const string Requirements = """
        {
          "type": "object",
          "required": ["requirements"],
          "properties": {
            "requirements": {
              "type": "array",
              "items": {
                "type": "object",
                "required": ["title", "summary", "requirementType", "confidence", "content"],
                "properties": {
                  "title": { "type": "string" },
                  "summary": { "type": "string" },
                  "module": { "type": ["string", "null"] },
                  "requirementType": { "type": "string" },
                  "confidence": { "type": "number" },
                  "content": { "type": "object" },
                  "conflicts": { "type": "array" }
                }
              }
            }
          }
        }
        """;

    public const string Tasks = """
        {
          "type": "object",
          "required": ["tasks"],
          "properties": {
            "tasks": {
              "type": "array",
              "items": {
                "type": "object",
                "required": ["title", "description", "taskType", "readinessScore", "agentPrompt"],
                "properties": {
                  "title": { "type": "string" },
                  "description": { "type": "string" },
                  "taskType": { "type": "string" },
                  "readinessScore": { "type": "number" },
                  "agentPrompt": { "type": "string" },
                  "acceptanceCriteria": { "type": "array", "items": { "type": "string" } },
                  "missingContext": { "type": "array", "items": { "type": "string" } },
                  "assumptions": { "type": "array", "items": { "type": "string" } },
                  "dependsOnTitles": { "type": "array", "items": { "type": "string" } }
                }
              }
            }
          }
        }
        """;
}
