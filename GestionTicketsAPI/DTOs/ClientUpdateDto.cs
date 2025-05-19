using System;

namespace GestionTicketsAPI.DTOs;

public class ClientUpdateDto
{
  public int Id { get; set; }
  public required string Email { get; set; }
  public required string FirstName { get; set; }
  public required string LastName { get; set; }
  public required string NumTelephone { get; set; }
  public required int PaysId { get; set; } 
  public required bool Actif { get; set; }
  public int SocieteId { get; set; }

  public string? NouveauPassword { get; set; }
  public string? ConfirmNouveauPassword { get; set; }
}
