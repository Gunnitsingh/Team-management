using Microsoft.EntityFrameworkCore;
using Shared.Entities;
using System.Text.Json;

public class OutboxDispatcher : BackgroundService
{
    private const int BatchSize = 20;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromHours(1);
    private static readonly TimeSpan ProcessedMessageRetention = TimeSpan.FromDays(7);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxDispatcher> _logger;
    private DateTime _nextCleanupUtc = DateTime.UtcNow;

    public OutboxDispatcher(IServiceScopeFactory scopeFactory, ILogger<OutboxDispatcher> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DispatchPendingMessages(stoppingToken);
                await CleanupProcessedMessages(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbox dispatcher failed while polling pending messages.");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task DispatchPendingMessages(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IMessagePublisher>();

        var messages = await context.OutboxMessages
            .Where(o => o.ProcessedOnUtc == null)
            .OrderBy(o => o.OccurredOnUtc)
            .Take(BatchSize)
            .ToListAsync(stoppingToken);

        foreach (var message in messages)
        {
            try
            {
                var taskEvent = JsonSerializer.Deserialize<TaskEvent>(message.Payload);

                if (taskEvent == null)
                {
                    throw new InvalidOperationException($"Outbox message {message.Id} payload could not be deserialized.");
                }

                publisher.Publish(taskEvent);

                message.ProcessedOnUtc = DateTime.UtcNow;
                message.Error = null;
            }
            catch (Exception ex)
            {
                message.RetryCount += 1;
                message.Error = ex.Message;
                _logger.LogError(ex, "Failed to dispatch outbox message {OutboxMessageId}.", message.Id);
            }
        }

        if (messages.Count > 0)
        {
            await context.SaveChangesAsync(stoppingToken);
        }
    }

    private async Task CleanupProcessedMessages(CancellationToken stoppingToken)
    {
        if (DateTime.UtcNow < _nextCleanupUtc)
        {
            return;
        }

        _nextCleanupUtc = DateTime.UtcNow.Add(CleanupInterval);

        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var cutoffUtc = DateTime.UtcNow.Subtract(ProcessedMessageRetention);

        var deletedCount = await context.OutboxMessages
            .Where(o => o.ProcessedOnUtc != null && o.ProcessedOnUtc < cutoffUtc)
            .ExecuteDeleteAsync(stoppingToken);

        if (deletedCount > 0)
        {
            _logger.LogInformation("Deleted {OutboxMessageCount} processed outbox messages.", deletedCount);
        }
    }
}
