using Microsoft.Extensions.Logging;
using team_management_api.Interface;
using team_management_api.AI.SprintSummary.DTOs;

namespace team_management_api.AI.SprintSummary;

public class SprintSummaryService : ISprintSummaryService
{
    private readonly ILLMClient _llmClient;
    private readonly ISprintSummaryPromptBuilder _promptBuilder;
    private readonly ILogger<SprintSummaryService> _logger;

    public SprintSummaryService(
        ILLMClient llmClient,
        ISprintSummaryPromptBuilder promptBuilder,
        ILogger<SprintSummaryService> logger)
    {
        _llmClient = llmClient ?? throw new ArgumentNullException(nameof(llmClient));
        _promptBuilder = promptBuilder ?? throw new ArgumentNullException(nameof(promptBuilder));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<SprintSummaryResponse> GenerateSummaryAsync(SprintSummaryRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            _logger.LogInformation("Generating sprint summary with metrics - Completed: {Completed}, InProgress: {InProgress}, Blocked: {Blocked}, Pending: {Pending}",
                request.Completed, request.InProgress, request.Blocked, request.Pending);

            var prompt = _promptBuilder.BuildPrompt(request);

            var response = await _llmClient.GenerateAsync(prompt, cancellationToken);

            if (!response.Success)
            {
                _logger.LogWarning("Failed to generate sprint summary: {Message}", response.Message);
                return new SprintSummaryResponse
                {
                    Success = false,
                    Message = response.Message
                };
            }

            _logger.LogInformation("Successfully generated sprint summary");

            return new SprintSummaryResponse
            {
                Success = true,
                Summary = response.Content,
                PromptTokens = response.PromptTokens,
                ResponseTokens = response.ResponseTokens
            };
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Sprint summary generation was cancelled");
            return new SprintSummaryResponse
            {
                Success = false,
                Message = "Operation was cancelled"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while generating sprint summary");
            return new SprintSummaryResponse
            {
                Success = false,
                Message = $"Unexpected error: {ex.Message}"
            };
        }
    }
}
