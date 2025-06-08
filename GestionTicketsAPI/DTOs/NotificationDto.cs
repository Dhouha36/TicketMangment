using System;

namespace GestionTicketsAPI.DTOs;

public class NotificationDto
{
  public int Id { get; set; }
  public string Message { get; set; } = string.Empty;
  public DateTime DateEnvoi { get; set; }

  public bool IsRead { get; set; }

  public string? EntityType { get; set; }
  public int? EntityId { get; set; }
  public int? UserId { get; set; }
  public int? ClientId { get; set; }
}
