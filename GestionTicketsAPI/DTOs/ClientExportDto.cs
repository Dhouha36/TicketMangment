using System;

namespace GestionTicketsAPI.DTOs;

public class ClientExportDto
{
  public string FullName { get; set; } = string.Empty;
  public string Email { get; set; } = string.Empty;
  public string Actif { get; set; } = string.Empty;
  public string Societe { get; set; } = string.Empty;
  public DateTime CreatedAt { get; set; }
  public DateTime? DeletedAt { get; set; }
}