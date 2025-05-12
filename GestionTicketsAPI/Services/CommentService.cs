using System;
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
  private readonly IClientService _clientService;
  private readonly IMapper _mapper;
  private readonly IPhotoService _photoService;
  private readonly IHttpContextAccessor _httpContextAccessor;

  public CommentService(
    DataContext context,
    IMapper mapper,
    EmailService emailService,
    IUserService userService,
    IClientService clientService,
    NotificationService notifService,
    IPhotoService photoService,
    IHttpContextAccessor httpContextAccessor)
  {
    _context = context;
    _mapper = mapper;
    _emailService = emailService;
    _userService = userService;
    _clientService = clientService;
    _notifService = notifService;
    _photoService = photoService;
    _httpContextAccessor = httpContextAccessor;
  }

  public async Task<CommentDto> CreateCommentAsync(CommentCreateDto dto, int? userId = null, int? clientId = null)
  {
    bool isClient = clientId.HasValue;

    var commentaire = new Commentaire
    {
      Contenu = dto.Contenu ?? string.Empty,
      Date = DateTime.UtcNow,
      TicketId = dto.TicketId,
      UserId = isClient ? null : userId,
      ClientId = isClient ? clientId : null
    };
    _context.Commentaires.Add(commentaire);
    await _context.SaveChangesAsync();

    var photoUrls = new List<string>();
    if (dto.Files?.Any() == true)
    {
      var request = _httpContextAccessor.HttpContext.Request;
      var baseUrl = $"{request.Scheme}://{request.Host.Value}";
      var uploadsRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "comments");
      Directory.CreateDirectory(uploadsRoot);

      foreach (var file in dto.Files)
      {
        if (file.Length > 0)
        {
          var ext = Path.GetExtension(file.FileName);
          var publicId = $"{Guid.NewGuid()}{ext}";
          var fullPath = Path.Combine(uploadsRoot, publicId);
          using var stream = new FileStream(fullPath, FileMode.Create);
          await file.CopyToAsync(stream);

          var absoluteUrl = $"{baseUrl}/comments/{publicId}";
          photoUrls.Add(absoluteUrl);

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

    var ticket = await _context.Tickets
        .Include(t => t.Owner)
        .Include(t => t.Projet).ThenInclude(p => p.ChefProjet)
        .Include(t => t.Responsible)
        .FirstOrDefaultAsync(t => t.Id == commentaire.TicketId);
    if (ticket == null)
      return null;

    string authorName, authorEmail;
    if (isClient)
    {
      var client = await _clientService.GetClientByIdAsync(clientId.Value);
      authorName = $"{client.FirstName} {client.LastName}";
      authorEmail = client.Email;
    }
    else
    {
      var user = await _userService.GetUserByIdAsync(userId.Value);
      authorName = $"{user.FirstName} {user.LastName}";
      authorEmail = user.Email;
    }

    var recipients = new List<(int Id, string Name, string Email)>();
    if (isClient)
    {
      // client comments go to project manager and responsible
      if (ticket.Projet?.ChefProjet != null)
        recipients.Add((ticket.Projet.ChefProjet.Id,
                        $"{ticket.Projet.ChefProjet.FirstName} {ticket.Projet.ChefProjet.LastName}",
                        ticket.Projet.ChefProjet.Email));
      if (ticket.Responsible != null)
        recipients.Add((ticket.Responsible.Id,
                        $"{ticket.Responsible.FirstName} {ticket.Responsible.LastName}",
                        ticket.Responsible.Email));
    }
    else
    {
      // user comments: depending on their role stored in user.Role
      var user = await _userService.GetUserByIdAsync(userId.Value);
      var role = user.Role?.ToLower() ?? string.Empty;
      if (role == "chef de projet")
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
        // super admin notifies everyone involved
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
    }

    var superAdmins = await _userService.GetUsersByRoleAsync("super admin");
    recipients.AddRange(superAdmins.Select(sa =>
        (sa.Id, $"{sa.FirstName} {sa.LastName}", sa.Email)));

    string subject = $"Nouveau commentaire sur le ticket #{ticket.Id}";
    var sb = new StringBuilder();
    sb.Append($"Un nouveau commentaire a été ajouté par {authorName} au ticket '<strong>{ticket.Title}</strong>' (n°{ticket.Id}).<br/><br>");
    if (!string.IsNullOrWhiteSpace(commentaire.Contenu))
      sb.Append($"<strong>Contenu :</strong> {commentaire.Contenu}<br/>");
    if (photoUrls.Any())
    {
      sb.Append("<br/><strong>Fichiers joints :</strong><ul>");
      foreach (var url in photoUrls)
        sb.AppendFormat("<li><a href=\"{0}\" target=\"_blank\">{1}</a></li>", url, Path.GetFileName(url));
      sb.Append("</ul>");
    }
    var bodyHtml = sb.ToString();

    foreach (var r in recipients.Where(r => r.Email != authorEmail).GroupBy(r => r.Id).Select(g => g.First()))
    {
      var personalized = $"Bonjour {r.Name},<br/><br>{bodyHtml}";
      await _emailService.SendEmailAsync(r.Name, r.Email, subject, personalized);
      var notifDto = new NotificationDto
      {
        Message = $"Nouveau commentaire sur le ticket #{ticket.Id}.",
        DateEnvoi = DateTime.UtcNow,
        EntityType = "Tickets",
        EntityId = ticket.Id
      };
      BackgroundJob.Enqueue(() => _notifService.NotifyAsync(r.Id, null, notifDto));
    }

    return new CommentDto
    {
      Id = commentaire.Id,
      Contenu = commentaire.Contenu,
      Date = commentaire.Date,
      UserId = commentaire.UserId,
      ClientId = commentaire.ClientId,
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
      UserId = comment.UserId,
      TicketId = comment.TicketId
    };
  }

  public async Task<IEnumerable<CommentDto>> GetCommentsByTicketAsync(int ticketId)
  {
    var comments = await _context.Commentaires
        .Include(c => c.User)
          .ThenInclude(p => p.Role)
        .Include(c => c.Photos)
        .Where(c => c.TicketId == ticketId)
        .OrderByDescending(c => c.Date)
        .ToListAsync();

    return _mapper.Map<IEnumerable<CommentDto>>(comments);
  }


}
