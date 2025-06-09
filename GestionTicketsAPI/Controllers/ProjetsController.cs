using System.Security.Claims;
using AutoMapper;
using GestionTicketsAPI.DTOs;
using GestionTicketsAPI.Extensions;
using GestionTicketsAPI.Helpers;
using GestionTicketsAPI.Interfaces;
using GestionTicketsAPI.Services;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
namespace GestionTicketsAPI.Controllers
{
  [ApiController]
  [Authorize]
  public class ProjetsController : BaseApiController
  {
    private readonly IProjetService _projetService;
    private readonly ExcelExportServiceClosedXML _excelExportService;
    private readonly IMapper _mapper;
    private readonly IUserNotificationService _userNotifService;
    private readonly IUserService _userService;

    public ProjetsController(ExcelExportServiceClosedXML excelExportService, IMapper mapper, IProjetService projetService,
        IUserNotificationService userNotifService,
        IUserService userService)
    {
      _projetService = projetService;
      _mapper = mapper;
      _excelExportService = excelExportService;
      _userNotifService = userNotifService;
      _userService = userService;
    }

    // Récupérer tous les projets
    [HttpPost("search")]
    public async Task<ActionResult<IEnumerable<ProjetDto>>> GetProjects([FromBody] ProjectFilterParams filterParams)
    {
      // Extraction des infos de l'utilisateur connecté
      var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
      var roleClaim = HttpContext.User.FindFirst(ClaimTypes.Role);
      if (userIdClaim != null && roleClaim != null)
      {
        filterParams.UserId = int.Parse(userIdClaim.Value);
        filterParams.Role = roleClaim.Value.ToLower().Trim();
      }

      var pagedProjects = await _projetService.GetProjetsPagedAsync(filterParams);

      var pagination = new
      {
        currentPage = pagedProjects.CurrentPage,
        pageSize = pagedProjects.PageSize,
        totalItems = pagedProjects.TotalCount,
        totalPages = pagedProjects.TotalPages
      };
      Response.Headers["Pagination"] = JsonConvert.SerializeObject(pagination);
      return Ok(pagedProjects);
    }

    // Récupérer les projets paginés
    [HttpPost("paged")]
    public async Task<ActionResult<PagedList<ProjetDto>>> GetProjetsPaged([FromBody] ProjectFilterParams filterParams)
    {
      Console.WriteLine($"Role: {filterParams.Role}, UserId: {filterParams.UserId}");
      var projetsPaged = await _projetService.GetProjetsPagedAsync(filterParams);
      Response.AddPaginationHeader(projetsPaged);
      return Ok(projetsPaged);
    }


    // Récupérer un projet par ID
    [HttpGet("{id}")]
    public async Task<ActionResult<ProjetDto>> GetProjet(int id)
    {
      var projetDto = await _projetService.GetProjetByIdAsync(id);
      if (projetDto == null)
        return NotFound();
      return Ok(projetDto);
    }

    // Ajouter un projet
    [HttpPost("ajouterProjet")]
    public async Task<ActionResult<ProjetDto>> PostProjet([FromBody] ProjetDto projetDto)
    {
      if (await _projetService.ProjetExists(projetDto.Nom))
        return BadRequest("Le projet existe déjà");

      var createdProjetDto = await _projetService.AddProjetAsync(projetDto);

      if (createdProjetDto.ChefProjet != null)
      {
        var cp = createdProjetDto.ChefProjet;
        var notifDto = new NotificationDto
        {
          Message = $"Vous êtes désormais chef du projet « {createdProjetDto.Nom} ».",
          DateEnvoi = DateTime.UtcNow,
          EntityType = "Projets",
          EntityId = createdProjetDto.Id
        };
        BackgroundJob.Enqueue(() => _userNotifService.NotifyRealtimeAsync(cp.Id, notifDto));
        BackgroundJob.Enqueue(() => _userNotifService.SendPushNotification(cp.Id, notifDto));
      }

      return CreatedAtAction(nameof(GetProjet), new { id = createdProjetDto.Id }, createdProjetDto);
    }

    [HttpPost("validateProjet")]
    public async Task<IActionResult> ValidateProjetAsync([FromBody] ProjetDto projetDto)
    {
      if (await _projetService.ProjetExists(projetDto.Nom))
        return BadRequest("Le projet existe déjà");
      return Ok();
    }


