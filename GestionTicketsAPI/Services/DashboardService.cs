using System;
using System.Globalization;
using System.Linq;
using GestionTicketsAPI.Data;
using GestionTicketsAPI.DTOs;
using GestionTicketsAPI.Entities;

namespace GestionTicketsAPI.Services
{
  public interface IDashboardService
  {
    DashboardCountsDto GetDashboardCounts(int userId, string role);
    int GetMyTicketsCount(int userId);
    IEnumerable<TicketStatDto> GetTicketCountsByUserAndPeriod(
           int userId,
           string role,
           DateTime? start,
           DateTime? end,
           string granularity,
           int? filterUserId,
           int? projetId
       );

    IEnumerable<TicketStatDto> GetTicketCountsByStatusAndPeriod(
          int userId,
          string role,
          DateTime? start,
          DateTime? end,
          string granularity,
          int? clientId,
          int? personnelId,
          int? projetId
      );
    IEnumerable<TicketStatDto> GetTicketsFiltered(
        int currentUserId,
        string role,
        DateTime? start,
        DateTime? end,
        string granularity,
        int? clientId,
        int? personnelId,
        int? projetId
    );
  }

  public class DashboardService : IDashboardService
  {
    private readonly DataContext _context;

    public DashboardService(DataContext context)
    {
      _context = context;
    }

    private bool IsClient(int userId)
    {
      return _context.Clients.Any(c => c.Id == userId);
    }
    public DashboardCountsDto GetDashboardCounts(int userId, string role)
    {
      var dto = new DashboardCountsDto();

      // 1) Les counts visibles uniquement pour le super admin
      //    => si l’utilisateur est super admin, on compte tout ; sinon on ne renvoie rien (ou 0).
      bool isSuperAdmin = string.Equals(role, "super admin", StringComparison.OrdinalIgnoreCase);
      bool isClient = IsClient(userId);
      int totalUsers = _context.Users.Count();

      // 1) Comptage distinct Clients vs Personnel (comparaison en ToLower())
      int clients = _context.Clients.Count();

      if (isSuperAdmin)
      {
        dto.CategoriesCount = _context.CategorieProblemes.Count();
        dto.PaysCount = _context.Pays.Count();
        dto.SocietesCount = _context.Societes.Count();
        dto.StatutsCount = _context.StatutsDesTickets.Count();
        dto.ClientsCount = clients;
        dto.PersonnelCount = totalUsers;
      }

      // 2) Comptage des Projets
      if (isSuperAdmin)
      {
        dto.ProjectsCount = _context.Projets.Count();
      }
      else if (isClient)
      {
        // For clients: count projects where they are associated via ProjetClient
        dto.ProjectsCount = _context.ProjetClients
              .Count(pc => pc.ClientId == userId);
      }
      else
      {
        // L'utilisateur est considéré associé s'il est ChefProjet ou présent dans ProjetUsers.
        dto.ProjectsCount = _context.Projets
            .Where(p => p.ChefProjetId == userId || p.ProjetUsers.Any(pu => pu.UserId == userId))
            .Count();
      }

      // 3) Comptage des Tickets selon la même logique que pour GetTicketsPagedAsync
      if (isSuperAdmin)
      {
        dto.TicketsCount = _context.Tickets.Count();
      }
      else
      {
        if (string.Equals(role, "chef de projet", StringComparison.OrdinalIgnoreCase))
        {
          dto.TicketsCount = _context.Tickets
              .Where(t => t.Projet != null &&
                         (t.Projet.ChefProjetId == userId ||
                          t.ResponsibleId == userId ||
                          t.Projet.ProjetUsers.Any(pu => pu.UserId == userId)))
              .Count();
        }
        else if (string.Equals(role, "collaborateur", StringComparison.OrdinalIgnoreCase))
        {
          dto.TicketsCount = _context.Tickets
              .Where(t => t.Projet != null &&
                         (t.ResponsibleId == userId ||
                          t.Projet.ProjetUsers.Any(pu => pu.UserId == userId)))
              .Count();
        }
        else if (isClient)
        {
          dto.TicketsCount = _context.Tickets.Where(t => t.OwnerId == userId).Count();
        }
        else
        {
          // Pour tout autre rôle, on compte les tickets associés via ProjetUsers.
          dto.TicketsCount = _context.Tickets
              .Where(t => t.Projet != null &&
                          t.Projet.ProjetUsers.Any(pu => pu.UserId == userId))
              .Count();
        }
      }

      // 4) Comptage par statut en appliquant le même filtrage que pour les tickets
      var filteredTickets = _context.Tickets.AsQueryable();
      if (!isSuperAdmin)
      {
        if (string.Equals(role, "chef de projet", StringComparison.OrdinalIgnoreCase))
        {
          filteredTickets = filteredTickets.Where(t => t.Projet != null &&
              (t.Projet.ChefProjetId == userId ||
               t.ResponsibleId == userId ||
               t.Projet.ProjetUsers.Any(pu => pu.UserId == userId)));
        }
        else if (string.Equals(role, "collaborateur", StringComparison.OrdinalIgnoreCase))
        {
          filteredTickets = filteredTickets.Where(t => t.Projet != null &&
              (t.ResponsibleId == userId ||
               t.Projet.ProjetUsers.Any(pu => pu.UserId == userId)));
        }
        else if (isClient)
        {
          filteredTickets = filteredTickets.Where(t => t.OwnerId == userId);
        }
        else
        {
          filteredTickets = filteredTickets.Where(t => t.Projet != null &&
              t.Projet.ProjetUsers.Any(pu => pu.UserId == userId));
        }
      }

      var ticketCountByStatus = _context.StatutsDesTickets
          .GroupJoin(
              filteredTickets,
              statut => statut.Id,
              ticket => ticket.StatutId,
              (statut, ticketsGroup) => new
              {
                Id = statut.Id,
                Name = statut.Name,
                Count = ticketsGroup.Count()
              }
          )
          .ToList();

      dto.TicketCountByStatus = ticketCountByStatus.Cast<object>().ToList();

      return dto;
    }

