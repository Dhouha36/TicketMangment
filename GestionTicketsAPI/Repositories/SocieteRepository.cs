using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using GestionTicketsAPI.Data;
using GestionTicketsAPI.Entities;
using GestionTicketsAPI.Helpers;
using GestionTicketsAPI.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GestionTicketsAPI.Repositories
{
  public class SocieteRepository : ISocieteRepository
  {
    private readonly DataContext _context;

    public SocieteRepository(DataContext context)
    {
      _context = context;
    }

    // Récupérer toutes les sociétés
    public async Task<IEnumerable<Societe>> GetAllSocietesAsync(string? searchTerm = null, string? pays = null)
    {
      var query = _context.Societes
                  .Include(s => s.Pays) // Inclure l'entité Pays
                  .AsQueryable();

      // Filtrage par nom de société
      if (!string.IsNullOrEmpty(searchTerm))
      {
        var lowerSearchTerm = searchTerm.ToLower();
        query = query.Where(s => s.Nom.ToLower().Contains(lowerSearchTerm));
      }

      // Filtrage par nom de pays
      if (!string.IsNullOrEmpty(pays))
      {
        var lowerPays = pays.ToLower();
        query = query.Where(s => s.Pays != null && s.Pays.Nom.ToLower().Contains(lowerPays));
      }

      return await query.ToListAsync();
    }

    public async Task<PagedList<Societe>> GetSocietesPagedAsync(UserParams userParams)
    {
      var query = _context.Societes
                          .Include(s => s.Pays)
                          .AsQueryable();

      // Filtrage par nom de société
      if (!string.IsNullOrWhiteSpace(userParams.SearchTerm))
      {
        var term = userParams.SearchTerm.Trim().ToLower();
        query = query.Where(s => s.Nom.ToLower().Contains(term));
      }

      // Filtrage par nom de pays
      if (!string.IsNullOrWhiteSpace(userParams.Pays))
      {
        var paysTerm = userParams.Pays.Trim().ToLower();
        query = query.Where(s =>
            s.Pays != null
            && s.Pays.Nom.ToLower().Contains(paysTerm)
        );
      }

      // Pagination
      return await PagedList<Societe>.CreateAsync(
          query.OrderBy(s => s.Nom),
          userParams.PageNumber,
          userParams.PageSize
      );
    }


    // Récupérer une société par ID (simple)
    public async Task<Societe?> GetSocieteByIdAsync(int id)
    {
      return await _context.Societes.FindAsync(id);
    }

    // Récupérer une société avec ses détails (utilisateurs et projets)
    public async Task<Societe?> GetSocieteWithDetailsByIdAsync(int id)
    {
      return await _context.Societes
        .Include(s => s.SocieteUsers)
            .ThenInclude(su => su.User)
        .Include(s => s.Projets!)
            .ThenInclude(p => p.Pays)
        .FirstOrDefaultAsync(s => s.Id == id);

    }

    // Ajouter une société
    public async Task AddSocieteAsync(Societe societe)
    {
      await _context.Societes.AddAsync(societe);

    }

    // Mettre à jour une société
    public void UpdateSociete(Societe societe)
    {
      _context.Societes.Update(societe);
    }

    // Méthode standard (non utilisée ici) pour supprimer une société
    public void RemoveSociete(Societe societe)
    {
      _context.Societes.Remove(societe);
    }

    // Suppression d'une société en chargeant explicitement ses associations
    public async Task<bool> DeleteSocieteWithAssociationsAsync(int id)
    {
      // Charger la société avec ses associations
      var societe = await _context.Societes
          .Include(s => s.SocieteUsers)
              .ThenInclude(su => su.User)
          .Include(s => s.Projets)
              .ThenInclude(p => p.Pays)
          .FirstOrDefaultAsync(s => s.Id == id);

      if (societe == null)
      {
        return false;
      }

      // Vérifier si la société possède des projets associés
      if (await SocieteHasProjectsAsync(id))
      {
        throw new InvalidOperationException("Impossible de supprimer la société car elle contient des projets associés.");
      }

      // Supprimer les associations dans la table de jonction
      if (societe.SocieteUsers?.Any() == true)
      {
        _context.RemoveRange(societe.SocieteUsers);
      }

      // Ne pas supprimer les projets ici, car la présence d'au moins un projet empêche la suppression de la société.

      // Supprimer la société elle-même
      _context.Societes.Remove(societe);

      // Utilisation d'une transaction pour garantir l'atomicité
      using (var transaction = await _context.Database.BeginTransactionAsync())
      {
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
      }

      return true;
    }


    public async Task<PagedList<User>> GetSocieteUsersPagedAsync(int societeId, UserParams userParams)
    {
      var query = _context.Users
          .Include(u => u.SocieteUsers)
          .Include(u => u.Role)
          .Where(u => u.SocieteUsers.Any(su => su.SocieteId == societeId))
          .AsQueryable();

      if (!string.IsNullOrEmpty(userParams.SearchTerm))
      {
        var lowerSearchTerm = userParams.SearchTerm.ToLower();
        query = query.Where(u =>
            u.FirstName.ToLower().Contains(lowerSearchTerm) ||
            u.LastName.ToLower().Contains(lowerSearchTerm) ||
            u.Email.ToLower().Contains(lowerSearchTerm));
      }

      return await PagedList<User>.CreateAsync(query, userParams.PageNumber, userParams.PageSize);
    }

    public async Task<bool> AttachUserToSocieteAsync(int societeId, int userId)
    {
      // Récupérer l'utilisateur pour connaître son rôle
      var user = await _context.Users
        .Include(u => u.Role) // Charger la relation Role
        .FirstOrDefaultAsync(u => u.Id == userId);
      if (user == null)
      {
        // Gestion de l'erreur : utilisateur non trouvé
        return false;
      }

      // Si l'utilisateur est client, vérifier qu'il n'est associé à aucune autre société
      if (user.Role.Name.ToLower() == "client")
      {
        bool isAlreadyClient = await _context.SocieteUsers
            .AnyAsync(su => su.UserId == userId);
        if (isAlreadyClient)
        {
          // Ne pas autoriser plusieurs associations pour un client
          return false;
        }
      }

      // Vérifier si l'association existe déjà (pour tous les rôles)
      var association = await _context.SocieteUsers
          .FirstOrDefaultAsync(su => su.SocieteId == societeId && su.UserId == userId);
      if (association != null)
      {
        Console.WriteLine($"Association existante : SocieteId {societeId}, UserId {userId}");
        return false;
      }

      // Créer l'association
      var societeUser = new SocieteUser
      {
        SocieteId = societeId,
        UserId = userId,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
      };

      await _context.SocieteUsers.AddAsync(societeUser);
      var result = await _context.SaveChangesAsync() > 0;
      Console.WriteLine(result
        ? $"Nouvelle association créée : SocieteId {societeId}, UserId {userId}"
        : $"Erreur lors de l'insertion dans la base pour SocieteId {societeId}, UserId {userId}");
      return result;
    }


    public async Task<bool> DetachUserFromSocieteAsync(int societeId, int userId)
    {
      // Rechercher l'association à supprimer
      var association = await _context.SocieteUsers
          .FirstOrDefaultAsync(su => su.SocieteId == societeId && su.UserId == userId);

      if (association == null)
      {
        // Aucun lien n'existe entre cet utilisateur et cette société
        return false;
      }

      _context.SocieteUsers.Remove(association);
      return await _context.SaveChangesAsync() > 0;
    }


    public async Task UpdateRelatedEntitiesForSocietePaysChangeAsync(int societeId, int newPaysId)
    {
      // Récupérer la société concernée
      var societe = await _context.Societes.FirstOrDefaultAsync(s => s.Id == societeId);
      if (societe != null)
      {
        // Mettre à jour la propriété PaysId de la société
        societe.PaysId = newPaysId;

        // Optionnel : si vous avez une navigation vers l'entité Pays, vous pouvez également mettre à jour la relation
        // par exemple : societe.Pays = await _context.Pays.FindAsync(newPaysId);

        _context.Societes.Update(societe);
      }

      // Les projets liés à la société n'ont pas besoin d'être modifiés directement,
      // car leur propriété calculée IdPays se mettra à jour via la relation avec la société.

      // Si vous avez d'autres entités qui stockent le pays de façon redondante, mettez-les à jour ici.
      var users = await _context.Users
          .Where(u => u.SocieteUsers.Any(su => su.SocieteId == societeId))
          .ToListAsync();
      foreach (var user in users)
      {
        // Ici, on met à jour la propriété correspondant au pays de l'utilisateur
        // Assurez-vous que la propriété (par exemple user.Pays) est du bon type (int ou une entité)
        user.Pays = newPaysId;
        _context.Users.Update(user);
      }

      // Sauvegarder les modifications dans la base de données
      await _context.SaveChangesAsync();
    }


    public async Task<bool> SocieteExists(string nom)
    {
      return await _context.Societes.AnyAsync(s => s.Nom == nom);
    }

    // Sauvegarder les modifications
    public async Task<bool> SaveAllAsync()
    {
      return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> SocieteHasProjectsAsync(int societeId)
    {
      return await _context.Projets.AnyAsync(p => p.SocieteId == societeId);
    }

  }
}
