service using System;
using System.Text;
using AutoMapper;
using GestionTicketsAPI.Data;
using GestionTicketsAPI.DTOs;
using GestionTicketsAPI.Entities;
using GestionTicketsAPI.Interfaces;
using Hangfire;
using Microsoft.EntityFrameworkCore;


namespace GestionTicketsAPI.Services;

public class CommentService : ICommentService
{
  private readonly DataContext _context;
  private readonly EmailService _emailService;
  private readonly NotificationService _notifService;
  private readonly IUserService _userService;
  private readonly IMapper _mapper;
  private readonly IPhotoService _photoService;
  private readonly IHttpContextAccessor _httpContextAccessor;

  public CommentService(
    DataContext context,
    IMapper mapper,
    EmailService emailService,
    IUserService userService,
    NotificationService notifService,
    IPhotoService photoService,
    IHttpContextAccessor httpContextAccessor)
  {
    _context = context;
    _mapper = mapper;
    _emailService = emailService;
    _userService = userService;
    _notifService = notifService;
    _photoService = photoService;
    _httpContextAccessor = httpContextAccessor;
  }

  public async Task<CommentDto> CreateCommentAsync(CommentCreateDto commentCreateDto, int userId)
  {
    // 1) Création de l’entité Commentaire (texte vide si null)
    var commentaire = new Commentaire
    {
      Contenu = commentCreateDto.Contenu ?? string.Empty,
      Date = DateTime.UtcNow,
      TicketId = commentCreateDto.TicketId,
      UtilisateurId = userId
    };

    _context.Commentaires.Add(commentaire);
    if (await _context.SaveChangesAsync() <= 0)
      return null;

    // 2) Détermination du baseUrl pour construire les URLs absolues
    var request = _httpContextAccessor.HttpContext.Request;
    var baseUrl = $"{request.Scheme}://{request.Host.Value}";

    // 3) Sauvegarde des fichiers joints et collecte des URLs
    var photoUrls = new List<string>();
    if (commentCreateDto.Files != null && commentCreateDto.Files.Any())
    {
      var uploadsRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "comments");
      Directory.CreateDirectory(uploadsRoot);

      foreach (var file in commentCreateDto.Files)
      {
        if (file.Length > 0)
        {
          var ext = Path.GetExtension(file.FileName);
          var publicId = $"{Guid.NewGuid()}{ext}";
          var fullPath = Path.Combine(uploadsRoot, publicId);

          // Sauvegarde physique du fichier
          using var stream = new FileStream(fullPath, FileMode.Create);
          await file.CopyToAsync(stream);

          // URL absolue pour l’e-mail
          var absoluteUrl = $"{baseUrl}/comments/{publicId}";
          photoUrls.Add(absoluteUrl);

          // Entité Photo (URL relative pour le front)
          _context.Photos.Add(new Photo
          {
            Url = $"/comments/{publicId}",
            PublicId = publicId,
            CommentaireId = commentaire.Id
          });
        }
      }

      await _context.SaveChangesAsync();
    }

    // 4) Chargement du ticket et de ses relations
    var ticket = await _context.Tickets
        .Include(t => t.Owner)
        .Include(t => t.Projet).ThenInclude(p => p.ChefProjet)
        .Include(t => t.Responsible)
        .FirstOrDefaultAsync(t => t.Id == commentaire.TicketId);
    if (ticket == null)
      return null;

    // 5) Récupération de l’auteur et détermination des destinataires
    var sender = await _userService.GetUserByIdAsync(userId);
    if (sender == null)
      return null;
    var role = sender.Role?.ToLower() ?? "";
    var recipients = new List<(int Id, string Name, string Email)>();

