using SistemaEleitoral.Domain.Models;
using SistemaEleitoral.Domain.Entities;

namespace SistemaEleitoral.Domain.Services;

public interface INotificationService
{
    // Notificações em tempo real
    Task SendToUserAsync(int userId, string message, NotificationType type = NotificationType.Info);
    Task SendToUsersAsync(List<int> userIds, string message, NotificationType type = NotificationType.Info);
    Task SendToGroupAsync(string groupName, string message, NotificationType type = NotificationType.Info);
    Task SendToAllAsync(string message, NotificationType type = NotificationType.Info);
    
    // Gerenciamento de grupos
    Task AddUserToGroupAsync(int userId, string groupName);
    Task RemoveUserFromGroupAsync(int userId, string groupName);
    
    // Notificações persistentes
    Task<NotificationMessage> CreateNotificationAsync(NotificationMessage notification);
    Task<List<NotificationMessage>> GetUserNotificationsAsync(int userId, bool unreadOnly = false);
    Task<bool> MarkAsReadAsync(int notificationId, int userId);
    Task<bool> MarkAllAsReadAsync(int userId);
    Task<int> GetUnreadCountAsync(int userId);
    
    // Notificações do sistema eleitoral
    Task NotifyTicketStatusChangeAsync(int ticketId, string oldStatus, string newStatus);
    Task NotifyJudgmentCreatedAsync(int judgmentId);
    Task NotifyDeadlineAlertAsync(int calendarId, DateTime deadline);
    Task NotifyPendencyAlertAsync(int memberId, int ticketId, List<string> pendencies);
}

public enum NotificationType
{
    Info,
    Success,
    Warning,
    Error,
    System
}

public class NotificationMessage : BaseEntity
{
    public int UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; } = NotificationType.Info;
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }
    public string? ActionUrl { get; set; }
    public string? ActionText { get; set; }
    public string? ReferenceId { get; set; }
    public string? ReferenceType { get; set; }
    public DateTime ExpiresAt { get; set; } = DateTime.Now.AddDays(30);
    public string? Icon { get; set; }
    public string? Data { get; set; } // JSON adicional
}