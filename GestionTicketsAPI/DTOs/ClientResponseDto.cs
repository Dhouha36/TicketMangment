using System;

namespace GestionTicketsAPI.DTOs;

public class ClientResponseDto
{
  public int Id { get; set; }
  public required string Email { get; set; }
  public required string FirstName { get; set; }
  public required string LastName { get; set; }
  public required string NumTelephone { get; set; }
  public required string Pays { get; set; }
  public required bool Actif { get; set; }
  public SocieteDto Societe { get; set; } = default!;
  public ICollection<ProjetDto> Projets { get; set; } = new List<ProjetDto>();
}