    if (role == "client")
    {
      if (ticket.Projet?.ChefProjet != null)
        recipients.Add((ticket.Projet.ChefProjet.Id,
                        $"{ticket.Projet.ChefProjet.FirstName} {ticket.Projet.ChefProjet.LastName}",
                        ticket.Projet.ChefProjet.Email));
      if (ticket.Responsible != null)
        recipients.Add((ticket.Responsible.Id,
                        $"{ticket.Responsible.FirstName} {ticket.Responsible.LastName}",
                        ticket.Responsible.Email));
    }
    else if (role == "chef de projet")
    {
      if (ticket.Owner != null)
        recipients.Add((ticket.Owner.Id,
                        $"{ticket.Owner.FirstName} {ticket.Owner.LastName}",
                        ticket.Owner.Email));
      if (ticket.Responsible != null)
        recipients.Add((ticket.Responsible.Id,
                        $"{ticket.Responsible.FirstName} {ticket.Responsible.LastName}",
                        ticket.Responsible.Email));
    }
    else if (role == "responsable")
    {
      if (ticket.Owner != null)
        recipients.Add((ticket.Owner.Id,
                        $"{ticket.Owner.FirstName} {ticket.Owner.LastName}",
                        ticket.Owner.Email));
      if (ticket.Projet?.ChefProjet != null)
        recipients.Add((ticket.Projet.ChefProjet.Id,
                        $"{ticket.Projet.ChefProjet.FirstName} {ticket.Projet.ChefProjet.LastName}",
                        ticket.Projet.ChefProjet.Email));
    }
    else if (role == "super admin")
    {
      if (ticket.Owner != null)
        recipients.Add((ticket.Owner.Id,
                        $"{ticket.Owner.FirstName} {ticket.Owner.LastName}",
                        ticket.Owner.Email));
      if (ticket.Projet?.ChefProjet != null)
        recipients.Add((ticket.Projet.ChefProjet.Id,
                        $"{ticket.Projet.ChefProjet.FirstName} {ticket.Projet.ChefProjet.LastName}",
                        ticket.Projet.ChefProjet.Email));
      if (ticket.Responsible != null)
        recipients.Add((ticket.Responsible.Id,
                        $"{ticket.Responsible.FirstName} {ticket.Responsible.LastName}",
                        ticket.Responsible.Email));
    }

    // Toujours notifier les super-admins
    var superAdmins = await _userService.GetUsersByRoleAsync("super admin");
    recipients.AddRange(superAdmins.Select(sa =>
        (sa.Id, $"{sa.FirstName} {sa.LastName}", sa.Email)));

    // 6) Préparation du sujet et du corps de l’e-mail
    var subject = $"Nouveau commentaire sur le ticket #{ticket.Id}";
    var sb = new StringBuilder();
    sb.Append($"Un nouveau commentaire a été ajouté par {sender.FirstName} {sender.LastName} " +
              $"au ticket '<strong>{ticket.Title}</strong>' (n°{ticket.Id}).<br/><br>");

    if (!string.IsNullOrWhiteSpace(commentaire.Contenu))
      sb.Append($"<strong>Contenu :</strong> {commentaire.Contenu}<br/>");

    if (photoUrls.Any())
    {
      sb.Append("<br/><strong>Fichiers joints :</strong><ul>");
      foreach (var url in photoUrls)
      {
        var name = Path.GetFileName(url);
        sb.AppendFormat("<li><a href=\"{0}\" target=\"_blank\">{1}</a></li>", url, name);
      }
      sb.Append("</ul>");
    }

    var bodyHtml = sb.ToString();

    // 7) Envoi des e-mails et notifications
    foreach (var recipient in recipients
             .Where(r => r.Id != userId)
             .GroupBy(r => r.Id).Select(g => g.First()))
    {
      var personalized = $"Bonjour {recipient.Name},<br/><br>{bodyHtml}";
      await _emailService.SendEmailAsync(recipient.Name, recipient.Email, subject, personalized);

      var notifDto = new NotificationDto
      {
        Message = $"Nouveau commentaire sur le ticket #{ticket.Id}.",
        DateEnvoi = DateTime.UtcNow,
        EntityType = "Tickets",
        EntityId = ticket.Id
      };
      BackgroundJob.Enqueue(() => _notifService.NotifyRealtimeAsync(recipient.Id, notifDto));
      BackgroundJob.Enqueue(() => _notifService.NotifyPushAsync(recipient.Id, notifDto));
    }

    // 8) Retour du DTO
    return new CommentDto
    {
      Id = commentaire.Id,
      Contenu = commentaire.Contenu,
      Date = commentaire.Date,
      UtilisateurId = commentaire.UtilisateurId,
      TicketId = commentaire.TicketId
    };
  }



  public async Task<CommentDto> GetCommentByIdAsync(int id)
  {
    var comment = await _context.Commentaires.FindAsync(id);
    if (comment == null)
      return null;

    return new CommentDto
    {
      Id = comment.Id,
      Contenu = comment.Contenu,
      Date = comment.Date,
      UtilisateurId = comment.UtilisateurId,
      TicketId = comment.TicketId
    };
  }

  public async Task<IEnumerable<CommentDto>> GetCommentsByTicketAsync(int ticketId)
  {
    var comments = await _context.Commentaires
        .Include(c => c.Utilisateur)
          .ThenInclude(p => p.Role)
        .Include(c => c.Photos)
        .Where(c => c.TicketId == ticketId)
        .OrderByDescending(c => c.Date)
        .ToListAsync();

    return _mapper.Map<IEnumerable<CommentDto>>(comments);
  }


}