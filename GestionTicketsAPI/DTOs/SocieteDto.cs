using System.ComponentModel.DataAnnotations;
using GestionTicketsAPI.Entities;

namespace GestionTicketsAPI.DTOs
{
  public class SocieteDto
  {
    public int Id { get; set; }

    [Required]
    public string Nom { get; set; } = string.Empty;

    [Required]
    public string Adresse { get; set; } = string.Empty;

    [Required]
    public string Telephone { get; set; } = string.Empty;
    public string Ville { get; set; } = string.Empty;
    public string CodePostal { get; set; } = string.Empty;

    public int PaysId { get; set; }
    public PaysDto? Pays { get; set; }
  }
}
