using System;

namespace GestionTicketsAPI.DTOs;

public class TicketStatDto
{
  public string Key { get; set; }    // ex. "2025-04" ou "Résolu"
  public int Count { get; set; }
  public double? Value { get; set; } 
}
