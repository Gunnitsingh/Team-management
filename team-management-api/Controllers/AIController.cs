using Microsoft.AspNetCore.Mvc;
using team_management_api.AI.SprintSummary;
using team_management_api.AI.SprintSummary.DTOs;

namespace team_management_api.Controllers;

[ApiController]
[Route("api/ai")]
public class AIController : ControllerBase
{
    private readonly ISprintSummaryService _sprintSummaryService;
    private readonly ILogger<AIController> _logger;

    public AIController(ISprintSummaryService sprintSummaryService, ILogger<AIController> logger)
    {
        _sprintSummaryService = sprintSummaryService ?? throw new ArgumentNullException(nameof(sprintSummaryService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost("sprint-summary")]
    public async Task<IActionResult> GenerateSprintSummary([FromBody] SprintSummaryRequest request, CancellationToken cancellationToken)
    {
        if (request == null)
        {
            _logger.LogWarning("Sprint summary request received with null request body");
            return BadRequest(new { message = "Request body cannot be empty" });
        }

        _logger.LogInformation("Processing sprint summary request with metrics - Completed: {Completed}, InProgress: {InProgress}, Blocked: {Blocked}, Pending: {Pending}",
            request.Completed, request.InProgress, request.Blocked, request.Pending);

        var response = await _sprintSummaryService.GenerateSummaryAsync(request, cancellationToken);

        if (!response.Success)
        {
            _logger.LogWarning("Failed to generate sprint summary: {Message}", response.Message);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, response);
        }

        return Ok(response);
    }
}
