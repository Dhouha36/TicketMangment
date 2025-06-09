using GestionTicketsAPI.DTOs;
using GestionTicketsAPI.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GestionTicketsAPI.Controllers
{
    [ApiController]
[Route("api/clients/{clientId}/notifications")]
public class ClientNotificationsController : ControllerBase
{
    private readonly IClientNotificationService _service;
    public ClientNotificationsController(IClientNotificationService svc) => _service = svc;

    [HttpPost("send")]
    public async Task<IActionResult> Send(int clientId, [FromBody] NotificationDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Message))
            return BadRequest();
        await _service.NotifyClientAsync(clientId, dto);
        return Ok(dto);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<NotificationDto>>> Get(int clientId)
        => Ok(await _service.GetClientNotificationsAsync(clientId));

    [HttpPost("markasread")]
    public async Task<IActionResult> MarkAll(int clientId)
    {
        await _service.MarkAllAsReadClientAsync(clientId);
        return NoContent();
    }

    [HttpPost("{notificationId}/markasread")]
    public async Task<IActionResult> MarkOne(int clientId, int notificationId)
    {
        await _service.MarkAsReadClientAsync(notificationId);
        return NoContent();
    }

    [HttpDelete("{notificationId}")]
    public async Task<IActionResult> Hide(int clientId, int notificationId)
    {
        await _service.SoftDeleteClientNotificationAsync(notificationId);
        return NoContent();
    }
}

}
