using GestionTicketsAPI.Entities;
using GestionTicketsAPI.Interfaces;
using GestionTicketsAPI.Repositories;
using Microsoft.AspNetCore.SignalR;

namespace GestionTicketsAPI.Services
{
  public class NotificationService : INotificationService
  {
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly INotificationRepository _notificationRepository;

    public NotificationService(IHubContext<NotificationHub> hubContext, INotificationRepository notificationRepository)
    {
      _hubContext = hubContext;
      _notificationRepository = notificationRepository;
    }

    public async Task SendNotificationAsync(Notification notification)
    {
      try
      {
        // Ajoute la notification en base
        await _notificationRepository.AddNotificationAsync(notification);
        // Envoi via SignalR à tous (ou en broadcast, selon votre logique existante)
        await _hubContext.Clients.User(notification.UtilisateurId.ToString()).SendAsync("ReceiveNotification", notification);
      }
      catch (Exception ex)
      {
        Console.WriteLine("Erreur lors de l'ajout de la notification : " + ex.Message);
        throw;
      }
    }

    // Méthode pour envoyer les notifications en attente à l'utilisateur connecté
    public async Task SendPendingNotificationsAsync(int utilisateurId)
    {
        var notifications = await _notificationRepository.GetNotificationsForUserAsync(utilisateurId);
        foreach (var notif in notifications)
        {
            // On envoie directement la notification uniquement à ce client
            await _hubContext.Clients.User(utilisateurId.ToString())
                .SendAsync("ReceiveNotification", notif);
        }
    }
  }
}
