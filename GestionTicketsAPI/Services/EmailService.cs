
using Hangfire;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using GestionTicketsAPI.Entities;
using GestionTicketsAPI.Interfaces;

namespace GestionTicketsAPI.Services
{
  public class EmailService
  {
    private readonly INotificationService _notificationService;

    public EmailService(INotificationService notificationService)
    {
      _notificationService = notificationService;
    }

    /// <summary>
    /// Envoie un e-mail et programme l'envoi d'une notification associée en tâche de fond.
    /// </summary>
    /// <param name="name">Nom du destinataire.</param>
    /// <param name="email">Adresse e-mail du destinataire.</param>
    /// <param name="subject">Sujet de l'e-mail.</param>
    /// <param name="body">Contenu de l'e-mail (au format HTML).</param>
    /// <param name="utilisateurId">Identifiant de l'utilisateur concerné (facultatif).</param>
    /// <returns>True si l'e-mail a été envoyé, sinon false.</returns>
    public async Task<bool> SendEmailAsync(string name, string email, string subject, string body, int utilisateurId)
    {
      try
      {
        // Création du message e-mail
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Simsoft Technologies", "simsoft2021@gmail.com"));
        message.To.Add(new MailboxAddress(name, email));
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder
        {
          HtmlBody = $"<p>{body}</p>"
        };
        message.Body = bodyBuilder.ToMessageBody();

        // Envoi de l'e-mail via SMTP avec MailKit
        using var client = new SmtpClient();
        await client.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
        await client.AuthenticateAsync("drgaieg@gmail.com", "awtz msbn rhof rrwf"); // Utilisez un App Password
        await client.SendAsync(message);
        await client.DisconnectAsync(true);

        // Utiliser le même contenu (body) pour la notification
        var notification = new Notification
        {
          Message = body,  // Le contenu de l'e-mail devient le contenu de la notification
          DateEnvoi = DateTime.UtcNow,
          UtilisateurId = utilisateurId
        };

        // Envoi de la notification en tâche de fond via Hangfire
        BackgroundJob.Enqueue(() => _notificationService.SendNotificationAsync(notification));

        return true;
      }
      catch (Exception)
      {
        // Logger l'exception au besoin
        return false;
      }
    }

  }
}
