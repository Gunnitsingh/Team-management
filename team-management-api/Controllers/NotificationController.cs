using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Shared.Entities;
using Shared.Models;

[ApiController]
[Route("api/notifications")]
public class NotificationController : ControllerBase
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public NotificationController(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    [HttpPost("push")]
    public async Task<IActionResult> PushNotification([FromBody] Notification notification)
    {
        await _hubContext.Clients.All
            .SendAsync("ReceiveNotification", notification);

        return Ok();
    }

    [HttpPost("tasks/sync")]
    public async Task<IActionResult> SyncTaskProjection([FromBody] TaskProjectionEvent projectionEvent)
    {
        await _hubContext.Clients.All
            .SendAsync("TaskProjectionUpdated", projectionEvent);

        return Ok();
    }
}
