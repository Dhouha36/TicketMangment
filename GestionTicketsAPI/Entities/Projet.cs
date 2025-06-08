using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestionTicketsAPI.Entities
{
  public class Projet
  {
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Nom { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int? ChefProjetId { get; set; }
    public User? ChefProjet { get; set; }

    public int? SocieteId { get; set; }

    // Utilisation d'un champ privé pour la société
    private Societe? _societe;
    public Societe? Societe
    {
      get => _societe;
      set
      {
        _societe = value;
        // Synchronisation de la propriété IdPays avec le pays de la société
        IdPays = _societe?.PaysId;
      }
    }

    // Maintenant mappée en base avec un setter
    [ForeignKey("Pays")]
    [Column("id_pays")]
    public int? IdPays { get; set; }

    public Pays? Pays { get; set; }

    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public ICollection<Ticket>? Tickets { get; set; }
    public ICollection<ProjetUser> ProjetUsers { get; set; } = new List<ProjetUser>();
    public ICollection<ProjetClient> ProjetClients { get; set; } = new List<ProjetClient>();
    public ContratProjet? ContratProjet { get; set; }
  }
}
