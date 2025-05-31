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

  public async Task<CommentDto> CreateCommentAsync(
    CommentCreateDto dto,
    int? userId = null,
    int? clientId = null)
{
    // Récupère baseUrl dès le début pour l’utiliser partout
    var req = _httpContextAccessor.HttpContext!.Request;
    var baseUrl = $"{req.Scheme}://{req.Host.Value}";

    // 1) Création de l’entité Commentaire
    bool isClient = clientId.HasValue;
    var com = new Commentaire
    {
        Contenu   = dto.Contenu ?? string.Empty,
        Date      = DateTime.UtcNow,
        TicketId  = dto.TicketId,
        UserId    = isClient ? (int?)null : userId,
        ClientId  = isClient ? clientId : null
    };
    _context.Commentaires.Add(com);
    await _context.SaveChangesAsync();

    // 2) Sauvegarde des fichiers et collecte des URLs publiques
    var photoUrls = new List<string>();
    if (dto.Files?.Any() == true)
    {
        var root = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "comments");
        Directory.CreateDirectory(root);

        foreach (var f in dto.Files!)
        {
            if (f.Length == 0) continue;
            var ext      = Path.GetExtension(f.FileName);
            var publicId = $"{Guid.NewGuid()}{ext}";
            var fullPath = Path.Combine(root, publicId);

            await using var fs = new FileStream(fullPath, FileMode.Create);
            await f.CopyToAsync(fs);

            // URL relative stockée en base, URL absolue envoyée par mail/DTO
            var publicUrl = $"/comments/{publicId}";
            photoUrls.Add($"{baseUrl}{publicUrl}");
            _context.Photos.Add(new Photo
            {
                Url           = publicUrl,
                PublicId      = publicId,
                CommentaireId = com.Id
            });
        }
        await _context.SaveChangesAsync();
    }

    // 3) Chargement complet du ticket (pour mails/notifications)
    var ticket = await _context.Tickets
        .Include(t => t.Owner)
        .Include(t => t.Responsible)
        .Include(t => t.Projet).ThenInclude(p => p.ChefProjet)
        .FirstOrDefaultAsync(t => t.Id == com.TicketId)
        ?? throw new InvalidOperationException("Ticket introuvable.");

    // 4) Détermination de l’auteur (nom + email)
    string authorName, authorEmail;
    if (isClient)
    {
        var client = await _clientService.GetClientByIdAsync(clientId!.Value);
        authorName  = $"{client.FirstName} {client.LastName}";
        authorEmail = client.Email!;
    }
    else
    {
        var user = await _userService.GetUserByIdAsync(userId!.Value)
                    ?? throw new InvalidOperationException("Utilisateur introuvable.");
        authorName  = $"{user.FirstName} {user.LastName}";
        authorEmail = user.Email!;
    }

    // 5) Construction de la liste des destinataires
    var recipients = new List<(int Id, string Name, string Email)>();
    if (isClient)
    {
        if (ticket.Projet?.ChefProjet != null)
            recipients.Add((ticket.Projet.ChefProjet.Id,
                            $"{ticket.Projet.ChefProjet.FirstName} {ticket.Projet.ChefProjet.LastName}",
                            ticket.Projet.ChefProjet.Email!));
        if (ticket.Responsible != null)
            recipients.Add((ticket.Responsible.Id,
                            $"{ticket.Responsible.FirstName} {ticket.Responsible.LastName}",
                            ticket.Responsible.Email!));
    }
    else
    {
        var user = await _userService.GetUserByIdAsync(userId!.Value)!;
        var role = user.Role?.ToLower() ?? "";
        if (role == "chef de projet")
        {
            if (ticket.Owner != null)    recipients.Add((ticket.Owner.Id,    $"{ticket.Owner.FirstName} {ticket.Owner.LastName}",    ticket.Owner.Email!));
            if (ticket.Responsible != null) recipients.Add((ticket.Responsible.Id, $"{ticket.Responsible.FirstName} {ticket.Responsible.LastName}", ticket.Responsible.Email!));
        }
        else if (role == "responsable")
        {
            if (ticket.Owner != null)      recipients.Add((ticket.Owner.Id,      $"{ticket.Owner.FirstName} {ticket.Owner.LastName}",      ticket.Owner.Email!));
            if (ticket.Projet?.ChefProjet != null) recipients.Add((ticket.Projet.ChefProjet.Id, $"{ticket.Projet.ChefProjet.FirstName} {ticket.Projet.ChefProjet.LastName}", ticket.Projet.ChefProjet.Email!));
        }
        else if (role == "super admin")
        {
            if (ticket.Owner != null)      recipients.Add((ticket.Owner.Id,      $"{ticket.Owner.FirstName} {ticket.Owner.LastName}",      ticket.Owner.Email!));
            if (ticket.Projet?.ChefProjet != null) recipients.Add((ticket.Projet.ChefProjet.Id, $"{ticket.Projet.ChefProjet.FirstName} {ticket.Projet.ChefProjet.LastName}", ticket.Projet.ChefProjet.Email!));
            if (ticket.Responsible != null) recipients.Add((ticket.Responsible.Id, $"{ticket.Responsible.FirstName} {ticket.Responsible.LastName}", ticket.Responsible.Email!));
        }
    }
    var superAdmins = await _userService.GetUsersByRoleAsync("super admin");
    recipients.AddRange(superAdmins.Select(sa => (sa.Id, $"{sa.FirstName} {sa.LastName}", sa.Email!)));

    // 6) Préparation du mail et des notifications
    string subject = $"Nouveau commentaire sur le ticket #{ticket.Id}";
    var sb = new StringBuilder();
    sb.Append($"Un nouveau commentaire de <strong>{authorName}</strong> sur « {ticket.Title} » (#{ticket.Id}).<br/><br>");
    if (!string.IsNullOrWhiteSpace(com.Contenu))
        sb.Append($"<strong>Contenu :</strong> {com.Contenu}<br/>");
    if (photoUrls.Any())
    {
        sb.Append("<br/><strong>Pièces jointes :</strong><ul>");
        foreach (var url in photoUrls)
            sb.Append($"<li><a href=\"{url}\" target=\"_blank\">{Path.GetFileName(url)}</a></li>");
        sb.Append("</ul>");
    }
    var bodyHtml = sb.ToString();

    foreach (var r in recipients.Where(r => r.Email != authorEmail).DistinctBy(r => r.Email))
    {
        BackgroundJob.Enqueue(() =>
            _emailService.SendEmailAsync(r.Name, r.Email, subject, $"Bonjour {r.Name},<br/><br>{bodyHtml}")
        );
        var notifDto = new NotificationDto
        {
            Message   = $"Nouveau commentaire sur le ticket #{ticket.Id}.",
            DateEnvoi = DateTime.UtcNow,
            EntityType= "Tickets",
            EntityId  = ticket.Id
        };
        BackgroundJob.Enqueue(() =>
            _notifService.NotifyAsync(r.Id, null, notifDto)
        );
    }

    // 7) Construction finale du CommentDto à retourner
    var dtoResult = new CommentDto
    {
        Id          = com.Id,
        Contenu     = com.Contenu,
        Date        = com.Date,
        UserId      = com.UserId,
        ClientId    = com.ClientId,
        TicketId    = com.TicketId,
        Utilisateur = !isClient
            ? _mapper.Map<UserDto>(await _userService.GetUserByIdAsync(userId!.Value))
            : null,
        Client      =  isClient
            ? _mapper.Map<ClientDto>(await _clientService.GetClientByIdAsync(clientId!.Value))
            : null,
        Photos      = photoUrls
            .Select(url => new PhotoDto { Url = url.Replace(baseUrl, "") })
            .ToList()
    };

    return dtoResult;
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

    var dtos = new List<CommentDto>();
  foreach (var com in comments)
  {
    bool isClient = com.ClientId.HasValue;
    var dto = new CommentDto
    {
      Id = com.Id,
      Contenu = com.Contenu,
      Date = com.Date,
      UserId = com.UserId,
      ClientId = com.ClientId,
      TicketId = com.TicketId,
      Utilisateur = !isClient ? _mapper.Map<UserDto>(com.User!) : null,
      Client = isClient ? _mapper.Map<ClientDto>(await _clientService.GetClientByIdAsync(com.ClientId!.Value)) : null,
      Photos      = com.Photos?.Select(p => new PhotoDto { Url = p.Url }).ToList()
    };
    dtos.Add(dto);
  }
  return dtos;
  }


}
