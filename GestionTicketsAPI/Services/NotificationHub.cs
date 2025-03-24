using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using GestionTicketsAPI.Interfaces;

namespace GestionTicketsAPI.Services
{
  public class NotificationHub : Hub
  {
    private readonly INotificationService _notificationService;

    public NotificationHub(INotificationService notificationService)
    {
      _notificationService = notificationService;
    }

    public override async Task OnConnectedAsync()
    {
      var claimsList = Context.User?.Claims.Select(c => $"{c.Type}: {c.Value}");
      Console.WriteLine("Claims du client connecté : " + (claimsList != null ? string.Join(", ", claimsList) : "Aucun"));

      var utilisateurId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
      Console.WriteLine("Utilisateur connecté (via Hub) : " + (utilisateurId ?? "null"));

      if (int.TryParse(utilisateurId, out int id))
      {
        await _notificationService.SendPendingNotificationsAsync(id);
      }
      await base.OnConnectedAsync();
    }



  }

}
