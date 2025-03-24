using System;

namespace GestionTicketsAPI.Helpers;

using Microsoft.AspNetCore.SignalR;

using System.Security.Claims;

public class NameUserIdProvider : IUserIdProvider
{
    public string GetUserId(HubConnectionContext connection)
    {
        // Utiliser le claim NameIdentifier qui correspond au token
        return connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}


