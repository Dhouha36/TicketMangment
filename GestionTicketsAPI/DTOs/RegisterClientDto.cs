using System;
using System.ComponentModel.DataAnnotations;

namespace GestionTicketsAPI.DTOs;

public class RegisterClientDto
{
  [Required, EmailAddress]
  public string Email { get; set; } = string.Empty;

  [Required]
  public string FirstName { get; set; } = string.Empty;

  [Required]
  public string LastName { get; set; } = string.Empty;

  [Required, Phone]
  public string NumTelephone { get; set; } = string.Empty;

  [Required]
  public int Pays { get; set; }

  // **Société obligatoire**
  [Required]
  public int SocieteId { get; set; }
  [Required]
  public bool Actif { get; set; } = true;
  public List<int> ProjetIds { get; set; } = new List<int>();
}
