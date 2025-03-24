using System;
using AutoMapper;
using GestionTicketsAPI.Data;
using GestionTicketsAPI.DTOs;
using GestionTicketsAPI.Entities;
using GestionTicketsAPI.Interfaces;
using Microsoft.EntityFrameworkCore;


namespace GestionTicketsAPI.Services;

public class CommentService : ICommentService
{
  private readonly DataContext _context;
  private readonly EmailService _emailService;
  private readonly IUserService _userService;
  private readonly IMapper _mapper;

  public CommentService(DataContext context, IMapper mapper, EmailService emailService, IUserService userService)
  {
    _context = context;
    _emailService = emailService;
    _userService = userService;
    _mapper = mapper;
  }

  public async Task<CommentDto> CreateCommentAsync(CommentCreateDto commentCreateDto, int userId)
  {
    // Création du commentaire
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

    // Chargement du ticket associé avec ses relations (Owner, Projet avec ChefProjet, Responsible)
    var ticket = await _context.Tickets
        .Include(t => t.Owner)
        .Include(t => t.Projet)
            .ThenInclude(p => p.ChefProjet)
        .Include(t => t.Responsible)
        .FirstOrDefaultAsync(t => t.Id == commentaire.TicketId);

    if (ticket == null)
      return null;

    // Récupérer l'utilisateur qui a créé le commentaire
    var user = await _userService.GetUserByIdAsync(userId);
    if (user == null)
      return null;

    // Liste pour stocker les destinataires avec leur nom complet et email
    var recipients = new List<(int Id,string Name, string Email)>();

    // Détermination des destinataires à notifier en fonction du rôle de l'auteur
    var senderRole = user.Role?.ToLower();

    if (senderRole == "client")
    {
      // Le client notifie le chef de projet, le responsable et les super admins
      if (ticket.Projet?.ChefProjet != null && !string.IsNullOrEmpty(ticket.Projet.ChefProjet.Email))
        recipients.Add((ticket.Projet.ChefProjet.Id, ticket.Projet.ChefProjet.FirstName + " " + ticket.Projet.ChefProjet.LastName, ticket.Projet.ChefProjet.Email));
      if (ticket.Responsible != null && !string.IsNullOrEmpty(ticket.Responsible.Email))
        recipients.Add((ticket.Responsible.Id, ticket.Responsible.FirstName + " " + ticket.Responsible.LastName, ticket.Responsible.Email));

      var superAdmins = await _userService.GetUsersByRoleAsync("super admin");
      recipients.AddRange(superAdmins.Select(sa => (sa.Id, sa.FirstName + " " + sa.LastName, sa.Email)));
    }
    else if (senderRole == "chef de projet")
    {
      // Le chef de projet notifie le client, le responsable et les super admins
      if (ticket.Owner != null && !string.IsNullOrEmpty(ticket.Owner.Email))
        recipients.Add((ticket.Owner.Id, ticket.Owner.FirstName + " " + ticket.Owner.LastName, ticket.Owner.Email));
      if (ticket.Responsible != null && !string.IsNullOrEmpty(ticket.Responsible.Email))
        recipients.Add((ticket.Responsible.Id, ticket.Responsible.FirstName + " " + ticket.Responsible.LastName, ticket.Responsible.Email));

      var superAdmins = await _userService.GetUsersByRoleAsync("super admin");
      recipients.AddRange(superAdmins.Select(sa => (sa.Id, sa.FirstName + " " + sa.LastName, sa.Email)));
    }
    else if (senderRole == "responsable")
    {
      // Le responsable notifie le client, le chef de projet et les super admins
      if (ticket.Owner != null && !string.IsNullOrEmpty(ticket.Owner.Email))
        recipients.Add((ticket.Owner.Id, ticket.Owner.FirstName + " " + ticket.Owner.LastName, ticket.Owner.Email));
      if (ticket.Projet?.ChefProjet != null && !string.IsNullOrEmpty(ticket.Projet.ChefProjet.Email))
        recipients.Add((ticket.Projet.ChefProjet.Id, ticket.Projet.ChefProjet.FirstName + " " + ticket.Projet.ChefProjet.LastName, ticket.Projet.ChefProjet.Email));

      var superAdmins = await _userService.GetUsersByRoleAsync("super admin");
      recipients.AddRange(superAdmins.Select(sa => (sa.Id, sa.FirstName + " " + sa.LastName, sa.Email)));
    }
    else if (senderRole == "super admin")
    {
      // Le super admin notifie le client, le chef de projet et le responsable
      if (ticket.Owner != null && !string.IsNullOrEmpty(ticket.Owner.Email))
        recipients.Add((ticket.Owner.Id, ticket.Owner.FirstName + " " + ticket.Owner.LastName, ticket.Owner.Email));
      if (ticket.Projet?.ChefProjet != null && !string.IsNullOrEmpty(ticket.Projet.ChefProjet.Email))
        recipients.Add((ticket.Projet.ChefProjet.Id, ticket.Projet.ChefProjet.FirstName + " " + ticket.Projet.ChefProjet.LastName, ticket.Projet.ChefProjet.Email));
      if (ticket.Responsible != null && !string.IsNullOrEmpty(ticket.Responsible.Email))
        recipients.Add((ticket.Responsible.Id, ticket.Responsible.FirstName + " " + ticket.Responsible.LastName, ticket.Responsible.Email));
    }
    // D'autres cas peuvent être ajoutés selon vos besoins

    // Préparation du sujet et du message de base sans salutation
    var subject = $"Nouveau commentaire sur le ticket #{ticket.Id}";
    var baseMessage = $"Un nouveau commentaire a été ajouté par {user.FirstName} {user.LastName} sur le ticket '{ticket.Title}' (n° {ticket.Id}).<br><br>Contenu : {commentaire.Contenu}";

    // Envoi des notifications par email en évitant les doublons (basé sur l'email)
    foreach (var recipient in recipients.GroupBy(r => r.Email).Select(g => g.First()))
    {
      // Ajout de la salutation personnalisée pour chaque destinataire
      var personalizedMessage = $"Bonjour {recipient.Name},<br><br>" + baseMessage;
      await _emailService.SendEmailAsync(recipient.Name, recipient.Email, subject, personalizedMessage, recipient.Id);
    }

    // Retourner le DTO du commentaire créé
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
