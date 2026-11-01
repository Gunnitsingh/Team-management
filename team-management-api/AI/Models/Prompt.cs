namespace team_management_api.Infrastructure;

public record Prompt
{
    public string SystemPrompt { get; init; } = string.Empty;
    public string UserPrompt { get; init; } = string.Empty;
    public double? Temperature { get; init; }
    public int? MaxTokens { get; init; }
}
