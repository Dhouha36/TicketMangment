using System;

namespace GestionTicketsAPI.DTOs;

public class ClientExportDto
{
  public int Id { get; set; }
  public string Nom { get; set; }
  public string Email { get; set; }
  public string Telephone { get; set; }
  public bool Actif { get; set; }
  public string Societe { get; set; }  
  public string Projets { get; set; }
}