using FileManager.Application.Services;
using System.Security.Claims;

namespace FileManager.Web.Middleware;

public class UserActivityMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<UserActivityMiddleware> _logger;

    public UserActivityMiddleware(RequestDelegate next, ILogger<UserActivityMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, UserService userService)
    {
        await _next(context);

        // Обновляем активность только для авторизованных пользователей
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(userIdClaim, out var userId))
            {
                try
                {
                    var ipAddress = context.Connection.RemoteIpAddress?.ToString();
                    await userService.UpdateLastActivityAsync(userId, ipAddress);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка обновления активности пользователя {UserId}", userId);
                }
            }
        }
    }
}

public static class UserActivityMiddlewareExtensions
{
    public static IApplicationBuilder UseUserActivity(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<UserActivityMiddleware>();
    }
}