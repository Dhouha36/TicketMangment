using System;

namespace GestionTicketsAPI.Helpers;

public class ClientParams
{
  private const int MaxPageSize = 50;
  public int PageNumber { get; set; } = 1;

  private int pageSize = 10;
  public int PageSize
  {
    get => pageSize;
    set => pageSize = (value > MaxPageSize) ? MaxPageSize : value;
  }

  // Terme de recherche (prénom ou nom)
  public string? SearchTerm { get; set; }

  // Filtres spécifiques aux clients
  public bool? Actif { get; set; }
  public int? SocieteId { get; set; }
  public string? Pays { get; set; }
}
