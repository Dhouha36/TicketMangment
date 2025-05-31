using System.Security.Claims;
using AutoMapper;
using CloudinaryDotNet.Actions;
using GestionTicketsAPI.DTOs;
using GestionTicketsAPI.Entities;
using GestionTicketsAPI.Helpers;
using GestionTicketsAPI.Interfaces;
using GestionTicketsAPI.Services;
using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ClosedXML.Excel;
using System.Text;

namespace GestionTicketsAPI.Controllers
{
  [ApiController]
  public class TicketsController : BaseApiController
  {
    private readonly ITicketService _ticketService;
    private readonly IMapper _mapper;
    private readonly IPhotoService _photoService;
    private readonly EmailService _emailService;
    private readonly IUserService _userService;
    private readonly ICommentService _commentService;
    private readonly ExcelExportServiceClosedXML _excelExportService;
    private readonly NotificationService _notifService;
    private readonly IWebHostEnvironment _env;

    public TicketsController(IWebHostEnvironment env, ExcelExportServiceClosedXML excelExportService, ITicketService ticketService, IMapper mapper, IPhotoService photoService, IUserService userService, EmailService emailService, ICommentService commentService,
    NotificationService notifService)
    {
      _ticketService = ticketService;
      _mapper = mapper;
      _photoService = photoService;
      _userService = userService;
      _emailService = emailService;
      _commentService = commentService;
      _excelExportService = excelExportService;
      _env = env;
      _notifService = notifService;
    }

    // GET api/tickets?...
    [HttpPost("paged")]
    public async Task<ActionResult<IEnumerable<TicketDto>>> GetTickets([FromBody] TicketFilterParams filterParams)
    {
      // Récupération des claims
      var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
      var roleClaim = HttpContext.User.FindFirst(ClaimTypes.Role);
      if (userIdClaim != null && roleClaim != null)
      {
        filterParams.UserId = int.Parse(userIdClaim.Value);
        filterParams.Role = roleClaim.Value;
      }

      // Appel au service
      var pagedTickets = await _ticketService.GetTicketsPagedAsync(filterParams);

      // Si null ou aucun ticket, on crée un PagedList vide
      if (pagedTickets == null || !pagedTickets.Any())
      {
        pagedTickets = new PagedList<TicketDto>(
            items: Enumerable.Empty<TicketDto>(),
            count: 0,
            pageNumber: filterParams.PageNumber,
            pageSize: filterParams.PageSize
        );
      }

      // Préparation des en‑têtes de pagination
      var pagination = new
      {
        currentPage = pagedTickets.CurrentPage,
        pageSize = pagedTickets.PageSize,
        totalItems = pagedTickets.TotalCount,
        totalPages = pagedTickets.TotalPages
      };
      Response.Headers["Pagination"] = JsonConvert.SerializeObject(pagination);

      return Ok(pagedTickets);
    }



    // GET api/tickets/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<TicketDto>> GetTicket(int id)
    {
      var ticketDto = await _ticketService.GetTicketByIdAsync(id);
      if (ticketDto == null) return NotFound();
      return Ok(ticketDto);
    }

