using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestionTicketsAPI.Entities;
public enum TypeContrat
{
    CDD,
    CDI,
    Projet
}

public class Contrat
{
    [Key]
    public int Id { get; set; }

    [Required]
    public DateTime DateDebut { get; set; }

    public DateTime? DateFin { get; set; }

    [Required]
    public TypeContrat Type { get; set; }

    // === Relation vers User (pour Type != Projet) ===
    public int? UserId { get; set; }
    [ForeignKey("UserId")]
    public User? User { get; set; }

    // === Relation vers Projet (pour Type == Projet) ===
    public int? ProjetId { get; set; }
    [ForeignKey("ProjetId")]
    public Projet? Projet { get; set; }
}
