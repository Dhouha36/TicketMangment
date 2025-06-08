using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestionTicketsAPI.Entities;

[Table("ContratsUsers")]
public class ContratUser
{
  [Key]
  public int Id { get; set; }

  [Required]
  public DateTime DateDebut { get; set; }

  public DateTime? DateFin { get; set; }

  [Required]
  public string? Type { get; set; }  // CDD ou CDI

  public decimal? SalaireMensuel { get; set; }

  // Relation vers User
  [Required]
  public int UserId { get; set; }
  [ForeignKey(nameof(UserId))]
  public User User { get; set; } = null!;
}