    [HttpPost]
    public async Task<ActionResult<TicketDto>> CreateTicket(
    [FromForm] string ticketJson,
    [FromForm] IFormFile file)
    {
      // Deserialize the JSON string into DTO
      var dto = JsonConvert.DeserializeObject<TicketCreateDto>(ticketJson);
      // 0) Validation d’existence
      if (await _ticketService.TicketExists(dto.Title))
        return BadRequest("Un ticket avec ce titre existe déjà");

      // Valider le DTO (vérifier que les champs requis sont présents)
      if (dto == null || !ModelState.IsValid)
        return BadRequest(ModelState);
      // 1) Mapping initial
      var ticket = _mapper.Map<Ticket>(dto);
      ticket.CreatedAt = DateTime.UtcNow;

      // 2) Si un fichier est présent, on l’upload directement
      if (file != null && file.Length > 0)
      {
        // Upload vers Cloudinary via IFormFile
        var uploadResult = await _photoService.UploadFileAsync(file);
        ticket.Attachments = uploadResult.SecureUrl?.ToString()
                       ?? uploadResult.Uri?.ToString();
      }

      // 3) Statut par défaut
      var defaultStatus = await _ticketService.GetStatusByNameAsync("—");
      if (defaultStatus == null)
        return BadRequest("Statut par défaut introuvable");
      ticket.StatutId = defaultStatus.Id;

      // 4) Persistance
      await _ticketService.AddTicketAsync(ticket);
      await _ticketService.SaveAllAsync();

      // 5) Rechargement complet
      var ticketFromDb = await _ticketService.GetTicketByIdAsync(ticket.Id);
      if (ticketFromDb == null)
        return NotFound();

      // 6) Notifications et emails
      // 6.a) Chef de projet
      if (ticketFromDb.Projet?.ChefProjet is { } chef)
      {
        BackgroundJob.Enqueue(() => _emailService.SendEmailAsync(
            $"{chef.FirstName} {chef.LastName}",
            chef.Email,
            "Nouveau ticket créé",
            $"Bonjour {chef.FirstName}, un nouveau ticket #{ticket.Id} a été créé."
        ));

        var notifDto = new NotificationDto
        {
          Message = $"Nouveau ticket #{ticket.Id} créé par {ticket.Owner.FirstName} {ticket.Owner.LastName}.",
          DateEnvoi = DateTime.UtcNow,
          EntityType = "Tickets",
          EntityId = ticket.Id
        };
        BackgroundJob.Enqueue(() => _notifService.NotifyRealtimeAsync(chef.Id, notifDto));
        BackgroundJob.Enqueue(() => _notifService.NotifyPushAsync(chef.Id, notifDto));
      }

      // 6.b) Client (email uniquement)
      if (ticketFromDb.Owner is { } client)
      {
        BackgroundJob.Enqueue(() => _emailService.SendEmailAsync(
            $"{client.FirstName} {client.LastName}",
            client.Email,
            "Confirmation de création de ticket",
            $"Bonjour {client.FirstName}, votre ticket #{ticket.Id} a bien été créé."
        ));
      }

      // 6.c) Super-admins
      var superAdmins = await _userService.GetUsersByRoleAsync("super admin");
      foreach (var admin in superAdmins)
      {
        BackgroundJob.Enqueue(() => _emailService.SendEmailAsync(
            $"{admin.FirstName} {admin.LastName}",
            admin.Email,
            "Nouveau ticket créé",
            $"Bonjour {admin.FirstName}, un nouveau ticket #{ticket.Id} a été créé."
        ));

        var notifDto = new NotificationDto
        {
          Message = $"Nouveau ticket #{ticket.Id} créé par {ticket.Owner.FirstName} {ticket.Owner.LastName}.",
          DateEnvoi = DateTime.UtcNow,
          EntityType = "Tickets",
          EntityId = ticket.Id
        };
        BackgroundJob.Enqueue(() => _notifService.NotifyRealtimeAsync(admin.Id, notifDto));
        BackgroundJob.Enqueue(() => _notifService.NotifyPushAsync(admin.Id, notifDto));
      }

      // 7) Retour
      var resultDto = _mapper.Map<TicketDto>(ticketFromDb);
      return CreatedAtAction(nameof(GetTicket), new { id = ticket.Id }, resultDto);
    }

