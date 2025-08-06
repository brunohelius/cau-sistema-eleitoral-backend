using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using SistemaEleitoral.Domain.Models;
using SistemaEleitoral.Domain.Services;
using SistemaEleitoral.Infrastructure.Hubs;
using System.Text.Json;

namespace SistemaEleitoral.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly INotificationRepository _repository;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IHubContext<NotificationHub> hubContext,
        INotificationRepository repository,
        ILogger<NotificationService> logger)
    {
        _hubContext = hubContext;
        _repository = repository;
        _logger = logger;
    }

    public async Task SendToUserAsync(int userId, string message, NotificationType type = NotificationType.Info)
    {
        try
        {
            var notification = new
            {
                UserId = userId,
                Message = message,
                Type = type.ToString(),
                Timestamp = DateTime.Now
            };

            await _hubContext.SendNotificationToUserAsync(userId, notification);
            _logger.LogInformation("Notificação enviada para usuário {UserId}: {Message}", userId, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar notificação para usuário {UserId}", userId);
        }
    }

    public async Task SendToUsersAsync(List<int> userIds, string message, NotificationType type = NotificationType.Info)
    {
        var tasks = userIds.Select(userId => SendToUserAsync(userId, message, type));
        await Task.WhenAll(tasks);
    }

    public async Task SendToGroupAsync(string groupName, string message, NotificationType type = NotificationType.Info)
    {
        try
        {
            var notification = new
            {
                GroupName = groupName,
                Message = message,
                Type = type.ToString(),
                Timestamp = DateTime.Now
            };

            await _hubContext.SendNotificationToGroupAsync(groupName, notification);
            _logger.LogInformation("Notificação enviada para grupo {GroupName}: {Message}", groupName, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar notificação para grupo {GroupName}", groupName);
        }
    }

    public async Task SendToAllAsync(string message, NotificationType type = NotificationType.Info)
    {
        try
        {
            var notification = new
            {
                Message = message,
                Type = type.ToString(),
                Timestamp = DateTime.Now,
                IsGlobal = true
            };

            await _hubContext.SendNotificationToAllAsync(notification);
            _logger.LogInformation("Notificação global enviada: {Message}", message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar notificação global");
        }
    }

    public async Task AddUserToGroupAsync(int userId, string groupName)
    {
        // Esta funcionalidade seria implementada quando o usuário se conecta
        // Por enquanto, apenas log
        _logger.LogInformation("Usuário {UserId} adicionado ao grupo {GroupName}", userId, groupName);
    }

    public async Task RemoveUserFromGroupAsync(int userId, string groupName)
    {
        // Esta funcionalidade seria implementada quando o usuário se desconecta
        // Por enquanto, apenas log
        _logger.LogInformation("Usuário {UserId} removido do grupo {GroupName}", userId, groupName);
    }

    public async Task<NotificationMessage> CreateNotificationAsync(NotificationMessage notification)
    {
        try
        {
            notification.CreatedAt = DateTime.Now;
            
            // Salvar no banco
            var saved = await _repository.CreateAsync(notification);
            
            // Enviar por SignalR
            await SendToUserAsync(notification.UserId, notification.Message, notification.Type);
            
            return saved;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar notificação para usuário {UserId}", notification.UserId);
            throw;
        }
    }

    public async Task<List<NotificationMessage>> GetUserNotificationsAsync(int userId, bool unreadOnly = false)
    {
        try
        {
            return await _repository.GetUserNotificationsAsync(userId, unreadOnly);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar notificações do usuário {UserId}", userId);
            return new List<NotificationMessage>();
        }
    }

    public async Task<bool> MarkAsReadAsync(int notificationId, int userId)
    {
        try
        {
            var result = await _repository.MarkAsReadAsync(notificationId, userId);
            
            if (result)
            {
                await _hubContext.Clients.Group($"User_{userId}")
                    .SendAsync("NotificationMarkedAsRead", notificationId);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao marcar notificação {NotificationId} como lida", notificationId);
            return false;
        }
    }

    public async Task<bool> MarkAllAsReadAsync(int userId)
    {
        try
        {
            var result = await _repository.MarkAllAsReadAsync(userId);
            
            if (result)
            {
                await _hubContext.Clients.Group($"User_{userId}")
                    .SendAsync("AllNotificationsMarkedAsRead");
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao marcar todas as notificações como lidas para usuário {UserId}", userId);
            return false;
        }
    }

    public async Task<int> GetUnreadCountAsync(int userId)
    {
        try
        {
            return await _repository.GetUnreadCountAsync(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar quantidade de notificações não lidas do usuário {UserId}", userId);
            return 0;
        }
    }

    // Notificações específicas do sistema eleitoral
    public async Task NotifyTicketStatusChangeAsync(int ticketId, string oldStatus, string newStatus)
    {
        try
        {
            var notification = new NotificationMessage
            {
                Title = "Status da Chapa Alterado",
                Message = $"Chapa #{ticketId}: {oldStatus} → {newStatus}",
                Type = NotificationType.Info,
                ReferenceId = ticketId.ToString(),
                ReferenceType = "CHAPA",
                ActionUrl = $"/chapa/{ticketId}",
                ActionText = "Ver Chapa",
                Icon = "status-change"
            };

            // Buscar interessados na chapa e notificar
            var interestedUsers = await GetTicketInterestedUsersAsync(ticketId);
            
            foreach (var userId in interestedUsers)
            {
                notification.UserId = userId;
                await CreateNotificationAsync(notification);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao notificar mudança de status da chapa {TicketId}", ticketId);
        }
    }

    public async Task NotifyJudgmentCreatedAsync(int judgmentId)
    {
        try
        {
            var notification = new NotificationMessage
            {
                Title = "Novo Julgamento",
                Message = $"Um novo julgamento foi registrado",
                Type = NotificationType.Info,
                ReferenceId = judgmentId.ToString(),
                ReferenceType = "JULGAMENTO",
                ActionUrl = $"/julgamento/{judgmentId}",
                ActionText = "Ver Julgamento",
                Icon = "gavel"
            };

            // Notificar usuários relevantes
            var interestedUsers = await GetJudgmentInterestedUsersAsync(judgmentId);
            
            foreach (var userId in interestedUsers)
            {
                notification.UserId = userId;
                await CreateNotificationAsync(notification);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao notificar criação de julgamento {JudgmentId}", judgmentId);
        }
    }

    public async Task NotifyDeadlineAlertAsync(int calendarId, DateTime deadline)
    {
        try
        {
            var daysUntilDeadline = (deadline - DateTime.Now).Days;
            var urgency = daysUntilDeadline <= 1 ? NotificationType.Error :
                         daysUntilDeadline <= 3 ? NotificationType.Warning :
                         NotificationType.Info;

            var notification = new NotificationMessage
            {
                Title = "Prazo se aproximando",
                Message = $"Prazo em {daysUntilDeadline} dias: {deadline:dd/MM/yyyy}",
                Type = urgency,
                ReferenceId = calendarId.ToString(),
                ReferenceType = "CALENDARIO",
                ActionUrl = $"/calendario/{calendarId}",
                ActionText = "Ver Prazo",
                Icon = "clock"
            };

            // Notificar usuários com prazos pendentes
            var usersWithDeadlines = await GetCalendarInterestedUsersAsync(calendarId);
            
            foreach (var userId in usersWithDeadlines)
            {
                notification.UserId = userId;
                await CreateNotificationAsync(notification);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao notificar prazo do calendário {CalendarId}", calendarId);
        }
    }

    public async Task NotifyPendencyAlertAsync(int memberId, int ticketId, List<string> pendencies)
    {
        try
        {
            var notification = new NotificationMessage
            {
                Title = "Pendências na Chapa",
                Message = $"{pendencies.Count} pendência(s) encontrada(s) na sua participação",
                Type = NotificationType.Warning,
                ReferenceId = $"{ticketId}_{memberId}",
                ReferenceType = "PENDENCIA",
                ActionUrl = $"/chapa/{ticketId}/pendencias",
                ActionText = "Ver Pendências",
                Icon = "alert-triangle",
                Data = JsonSerializer.Serialize(new { Pendencies = pendencies })
            };

            notification.UserId = memberId;
            await CreateNotificationAsync(notification);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao notificar pendências do membro {MemberId} na chapa {TicketId}", 
                memberId, ticketId);
        }
    }

    // Métodos auxiliares para buscar usuários interessados
    private async Task<List<int>> GetTicketInterestedUsersAsync(int ticketId)
    {
        // Implementar lógica para buscar usuários interessados na chapa
        // Por enquanto, retorna lista vazia
        return new List<int>();
    }

    private async Task<List<int>> GetJudgmentInterestedUsersAsync(int judgmentId)
    {
        // Implementar lógica para buscar usuários interessados no julgamento
        // Por enquanto, retorna lista vazia
        return new List<int>();
    }

    private async Task<List<int>> GetCalendarInterestedUsersAsync(int calendarId)
    {
        // Implementar lógica para buscar usuários com prazos no calendário
        // Por enquanto, retorna lista vazia
        return new List<int>();
    }
    
    // Métodos adicionais para implementar a interface INotificationService
    public async Task NotificarNovaChapa(int chapaId)
    {
        await SendToAllAsync($"Nova chapa registrada: #{chapaId}", NotificationType.Info);
    }

    public async Task NotificarNovaDenuncia(int denunciaId)
    {
        await SendToAllAsync($"Nova denúncia registrada: #{denunciaId}", NotificationType.Warning);
    }

    public async Task NotificarResultadoApuracao(int resultadoId)
    {
        await SendToAllAsync($"Resultado de apuração disponível: #{resultadoId}", NotificationType.Success);
    }

    public async Task NotificarSolicitacaoRecontagem(int solicitacaoId)
    {
        await SendToAllAsync($"Solicitação de recontagem: #{solicitacaoId}", NotificationType.Info);
    }

    public async Task NotificarResultadoRecontagem(int recontagemId)
    {
        await SendToAllAsync($"Resultado de recontagem disponível: #{recontagemId}", NotificationType.Success);
    }

    public async Task NotificarHomologacaoResultado(int resultadoId)
    {
        await SendToAllAsync($"Resultado homologado: #{resultadoId}", NotificationType.Success);
    }

    public async Task NotificarImpugnacaoResultado(int impugnacaoId)
    {
        await SendToAllAsync($"Impugnação de resultado: #{impugnacaoId}", NotificationType.Warning);
    }

    public async Task DivulgarResultadoOficial(int resultadoId)
    {
        await SendToAllAsync($"Resultado oficial divulgado: #{resultadoId}", NotificationType.Success);
    }
}