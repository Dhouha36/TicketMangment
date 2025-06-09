using System;
using GestionTicketsAPI.DTOs;

namespace GestionTicketsAPI.Interfaces;

public interface IClientNotificationService
{
    Task NotifyClientAsync(int clientId, NotificationDto dto);
    Task<IEnumerable<NotificationDto>> GetClientNotificationsAsync(int clientId);
    Task MarkAllAsReadClientAsync(int clientId);
    Task MarkAsReadClientAsync(int notificationId);
    Task SoftDeleteClientNotificationAsync(int notificationId);
}
