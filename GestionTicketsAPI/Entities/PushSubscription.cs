using System;

namespace GestionTicketsAPI.Entities;

public class PushSubscription
{
    public int Id { get; set; }
    public string Endpoint { get; set; }
    public string P256DH { get; set; }
    public string Auth { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
