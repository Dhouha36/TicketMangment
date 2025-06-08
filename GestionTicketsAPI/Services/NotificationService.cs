using GestionTicketsAPI.Data;
using GestionTicketsAPI.Entities;
using Microsoft.AspNetCore.SignalR;
using Lib.Net.Http.WebPush;
using Microsoft.EntityFrameworkCore;
using GestionTicketsAPI.DTOs;
using GestionTicketsAPI.hubs;
using System.Text.Json;

namespace GestionTicketsAPI.Services
{
  public interface INotificationService
  {
    Task NotifyRealtimeAsync(int userId, NotificationDto notification);
    Task NotifyAsync(int? userId, int? clientId, NotificationDto notification);
    Task NotifyPushAsync(int userId, NotificationDto notification);
    Task MarkAllAsReadAsync(int userId);
    Task MarkAsReadAsync(int notificationId);
    Task SoftDeleteAsync(int notificationId);
  }

  public class NotificationService : INotificationService
  {
    private readonly IHubContext<NotificationHub> _hub;
    private readonly DataContext _context;
    private readonly PushServiceClient _pushClient;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IHubContext<NotificationHub> hub,
        DataContext context,
        PushServiceClient pushClient,
        ILogger<NotificationService> logger)
    {
      _hub = hub;
      _context = context;
      _pushClient = pushClient;
      _logger = logger;
    }

    public async Task NotifyAsync(int? userId, int? clientId, NotificationDto dto)
    {
      // 1) Persister d'abord
      var notif = new Notification
      {
        UserId = userId,
        ClientId = clientId,
        Message = dto.Message,
        DateEnvoi = dto.DateEnvoi,
        EntityType = dto.EntityType,
        EntityId = dto.EntityId,
        IsRead = false // Explicitement à false
      };

      _context.Notification.Add(notif);
      await _context.SaveChangesAsync();

      // 2) Mettre à jour le DTO avec l'ID généré
      dto.Id = notif.Id;
      dto.IsRead = false; 
      dto.UserId = userId;

      // 3) Envoyer via SignalR SEULEMENT si on a un userId
      if (userId.HasValue)
      {
        try
        {
          var groupName = userId.Value.ToString();
          _logger.LogInformation($"Envoi notification à {groupName}: {dto.Message}");

          await _hub.Clients.Group(groupName).SendAsync("ReceiveNotification", dto);

          _logger.LogInformation($"Notification envoyée avec succès à {groupName}");
        }
        catch (Exception ex)
        {
          _logger.LogError(ex, $"Erreur lors de l'envoi SignalR pour userId {userId}");
        }

        // 4) Envoi push (inchangé)
        await SendPushNotification(userId.Value, dto);
      }
    }

    public async Task NotifyRealtimeAsync(int userId, NotificationDto dto)
    {
      // 1) On persiste en base :
      var notif = new Notification { /* … */ UserId = userId, /* … */ };
      _context.Notification.Add(notif);
      await _context.SaveChangesAsync();

      dto.Id = notif.Id;
      dto.DateEnvoi = notif.DateEnvoi;
      dto.UserId = userId;

      _logger.LogInformation("Envoi Notification à {userId} : ID={id}", userId, dto.Id);
      // → Ici on s’assure que la connexion avec ce userId existe
      await _hub.Clients.Group(userId.ToString()).SendAsync("ReceiveNotification", dto);
      _logger.LogInformation("Notification envoyée au groupe {groupName}", userId);
    }

    private async Task SendPushNotification(int userId, NotificationDto dto)
    {
      try
      {
        var subs = await _context.PushSubscriptions
            .Where(s => s.UserId == userId.ToString())
            .ToListAsync();

        if (!subs.Any())
        {
          _logger.LogInformation($"Aucun abonnement push trouvé pour userId {userId}");
          return;
        }

        var title = dto.EntityType != null && dto.EntityId.HasValue
            ? $"Nouvel {dto.EntityType} #{dto.EntityId.Value}"
            : "Nouvelle notification";
        var url = dto.EntityType != null && dto.EntityId.HasValue
            ? $"https://votre-client/#/{dto.EntityType.ToLower()}/{dto.EntityId.Value}"
            : null;

        var payload = JsonSerializer.Serialize(new { title, message = dto.Message, url });
        var pushMessage = new PushMessage(payload) { Urgency = PushMessageUrgency.High };

        foreach (var sub in subs)
        {
          var pushSub = new PushSubscription
          {
            Endpoint = sub.Endpoint,
            Keys = new Dictionary<string, string>
                        {
                            { PushEncryptionKeyName.P256DH.ToString().ToLower(), sub.P256DH },
                            { PushEncryptionKeyName.Auth.ToString().ToLower(), sub.Auth }
                        }
          };

          try
          {
            await _pushClient.RequestPushMessageDeliveryAsync(pushSub, pushMessage);
          }
          catch (Exception ex)
          {
            _logger.LogError(ex, "Échec de l'envoi push à {Endpoint}", sub.Endpoint);
          }
        }
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Erreur lors de l'envoi push pour userId {userId}");
      }
    }

    public async Task NotifyPushAsync(int userId, NotificationDto dto)
    {
      await SendPushNotification(userId, dto);
    }

    public async Task MarkAllAsReadAsync(int userId)
    {
      var notifs = await _context.Notification
          .Where(n => n.UserId == userId && !n.IsRead)
          .ToListAsync();

      notifs.ForEach(n => n.IsRead = true);
      await _context.SaveChangesAsync();
    }

    public async Task MarkAsReadAsync(int notificationId)
    {
      var notif = await _context.Notification
          .FirstOrDefaultAsync(n => n.Id == notificationId);
      if (notif == null)
        throw new KeyNotFoundException($"Notification {notificationId} introuvable.");

      notif.IsRead = true;
      await _context.SaveChangesAsync();
    }

    public async Task SoftDeleteAsync(int notificationId)
    {
      var notif = await _context.Notification
          .FirstOrDefaultAsync(n => n.Id == notificationId);
      if (notif == null)
        throw new KeyNotFoundException($"Notification {notificationId} introuvable.");

      notif.IsDeleted = true;
      await _context.SaveChangesAsync();
    }
  }
}