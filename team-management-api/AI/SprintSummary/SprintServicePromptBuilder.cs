using team_management_api.Infrastructure;
using team_management_api.AI.SprintSummary.DTOs;

namespace team_management_api.AI.SprintSummary;

public class SprintSummaryPromptBuilder : ISprintSummaryPromptBuilder
{
    public Prompt BuildPrompt(SprintSummaryRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var systemPrompt = "You are a sprint summary expert. Provide a concise, professional summary of sprint progress.";

        var userPrompt = $"""
            Please provide a concise sprint summary based on the following sprint metrics:
            - Completed tasks: {request.Completed}
            - In Progress tasks: {request.InProgress}
            - Blocked tasks: {request.Blocked}
            - Pending tasks: {request.Pending}

            The summary should:
            1. Highlight overall progress
            2. Mention any concerning blocked items
            3. Provide a brief outlook for the next sprint
            4. Keep the summary to 3-4 sentences
            """;

        return new Prompt
        {
            SystemPrompt = systemPrompt,
            UserPrompt = userPrompt,
            Temperature = 0.7,
            MaxTokens = 300
        };
    }
}
