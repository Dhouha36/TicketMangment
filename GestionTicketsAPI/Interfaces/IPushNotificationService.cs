// IPushNotificationService.cs
using GestionTicketsAPI.Controllers;

public interface IPushNotificationService
{
    Task SendNotificationToAllAsync(NotificationPayload payload);
}
