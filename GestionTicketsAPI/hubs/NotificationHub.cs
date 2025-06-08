using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace GestionTicketsAPI.hubs
{
  public class NotificationHub : Hub
  {

    private static readonly ConcurrentDictionary<string, HashSet<string>> UserConnections = new();
    public override async Task OnConnectedAsync()
    {
      var httpCtx = Context.GetHttpContext();
      var userId = httpCtx.Request.Query["userId"].FirstOrDefault();
      Console.WriteLine($"[SignalR] Connexion de l'utilisateur : {userId}"); // Log

      if (!string.IsNullOrEmpty(userId))
      {
        await Groups.AddToGroupAsync(Context.ConnectionId, userId);
        Console.WriteLine($"[SignalR] Utilisateur {userId} ajouté au groupe"); // Log
      }
      await base.OnConnectedAsync();
    }


    // Méthode pour envoyer une notification à un utilisateur
    public Task SendNotification(string userId, string message)
    {
      // n’enverra QUE dans le groupe userId
      return Clients.Group(userId).SendAsync("ReceiveNotification", message);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
      // Facultatif : on peut aussi retirer du groupe à la déconnexion
      var userId = Context.GetHttpContext().Request.Query["userId"].FirstOrDefault();
      if (!string.IsNullOrEmpty(userId))
      {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
      }
      await base.OnDisconnectedAsync(exception);
    }

    public async Task<string> GetUserGroup()
    {
      var httpCtx = Context.GetHttpContext();
      var userId = httpCtx?.Request.Query["userId"].FirstOrDefault();
      return $"User_{userId}";
    }
  }
}
