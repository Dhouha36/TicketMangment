using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestionTicketsAPI.Entities;

public class Notification
{
  [Key]
  public int Id { get; set; }

  [Required]
  public string Message { get; set; } = string.Empty;

  [Required]
  public DateTime DateEnvoi { get; set; } = DateTime.UtcNow;

  // Auteur : User ou Client
  public int? UserId { get; set; }
  [ForeignKey(nameof(UserId))]
  public User? User { get; set; }

  public int? ClientId { get; set; }
  [ForeignKey(nameof(ClientId))]
  public Client? Client { get; set; }
  public bool IsRead { get; set; } = false;
  public bool IsDeleted { get; set; } = false;

  public string? EntityType { get; set; }   // ex: "Projets", "Tickets", "Societes"
  public int? EntityId { get; set; }
}
