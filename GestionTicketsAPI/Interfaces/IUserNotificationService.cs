using System;
using GestionTicketsAPI.DTOs;

namespace GestionTicketsAPI.Interfaces;

public interface IUserNotificationService
    {
        Task NotifyRealtimeAsync(int userId, NotificationDto dto);
        Task SendPushNotification(int userId, NotificationDto dto);
        Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(int userId);
        Task MarkAllAsReadAsync(int userId);
        Task MarkAsReadAsync(int notificationId);
        Task SoftDeleteAsync(int notificationId);
    }
