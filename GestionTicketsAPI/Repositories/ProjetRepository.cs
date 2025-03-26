using System;
using GestionTicketsAPI.Data;
using GestionTicketsAPI.Entities;
using GestionTicketsAPI.Helpers;
using GestionTicketsAPI.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GestionTicketsAPI.Repositories;

public class ProjetRepository : IProjetRepository
{
  private readonly DataContext _context;
  public ProjetRepository(DataContext context)
  {
    _context = context;
  }

  public async Task<IEnumerable<Projet>> GetProjetsAsync()
  {
    return await _context.Projets
        .Include(p => p.ChefProjet)
        .Include(p => p.Societe)
            .ThenInclude(s => s.Pays)
        .Include(p => p.ProjetUsers)
        .ToListAsync();
  }

  public async Task<PagedList<Projet>> GetProjetsPagedAsync(UserParams projetParams)
  {
    var query = _context.Projets
        .Include(p => p.ChefProjet)   
        .Include(p => p.Societe)
            .ThenInclude(s => s.Pays)
        .OrderByDescending(t => t.CreatedAt)
        .AsQueryable();

    if (!string.IsNullOrEmpty(projetParams.SearchTerm))
    {
      var lowerSearchTerm = projetParams.SearchTerm.ToLower();
      query = query.Where(p => p.Nom.ToLower().Contains(lowerSearchTerm));
    }

    if (projetParams.SocieteId.HasValue)
    {
      query = query.Where(p => p.SocieteId == projetParams.SocieteId.Value);
    }

    return await PagedList<Projet>.CreateAsync(query, projetParams.PageNumber, projetParams.PageSize);
  }


  public async Task<Projet?> GetProjetByIdAsync(int id)
  {
    return await _context.Projets
        .Include(p => p.ChefProjet)
        .Include(p => p.Societe)
            .ThenInclude(s => s.Pays)
        .Include(p => p.ProjetUsers)
        .AsNoTracking()
        .FirstOrDefaultAsync(p => p.Id == id);
  }

  public async Task AddProjetAsync(Projet projet)
  {
    await _context.Projets.AddAsync(projet);
  }

  public void UpdateProjet(Projet projet)
  {
    _context.Entry(projet).State = EntityState.Modified;
  }

  public void RemoveProjet(Projet projet)
  {
    _context.Projets.Remove(projet);
  }

  public async Task<bool> ProjetExistsAsync(int id)
  {
    return await _context.Projets.AnyAsync(e => e.Id == id);
  }

  public async Task<bool> SaveAllAsync()
  {
    return await _context.SaveChangesAsync() > 0;
  }

  public async Task AddProjetUserAsync(ProjetUser projetUser)
  {
    await _context.ProjetUser.AddAsync(projetUser);
  }

  public async Task<ProjetUser?> GetProjetUserAsync(int projetId, int userId)
  {
    return await _context.ProjetUser
        .FirstOrDefaultAsync(pu => pu.ProjetId == projetId && pu.UserId == userId);
  }

  public void RemoveProjetUser(ProjetUser projetUser)
  {
    _context.ProjetUser.Remove(projetUser);
  }

  public async Task<IEnumerable<dynamic>> GetMembresProjetAsync(int projetId)
  {
    return await _context.ProjetUser
    .Where(pu => pu.ProjetId == projetId)
    .Select(pu => new
    {
      pu.UserId,
      pu.User.FirstName,
      pu.User.LastName,
      Role = pu.User.Role.Name  // Assurez-vous que "Role" existe dans User
    })
    .ToListAsync();

  }

  public async Task<bool> ProjetExists(string nom)
  {
    return await _context.Projets.AnyAsync(p => p.Nom == nom);
  }

  public async Task<IEnumerable<Projet>> GetProjetsForUserAsync(int userId)
  {
    return await _context.Projets
      .Where(p => p.ProjetUsers.Any(pu => pu.UserId == userId))
      .ToListAsync();
  }

}