    [HttpPost("validate/{id}")]
    public async Task<IActionResult> ValidateTicket(int id, [FromBody] TicketValidationDto validationDto)
    {
      // 1) Récupérer l’entité ticket
      var ticket = await _ticketService.GetTicketEntityByIdAsync(id);
      if (ticket == null)
        return NotFound("Ticket non trouvé");

      // 2) Authentification & autorisation
      var currentUserId = int.Parse(HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
      var rawRole = HttpContext.User.FindFirst(ClaimTypes.Role)?.Value?.Trim();

      // Normalisation : tout en minuscules, suppression des espaces et traits d’union
      var normalizedRole = rawRole?
          .ToLowerInvariant()
          .Replace(" ", "")
          .Replace("-", "");
      var isChef = ticket.Projet?.ChefProjet?.Id == currentUserId;
      var isSuperAdmin = string.Equals(normalizedRole, "superadmin", StringComparison.OrdinalIgnoreCase);
      if (!isChef && !isSuperAdmin)
        return Unauthorized("Vous n'êtes pas autorisé à valider ce ticket.");

      // 3) Acceptation ou refus
      if (validationDto.IsAccepted)
      {
        // Changer statut en « Accepté »
        var acceptStatus = await _ticketService.GetStatusByNameAsync("Accepté");
        if (acceptStatus == null)
          return BadRequest("Statut 'Accepté' introuvable");
        ticket.StatutId = acceptStatus.Id;
        ticket.ApprovedAt = DateTime.UtcNow;

        // Notification client accepté
        if (ticket.Owner is { } client)
        {
          var notifDto = new NotificationDto
          {
            Message = $"Votre ticket #{ticket.Id} a été accepté.",
            DateEnvoi = DateTime.UtcNow,
            EntityType = "Tickets",
            EntityId = ticket.Id
          };
          BackgroundJob.Enqueue(() => _emailService.SendEmailAsync(
              $"{client.FirstName} {client.LastName}",
              client.Email,
              "Ticket accepté",
              $"Bonjour {client.FirstName}, votre ticket #{ticket.Id} a été accepté."
          ));
          BackgroundJob.Enqueue(() => _notifService.NotifyRealtimeAsync(client.Id, notifDto));
          BackgroundJob.Enqueue(() => _notifService.NotifyPushAsync(client.Id, notifDto));
        }

        // Si on assigne un responsable en même temps
        if (validationDto.ResponsibleId.HasValue)
        {
          ticket.ResponsibleId = validationDto.ResponsibleId.Value;
          var inProgStatus = await _ticketService.GetStatusByNameAsync("En cours");
          if (inProgStatus == null)
            return BadRequest("Statut 'En cours' introuvable");
          ticket.StatutId = inProgStatus.Id;

          if (await _ticketService.SaveAllAsync())
          {
            var resp = await _userService.GetUserByIdAsync(ticket.ResponsibleId.Value);
            if (resp != null && resp.Id != currentUserId)
            {
              var notifDto = new NotificationDto
              {
                Message = $"Vous avez été désigné responsable du ticket #{ticket.Id}.",
                DateEnvoi = DateTime.UtcNow,
                EntityType = "Tickets",
                EntityId = ticket.Id
              };
              BackgroundJob.Enqueue(() => _emailService.SendEmailAsync(
                  $"{resp.FirstName} {resp.LastName}",
                  resp.Email,
                  "Nouveau ticket assigné",
                  $"Bonjour {resp.FirstName} {resp.LastName}, vous êtes responsable du ticket #{ticket.Id}."
              ));
              BackgroundJob.Enqueue(() => _notifService.NotifyRealtimeAsync(resp.Id, notifDto));
              BackgroundJob.Enqueue(() => _notifService.NotifyPushAsync(resp.Id, notifDto));
            }
          }
        }
      }
      else
      {
        // Refus
        var refusedStatus = await _ticketService.GetStatusByNameAsync("Refusé");
        if (refusedStatus == null)
          return BadRequest("Statut 'Refusé' introuvable");
        ticket.StatutId = refusedStatus.Id;
        ticket.ValidationReason = validationDto.Reason;

        if (ticket.Owner is { } client)
        {
          var notifDto = new NotificationDto
          {
            Message = $"Votre ticket #{ticket.Id} a été refusé. Raison : {validationDto.Reason}",
            DateEnvoi = DateTime.UtcNow,
            EntityType = "Tickets",
            EntityId = ticket.Id
          };
          BackgroundJob.Enqueue(() => _emailService.SendEmailAsync(
              $"{client.FirstName} {client.LastName}",
              client.Email,
              "Ticket refusé",
              $"Bonjour {client.FirstName} {client.LastName}, votre ticket #{ticket.Id} a été refusé. Raison : {validationDto.Reason}"
          ));
          BackgroundJob.Enqueue(() => _notifService.NotifyRealtimeAsync(client.Id, notifDto));
          BackgroundJob.Enqueue(() => _notifService.NotifyPushAsync(client.Id, notifDto));
        }
      }

      ticket.UpdatedAt = DateTime.UtcNow;
      await _ticketService.UpdateTicketAsync(ticket);

      return NoContent();
    }

    // Endpoint pour l'upload du fichier en arrière-plan
    [HttpPost("upload")]
    public async Task<IActionResult> UploadFile([FromBody] IFormFile file)
    {
      var uploadResult = await _photoService.UploadFileAsync(file);
      if (uploadResult == null || uploadResult.Error != null)
        return BadRequest("L'upload a échoué.");
      return Ok(new { secureUrl = uploadResult.SecureUrl.AbsoluteUri });
    }

    // DELETE api/tickets/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTicket(int id)
    {
      var ticketDto = await _ticketService.GetTicketByIdAsync(id);
      if (ticketDto == null) return NotFound();
      var ticket = _mapper.Map<Ticket>(ticketDto);
      var result = await _ticketService.DeleteTicketAsync(ticket);
      if (result) return NoContent();
      return BadRequest("La suppression du ticket a échoué");
    }

    // DELETE api/tickets/bulk
    [HttpDelete("bulk")]
    public async Task<IActionResult> DeleteMultipleTickets([FromBody] IEnumerable<int> ticketIds)
    {
      if (ticketIds == null || !ticketIds.Any())
      {
        return BadRequest("Aucun ticket spécifié pour la suppression.");
      }
      var result = await _ticketService.DeleteMultipleTicketsAsync(ticketIds);
      if (result)
        return NoContent();
      return BadRequest("La suppression des tickets a échoué.");
    }

    [HttpPost("finish/{id}")]
    public async Task<IActionResult> FinishTicket(int id, [FromBody] TicketCompletionDto completionDto)
    {
      var ticket = await _ticketService.GetTicketEntityByIdAsync(id);
      if (ticket == null)
        return NotFound("Ticket non trouvé");

      // Autorisation (Chef, SuperAdmin ou Responsable)
      var currentUserId = int.Parse(HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
      var role = HttpContext.User.FindFirst(ClaimTypes.Role)!.Value.ToLower();
      var isAllowed = ticket.Projet?.ChefProjet?.Id == currentUserId
                      || ticket.ResponsibleId == currentUserId
                      || role == "super admin";
      if (!isAllowed)
        return Unauthorized("Vous n'êtes pas autorisé à terminer ce ticket.");

      // Changer statut
      var newStatusName = completionDto.IsResolved ? "Résolu" : "Non Résolu";
      var newStatus = await _ticketService.GetStatusByNameAsync(newStatusName);
      if (newStatus == null)
        return BadRequest($"Statut '{newStatusName}' introuvable");

      ticket.StatutId = newStatus.Id;
      ticket.CompletionComment = completionDto.Comment;
      ticket.HoursSpent = completionDto.DurationInMinutes / 60;
      ticket.MinutesSpent = completionDto.DurationInMinutes % 60;
      ticket.SolvedAt = completionDto.CompletionDate;
      ticket.UpdatedAt = DateTime.UtcNow;

      await _ticketService.UpdateTicketAsync(ticket);

      // 1) Notification au propriétaire
      if (ticket.Owner is { } owner)
      {
        var notifDto = new NotificationDto
        {
          Message = $"Votre ticket #{ticket.Id} est {(completionDto.IsResolved ? "résolu" : "non résolu")}.",
          DateEnvoi = DateTime.UtcNow,
          EntityType = "Tickets",
          EntityId = ticket.Id
        };
        BackgroundJob.Enqueue(() => _notifService.NotifyRealtimeAsync(owner.Id, notifDto));
        BackgroundJob.Enqueue(() => _notifService.NotifyPushAsync(owner.Id, notifDto));
      }

      // 2) Notification au chef de projet si différent
      if (ticket.Projet?.ChefProjet is { } chefProj && chefProj.Id != currentUserId)
      {
        var notifDto = new NotificationDto
        {
          Message = $"Le ticket #{ticket.Id} du projet « {ticket.Projet.Nom} » est {(completionDto.IsResolved ? "résolu" : "non résolu")}.",
          DateEnvoi = DateTime.UtcNow,
          EntityType = "Tickets",
          EntityId = ticket.Id
        };
        BackgroundJob.Enqueue(() => _notifService.NotifyRealtimeAsync(chefProj.Id, notifDto));
        BackgroundJob.Enqueue(() => _notifService.NotifyPushAsync(chefProj.Id, notifDto));
      }

      // 3) Notification au responsable si différent
      if (ticket.Responsible is { } resp && resp.Id != currentUserId)
      {
        var notifDto = new NotificationDto
        {
          Message = $"Le ticket #{ticket.Id} qui vous était assigné est {(completionDto.IsResolved ? "résolu" : "non résolu")}.",
          DateEnvoi = DateTime.UtcNow,
          EntityType = "Tickets",
          EntityId = ticket.Id
        };
        BackgroundJob.Enqueue(() => _notifService.NotifyRealtimeAsync(resp.Id, notifDto));
        BackgroundJob.Enqueue(() => _notifService.NotifyPushAsync(resp.Id, notifDto));
      }

      // 4) Commentaire interne
      var totalMinutes = completionDto.DurationInMinutes;
      var hours = totalMinutes / 60;
      var minutes = totalMinutes % 60;

      var sb = new StringBuilder()
          .AppendLine($"Votre ticket est {(completionDto.IsResolved ? "résolu" : "non résolu")}.")
          // Date de début = date de création du ticket
          .AppendLine($"Date de début : {ticket.CreatedAt.ToLocalTime():dd/MM/yyyy HH:mm}")
          // Date de fin = date fournie dans le DTO
          .AppendLine($"Date de fin   : {completionDto.CompletionDate.ToLocalTime():dd/MM/yyyy HH:mm}")
          // Temps passé en heures et minutes
          .AppendLine($"Temps passé   : {hours} h {minutes} min");

      var commentText = sb.ToString();

      await _commentService.CreateCommentAsync(new CommentCreateDto
      {
        Contenu = commentText,
        TicketId = ticket.Id
      }, currentUserId);

      return NoContent();
    }

    [HttpPost("updateResponsible/{id}")]
    public async Task<IActionResult> UpdateResponsible(int id, [FromBody] TicketResponsibleDto responsibleDto)
    {
      var ticket = await _ticketService.GetTicketEntityByIdAsync(id);
      if (ticket == null)
        return NotFound("Ticket non trouvé");

      var currentUserId = int.Parse(HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
      var role = HttpContext.User.FindFirst(ClaimTypes.Role)!.Value.ToLower();
      var isAllowed = ticket.Projet?.ChefProjet?.Id == currentUserId
                      || ticket.ResponsibleId == currentUserId
                      || role == "super admin";
      if (!isAllowed)
        return Unauthorized("Vous n'êtes pas autorisé à modifier le responsable.");

      if (ticket.ResponsibleId == responsibleDto.ResponsibleId)
        return BadRequest("Le responsable n'a pas été modifié.");

      ticket.ResponsibleId = responsibleDto.ResponsibleId;
      await _ticketService.UpdateTicketAsync(ticket);

      if (ticket.Responsible is { } newResp)
      {
        var notifDto = new NotificationDto
        {
          Message = $"Vous avez été désigné responsable du ticket #{ticket.Id}.",
          DateEnvoi = DateTime.UtcNow,
          EntityType = "Tickets",
          EntityId = ticket.Id
        };
        BackgroundJob.Enqueue(() => _emailService.SendEmailAsync(
            $"{newResp.FirstName} {newResp.LastName}",
            newResp.Email,
            "Nouveau responsable de ticket",
            $"Bonjour {newResp.FirstName} {newResp.LastName}, vous êtes désormais responsable du ticket #{ticket.Id}."
        ));
        BackgroundJob.Enqueue(() => _notifService.NotifyRealtimeAsync(newResp.Id, notifDto));
        BackgroundJob.Enqueue(() => _notifService.NotifyPushAsync(newResp.Id, notifDto));
      }

      return NoContent();
    }

    [HttpGet("status-count")]
    public IActionResult GetTicketCountByStatus()
    {
      var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
      var roleClaim = HttpContext.User.FindFirst(ClaimTypes.Role);
      if (userIdClaim == null || roleClaim == null)
      {
        return Unauthorized("Utilisateur non authentifié.");
      }

      int userId = int.Parse(userIdClaim.Value);
      string role = roleClaim.Value;

      var result = _ticketService.GetTicketCountByStatus(userId, role);
      return Ok(result);
    }

    [HttpPost("export")]
    public async Task<IActionResult> ExportTickets([FromBody] TicketFilterParams filterParams)
    {
      var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
      var roleClaim = HttpContext.User.FindFirst(ClaimTypes.Role);
      if (userIdClaim != null && roleClaim != null)
      {
        filterParams.UserId = int.Parse(userIdClaim.Value);
        filterParams.Role = roleClaim.Value;
      }

      var tickets = await _ticketService.GetTicketsFilteredAsync(filterParams);
      var ticketExportDtos = _mapper.Map<IEnumerable<TicketExportDto>>(tickets);
      var content = _excelExportService.ExportToExcel(ticketExportDtos, "Tickets");

      return File(content,
          "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
          $"TicketsExport_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
    }

  }
}
