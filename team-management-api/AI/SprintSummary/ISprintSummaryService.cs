using team_management_api.AI.SprintSummary.DTOs;

namespace team_management_api.AI.SprintSummary;

public interface ISprintSummaryService
{
    Task<SprintSummaryResponse> GenerateSummaryAsync(SprintSummaryRequest request, CancellationToken cancellationToken = default);
}