    // Mettre à jour un projet
    [HttpPut("modifierProjet/{id}")]
    public async Task<IActionResult> PutProjet(int id, [FromBody] ProjetUpdateDto projetUpdateDto)
    {
      if (id != projetUpdateDto.Id)
        return BadRequest("L'ID du projet ne correspond pas.");

      var ancienProjet = await _projetService.GetProjetByIdAsync(id);
      if (ancienProjet == null) return NotFound();

      var oldChefId = ancienProjet.ChefProjetId;
      var result = await _projetService.UpdateProjetAsync(id, projetUpdateDto);
      if (!result) return NotFound();

      if (projetUpdateDto.ChefProjetId.HasValue && projetUpdateDto.ChefProjetId != oldChefId)
      {
        // Nouveau chef
        var nouveauCp = await _userService.GetUserByIdAsync(projetUpdateDto.ChefProjetId.Value);
        if (nouveauCp != null)
        {
          var dto = new NotificationDto
          {
            Message = $"Vous êtes désormais chef du projet « {projetUpdateDto.Nom} ».",
            DateEnvoi = DateTime.UtcNow,
            EntityType = "Projets",
            EntityId = projetUpdateDto.Id
          };
          BackgroundJob.Enqueue(() => _userNotifService.NotifyRealtimeAsync(nouveauCp.Id, dto));
          BackgroundJob.Enqueue(() => _userNotifService.SendPushNotification(nouveauCp.Id, dto));
        }

        // Ancien chef
        if (oldChefId.HasValue)
        {
          var ancienCp = await _userService.GetUserByIdAsync(oldChefId.Value);
          if (ancienCp != null)
          {
            var dto = new NotificationDto
            {
              Message = $"Vous n’êtes plus chef du projet « {projetUpdateDto.Nom} ».",
              DateEnvoi = DateTime.UtcNow,
              EntityType = "Projets",
              EntityId = projetUpdateDto.Id
            };
            BackgroundJob.Enqueue(() => _userNotifService.NotifyRealtimeAsync(ancienCp.Id, dto));
            BackgroundJob.Enqueue(() => _userNotifService.SendPushNotification(ancienCp.Id, dto));
          }
        }
      }

      return NoContent();
    }



    // Supprimer un projet
    [HttpGet("supprimerProjet/{id}")]
    public async Task<IActionResult> DeleteProjet(int id)
    {
      var result = await _projetService.DeleteProjetAsync(id);
      if (!result)
        return NotFound();
      return NoContent();
    }

    // Supprimer plusieurs projets
    [HttpGet("supprimerProjets")]
    public async Task<IActionResult> DeleteProjets([FromBody] List<int> ids)
    {
      if (ids == null || !ids.Any())
        return BadRequest("Aucun identifiant fourni.");

      var result = await _projetService.DeleteProjetsAsync(ids);
      if (!result)
        return NotFound("Un ou plusieurs projets non trouvés.");
      return NoContent();
    }

    // Ajouter un utilisateur au projet
    [HttpPost("{projetId}/utilisateurs")]
    public async Task<IActionResult> AjouterUtilisateurAuProjet(int projetId, [FromBody] ProjetUserDto dto)
    {
      var success = await _projetService.AjouterUtilisateurAuProjetAsync(projetId, dto);
      if (!success) return NotFound();

      var projet = await _projetService.GetProjetByIdAsync(projetId);
      var user = await _userService.GetUserByIdAsync(dto.UserId);
      if (user != null)
      {
        var notifDto = new NotificationDto
        {
          Message = $"Vous avez été ajouté au projet « {projet.Nom} ».",
          DateEnvoi = DateTime.UtcNow,
          EntityType = "Projets",
          EntityId = projetId
        };
        BackgroundJob.Enqueue(() => _userNotifService.NotifyRealtimeAsync(user.Id, notifDto));
        BackgroundJob.Enqueue(() => _userNotifService.SendPushNotification(user.Id, notifDto));
      }

      return Ok();
    }


    // Récupérer les membres d'un projet
    [HttpGet("membres/{projetId}")]
    public async Task<IActionResult> GetMembresProjet(int projetId)
    {
      var membres = await _projetService.GetMembresProjetAsync(projetId);
      return Ok(membres);
    }

