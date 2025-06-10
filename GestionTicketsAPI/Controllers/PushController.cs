using GestionTicketsAPI.Data;
using GestionTicketsAPI.DTOs;
using GestionTicketsAPI.Entities;
using GestionTicketsAPI.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace GestionTicketsAPI.Controllers
{
  [ApiController]
  [Route("api/push")]
  public class PushController : ControllerBase
  {
    private readonly DataContext _context;
    private readonly IUserNotificationService _userNotifService;

    public PushController(DataContext context, IUserNotificationService userNotifService)
    {
      _context = context;
      _userNotifService = userNotifService;
    }

    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe([FromBody] PushSubscriptionDto dto)
    {
      if (dto == null
          || string.IsNullOrWhiteSpace(dto.UserId)
          || dto.Subscription == null
          || string.IsNullOrWhiteSpace(dto.Subscription.Endpoint))
      {
        return BadRequest("Donnée d'abonnement invalide.");
      }

      var exists = await _context.PushSubscriptions
          .AnyAsync(s =>
              s.Endpoint == dto.Subscription.Endpoint
              && s.UserId == dto.UserId);

      if (!exists)
      {
        var entity = new PushSubscriptionEntity
        {
          UserId = dto.UserId,
          Endpoint = dto.Subscription.Endpoint,
          P256DH = dto.Subscription.Keys.P256dh,
          Auth = dto.Subscription.Keys.Auth
        };
        _context.PushSubscriptions.Add(entity);
        await _context.SaveChangesAsync();
      }

      return Ok();
    }

    [HttpPost("unsubscribe")]
    public async Task<IActionResult> Unsubscribe([FromBody] PushSubscriptionDto dto)
    {
      var sub = await _context.PushSubscriptions
          .FirstOrDefaultAsync(s =>
              s.Endpoint == dto.Subscription.Endpoint
              && s.UserId == dto.UserId);

      if (sub != null)
      {
        _context.PushSubscriptions.Remove(sub);
        await _context.SaveChangesAsync();
      }

      return NoContent();
    }

    [HttpPost("test-push/{userId}")]
    public async Task<IActionResult> TestPush(int userId)
    {
      var dto = new NotificationDto
      {
        Message = "🌟 Notification de test",
        DateEnvoi = DateTime.UtcNow,
        EntityType = "Test",
        EntityId = 0
      };
      await _userNotifService.SendPushNotification(userId, dto);
      return Ok("Push envoyé");
    }


  }

}
