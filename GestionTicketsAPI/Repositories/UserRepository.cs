using GestionTicketsAPI.Data;
using GestionTicketsAPI.Entities;
using GestionTicketsAPI.Helpers;
using GestionTicketsAPI.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GestionTicketsAPI.Repositories
{
  public class UserRepository : IUserRepository
  {
    private readonly DataContext _context;

    public UserRepository(DataContext context)
    {
      _context = context;
    }

    public async Task<PagedList<User>> GetUsersAsync(UserParams userParams)
    {
      var query = _context.Users
                  .Include(u => u.Contrats)
                  .Include(u => u.Role)
                  .Include(u => u.SocieteUsers)
                    .ThenInclude(su => su.Societe)
                  .OrderByDescending(u => u.CreatedAt)
                  .AsQueryable();

      // Filtrage par SearchTerm (prénom ou nom)
      if (!string.IsNullOrEmpty(userParams.SearchTerm))
      {
        var lowerSearchTerm = userParams.SearchTerm.ToLower();
        query = query.Where(u =>
            u.FirstName.ToLower().Contains(lowerSearchTerm) ||
            u.LastName.ToLower().Contains(lowerSearchTerm));
      }

      // Filtrage par rôle
      if (!string.IsNullOrEmpty(userParams.Role))
      {
        var lowerRole = userParams.Role.ToLower();
        query = query.Where(u => u.Role.Name.ToLower().Contains(lowerRole));
      }

      // Filtrage par actif/inactif
      if (userParams.Actif.HasValue)
      {
        query = query.Where(u => u.Actif == userParams.Actif.Value);
      }

      // Filtrage par présence de contrat
      if (userParams.HasContract.HasValue)
      {
        if (userParams.HasContract.Value)
        {
          // Utilisateurs possédant au moins un contrat
          query = query.Where(u => u.Contrats != null && u.Contrats.Any());
        }
        else
        {
          // Utilisateurs sans contrat
          query = query.Where(u => u.Contrats == null || !u.Contrats.Any());
        }
      }

      return await PagedList<User>.CreateAsync(query, userParams.PageNumber, userParams.PageSize);
    }

    public async Task<IEnumerable<User>> GetUsersFilteredAsync(UserParams userParams)
    {
      var query = _context.Users
                  .Include(u => u.Contrats)
                  .Include(u => u.Role)
                  .Include(u => u.SocieteUsers)
                    .ThenInclude(su => su.Societe)
                  .AsQueryable();

      // Filtrage par SearchTerm (prénom ou nom)
      if (!string.IsNullOrEmpty(userParams.SearchTerm))
      {
        var lowerSearchTerm = userParams.SearchTerm.ToLower();
        query = query.Where(u =>
            u.FirstName.ToLower().Contains(lowerSearchTerm) ||
            u.LastName.ToLower().Contains(lowerSearchTerm));
      }

      // Filtrage par rôle
      if (!string.IsNullOrEmpty(userParams.Role))
      {
        var lowerRole = userParams.Role.ToLower();
        query = query.Where(u => u.Role.Name.ToLower().Contains(lowerRole));
      }

      // Filtrage par actif/inactif
      if (userParams.Actif.HasValue)
      {
        query = query.Where(u => u.Actif == userParams.Actif.Value);
      }

      // Filtrage par présence de contrat
      if (userParams.HasContract.HasValue)
      {
        if (userParams.HasContract.Value)
        {
          query = query.Where(u => u.Contrats != null && u.Contrats.Any());
        }
        else
        {
          query = query.Where(u => u.Contrats == null || !u.Contrats.Any());
        }
      }

      return await query.ToListAsync();
    }

    public async Task<IEnumerable<User>> GetUsersNoPaginationAsync()
    {
      return await _context.Users
                    .Include(u => u.Contrats)
                    .Include(u => u.Role)
                    .Include(u => u.SocieteUsers)
                      .ThenInclude(su => su.Societe)
                    .ToListAsync();
    }

    public async Task<User?> GetUserByIdAsync(int id)
    {
      return await _context.Users
          .Include(u => u.Contrats)
          .Include(u => u.Role)
          .Include(u => u.SocieteUsers)
              .ThenInclude(su => su.Societe)
          .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<User?> GetUserWithProjetUsersAsync(int id)
    {
      return await _context.Users
          .Include(u => u.ProjetUsers)
          .Include(u => u.Role)
          .Include(u => u.SocieteUsers)
              .ThenInclude(su => su.Societe)
          .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<bool> DeleteUserAsync(User user)
    {
      // Vérifier si l'utilisateur est assigné à un ticket
      bool isAssignedToTickets = await _context.Tickets.AnyAsync(t =>
          t.OwnerId == user.Id ||
          t.ResponsibleId == user.Id ||
          (t.Projet != null && t.Projet.ChefProjetId == user.Id)
      );

      if (isAssignedToTickets)
      {
        throw new InvalidOperationException("Impossible de supprimer l'utilisateur car il est assigné à des tickets.");
      }

      // Supprimer les associations dans ProjetUser
      var projetUsers = await _context.ProjetUser
          .Where(pu => pu.UserId == user.Id)
          .ToListAsync();
      if (projetUsers.Any())
      {
        _context.ProjetUser.RemoveRange(projetUsers);
      }

      // Supprimer les associations dans SocieteUser
      var societeUsers = await _context.SocieteUsers
          .Where(su => su.UserId == user.Id)
          .ToListAsync();
      if (societeUsers.Any())
      {
        _context.SocieteUsers.RemoveRange(societeUsers);
      }

      // Supprimer les commentaires de l'utilisateur
      var userComments = await _context.Commentaires
          .Where(c => c.UserId == user.Id)
          .ToListAsync();
      if (userComments.Any())
      {
        _context.Commentaires.RemoveRange(userComments);
      }

      // Enfin, supprimer l'utilisateur
      _context.Users.Remove(user);

      return await _context.SaveChangesAsync() > 0;
    }



    public async Task<bool> SaveAllAsync()
    {
      return await _context.SaveChangesAsync() > 0;
    }

    // Implémentation de GetUserProjectsAsync
    public async Task<PagedList<Projet>> GetUserProjectsAsync(int userId, UserParams userParams)
    {
      // Projets associés via l'entité ProjetUser
      var associatedProjects = _context.ProjetUser
          .Include(pm => pm.Projet)
              .ThenInclude(p => p.Pays)
          .Include(pm => pm.Projet)
              .ThenInclude(p => p.Societe)
          .Include(pm => pm.Projet)    
              .ThenInclude(p => p.ChefProjet)
          .Where(pm => pm.UserId == userId)
          .Select(pm => pm.Projet);

      // Projets où l'utilisateur est ChefProjet
      var chefProjects = _context.Projets
          .Include(p => p.Pays)
          .Include(p => p.Societe)
          .Include(p => p.ChefProjet)
          .Where(p => p.ChefProjetId == userId);

      // Combinaison des deux requêtes
      var combinedQuery = associatedProjects.Union(chefProjects).AsQueryable();

      if (!string.IsNullOrEmpty(userParams.SearchTerm))
      {
        combinedQuery = combinedQuery.Where(p => p.Nom.Contains(userParams.SearchTerm));
      }

      return await PagedList<Projet>.CreateAsync(combinedQuery, userParams.PageNumber, userParams.PageSize);
    }


    // Implémentation de GetUserTicketsAsync
    public async Task<PagedList<Ticket>> GetUserTicketsAsync(int userId, UserParams userParams)
    {
      // Récupérer d'abord les identifiants des tickets
      var ownerTicketIds = _context.Tickets
          .Where(t => t.OwnerId == userId)
          .Select(t => t.Id);

      var responsibleTicketIds = _context.Tickets
          .Where(t => t.ResponsibleId == userId)
          .Select(t => t.Id);

      var chefTicketIds = _context.Tickets
          .Where(t => t.Projet.ChefProjetId == userId)
          .Select(t => t.Id);

      var associatedTicketIds = _context.Tickets
          .Where(t => _context.ProjetUser.Any(pu => pu.ProjetId == t.ProjetId && pu.UserId == userId))
          .Select(t => t.Id);

      var combinedIds = ownerTicketIds
          .Union(responsibleTicketIds)
          .Union(chefTicketIds)
          .Union(associatedTicketIds);

      // Ensuite, chargez les tickets avec tous les Include nécessaires
      var combinedQuery = _context.Tickets
          .Include(t => t.Projet)
          .Include(t => t.Owner)
          .Include(t => t.Responsible)
          .Include(t => t.ProblemCategory)
          .Where(t => combinedIds.Contains(t.Id));

      if (!string.IsNullOrEmpty(userParams.SearchTerm))
      {
        combinedQuery = combinedQuery.Where(t => t.Title.Contains(userParams.SearchTerm));
      }

      return await PagedList<Ticket>.CreateAsync(combinedQuery, userParams.PageNumber, userParams.PageSize);

    }


    // Méthode Update pour marquer l'entité comme modifiée
    public void Update(User user)
    {
      _context.Entry(user).State = EntityState.Modified;
    }

    public async Task<IEnumerable<User>> GetUsersByRoleAsync(string roleName)
    {
      return await _context.Users
          .Include(u => u.Role)
          .Where(u => u.Role != null && u.Role.Name.ToLower() == roleName.ToLower())
          .ToListAsync();
    }
  }
}
