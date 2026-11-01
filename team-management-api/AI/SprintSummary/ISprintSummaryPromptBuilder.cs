using team_management_api.Infrastructure;
using team_management_api.AI.SprintSummary.DTOs;

namespace team_management_api.AI.SprintSummary;

public interface ISprintSummaryPromptBuilder
{
    Prompt BuildPrompt(SprintSummaryRequest request);
}
