using System;
using GestionTicketsAPI.Data;
using GestionTicketsAPI.Entities;
using GestionTicketsAPI.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GestionTicketsAPI.Repositories;

public class PaysRepository : IPaysRepository
{
  private readonly DataContext _context;
  public PaysRepository(DataContext context)
  {
    _context = context;
  }

  public async Task<IEnumerable<Pays>> GetPaysAsync()
  {
    return await _context.Pays
        .Include(p => p.paysPhoto) // On inclut la photo associée
        .ToListAsync();
  }

  // Nouvelle méthode pour récupérer les pays en fonction d'un terme de recherche
  public async Task<IEnumerable<Pays>> GetPaysAsync(string searchTerm)
  {
    var query = _context.Pays
        .Include(p => p.paysPhoto)
        .AsQueryable();

    if (!string.IsNullOrWhiteSpace(searchTerm))
    {
      // On effectue la recherche sur le nom (en ignorant la casse)
      query = query.Where(p => p.Nom.ToLower().Contains(searchTerm.ToLower()));
    }

    return await query.ToListAsync();
  }

  public async Task<Pays?> GetPaysByIdAsync(int idPays)
  {
    return await _context.Pays
        .Include(p => p.Societes) 
        .Include(p => p.paysPhoto)
        .FirstOrDefaultAsync(p => p.IdPays == idPays);
  }

  public async Task AddPaysAsync(Pays pays)
  {
    await _context.Pays.AddAsync(pays);
  }

  public void RemovePays(Pays pays)
  {
    _context.Pays.Remove(pays);
  }

  public async Task<bool> PaysExists(string nom)
  {
    return await _context.Pays.AnyAsync(p => p.Nom == nom);
  }

  public async Task<bool> SaveAllAsync()
  {
    return await _context.SaveChangesAsync() > 0;
  }
}
