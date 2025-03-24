using System.Security.Claims;
using AutoMapper;
using CloudinaryDotNet.Actions;
using GestionTicketsAPI.DTOs;
using GestionTicketsAPI.Entities;
using GestionTicketsAPI.Helpers;
using GestionTicketsAPI.Interfaces;
using GestionTicketsAPI.Repositories;
using GestionTicketsAPI.Services;
using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace GestionTicketsAPI.Controllers
{
  [ApiController]
  [Route("api/[controller]")]
  public class TicketsController : ControllerBase
  {
    private readonly ITicketService _ticketService;
    private readonly IMapper _mapper;
    private readonly IPhotoService _photoService;
    private readonly EmailService _emailService;

    private readonly IUserService _userService;
    private readonly ICommentService _commentService;
    private readonly INotificationService _notificationService;
    private readonly INotificationRepository _notificationRepository;

    public TicketsController(INotificationRepository notificationRepository,INotificationService notificationService, ITicketService ticketService, IMapper mapper, IPhotoService photoService, IUserService userService, EmailService emailService, ICommentService commentService)
    {
      _ticketService = ticketService;
      _mapper = mapper;
      _photoService = photoService;
      _userService = userService;
      _emailService = emailService;
      _commentService = commentService;
      _notificationService = notificationService;
      _notificationRepository = notificationRepository;
    }

    [HttpGet("test-notification")]
    public async Task<IActionResult> TestNotification()
    {
      var notification = new Notification
      {
        Message = "Notification de test",
        DateEnvoi = DateTime.UtcNow,
        UtilisateurId = 1
      };

      await _notificationService.SendNotificationAsync(notification);
      return Ok("Notification envoyée");
    }

    [HttpGet("test-insertion")]
    public async Task<IActionResult> TestInsertion()
    {
      var notification = new Notification
      {
        Message = "Insertion directe de test",
        DateEnvoi = DateTime.UtcNow,
        UtilisateurId = 1
      };

      await _notificationRepository.AddNotificationAsync(notification);

      // Récupérer la notification insérée
      var result = await _notificationRepository.GetNotificationsForUserAsync(1);
      return Ok(result);
    }

    // GET api/tickets?...
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TicketDto>>> GetTickets([FromQuery] UserParams ticketParams)
    {
      // Extraction des informations de l'utilisateur connecté via les claims
      var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
      var roleClaim = HttpContext.User.FindFirst(ClaimTypes.Role);
      if (userIdClaim != null && roleClaim != null)
      {
        ticketParams.UserId = int.Parse(userIdClaim.Value);
        ticketParams.Role = roleClaim.Value;
      }

      var pagedTickets = await _ticketService.GetTicketsPagedAsync(ticketParams);
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

    // POST api/tickets
    [HttpPost]
    public async Task<ActionResult<TicketDto>> CreateTicket([FromForm] TicketCreateDto ticketCreateDto)
    {
      // Vérification de l'unicité du titre du ticket
      if (await _ticketService.TicketExists(ticketCreateDto.Title))
        return BadRequest("Un ticket avec ce titre existe déjà");

      UploadResult uploadResult = null;
      if (ticketCreateDto.Attachment != null)
      {
        uploadResult = await _photoService.UploadFileAsync(ticketCreateDto.Attachment);
        if (uploadResult?.Error != null)
          return BadRequest(uploadResult.Error.Message);
      }

      // Mapper le DTO vers l'entité Ticket et définir la date de création
      var ticket = _mapper.Map<Ticket>(ticketCreateDto);
      ticket.CreatedAt = DateTime.UtcNow;
      ticket.UpdatedAt = null;

      // Récupération du statut par défaut depuis la table des statuts
      var defaultStatus = await _ticketService.GetStatusByNameAsync("—");
      if (defaultStatus == null)
        return BadRequest("Statut par défaut introuvable");
      ticket.StatutId = defaultStatus.Id;

      if (uploadResult?.SecureUrl != null)
      {
        ticket.Attachments = uploadResult.SecureUrl.AbsoluteUri;
      }

      // Ajout du ticket dans le repository
      await _ticketService.AddTicketAsync(ticket);
      await _ticketService.SaveAllAsync();

      // Recharger le ticket avec ses relations (incluant Projet et ChefProjet)
      var ticketFromDb = await _ticketService.GetTicketByIdAsync(ticket.Id);
      if (ticketFromDb == null)
      {
        return NotFound();
      }

      // Envoi d'e-mails en tâche de fond via Hangfire

      // Envoi d'email au chef de projet s'il existe
      var chefProjet = ticketFromDb.Projet?.ChefProjet;
      if (chefProjet != null)
      {
        BackgroundJob.Enqueue(() => _emailService.SendEmailAsync(
            $"{chefProjet.FirstName} {chefProjet.LastName}",
            chefProjet.Email,
            "Nouveau ticket créé",
            $"Bonjour {chefProjet.FirstName} {chefProjet.LastName},<br><br>" +
            $"Le client '{ticket.Owner.FirstName} {ticket.Owner.LastName}' a créé un nouveau ticket intitulé '{ticket.Title}' (n° {ticket.Id}) pour le projet '{ticketFromDb.Projet.Nom}'.",
            chefProjet.Id
        ));
      }

      // Envoi d'email de confirmation au client (celui qui a créé le ticket)
      var client = ticketFromDb.Owner;
      if (client != null)
      {
        BackgroundJob.Enqueue(() => _emailService.SendEmailAsync(
            $"{client.FirstName} {client.LastName}",
            client.Email,
            "Confirmation de création de ticket",
            $"Bonjour {client.FirstName} {client.LastName},<br><br>" +
            $"Votre ticket intitulé '{ticket.Title}' (n° {ticket.Id}) a été créé avec succès. Nous vous remercions pour votre confiance.",
            client.Id
        ));
      }

      // Envoi d'email aux utilisateurs ayant le rôle 'super admin'
      var superAdmins = await _userService.GetUsersByRoleAsync("super admin");
      foreach (var admin in superAdmins)
      {
        BackgroundJob.Enqueue(() => _emailService.SendEmailAsync(
            $"{admin.FirstName} {admin.LastName}",
            admin.Email,
            "Nouveau ticket créé",
            $"Bonjour {admin.FirstName} {admin.LastName},<br><br>" +
            $"Le client '{ticket.Owner.FirstName} {ticket.Owner.LastName}' a créé un nouveau ticket intitulé '{ticket.Title}' (n° {ticket.Id}). Veuillez vérifier les détails dans l'application.",
            admin.Id
        ));
      }

      var resultDto = _mapper.Map<TicketDto>(ticketFromDb);
      return CreatedAtAction(nameof(GetTicket), new { id = ticket.Id }, resultDto);
    }



    [HttpPost("validate/{id}")]
    public async Task<IActionResult> ValidateTicket(int id, [FromBody] TicketValidationDto ticketValidationDto)
    {
      // Récupération du ticket à valider
      var ticket = await _ticketService.GetTicketEntityByIdAsync(id);
      if (ticket == null)
        return NotFound("Ticket non trouvé");

      // Vérification de l'authentification
      var currentUserIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
      var currentUserRoleClaim = HttpContext.User.FindFirst(ClaimTypes.Role);
      if (currentUserIdClaim == null || currentUserRoleClaim == null)
        return Unauthorized("Utilisateur non authentifié");

      var currentUserId = int.Parse(currentUserIdClaim.Value);
      var currentUserRole = currentUserRoleClaim.Value;

      bool isAuthorized = false;
      if (ticket.Projet?.ChefProjet != null && ticket.Projet.ChefProjet.Id == currentUserId)
        isAuthorized = true;
      else if (currentUserRole.ToLower() == "super admin")
        isAuthorized = true;
      if (!isAuthorized)
        return Unauthorized("Vous n'êtes pas autorisé à valider ce ticket.");

      if (ticketValidationDto.IsAccepted)
      {
        // Si accepté, rechercher le statut "Accepté"
        var acceptedStatus = await _ticketService.GetStatusByNameAsync("Accepté");
        if (acceptedStatus == null)
          return BadRequest("Statut 'Accepté' introuvable");

        ticket.StatutId = acceptedStatus.Id;
        ticket.ApprovedAt = DateTime.UtcNow;

        // Envoi d'email d'acceptation au client en tâche de fond
        var client = ticket.Owner;
        if (client != null)
        {
          BackgroundJob.Enqueue(() => _emailService.SendEmailAsync(
              $"{client.FirstName} {client.LastName}",
              client.Email,
              "Ticket accepté",
              $"Bonjour {client.FirstName} {client.LastName},<br><br>" +
              $"Votre ticket '{ticket.Title}' (n°{ticket.Id}) a été accepté.",
              client.Id
          ));
        }

        // Si un responsable est assigné, on met à jour le statut et on envoie un email au responsable
        if (ticketValidationDto.ResponsibleId.HasValue)
        {
          ticket.ResponsibleId = ticketValidationDto.ResponsibleId.Value;
          var inProgressStatus = await _ticketService.GetStatusByNameAsync("En cours");
          if (inProgressStatus == null)
            return BadRequest("Statut 'En cours' introuvable");
          ticket.StatutId = inProgressStatus.Id;

          var responsible = await _userService.GetUserByIdAsync(ticket.ResponsibleId.Value);
          if (responsible != null)
          {
            BackgroundJob.Enqueue(() => _emailService.SendEmailAsync(
                $"{responsible.FirstName} {responsible.LastName}",
                responsible.Email,
                "Nouveau ticket assigné",
                $"Bonjour {responsible.FirstName} {responsible.LastName},<br><br>" +
                $"Le ticket '{ticket.Title}' (n°{ticket.Id}) vous a été assigné.",
                responsible.Id
            ));
          }
        }
      }
      else
      {
        // Si refusé, rechercher le statut "Refusé" et envoyer un email de refus au client
        var refusedStatus = await _ticketService.GetStatusByNameAsync("Refusé");
        if (refusedStatus == null)
          return BadRequest("Statut 'Refusé' introuvable");
        ticket.StatutId = refusedStatus.Id;
        ticket.ValidationReason = ticketValidationDto.Reason;

        var client = ticket.Owner;
        if (client != null)
        {
          BackgroundJob.Enqueue(() => _emailService.SendEmailAsync(
              $"{client.FirstName} {client.LastName}",
              client.Email,
              "Ticket refusé",
              $"Bonjour {client.FirstName} {client.LastName},<br><br>" +
              $"Votre ticket '{ticket.Title}' (n°{ticket.Id}) a été refusé. Raison : {ticket.ValidationReason}",
              client.Id
          ));
        }
      }

      ticket.UpdatedAt = DateTime.UtcNow;

      var updateResult = await _ticketService.UpdateTicketAsync(ticket);
      if (updateResult)
        return NoContent();
      return BadRequest("La validation du ticket a échoué");
    }



    // Endpoint pour l'upload du fichier en arrière-plan
    [HttpPost("upload")]
    public async Task<IActionResult> UploadFile([FromForm] IFormFile file)
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
      // Récupération du ticket
      var ticket = await _ticketService.GetTicketEntityByIdAsync(id);
      if (ticket == null)
        return NotFound("Ticket non trouvé");

      // Vérification de l'authentification et de l'autorisation
      var currentUserIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
      var currentUserRoleClaim = HttpContext.User.FindFirst(ClaimTypes.Role);
      if (currentUserIdClaim == null || currentUserRoleClaim == null)
        return Unauthorized("Utilisateur non authentifié");

      var currentUserId = int.Parse(currentUserIdClaim.Value);
      var currentUserRole = currentUserRoleClaim.Value.ToLower();

      bool isAuthorized = false;
      if (ticket.Projet?.ChefProjet != null && ticket.Projet.ChefProjet.Id == currentUserId)
        isAuthorized = true;
      else if (currentUserRole == "super admin")
        isAuthorized = true;
      else if (ticket.ResponsibleId.HasValue && ticket.ResponsibleId.Value == currentUserId)
        isAuthorized = true;

      if (!isAuthorized)
        return Unauthorized("Vous n'êtes pas autorisé à terminer ce ticket.");

      // Détermination du nouveau statut
      var newStatusName = completionDto.IsResolved ? "Résolu" : "Non Résolu";
      var newStatus = await _ticketService.GetStatusByNameAsync(newStatusName);
      if (newStatus == null)
        return BadRequest($"Statut '{newStatusName}' introuvable");

      ticket.StatutId = newStatus.Id;
      ticket.CompletionComment = completionDto.Comment;
      ticket.HoursSpent = completionDto.HoursSpent;
      ticket.SolvedAt = completionDto.CompletionDate;

      var updateResult = await _ticketService.UpdateTicketAsync(ticket);
      if (!updateResult)
        return BadRequest("La clôture du ticket a échoué");

      // Construction du texte du commentaire s'il existe
      string commentText = !string.IsNullOrWhiteSpace(completionDto.Comment)
                             ? $" Commentaire : {completionDto.Comment}"
                             : "";

      // Envoi d'emails aux différents destinataires en tâche de fond

      // Email au client (Owner)
      if (ticket.Owner != null)
      {
        BackgroundJob.Enqueue(() => _emailService.SendEmailAsync(
            $"{ticket.Owner.FirstName} {ticket.Owner.LastName}",
            ticket.Owner.Email,
            "Ticket terminé",
            $"Bonjour {ticket.Owner.FirstName} {ticket.Owner.LastName},<br><br>" +
            $"Votre ticket '{ticket.Title}' (n°{ticket.Id}) est {ticket.Statut.Name}.{commentText}",
            ticket.Owner.Id
        ));
      }

      // Email au chef de projet
      if (ticket.Projet?.ChefProjet != null)
      {
        BackgroundJob.Enqueue(() => _emailService.SendEmailAsync(
            $"{ticket.Projet.ChefProjet.FirstName} {ticket.Projet.ChefProjet.LastName}",
            ticket.Projet.ChefProjet.Email,
            "Ticket terminé",
            $"Bonjour {ticket.Projet.ChefProjet.FirstName} {ticket.Projet.ChefProjet.LastName},<br><br>" +
            $"Le ticket '{ticket.Title}' (n°{ticket.Id}) du projet '{ticket.Projet.Nom}' est {ticket.Statut.Name}.{commentText}",
            ticket.Projet.ChefProjet.Id
        ));
      }

      // Email au responsable assigné
      if (ticket.Responsible != null)
      {
        BackgroundJob.Enqueue(() => _emailService.SendEmailAsync(
            $"{ticket.Responsible.FirstName} {ticket.Responsible.LastName}",
            ticket.Responsible.Email,
            "Ticket terminé",
            $"Bonjour {ticket.Responsible.FirstName} {ticket.Responsible.LastName},<br><br>" +
            $"Le ticket '{ticket.Title}' (n°{ticket.Id}) qui vous a été assigné est {ticket.Statut.Name}.{commentText}",
            ticket.Responsible.Id
        ));
      }

      // Email à tous les super admins
      var superAdmins = await _userService.GetUsersByRoleAsync("super admin");
      foreach (var admin in superAdmins)
      {
        BackgroundJob.Enqueue(() => _emailService.SendEmailAsync(
            $"{admin.FirstName} {admin.LastName}",
            admin.Email,
            "Ticket terminé",
            $"Bonjour {admin.FirstName} {admin.LastName},<br><br>" +
            $"Le ticket '{ticket.Title}' (n°{ticket.Id}) est {ticket.Statut.Name}.{commentText}",
            admin.Id
        ));
      }

      // Création automatique d'un commentaire
      string resolutionStatus = completionDto.IsResolved ? "résolu" : "non résolu";
      string commentContent = $"Votre ticket est {resolutionStatus}.<br>" +
                              $"Date de début : {ticket.CreatedAt:dd/MM/yyyy HH:mm}<br>" +
                              $"Date de fin : {completionDto.CompletionDate:dd/MM/yyyy HH:mm}<br>" +
                              $"Nombre d'heures : {completionDto.HoursSpent}<br>";
      if (!completionDto.IsResolved && !string.IsNullOrEmpty(completionDto.Comment))
      {
        commentContent += $"Cause : {completionDto.Comment}";
      }

      var commentCreateDto = new CommentCreateDto
      {
        Contenu = commentContent,
        TicketId = ticket.Id
      };

      await _commentService.CreateCommentAsync(commentCreateDto, currentUserId);

      return NoContent();
    }




    [HttpPost("updateResponsible/{id}")]
    public async Task<IActionResult> UpdateResponsible(int id, [FromBody] TicketResponsibleDto responsibleDto)
    {
      var ticket = await _ticketService.GetTicketEntityByIdAsync(id);
      if (ticket == null)
        return NotFound("Erreur : Ticket non trouvé.");

      // Vérification de l'authentification et autorisation
      var currentUserIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
      var currentUserRoleClaim = HttpContext.User.FindFirst(ClaimTypes.Role);
      if (currentUserIdClaim == null || currentUserRoleClaim == null)
        return Unauthorized("Erreur : Utilisateur non authentifié.");

      var currentUserId = int.Parse(currentUserIdClaim.Value);
      var currentUserRole = currentUserRoleClaim.Value.ToLower();
      bool isAuthorized = false;
      if (ticket.Projet?.ChefProjet != null && ticket.Projet.ChefProjet.Id == currentUserId)
        isAuthorized = true;
      else if (currentUserRole == "super admin")
        isAuthorized = true;
      else if (ticket.ResponsibleId.HasValue && ticket.ResponsibleId.Value == currentUserId)
        isAuthorized = true;

      if (!isAuthorized)
        return Unauthorized("Erreur : Vous n'êtes pas autorisé à modifier le responsable.");

      // Vérifier si le responsable a réellement changé
      if (ticket.ResponsibleId.HasValue && ticket.ResponsibleId.Value == responsibleDto.ResponsibleId)
      {
        return BadRequest("Erreur : Le responsable n'a pas été modifié.");
      }

      ticket.ResponsibleId = responsibleDto.ResponsibleId;
      var updateResult = await _ticketService.UpdateTicketAsync(ticket);
      if (!updateResult)
        return BadRequest("Erreur : La mise à jour du responsable a échoué.");

      // Envoi d'email au nouveau responsable en tâche de fond
      var responsible = await _userService.GetUserByIdAsync(ticket.ResponsibleId.Value);
      if (responsible != null)
      {
        BackgroundJob.Enqueue(() => _emailService.SendEmailAsync(
          $"{responsible.FirstName} {responsible.LastName}",
          responsible.Email,
          "Ticket mis à jour - Nouveau responsable assigné",
          $"Bonjour {responsible.FirstName} {responsible.LastName},<br><br>" +
          $"Le ticket '{ticket.Title}' (n°{ticket.Id}) vous a été assigné en tant que responsable.",
          responsible.Id
        ));
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





  }
}
