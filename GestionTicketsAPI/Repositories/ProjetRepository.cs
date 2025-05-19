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
        .Include(p => p.Contrats)
        .ToListAsync();
  }

  public async Task<PagedList<Projet>> GetProjetsPagedAsync(ProjectFilterParams filterParams)
  {
    var query = _context.Projets
        .Include(p => p.ChefProjet)
        .Include(p => p.Societe)
            .ThenInclude(s => s.Pays)
        .Include(p => p.ProjetUsers)
        .Include(p => p.Contrats)
        .OrderByDescending(p => p.CreatedAt)
        .AsQueryable();

    // Log avant filtrage
    var countAvantFiltre = await query.CountAsync();
    Console.WriteLine($"[Filtrage] Nombre de projets avant filtrage : {countAvantFiltre}");

    // Vérifier que Role et UserId sont valides
    // Normalisation du rôle en supprimant les espaces
    var normalizedRole = (filterParams.Role ?? string.Empty).ToLower().Replace(" ", "");
    if (normalizedRole != "superadmin")
    {
      if (normalizedRole == "chefdeprojet")
      {
        query = query.Where(p => p.ChefProjetId == filterParams.UserId ||
                                 p.ProjetUsers.Any(pu => pu.UserId == filterParams.UserId));
      }
      else if (normalizedRole == "client" || normalizedRole == "collaborateur")
      {
        query = query.Where(p => p.ProjetUsers.Any(pu => pu.UserId == filterParams.UserId));
      }
    }


    // Log après filtrage
    var countApresFiltre = await query.CountAsync();
    Console.WriteLine($"[Filtrage] Nombre de projets après filtrage : {countApresFiltre}");

    // Autres filtres (SearchTerm, ChefProjet, Societe, Pays) si nécessaires
    if (!string.IsNullOrEmpty(filterParams.SearchTerm))
    {
      var lowerSearchTerm = filterParams.SearchTerm.ToLower();
      query = query.Where(p => p.Nom.ToLower().Contains(lowerSearchTerm) ||
                               (p.Description != null && p.Description.ToLower().Contains(lowerSearchTerm)));
    }
    if (!string.IsNullOrEmpty(filterParams.ChefProjet))
    {
      var lowerChef = filterParams.ChefProjet.ToLower();
      query = query.Where(p => (p.ChefProjet.FirstName + " " + p.ChefProjet.LastName).ToLower().Contains(lowerChef));
    }
    if (!string.IsNullOrEmpty(filterParams.Societe))
    {
      var lowerSociete = filterParams.Societe.ToLower();
      query = query.Where(p => p.Societe.Nom.ToLower().Contains(lowerSociete));
    }
    if (!string.IsNullOrEmpty(filterParams.Pays))
    {
      var lowerPays = filterParams.Pays.ToLower();
      query = query.Where(p => p.Societe.Pays.Nom.ToLower().Contains(lowerPays));
    }

    return await PagedList<Projet>.CreateAsync(query, filterParams.PageNumber, filterParams.PageSize);
  }



  public async Task<IEnumerable<Projet>> GetProjetsFilteredAsync(ProjectFilterParams filterParams)
  {
    var query = _context.Projets
        .Include(p => p.ChefProjet)
        .Include(p => p.Societe)
            .ThenInclude(s => s.Pays)
        .Include(p => p.ProjetUsers)
        .Include(p => p.Contrats)
        .OrderByDescending(p => p.CreatedAt)
        .AsQueryable();

    // Filtrage selon le rôle de l'utilisateur
    // Normalisation du rôle en supprimant les espaces
    var normalizedRole = filterParams.Role.ToLower().Replace(" ", "");
    if (normalizedRole != "superadmin")
    {
      if (normalizedRole == "chefdeprojet")
      {
        query = query.Where(p => p.ChefProjetId == filterParams.UserId ||
                                 p.ProjetUsers.Any(pu => pu.UserId == filterParams.UserId));
      }
      else if (normalizedRole == "client" || normalizedRole == "collaborateur")
      {
        query = query.Where(p => p.ProjetUsers.Any(pu => pu.UserId == filterParams.UserId));
      }
    }


    // Filtrage par SearchTerm sur le nom du projet uniquement
    if (!string.IsNullOrEmpty(filterParams.SearchTerm))
    {
      var lowerSearchTerm = filterParams.SearchTerm.ToLower();
      query = query.Where(p => p.Nom.ToLower().Contains(lowerSearchTerm));
    }

    // Filtre sur le chef de projet par nom complet
    if (!string.IsNullOrEmpty(filterParams.ChefProjet))
    {
      var lowerChef = filterParams.ChefProjet.ToLower();
      query = query.Where(p => (p.ChefProjet.FirstName + " " + p.ChefProjet.LastName).ToLower().Contains(lowerChef));
    }

    // Filtre sur la société par son nom
    if (!string.IsNullOrEmpty(filterParams.Societe))
    {
      var lowerSociete = filterParams.Societe.ToLower();
      query = query.Where(p => p.Societe.Nom.ToLower().Contains(lowerSociete));
    }

    // Filtre sur le pays par son nom (via la société)
    if (!string.IsNullOrEmpty(filterParams.Pays))
    {
      var lowerPays = filterParams.Pays.ToLower();
      query = query.Where(p => p.Societe.Pays.Nom.ToLower().Contains(lowerPays));
    }

    return await query.ToListAsync();
  }

  public async Task<Projet?> GetProjetByIdAsync(int id)
  {
    return await _context.Projets
        .Include(p => p.ChefProjet)
        .Include(p => p.Societe)
            .ThenInclude(s => s.Pays)
        .Include(p => p.ProjetUsers)
        .Include(p => p.Contrats)
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
    await _context.ProjetUser
          .AddAsync(projetUser);
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
      .Include(p => p.ChefProjet)  // Inclusion du chef de projet
      .Include(p => p.Societe)       // Inclusion de la société
          .ThenInclude(s => s.Pays)  // Inclusion du pays de la société
      .Include(p => p.Contrats)
      .Where(p => p.ProjetUsers.Any(pu => pu.UserId == userId))
      .ToListAsync();
  }


  public async Task<bool> ProjetHasTicketsAsync(int projetId)
  {
    return await _context.Tickets.AnyAsync(t => t.ProjetId == projetId);
  }

  public async Task<IEnumerable<Projet>> GetProjetsBySocieteIdAsync(int societeId)
  {
    return await _context.Projets
                         .Where(p => p.SocieteId == societeId)
                         .ToListAsync();
  }
  public async Task<IEnumerable<Client>> GetClientsByProjetIdAsync(int projetId)
    {
        return await _context.ProjetClients
            .Where(pc => pc.ProjetId == projetId)
            .Include(pc => pc.Client)              // inclut l’entité Client
                .ThenInclude(c => c.Societe)      // si besoin d’infos société
            .Select(pc => pc.Client)
            .ToListAsync();
    }

}
