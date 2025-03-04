using System;

namespace GestionTicketsAPI.Helpers;

public class UserParams
{
    private const int MaxPageSize = 50;
    public int PageNumber { get; set; } = 1;
    
    private int pageSize = 10;
    public int PageSize 
    { 
        get => pageSize;
        set => pageSize = (value > MaxPageSize) ? MaxPageSize : value;
    }
    
    // Ajout du terme de recherche
    public string? SearchTerm { get; set; }
}

