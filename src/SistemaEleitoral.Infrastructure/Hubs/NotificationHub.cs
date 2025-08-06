using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace SistemaEleitoral.Infrastructure.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(ILogger<NotificationHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        if (userId.HasValue)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
            _logger.LogInformation("User {UserId} connected to notification hub with connection {ConnectionId}", 
                userId, Context.ConnectionId);
        }
        
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        if (userId.HasValue)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"User_{userId}");
            _logger.LogInformation("User {UserId} disconnected from notification hub with connection {ConnectionId}", 
                userId, Context.ConnectionId);
        }
        
        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Connection {ConnectionId} joined group {GroupName}", Context.ConnectionId, groupName);
    }

    public async Task LeaveGroup(string groupName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Connection {ConnectionId} left group {GroupName}", Context.ConnectionId, groupName);
    }

    public async Task MarkNotificationAsRead(int notificationId)
    {
        var userId = GetUserId();
        if (userId.HasValue)
        {
            // Implementar lógica para marcar notificação como lida
            await Clients.User(userId.ToString()).SendAsync("NotificationMarkedAsRead", notificationId);
        }
    }

    private int? GetUserId()
    {
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}

public static class NotificationHubExtensions
{
    // Métodos de extensão para facilitar o uso do hub
    public static async Task SendNotificationToUserAsync(this IHubContext<NotificationHub> hubContext, 
        int userId, object notification)
    {
        await hubContext.Clients.Group($"User_{userId}").SendAsync("ReceiveNotification", notification);
    }

    public static async Task SendNotificationToGroupAsync(this IHubContext<NotificationHub> hubContext, 
        string groupName, object notification)
    {
        await hubContext.Clients.Group(groupName).SendAsync("ReceiveNotification", notification);
    }

    public static async Task SendNotificationToAllAsync(this IHubContext<NotificationHub> hubContext, 
        object notification)
    {
        await hubContext.Clients.All.SendAsync("ReceiveNotification", notification);
    }

    public static async Task SendSystemAlertAsync(this IHubContext<NotificationHub> hubContext, 
        string message, string type = "info")
    {
        await hubContext.Clients.All.SendAsync("SystemAlert", new { message, type, timestamp = DateTime.Now });
    }
}