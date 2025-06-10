using System.Security.Claims;
using GestionTicketsAPI.DTOs;
using GestionTicketsAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GestionTicketsAPI.Controllers
{
  [ApiController]
  public class DashboardController : BaseApiController
  {
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
      _dashboardService = dashboardService;
    }

    [HttpGet("counts")]
    public ActionResult<DashboardCountsDto> GetDashboardCounts()
    {
      var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
      var roleClaim = User.FindFirst(ClaimTypes.Role);

      if (userIdClaim == null || roleClaim == null)
      {
        return BadRequest("Les claims nécessaires (NameIdentifier et Role) ne sont pas présents dans le token.");
      }

      int userId = int.Parse(userIdClaim.Value);
      string role = roleClaim.Value;

      var result = _dashboardService.GetDashboardCounts(userId, role);
      return Ok(result);
    }

    [HttpGet("my-tickets-count")]
    public ActionResult<int> GetMyTicketsCount()
    {
      var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
      if (userIdClaim == null)
        return BadRequest("Claim NameIdentifier manquant.");

      int userId = int.Parse(userIdClaim.Value);
      int count = _dashboardService.GetTicketsCountAsResponsible(userId);
      return Ok(count);
    }

    [HttpGet("count/responsible/{userId}")]
    public ActionResult<int> GetTicketsCountAsResponsible(int userId)
    {
      var count = _dashboardService.GetTicketsCountAsResponsible(userId);
      return Ok(count);
    }
    [HttpGet("count/projectmember/{userId}")]
    public ActionResult<int> GetTicketsCountAsProjectMember(int userId)
    {
      var count = _dashboardService.GetTicketsCountAsProjectMember(userId);
      return Ok(count);
    }
    [HttpPost("tickets-by-user")]
    public ActionResult<IEnumerable<TicketStatDto>> GetTicketsByUser([FromBody] TicketFilterRequest req)
    {
      var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
      var role = User.FindFirst(ClaimTypes.Role)!.Value;

      // On passe désormais filterUserId = req.UserId
      var data = _dashboardService.GetTicketCountsByUserAndPeriod(
          userId,                 // identité de l'appelant (pour rôle et fallback)
          role,
          req.Start,
          req.End,
          req.Granularity,
          req.UserId,              // <— filtre éventuel sur un autre utilisateur
          req.ProjetId
      );

      return Ok(data);
    }

    [HttpPost("tickets-by-status")]
    public ActionResult<IEnumerable<TicketStatDto>> GetTicketsByStatus([FromBody] TicketFilterRequest req)
    {
      var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
      var role = User.FindFirst(ClaimTypes.Role)!.Value;

      var data = _dashboardService.GetTicketCountsByStatusAndPeriod(
          userId,
          role,
          req.Start,
          req.End,
          req.Granularity,
          req.OwnerId,
          req.PersonnelId,
          req.ProjetId
      );

      return Ok(data);
    }
    [HttpPost("tickets-filtered")] // nouvelle route
    public ActionResult<IEnumerable<TicketStatDto>> GetTicketsFiltered([FromBody] TicketFilterRequest req)
    {
      var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
      var role = User.FindFirst(ClaimTypes.Role)!.Value;

      var data = _dashboardService.GetTicketsFiltered(
          userId,
          role,
          req.Start,
          req.End,
          req.Granularity,
          req.OwnerId,
          req.PersonnelId,
          req.ProjetId
      );

      return Ok(data);
    }

    [HttpPost("timespent")]
    public ActionResult<IEnumerable<TicketStatDto>> GetTimeSpent([FromBody] TicketFilterRequest request)
    {
        if (request == null)
            return BadRequest("Requête invalide.");

        // 1️⃣ Récupérer l'ID de l'utilisateur (claim sub ou nameid)
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
            return Unauthorized("Utilisateur non authentifié.");

        // 2️⃣ Récupérer le rôle (claim role)
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        if (string.IsNullOrEmpty(role))
            return Forbid("Rôle non trouvé.");

        // 3️⃣ Appeler la méthode avec les bons arguments
        var result = _dashboardService.GetTimeSpentByPeriod(userId, role, request);

        return Ok(result.ToList());
    }

  }

}
