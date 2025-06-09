using GestionTicketsAPI.DTOs;
using GestionTicketsAPI.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GestionTicketsAPI.Controllers
{
  [ApiController]
  [Route("api/users/{userId}/notifications")]
  public class UserNotificationsController : ControllerBase
  {
    private readonly IUserNotificationService _service;
    public UserNotificationsController(IUserNotificationService svc) => _service = svc;

    [HttpPost("send")]
    public async Task<IActionResult> Send(int userId, [FromBody] NotificationDto dto)
    {
      if (dto == null || string.IsNullOrWhiteSpace(dto.Message))
        return BadRequest();
      dto.DateEnvoi = DateTime.UtcNow;
      await _service.NotifyRealtimeAsync(userId, dto);
      await _service.SendPushNotification(userId, dto);
      return Ok(dto);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<NotificationDto>>> Get(int userId)
        => Ok(await _service.GetUserNotificationsAsync(userId));

    [HttpPost("markasread")]
    public async Task<IActionResult> MarkAll(int userId)
    {
      await _service.MarkAllAsReadAsync(userId);
      return NoContent();
    }

    [HttpPost("{notificationId}/markasread")]
    public async Task<IActionResult> MarkOne(int userId, int notificationId)
    {
      await _service.MarkAsReadAsync(notificationId);
      return NoContent();
    }

    [HttpDelete("{notificationId}")]
    public async Task<IActionResult> Hide(int userId, int notificationId)
    {
      await _service.SoftDeleteAsync(notificationId);
      return NoContent();
    }
  }

}
