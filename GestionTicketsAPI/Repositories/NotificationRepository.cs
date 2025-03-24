using GestionTicketsAPI.Data;
using GestionTicketsAPI.Entities;
using GestionTicketsAPI.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GestionTicketsAPI.Repositories
{
  public class NotificationRepository : INotificationRepository
  {
    private readonly DataContext _context;

    public NotificationRepository(DataContext context)
    {
      _context = context;
    }

    public async Task AddNotificationAsync(Notification notification)
    {
      try
      {
        await _context.Notification.AddAsync(notification);
        await _context.SaveChangesAsync();
      }
      catch (Exception ex)
      {
        // Logguer l'erreur
        Console.WriteLine($"Erreur DB: {ex.Message}");
        throw;
      }
    }
    // Récupère les notifications non lues pour l'utilisateur donné
    public async Task<List<Notification>> GetNotificationsForUserAsync(int utilisateurId)
    {
      return await _context.Notification
          .Where(n => n.UtilisateurId == utilisateurId && !n.EstLu)
          .OrderByDescending(n => n.DateEnvoi)
          .ToListAsync();
    }
  }
}
