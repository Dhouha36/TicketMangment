using GestionTicketsAPI.Data;
using GestionTicketsAPI.Entities;
using GestionTicketsAPI.Helpers;
using GestionTicketsAPI.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GestionTicketsAPI.Repositories
{
  public class TicketRepository : ITicketRepository
  {
    private readonly DataContext _context;

    public TicketRepository(DataContext context)
    {
      _context = context;
    }

    public async Task<Ticket?> GetTicketByIdAsync(int id)
    {
      return await _context.Tickets
          .Include(t => t.Owner)
          .Include(t => t.ProblemCategory)
          .Include(t => t.Projet)
            .ThenInclude(p => p.ChefProjet)
          .Include(t => t.Responsible)
          .Include(t => t.Priority)
          .Include(t => t.Qualification)
          .Include(t => t.Statut)
          .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<IEnumerable<Ticket>> GetTicketsAsync()
    {
      return await _context.Tickets
          .Include(t => t.Owner)
          .Include(t => t.ProblemCategory)
          .Include(t => t.Projet)
            .ThenInclude(p => p.ChefProjet)
          .Include(t => t.Responsible)
          .Include(t => t.Priority)
          .Include(t => t.Qualification)
          .Include(t => t.Statut)
          .ToListAsync();
    }

    public async Task<PagedList<Ticket>> GetTicketsPagedAsync(UserParams ticketParams)
    {
      var query = _context.Tickets
          .Include(t => t.Owner)
          .Include(t => t.ProblemCategory)
          .Include(t => t.Projet)
              .ThenInclude(p => p.ChefProjet)
          .Include(t => t.Responsible)
          .Include(t => t.Priority)
          .Include(t => t.Qualification)
          .Include(t => t.Statut)
          .OrderByDescending(t => t.CreatedAt) // Tri par date de création
          .AsQueryable();

      // Si l'utilisateur n'est pas un super admin,
      // on impose que le ticket soit lié à un projet
      // et que l'utilisateur soit associé au projet.
      // Pour les chefs de projet, on vérifie l'association via le ChefProjetId.
      if (!ticketParams.Role?.Replace(" ", "").Equals("superadmin", StringComparison.OrdinalIgnoreCase) ?? true)
      {
        if (string.Equals(ticketParams.Role, "chef de projet", StringComparison.OrdinalIgnoreCase))
        {
          query = query.Where(t =>
              t.Projet != null &&
              t.Projet.ChefProjetId == ticketParams.UserId);
        }
        else
        {
          query = query.Where(t =>
              t.Projet != null &&
              _context.ProjetUser.Any(pu => pu.ProjetId == t.Projet.Id && pu.UserId == ticketParams.UserId));
        }
      }

      // Filtrage par terme de recherche
      if (!string.IsNullOrEmpty(ticketParams.SearchTerm))
      {
        var lowerSearchTerm = ticketParams.SearchTerm.ToLower();
        query = query.Where(t => t.Title.ToLower().Contains(lowerSearchTerm)
                               || t.Description.ToLower().Contains(lowerSearchTerm));
      }

      // Filtrage complémentaire selon le rôle
      if (!ticketParams.Role?.Replace(" ", "").Equals("superadmin", StringComparison.OrdinalIgnoreCase) ?? true)
      {
        if (string.Equals(ticketParams.Role, "client", StringComparison.OrdinalIgnoreCase))
        {
          // Le client voit uniquement les tickets dont il est le propriétaire
          query = query.Where(t => t.Owner != null && t.Owner.Id == ticketParams.UserId);
        }
        else if (string.Equals(ticketParams.Role, "collaborateur", StringComparison.OrdinalIgnoreCase))
        {
          // Le collaborateur voit uniquement les tickets qui lui sont assignés
          query = query.Where(t => t.ResponsibleId == ticketParams.UserId);
        }
        else if (string.Equals(ticketParams.Role, "chef de projet", StringComparison.OrdinalIgnoreCase))
        {
          // En plus du filtre par ChefProjetId, on peut autoriser
          // le chef de projet à voir les tickets qui lui sont assignés.
          query = query.Where(t => t.ResponsibleId == ticketParams.UserId || t.Projet.ChefProjetId == ticketParams.UserId);
        }
      }

      return await PagedList<Ticket>.CreateAsync(query, ticketParams.PageNumber, ticketParams.PageSize);
    }



    public async Task AddTicketAsync(Ticket ticket)
    {
      await _context.Tickets.AddAsync(ticket);
    }

    public void UpdateTicket(Ticket ticket)
    {
      _context.Entry(ticket).State = EntityState.Modified;
    }

    public void DeleteTicket(Ticket ticket)
    {
      _context.Tickets.Remove(ticket);
    }

    public async Task<bool> SaveAllAsync()
    {
      return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteMultipleTicketsAsync(IEnumerable<int> ticketIds)
    {
      var tickets = await _context.Tickets
          .Where(t => ticketIds.Contains(t.Id))
          .ToListAsync();

      if (!tickets.Any())
      {
        return false;
      }

      _context.Tickets.RemoveRange(tickets);
      return await SaveAllAsync();
    }

    public async Task<bool> TicketExists(string title)
    {
      return await _context.Tickets.AnyAsync(t => t.Title == title);
    }

    public async Task<StatutDesTicket?> GetStatusByNameAsync(string statusName)
    {
      return await _context.StatutsDesTickets.FirstOrDefaultAsync(s => s.Name == statusName);
    }

    public List<object> GetTicketCountByStatus(int userId, string role)
    {
      // Commence par récupérer tous les tickets
      IQueryable<Ticket> filteredTickets = _context.Tickets;

      // Si l'utilisateur n'est pas un superadmin, on filtre les tickets
      if (!string.Equals(role, "super admin", StringComparison.OrdinalIgnoreCase))
      {
        filteredTickets = filteredTickets.Where(t =>
            t.OwnerId == userId ||
            t.ResponsibleId == userId ||
            (t.Projet != null && t.Projet.ChefProjetId == userId)
        );
      }

      // Groupement par statut en utilisant uniquement les tickets filtrés
      var results = _context.StatutsDesTickets
           .GroupJoin(
                filteredTickets,
                statut => statut.Id,       // clé côté Statut
                ticket => ticket.StatutId, // clé côté Ticket
                (statut, ticketsGroup) => new
                {
                  Id = statut.Id,
                  Name = statut.Name,
                  Count = ticketsGroup.Count()
                }
           )
           .ToList();

      return results.Cast<object>().ToList();
    }

  }
}