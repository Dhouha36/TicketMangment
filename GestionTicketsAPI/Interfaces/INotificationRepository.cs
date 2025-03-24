using GestionTicketsAPI.Entities;
namespace GestionTicketsAPI.Repositories
{
    public interface INotificationRepository
    {
        Task AddNotificationAsync(Notification notification);
        Task<List<Notification>> GetNotificationsForUserAsync(int utilisateurId);
    }
}
