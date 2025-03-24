namespace GestionTicketsAPI.Interfaces
{
    public interface INotificationService
    {
        Task SendNotificationAsync(Entities.Notification notification);
        Task SendPendingNotificationsAsync(int utilisateurId);
    }
}
