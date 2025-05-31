using System.ComponentModel.DataAnnotations;

namespace GestionTicketsAPI.DTOs
{
  public class UserUpdateDto
  {
    public int Id { get; set; }
    public required string Email { get; set; }
    public required string Role { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string NumTelephone { get; set; }
    public required string Pays { get; set; }
    public required bool Actif { get; set; }
    [DataType(DataType.Upload)]
    public IFormFile? PhotoFile { get; set; }

    public string? NouveauPassword { get; set; }

    public ContratDto? Contrat { get; set; }
  }
}
