using System;
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

  public CommentService(
    DataContext context,
    IMapper mapper,
    EmailService emailService,
    IUserService userService,
    NotificationService notifService) 
  {
    _context = context;
    _mapper = mapper;
    _emailService = emailService;
    _userService = userService;
    _notifService = notifService;           
  }

  public async Task<CommentDto> CreateCommentAsync(CommentCreateDto commentCreateDto, int userId)
  {
    // 1) Création en base
    var commentaire = new Commentaire
    {
      Contenu = commentCreateDto.Contenu,
      Date = DateTime.UtcNow,
      TicketId = commentCreateDto.TicketId,
      UtilisateurId = userId
    };

    _context.Commentaires.Add(commentaire);
    if (await _context.SaveChangesAsync() <= 0)
      return null;

    // 2) Chargement du ticket et ses relations utiles
    var ticket = await _context.Tickets
        .Include(t => t.Owner)
        .Include(t => t.Projet).ThenInclude(p => p.ChefProjet)
        .Include(t => t.Responsible)
        .FirstOrDefaultAsync(t => t.Id == commentaire.TicketId);
    if (ticket == null)
      return null;

    // 3) Récupération de l’auteur
    var sender = await _userService.GetUserByIdAsync(userId);
    if (sender == null)
      return null;
    var senderRole = sender.Role?.ToLower();

    // 4) Destinataires selon rôle
    var recipients = new List<(int Id, string Name, string Email)>();
    if (senderRole == "client")
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
    else if (senderRole == "chef de projet")
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
    else if (senderRole == "responsable")
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
    else if (senderRole == "super admin")
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

    // Toujours notifier aussi les super-admins
    var superAdmins = await _userService.GetUsersByRoleAsync("super admin");
    recipients.AddRange(superAdmins.Select(sa =>
        (sa.Id, $"{sa.FirstName} {sa.LastName}", sa.Email)));

    // 5) Sujet et corps de base
    var subject = $"Nouveau commentaire sur le ticket #{ticket.Id}";
    var baseMessage = $"Un nouveau commentaire a été ajouté par {sender.FirstName} {sender.LastName} " +
                      $"au ticket '{ticket.Title}' (n°{ticket.Id}).<br><br>" +
                      $"Contenu : {commentaire.Contenu}";

    // 6) Envoi mail + notifications enrichies
    foreach (var recipient in recipients
             .Where(r => r.Id != userId)                 // exclut l’auteur
             .GroupBy(r => r.Id).Select(g => g.First())) // unique par Id
    {
      // 6.a) Email
      var personalized = $"Bonjour {recipient.Name},<br><br>{baseMessage}";
      await _emailService.SendEmailAsync(
          recipient.Name,
          recipient.Email,
          subject,
          personalized);

      // 6.b) Préparer NotificationDto
      var notifDto = new NotificationDto
      {
        Message = $"Nouveau commentaire sur le ticket #{ticket.Id}.",
        DateEnvoi = DateTime.UtcNow,
        EntityType = "Tickets",
        EntityId = ticket.Id
      };

      // 6.c) Notifications Hangfire
      BackgroundJob.Enqueue(() =>
          _notifService.NotifyRealtimeAsync(recipient.Id, notifDto));
      BackgroundJob.Enqueue(() =>
          _notifService.NotifyPushAsync(recipient.Id, notifDto));
    }

    // 7) Retour du DTO
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
        .Where(c => c.TicketId == ticketId)
        .OrderByDescending(c => c.Date)
        .ToListAsync();

    return _mapper.Map<IEnumerable<CommentDto>>(comments);
  }


}
