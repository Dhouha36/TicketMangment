using System;
using GestionTicketsAPI.Entities;

namespace GestionTicketsAPI.Helpers;

public class ProjectFilterParams
{
  public int PageNumber { get; set; } = 1;
  public int PageSize { get; set; } = 10;
  public string? Role { get; set; }
  public int? UserId { get; set; }  // Passage de int à int?
  public string? FilterType { get; set; }

  // Filtres avancés
  public string? ChefProjet { get; set; }
  public string? Societe { get; set; }
  public string? Pays { get; set; }
  public string? SearchTerm { get; set; }
  public ICollection<Contrat>? Contrats { get; set; } = new List<Contrat>();
}

