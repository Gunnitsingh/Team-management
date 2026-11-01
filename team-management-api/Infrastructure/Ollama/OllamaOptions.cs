namespace team_management_api.Options;

public class OllamaOptions
{
    public const string SectionName = "Ollama";

    public string BaseUrl { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
    public int HealthCheckTimeoutSeconds { get; set; } = 5;
}
