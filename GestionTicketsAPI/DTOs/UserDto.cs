using System;

namespace GestionTicketsAPI.DTOs;

public class UserDto
{
    public int Id { get; set; } 
    public required string Email { get; set; }
    public required string Role { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string NumTelephone { get; set; }
    public required string Pays { get; set; }
    public required bool Actif { get; set; }
    public required string Token { get; set; }
    public int? SocieteId { get; set; }
    public string? InitialPassword { get; set; }
    public SocieteDto? Societe { get; set; }
    public ContratDto? Contrat { get; set; }
}
