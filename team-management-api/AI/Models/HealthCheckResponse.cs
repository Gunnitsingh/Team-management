namespace team_management_api.Infrastructure;

public class HealthCheckResponse
{
    public string Status { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string? Message { get; set; }
}