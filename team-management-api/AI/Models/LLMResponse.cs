namespace team_management_api.Infrastructure;

public record LLMResponse
{
    public bool Success { get; init; }
    public string Content { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public int PromptTokens { get; init; }
    public int ResponseTokens { get; init; }
}
