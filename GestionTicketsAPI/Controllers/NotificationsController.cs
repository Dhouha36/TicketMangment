using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GestionTicketsAPI.Data;
using GestionTicketsAPI.DTOs;
using GestionTicketsAPI.Entities;
using GestionTicketsAPI.Interfaces;
using GestionTicketsAPI.Services;
using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionTicketsAPI.Controllers
{
  [ApiController]
  [Route("api/notifications")]
  public class NotificationsController : ControllerBase
  {
    private readonly INotificationService _notifService;
    private readonly DataContext _context;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(
        INotificationService notifService,
        DataContext context,
        ILogger<NotificationsController> logger)
    {
      _notifService = notifService;
      _context = context;
      _logger = logger;
    }

    // 1) Enregistrer la subscription Push venant du frontend
    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe([FromBody] PushSubscriptionEntity dto)
    {
      try
      {
        var entity = new PushSubscriptionEntity
        {
          UserId = dto.UserId,
          Endpoint = dto.Endpoint,
          P256DH = dto.P256DH,
          Auth = dto.Auth
        };
        _context.Add(entity);
        await _context.SaveChangesAsync();
        return Ok();
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Échec de l'abonnement push");
        return StatusCode(500, new
        {
          error = ex.Message,
          innerError = ex.InnerException?.Message
        });
      }
    }

    // 2) Envoi manuel d'une NotificationDto
    [HttpPost("send/{userId}")]
    public async Task<IActionResult> Send(int userId, [FromBody] NotificationDto notificationDto)
    {
      if (notificationDto == null || string.IsNullOrWhiteSpace(notificationDto.Message))
        return BadRequest("Notification invalide.");

      // On garantit une date d'envoi à jour
      notificationDto.DateEnvoi = DateTime.UtcNow;

      // Enfile les jobs SignalR et Push
      BackgroundJob.Enqueue(() => _notifService.NotifyRealtimeAsync(userId, notificationDto));
      BackgroundJob.Enqueue(() => _notifService.NotifyPushAsync(userId, notificationDto));

      return Ok();
    }

    // 3) Récupérer les notifs d'un utilisateur sous forme de DTO
    [HttpGet("user/{userId}")]
    public async Task<ActionResult<IEnumerable<NotificationDto>>> GetUserNotifications(int userId)
    {
      var entities = await _context.Notification
           .Where(n => !n.IsDeleted &&
                        (n.UserId == userId ||
                         n.ClientId == userId))
          .OrderByDescending(n => n.DateEnvoi)
          .ToListAsync();

      var dtos = entities.Select(n => new NotificationDto
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

      return Ok(dtos);
    }


    // 4) Marquer toutes les notifications comme lues
    [HttpPost("markasread/{userId}")]
    public async Task<IActionResult> MarkAllAsRead(int userId)
    {
      await _notifService.MarkAllAsReadAsync(userId);
      return NoContent();
    }

    [HttpPost("markasread/one/{id}")]
    public async Task<IActionResult> MarkOneAsRead(int id)
    {
      try
      {
        await _notifService.MarkAsReadAsync(id);
        return NoContent();
      }
      catch (KeyNotFoundException)
      {
        return NotFound($"Notification {id} introuvable.");
      }
    }

    [HttpPost("hide/{id}")]
    public async Task<IActionResult> HideNotification(int id)
    {
      try
      {
        await _notifService.SoftDeleteAsync(id);
        return NoContent();
      }
      catch (KeyNotFoundException)
      {
        return NotFound($"Notification {id} introuvable.");
      }
    }
    
    
  }
}
