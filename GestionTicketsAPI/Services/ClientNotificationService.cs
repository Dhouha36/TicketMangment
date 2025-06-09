using System;
using GestionTicketsAPI.Data;
using GestionTicketsAPI.DTOs;
using GestionTicketsAPI.Entities;
using GestionTicketsAPI.hubs;
using GestionTicketsAPI.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace GestionTicketsAPI.Services;

public class ClientNotificationService : IClientNotificationService
{
  private readonly DataContext _context;
  private readonly ILogger<ClientNotificationService> _logger;
  private readonly IHubContext<NotificationHub> _hubContext;

  public ClientNotificationService(
    DataContext context,
    ILogger<ClientNotificationService> logger,
      IHubContext<NotificationHub> hubContext)
      {
        _context = context;
        _logger = logger;
        _hubContext = hubContext;
      }

  public async Task NotifyClientAsync(int clientId, NotificationDto dto)
  {
    var notif = new Notification
    {
      ClientId = clientId,
      Message = dto.Message,
      DateEnvoi = DateTime.UtcNow,
      EntityType = dto.EntityType,
      EntityId = dto.EntityId,
      IsRead = false
    };
    _context.Notification.Add(notif);
    await _context.SaveChangesAsync();

    dto.Id = notif.Id;
    dto.ClientId = notif.ClientId;
    await _hubContext.Clients
            .Group($"client_{clientId}")
            .SendAsync("ReceiveNotification", dto);
  }

  public async Task<IEnumerable<NotificationDto>> GetClientNotificationsAsync(int clientId)
  {
    var list = await _context.Notification
        .Where(n => !n.IsDeleted && n.ClientId == clientId)
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

  public async Task MarkAllAsReadClientAsync(int clientId)
  {
    var unread = await _context.Notification
        .Where(n => n.ClientId == clientId && !n.IsRead)
        .ToListAsync();

    unread.ForEach(n => n.IsRead = true);
    await _context.SaveChangesAsync();
  }

  public async Task MarkAsReadClientAsync(int notificationId)
  {
    var notif = await _context.Notification.FindAsync(notificationId)
                 ?? throw new KeyNotFoundException($"Notification {notificationId} introuvable.");
    notif.IsRead = true;
    await _context.SaveChangesAsync();
  }

  public async Task SoftDeleteClientNotificationAsync(int notificationId)
  {
    var notif = await _context.Notification.FindAsync(notificationId)
                 ?? throw new KeyNotFoundException($"Notification {notificationId} introuvable.");
    notif.IsDeleted = true;
    await _context.SaveChangesAsync();
  }
}