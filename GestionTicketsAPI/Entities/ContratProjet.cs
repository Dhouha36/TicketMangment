using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestionTicketsAPI.Entities;

[Table("ContratsProjets")]
public class ContratProjet
{
  [Key]
  public int Id { get; set; }

  [Required]
  public DateTime DateDebut { get; set; }

  public DateTime? DateFin { get; set; }
  public decimal? MontantTotal { get; set; }

  // Relation vers Projet
  [Required]
  public int ProjetId { get; set; }
  [ForeignKey(nameof(ProjetId))]
  public Projet Projet { get; set; } = null!;
}
