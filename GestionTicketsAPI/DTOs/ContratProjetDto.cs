using System;

namespace GestionTicketsAPI.DTOs;

public class ContratProjetDto
{
  public int Id { get; set; }
  public DateTime DateDebut { get; set; }
  public DateTime? DateFin { get; set; }
  public decimal? MontantTotal { get; set; }
  public int ProjetId { get; set; }
}