    public int GetMyTicketsCount(int userId)
    {
      return _context.Tickets
          .Count(t => t.ResponsibleId == userId);
    }

    public IEnumerable<TicketStatDto> GetTicketCountsByUserAndPeriod(
            int userId,
            string role,
            DateTime? start,
            DateTime? end,
            string granularity,
            int? filterUserId,
            int? projetId
        )
    {
      var query = _context.Tickets.AsQueryable();
      query = ApplyRoleFilter(query, userId, role, filterUserId);

      if (projetId.HasValue)
        query = query.Where(t => t.ProjetId == projetId.Value);
      if (start.HasValue)
        query = query.Where(t => t.CreatedAt >= start.Value.Date);
      if (end.HasValue)
        query = query.Where(t => t.CreatedAt < end.Value.Date.AddDays(1));

      return granularity.ToLower() switch
      {
        "daily" => GenerateStats(
            query.GroupBy(t => t.CreatedAt.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToList(),
            g => new TicketStatDto { Key = g.Date.ToString("yyyy-MM-dd"), Count = g.Count }
        ),
        "weekly" => GenerateStats(
            query
              .AsEnumerable()
              .GroupBy(t => CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(
                  t.CreatedAt.Date, CalendarWeekRule.FirstDay, DayOfWeek.Monday))
              .Select(g => new { Year = g.First().CreatedAt.Year, Week = g.Key, Count = g.Count() })
              .ToList(),
            g => new TicketStatDto { Key = $"S{g.Week} {g.Year}", Count = g.Count }
        ),
        "monthly" => GenerateStats(query.GroupBy(t => new { t.CreatedAt.Year, t.CreatedAt.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
            .ToList(), (g) => new TicketStatDto { Key = $"{g.Year}-{g.Month:D2}", Count = g.Count }),
        "yearly" => GenerateStats(query.GroupBy(t => t.CreatedAt.Year)
            .Select(g => new { Year = g.Key, Count = g.Count() })
            .ToList(), (g) => new TicketStatDto { Key = g.Year.ToString(), Count = g.Count }),
        _ => throw new ArgumentException("Granularity must be 'monthly' or 'yearly'.")
      };
    }

    public IEnumerable<TicketStatDto> GetTicketCountsByStatusAndPeriod(
    int userId, string role,
    DateTime? start, DateTime? end,
    string granularity,
    int? clientId, int? personnelId,
    int? projetId)
    {
      // 1️⃣ Filtre par rôle
      var query = ApplyRoleFilter(_context.Tickets.AsQueryable(), userId, role, null);

      // 2️⃣ Filtre client/personnel
      if (clientId.HasValue)
        query = query.Where(t => t.OwnerId == clientId.Value);
      if (personnelId.HasValue)
        query = query.Where(t => t.ResponsibleId == personnelId.Value);

      if (projetId.HasValue)
        query = query.Where(t => t.ProjetId == projetId.Value);
      // 3️⃣ Filtre dates
      if (start.HasValue)
        query = query.Where(t => t.CreatedAt >= start.Value.Date);
      if (end.HasValue)
        query = query.Where(t => t.CreatedAt < end.Value.Date.AddDays(1));

      // 4️⃣ Aggregation “none” ou par granularité existante
      if (granularity.Equals("none", StringComparison.OrdinalIgnoreCase))
      {
        var raw = query.GroupBy(t => t.StatutId)
                       .Select(g => new { StatusId = g.Key, Count = g.Count() })
                       .ToList();
        var names = _context.StatutsDesTickets
                            .Where(s => raw.Select(r => r.StatusId).Contains(s.Id))
                            .ToDictionary(s => s.Id, s => s.Name);
        return raw
            .Select(r => new TicketStatDto { Key = names[r.StatusId], Count = r.Count })
            .OrderBy(dto => dto.Key)
            .ToList();
      }
      return granularity.ToLower() switch
      {
        "daily" => GenerateStats(
            query.GroupBy(t => t.CreatedAt.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToList(),
            g => new TicketStatDto { Key = g.Date.ToString("yyyy-MM-dd"), Count = g.Count }
        ),
        "weekly" => GenerateStats(
            query
              .AsEnumerable()
              .GroupBy(t => CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(
                  t.CreatedAt.Date, CalendarWeekRule.FirstDay, DayOfWeek.Monday))
              .Select(g => new { Year = g.First().CreatedAt.Year, Week = g.Key, Count = g.Count() })
              .ToList(),
            g => new TicketStatDto { Key = $"S{g.Week} {g.Year}", Count = g.Count }
        ),
        "monthly" => GenerateStats(query.GroupBy(t => new { t.Statut.Name, t.CreatedAt.Year, t.CreatedAt.Month })
            .Select(g => new { Name = g.Key.Name, g.Key.Year, g.Key.Month, Count = g.Count() })
            .ToList(),
            (r) => new TicketStatDto { Key = $"{r.Name} - {r.Year}-{r.Month:D2}", Count = r.Count }),
        "yearly" => GenerateStats(query.GroupBy(t => new { t.Statut.Name, Year = t.CreatedAt.Year })
            .Select(g => new { Name = g.Key.Name, g.Key.Year, Count = g.Count() })
            .ToList(),
            (r) => new TicketStatDto { Key = $"{r.Name} - {r.Year}", Count = r.Count }),
        _ => throw new ArgumentException("Granularity must be 'none', 'monthly' or 'yearly'.")
      };
    }

    // Méthode utilitaire pour factoriser tri et projection
    private List<TicketStatDto> GenerateStats<T>(List<T> raw,
        Func<T, TicketStatDto> projector)
    {
      return raw
          .Select(projector)
          .OrderBy(dto => dto.Key)
          .ToList();
    }

    private IQueryable<Ticket> ApplyRoleFilter(IQueryable<Ticket> query, int userId, string role, int? filterUserId)
    {
      bool isSuperAdmin = role.Equals("super admin", StringComparison.OrdinalIgnoreCase);
      bool isClient = IsClient(filterUserId ?? userId);

      // Super admin no filter
      if (isSuperAdmin && filterUserId == null)
        return query;

      int actualUser = filterUserId ?? userId;

      IQueryable<Ticket> byRole;
      if (role.Equals("chef de projet", StringComparison.OrdinalIgnoreCase))
      {
        byRole = query.Where(t => t.Projet != null && (t.Projet.ChefProjetId == actualUser ||
                           t.ResponsibleId == actualUser ||
                           t.Projet.ProjetUsers.Any(pu => pu.UserId == actualUser)));
      }
      else if (role.Equals("collaborateur", StringComparison.OrdinalIgnoreCase))
      {
        byRole = query.Where(t => t.ResponsibleId == actualUser ||
                           t.Projet.ProjetUsers.Any(pu => pu.UserId == actualUser));
      }
      else if (isClient)
      {
        byRole = query.Where(t => t.OwnerId == actualUser);
      }
      else
      {
        byRole = query.Where(t => t.Projet != null &&
                           t.Projet.ProjetUsers.Any(pu => pu.UserId == actualUser));
      }

      var byCreator = query.Where(t => t.OwnerId == actualUser);
      return byRole.Union(byCreator);
    }

    public IEnumerable<TicketStatDto> GetTicketsFiltered(
              int currentUserId,
              string role,
              DateTime? start,
              DateTime? end,
              string granularity,
              int? clientId,
              int? personnelId,
              int? projetId)
    {
      // 1️⃣ Filtrage rôle
      var query = ApplyRoleFilter(_context.Tickets.AsQueryable(), currentUserId, role, null);

      // 2️⃣ Filtre client/personnel basé sur le FK UserId
      if (clientId.HasValue)
      {
        // clientId est l'ID de la table Clients, on récupère son UserId
        var userIdFilter = _context.Clients
            .Where(c => c.Id == clientId.Value)
            .Select(c => c.Id)
            .FirstOrDefault();
        query = query.Where(t => t.OwnerId == userIdFilter);
      }
      if (personnelId.HasValue)
      {
        query = query.Where(t => t.ResponsibleId == personnelId.Value);
      }

      // 3️⃣ Filtre dates
      if (start.HasValue)
        query = query.Where(t => t.CreatedAt >= start.Value.Date);
      if (end.HasValue)
        query = query.Where(t => t.CreatedAt < end.Value.Date.AddDays(1));

      if (projetId.HasValue)
        query = query.Where(t => t.ProjetId == projetId.Value);
      // 4️⃣ Agrégation
      return granularity.ToLower() switch
      {
        "daily" => GenerateStats(
            query.GroupBy(t => t.CreatedAt.Date)
                 .Select(g => new { Date = g.Key, Count = g.Count() })
                 .ToList(),
            g => new TicketStatDto { Key = g.Date.ToString("yyyy-MM-dd"), Count = g.Count }
        ),
        "weekly" => GenerateStats(
            query.AsEnumerable()
                 .GroupBy(t => CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(
                     t.CreatedAt.Date, CalendarWeekRule.FirstDay, DayOfWeek.Monday))
                 .Select(g => new { Year = g.First().CreatedAt.Year, Week = g.Key, Count = g.Count() })
                 .ToList(),
            g => new TicketStatDto { Key = $"S{g.Week} {g.Year}", Count = g.Count }
        ),
        "monthly" => GenerateStats(
            query.GroupBy(t => new { t.CreatedAt.Year, t.CreatedAt.Month })
                 .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
                 .ToList(),
            g => new TicketStatDto { Key = $"{g.Year}-{g.Month:D2}", Count = g.Count }
        ),
        "yearly" => GenerateStats(
            query.GroupBy(t => t.CreatedAt.Year)
                 .Select(g => new { Year = g.Key, Count = g.Count() })
                 .ToList(),
            g => new TicketStatDto { Key = g.Year.ToString(), Count = g.Count }
        ),
        _ => throw new ArgumentException("Granularity must be 'daily', 'weekly', 'monthly' or 'yearly'.")
      };
    }
  }
}