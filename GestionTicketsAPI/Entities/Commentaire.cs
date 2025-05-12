using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestionTicketsAPI.Entities;

public class Commentaire
{
  [Key]
  public int Id { get; set; }

  public string? Contenu { get; set; } = string.Empty;

  [Required]
  public DateTime Date { get; set; } = DateTime.UtcNow;

  // -> soit un User, soit un Client
  public int? UserId { get; set; }
  [ForeignKey(nameof(UserId))]
  public User? User { get; set; }

  public int? ClientId { get; set; }
  [ForeignKey(nameof(ClientId))]
  public Client? Client { get; set; }

  [ForeignKey("Ticket")]
  public int TicketId { get; set; }

  public Ticket? Ticket { get; set; }
  public ICollection<Photo> Photos { get; set; } = new List<Photo>();
}
