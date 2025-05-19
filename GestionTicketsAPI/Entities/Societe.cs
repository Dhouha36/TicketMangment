using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestionTicketsAPI.Entities
{
  public class Societe
  {
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Nom { get; set; } = string.Empty;

    [Required]
    public string Adresse { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Telephone { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Ville { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string CodePostal { get; set; } = string.Empty;

    public int PaysId { get; set; }
    public Pays? Pays { get; set; }

    public ICollection<SocieteUser> SocieteUsers { get; set; } = new List<SocieteUser>();

    public ICollection<Projet>? Projets { get; set; }

  }
}
