using System.Text.Json.Serialization;

namespace team_management_api.AI.SprintSummary.DTOs;

public record SprintSummaryRequest
{
    [JsonPropertyName("completed")]
    public int Completed { get; init; }

    [JsonPropertyName("inProgress")]
    public int InProgress { get; init; }

    [JsonPropertyName("blocked")]
    public int Blocked { get; init; }

    [JsonPropertyName("pending")]
    public int Pending { get; init; }
}

