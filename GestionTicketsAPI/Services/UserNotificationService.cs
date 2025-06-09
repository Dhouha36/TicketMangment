using System;
using System.Text.Json;
using GestionTicketsAPI.Data;
using GestionTicketsAPI.DTOs;
using GestionTicketsAPI.Entities;
using GestionTicketsAPI.hubs;
using GestionTicketsAPI.Interfaces;
using Lib.Net.Http.WebPush;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace GestionTicketsAPI.Services;

public class UserNotificationService : IUserNotificationService
{
  private readonly IHubContext<NotificationHub> _hubContext;
  private readonly PushServiceClient _pushClient;
  private readonly DataContext _context;
  private readonly ILogger<UserNotificationService> _logger;

  public UserNotificationService(
      IHubContext<NotificationHub> hubContext,
      PushServiceClient pushClient,
      DataContext context,
      ILogger<UserNotificationService> logger)
  {
    _hubContext = hubContext;
    _context = context;
    _pushClient = pushClient;
    _logger = logger;
  }

  public async Task NotifyRealtimeAsync(int userId, NotificationDto dto)
  {
    var notif = new Notification
    {
      UserId = userId,
      Message = dto.Message,
      DateEnvoi = dto.DateEnvoi,
      EntityType = dto.EntityType,
      EntityId = dto.EntityId,
      IsRead = false
    };
    _context.Notification.Add(notif);
    await _context.SaveChangesAsync();

    dto.Id = notif.Id;
    await _hubContext.Clients
      .Group(userId.ToString())
      .SendAsync("ReceiveNotification", dto);
  }

  public async Task SendPushNotification(int userId, NotificationDto dto)
  {
    try
    {
      var subs = await _context.PushSubscriptions
          .Where(s => s.UserId.ToString() == userId.ToString())
          .ToListAsync();

      if (!subs.Any())
      {
        _logger.LogInformation("Aucun abonnement push trouvé pour userId {UserId}", userId);
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
      _logger.LogError(ex, "Erreur lors de l'envoi push pour userId {UserId}", userId);
    }
  }
  public async Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(int userId)
  {
    var list = await _context.Notification
        .Where(n => !n.IsDeleted && n.UserId == userId)
        .OrderByDescending(n => n.DateEnvoi)
        .ToListAsync();

    return list.Select(n => new NotificationDto
    {
      Id = n.Id,
      Message = n.Message,
      DateEnvoi = n.DateEnvoi,
      IsRead = n.IsRead,
      EntityType = n.EntityType,
      EntityId = n.EntityId,
      UserId = n.UserId,
      ClientId = n.ClientId
    });
  }

  public async Task MarkAllAsReadAsync(int userId)
  {
    var unread = await _context.Notification
        .Where(n => n.UserId == userId && !n.IsRead)
        .ToListAsync();

    unread.ForEach(n => n.IsRead = true);
    await _context.SaveChangesAsync();
  }

  public async Task MarkAsReadAsync(int notificationId)
  {
    var notif = await _context.Notification.FindAsync(notificationId)
                 ?? throw new KeyNotFoundException($"Notification {notificationId} introuvable.");
    notif.IsRead = true;
    await _context.SaveChangesAsync();
  }

  public async Task SoftDeleteAsync(int notificationId)
  {
    var notif = await _context.Notification.FindAsync(notificationId)
                 ?? throw new KeyNotFoundException($"Notification {notificationId} introuvable.");
    notif.IsDeleted = true;
    await _context.SaveChangesAsync();
  }
}
