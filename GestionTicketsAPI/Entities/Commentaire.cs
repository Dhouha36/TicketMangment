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

    [ForeignKey("Utilisateur")]
    public int UtilisateurId { get; set; }

    public User? Utilisateur { get; set; }

    [ForeignKey("Ticket")]
    public int TicketId { get; set; }

    public Ticket? Ticket { get; set; }
    public ICollection<Photo> Photos { get; set; } = new List<Photo>();
}
