public class TaskMiddleware
{
    private readonly RequestDelegate _next;

    public TaskMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault()
                            ?? Guid.NewGuid().ToString();
        var userId = context.Request.Headers["X-User-Id"].FirstOrDefault() ?? "0";
        var userName = context.Request.Headers["X-User-Name"].FirstOrDefault() ?? "Anonymous";

        context.Items["UserId"] = userId;
        context.Items["UserName"] = userName;

        context.Items["CorrelationId"] = correlationId;

        context.Response.Headers.Add("X-Correlation-Id", correlationId);

        await _next(context);
    }


}

