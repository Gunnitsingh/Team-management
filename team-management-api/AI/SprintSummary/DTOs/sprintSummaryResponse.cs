using System.Text.Json.Serialization;

namespace team_management_api.AI.SprintSummary.DTOs;

public record SprintSummaryResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("summary")]
    public string Summary { get; init; } = string.Empty;

    [JsonPropertyName("message")]
    public string? Message { get; init; }

    [JsonPropertyName("promptTokens")]
    public int PromptTokens { get; init; }

    [JsonPropertyName("responseTokens")]
    public int ResponseTokens { get; init; }
}

