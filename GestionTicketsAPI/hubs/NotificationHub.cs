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
        var clientId = httpCtx.Request.Query["clientId"].FirstOrDefault();
        
        //  Gérer les connexions User ET Client
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, userId);
            Console.WriteLine($"[SignalR] User {userId} ajouté au groupe");
        }
        
        if (!string.IsNullOrEmpty(clientId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"client_{clientId}");
            Console.WriteLine($"[SignalR] Client {clientId} ajouté au groupe client_{clientId}");
        }
        
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var httpCtx = Context.GetHttpContext();
        var userId = httpCtx?.Request.Query["userId"].FirstOrDefault();
        var clientId = httpCtx?.Request.Query["clientId"].FirstOrDefault();
        
        //  Retirer des groupes User ET Client
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
        }
        
        if (!string.IsNullOrEmpty(clientId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"client_{clientId}");
        }
        
        await base.OnDisconnectedAsync(exception);
    }

    // Méthode pour envoyer une notification à un utilisateur
    public Task SendNotification(string userId, string message)
    {
        return Clients.Group(userId).SendAsync("ReceiveNotification", message);
    }
    
    //  AJOUTER: Méthode pour envoyer une notification à un client
    public Task SendNotificationToClient(string clientId, string message)
    {
        return Clients.Group($"client_{clientId}").SendAsync("ReceiveNotification", message);
    }
}
}