    // Supprimer un utilisateur d'un projet
    [HttpGet("delete/{projetId}/utilisateurs/{userId}")]
    public async Task<IActionResult> SupprimerUtilisateurDuProjet(int projetId, int userId)
    {
      var projet = await _projetService.GetProjetByIdAsync(projetId);
      var success = await _projetService.SupprimerUtilisateurDuProjetAsync(projetId, userId);
      if (!success) return BadRequest();

      var user = await _userService.GetUserByIdAsync(userId);
      if (user != null)
      {
        var notifDto = new NotificationDto
        {
          Message = $"Vous avez été retiré du projet « {projet.Nom} ».",
          DateEnvoi = DateTime.UtcNow,
          EntityType = "Projets",
          EntityId = projetId
        };
        BackgroundJob.Enqueue(() => _userNotifService.NotifyRealtimeAsync(user.Id, notifDto));
        BackgroundJob.Enqueue(() => _userNotifService.SendPushNotification(user.Id, notifDto));
      }
      return NoContent();
    }

    // Supprimer plusieurs utilisateurs d'un projet
    [HttpDelete("supprimerUtilisateursDuProjet")]
    public async Task<IActionResult> SupprimerUtilisateursDuProjet([FromBody] ProjetUsersDeleteDto deleteDto)
    {
      if (deleteDto == null || deleteDto.UserIds == null || !deleteDto.UserIds.Any())
        return BadRequest("Aucun utilisateur spécifié.");

      var projet = await _projetService.GetProjetByIdAsync(deleteDto.ProjetId);
      if (projet == null)
        return NotFound($"Projet {deleteDto.ProjetId} non trouvé.");

      foreach (var userId in deleteDto.UserIds)
      {
        var removed = await _projetService.SupprimerUtilisateurDuProjetAsync(deleteDto.ProjetId, userId);
        if (!removed)
          return NotFound($"Utilisateur {userId} non trouvé dans le projet {deleteDto.ProjetId}.");

        var user = await _userService.GetUserByIdAsync(userId);
        if (user != null)
        {
          var dto = new NotificationDto
          {
            Message = $"Vous avez été retiré du projet « {projet.Nom} ».",
            DateEnvoi = DateTime.UtcNow,
            EntityType = "Projets",
            EntityId = deleteDto.ProjetId
          };
          BackgroundJob.Enqueue(() => _userNotifService.NotifyRealtimeAsync(user.Id, dto));
          BackgroundJob.Enqueue(() => _userNotifService.SendPushNotification(user.Id, dto));
        }
      }

      return NoContent();
    }


    [HttpGet("user")]
    public async Task<IActionResult> GetUserProjets()
    {
      var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
      if (userIdClaim == null)
        return Unauthorized();

      int userId = int.Parse(userIdClaim.Value);
      var projets = await _projetService.GetProjetsForUserAsync(userId);
      return Ok(projets);
    }

    [HttpGet("societe/{societeId}")]
    public async Task<ActionResult<IEnumerable<ProjetDto>>> GetProjetsBySocieteId(int societeId)
    {
      var projetsDto = await _projetService.GetProjetsBySocieteIdAsync(societeId);
      if (projetsDto == null || !projetsDto.Any())
        return NotFound($"Aucun projet trouvé pour la société ID {societeId}.");

      return Ok(projetsDto);
    }

    [HttpPost("export")]
    public async Task<IActionResult> ExportProjects([FromBody] ProjectFilterParams filterParams)
    {
      // Extraction des infos de l'utilisateur connecté
      var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
      var roleClaim = HttpContext.User.FindFirst(ClaimTypes.Role);
      if (userIdClaim != null && roleClaim != null)
      {
        filterParams.UserId = int.Parse(userIdClaim.Value);
        filterParams.Role = roleClaim.Value;
      }

      var projects = await _projetService.GetProjetsFilteredAsync(filterParams);
      var projectExportDtos = _mapper.Map<IEnumerable<ProjectExportDto>>(projects);
      var content = _excelExportService.ExportToExcel(projectExportDtos, "Projects");
      return File(content,
          "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
          $"ProjectsExport_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
    }

    [HttpGet("{projetId}/clients")]
    public async Task<ActionResult<IEnumerable<ClientDto>>> GetClientsByProjet(int projetId)
    {
        // Vérifier que le projet existe (optionnel)
        var projetExists = await _projetService.GetProjetByIdAsync(projetId);
        if (projetExists == null)
            return NotFound($"Projet {projetId} non trouvé.");

        var clients = await _projetService.GetClientsByProjetIdAsync(projetId);
        return Ok(clients);
    }

  }
}
