using System;
using System.Globalization;
using System.Linq;
using GestionTicketsAPI.Data;
using GestionTicketsAPI.DTOs;
using GestionTicketsAPI.Entities;
using Microsoft.EntityFrameworkCore;

namespace GestionTicketsAPI.Services
{
  public interface IDashboardService
  {
    DashboardCountsDto GetDashboardCounts(int userId, string role);
    int GetTicketsCountAsResponsible(int userId);
    int GetTicketsCountAsProjectMember(int userId);
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

    IEnumerable<TicketStatDto> GetTimeSpentByPeriod(int userId, string role, TicketFilterRequest filter);
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

      // 1) Déterminer si super admin ou client
      bool isSuperAdmin = string.Equals(role, "super admin", StringComparison.OrdinalIgnoreCase);
      bool isClient = IsClient(userId);

      // 1a) Comptage général (superadmin uniquement)
      if (isSuperAdmin)
      {
        dto.CategoriesCount = _context.CategorieProblemes.Count();
        dto.PaysCount = _context.Pays.Count();
        dto.SocietesCount = _context.Societes.Count();
        dto.StatutsCount = _context.StatutsDesTickets.Count();
        dto.ClientsCount = _context.Clients.Count();
        dto.PersonnelCount = _context.Users.Count();
        dto.ProjectsCount = _context.Projets.Count();
        dto.TicketsCount = _context.Tickets.Count();
      }
      else
      {
        // 1b) Comptage des projets selon le rôle / client
        if (isClient)
        {
          // Projets où le client est associé via ProjetClient
          dto.ProjectsCount = _context.ProjetClients
              .Count(pc => pc.ClientId == userId);
        }
        else
        {
          // Pour chef de projet ou collaborateur ou tout autre rôle non-admin non-client,
          // on compte les projets où l'utilisateur est chef de projet ou membre
          dto.ProjectsCount = _context.Projets
              .Where(p => p.ChefProjetId == userId
                          || p.ProjetUsers.Any(pu => pu.UserId == userId))
              .Count();
        }

        // 1c) Comptage des tickets visibles à l'utilisateur
        if (string.Equals(role, "chef de projet", StringComparison.OrdinalIgnoreCase))
        {
          // Chef de projet : tickets où il est responsable OU membre de projet
          dto.TicketsCount = _context.Tickets
              .Where(t => (t.ResponsibleId == userId)
                          || (t.Projet != null
                              && t.Projet.ProjetUsers.Any(pu => pu.UserId == userId)))
              .Count();
        }
        else if (string.Equals(role, "collaborateur", StringComparison.OrdinalIgnoreCase))
        {
          // Collaborateur : tickets où il est responsable OU membre de projet
          dto.TicketsCount = _context.Tickets
              .Where(t => (t.ResponsibleId == userId)
                          || (t.Projet != null
                              && t.Projet.ProjetUsers.Any(pu => pu.UserId == userId)))
              .Count();
        }
        else if (isClient)
        {
          // Client : tickets dont il est propriétaire
          dto.TicketsCount = _context.Tickets.Count(t => t.OwnerId == userId);
        }
        else
        {
          // Tout autre rôle : tickets où il est membre de projet
          dto.TicketsCount = _context.Tickets
              .Where(t => t.Projet != null
                          && t.Projet.ProjetUsers.Any(pu => pu.UserId == userId))
              .Count();
        }
      }

      // 2) Comptage par statut (mêmes filtres que pour TicketsCount)
      IQueryable<Ticket> filteredTickets = _context.Tickets.AsQueryable();
      if (!isSuperAdmin)
      {
        if (string.Equals(role, "chef de projet", StringComparison.OrdinalIgnoreCase))
        {
          filteredTickets = filteredTickets.Where(t =>
              (t.ResponsibleId == userId)
              || (t.Projet != null && t.Projet.ProjetUsers.Any(pu => pu.UserId == userId))
          );
        }
        else if (string.Equals(role, "collaborateur", StringComparison.OrdinalIgnoreCase))
        {
          filteredTickets = filteredTickets.Where(t =>
              (t.ResponsibleId == userId)
              || (t.Projet != null && t.Projet.ProjetUsers.Any(pu => pu.UserId == userId))
          );
        }
        else if (isClient)
        {
          filteredTickets = filteredTickets.Where(t => t.OwnerId == userId);
        }
        else
        {
          filteredTickets = filteredTickets.Where(t =>
              t.Projet != null && t.Projet.ProjetUsers.Any(pu => pu.UserId == userId)
          );
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

    public int GetTicketsCountAsResponsible(int userId)
    {
      return _context.Tickets
          .Count(t => t.ResponsibleId == userId);
    }

    public int GetTicketsCountAsProjectMember(int userId)
    {
      return _context.Tickets
      .Where(t => t.Projet != null)
      .Count(t =>
        t.Projet.ChefProjetId == userId
        || t.Projet.ProjetUsers.Any(pu => pu.UserId == userId)
      );
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
      // Appliquer le filtre de rôle dès le départ
      var query = ApplyRoleFilter(_context.Tickets.AsQueryable(), userId, role, filterUserId);

      if (projetId.HasValue)
      {
        query = query.Where(t => t.ProjetId == projetId.Value);
      }
      if (start.HasValue)
      {
        query = query.Where(t => t.CreatedAt >= start.Value.Date);
      }
      if (end.HasValue)
      {
        // On compare avec la date du lendemain pour inclure toute la journée de end
        query = query.Where(t => t.CreatedAt < end.Value.Date.AddDays(1));
      }

      switch (granularity.ToLower())
      {
        case "daily":
          var daily = query
              .GroupBy(t => t.CreatedAt.Date)
              .Select(g => new { Date = g.Key, Count = g.Count() })
              .ToList();
          return GenerateStats(
              daily,
              g => new TicketStatDto
              {
                Key = g.Date.ToString("yyyy-MM-dd"),
                Count = g.Count
              }
          );

        case "weekly":
          var weeklyRaw = query
              .AsEnumerable()
              .GroupBy(t => CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(
                  t.CreatedAt.Date, CalendarWeekRule.FirstDay, DayOfWeek.Monday))
              .Select(g => new { Year = g.First().CreatedAt.Year, Week = g.Key, Count = g.Count() })
              .ToList();
          return GenerateStats(
              weeklyRaw,
              g => new TicketStatDto
              {
                Key = $"S{g.Week} {g.Year}",
                Count = g.Count
              }
          );

        case "monthly":
          var monthly = query
              .GroupBy(t => new { t.CreatedAt.Year, t.CreatedAt.Month })
              .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
              .ToList();
          return GenerateStats(
              monthly,
              g => new TicketStatDto
              {
                Key = $"{g.Year}-{g.Month:D2}",
                Count = g.Count
              }
          );

        case "yearly":
          var yearly = query
              .GroupBy(t => t.CreatedAt.Year)
              .Select(g => new { Year = g.Key, Count = g.Count() })
              .ToList();
          return GenerateStats(
              yearly,
              g => new TicketStatDto
              {
                Key = g.Year.ToString(),
                Count = g.Count
              }
          );

        default:
          throw new ArgumentException("Granularity must be 'daily', 'weekly', 'monthly' or 'yearly'.");
      }
    }

    public IEnumerable<TicketStatDto> GetTicketCountsByStatusAndPeriod(
            int userId,
            string role,
            DateTime? start,
            DateTime? end,
            string granularity,
            int? clientId,
            int? personnelId,
            int? projetId
        )
    {
      // 1) Filtre initial selon rôle
      var query = ApplyRoleFilter(_context.Tickets.AsQueryable(), userId, role, null);

      // 2) Filtre par client ou personnel si spécifié
      if (clientId.HasValue)
      {
        query = query.Where(t => t.OwnerId == clientId.Value);
      }
      if (personnelId.HasValue)
      {
        query = query.Where(t => t.ResponsibleId == personnelId.Value);
      }

      // 3) Filtre par projet si spécifié
      if (projetId.HasValue)
      {
        query = query.Where(t => t.ProjetId == projetId.Value);
      }

      // 4) Filtre par plage de dates
      if (start.HasValue)
      {
        query = query.Where(t => t.CreatedAt >= start.Value.Date);
      }
      if (end.HasValue)
      {
        query = query.Where(t => t.CreatedAt < end.Value.Date.AddDays(1));
      }

      // 5) Si granularité = "none", on compte par statut directement
      if (granularity.Equals("none", StringComparison.OrdinalIgnoreCase))
      {
        var raw = query
            .GroupBy(t => t.StatutId)
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

      // 6) Sinon, on agrége selon la granularité spécifiée
      switch (granularity.ToLower())
      {
        case "daily":
          var daily = query
              .GroupBy(t => t.CreatedAt.Date)
              .Select(g => new { Date = g.Key, Count = g.Count() })
              .ToList();
          return GenerateStats(
              daily,
              g => new TicketStatDto
              {
                Key = g.Date.ToString("yyyy-MM-dd"),
                Count = g.Count
              }
          );

        case "weekly":
          var weeklyRaw = query
              .AsEnumerable()
              .GroupBy(t => CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(
                  t.CreatedAt.Date, CalendarWeekRule.FirstDay, DayOfWeek.Monday))
              .Select(g => new { Year = g.First().CreatedAt.Year, Week = g.Key, Count = g.Count() })
              .ToList();
          return GenerateStats(
              weeklyRaw,
              g => new TicketStatDto
              {
                Key = $"S{g.Week} {g.Year}",
                Count = g.Count
              }
          );

        case "monthly":
          var monthly = query
              .GroupBy(t => new { t.Statut.Name, t.CreatedAt.Year, t.CreatedAt.Month })
              .Select(g => new { Name = g.Key.Name, g.Key.Year, g.Key.Month, Count = g.Count() })
              .ToList();
          return GenerateStats(
              monthly,
              r => new TicketStatDto
              {
                Key = $"{r.Name} - {r.Year}-{r.Month:D2}",
                Count = r.Count
              }
          );

        case "yearly":
          var yearly = query
              .GroupBy(t => new { t.Statut.Name, Year = t.CreatedAt.Year })
              .Select(g => new { Name = g.Key.Name, g.Key.Year, Count = g.Count() })
              .ToList();
          return GenerateStats(
              yearly,
              r => new TicketStatDto
              {
                Key = $"{r.Name} - {r.Year}",
                Count = r.Count
              }
          );

        default:
          throw new ArgumentException("Granularity must be 'none', 'daily', 'weekly', 'monthly' or 'yearly'.");
      }
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

      // 2️ Filtre client/personnel basé sur le FK UserId
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

    public IEnumerable<TicketStatDto> GetTimeSpentByPeriod(int userId, string role, TicketFilterRequest filter)
{
    // Filtrage selon le rôle
    var query = ApplyRoleFilter(_context.Tickets.AsQueryable(), userId, role, null);

    // Filtre par propriétaire, personnel et projet
    if (filter.OwnerId.HasValue)
        query = query.Where(t => t.OwnerId == filter.OwnerId.Value);
    if (filter.PersonnelId.HasValue)
        query = query.Where(t => t.ResponsibleId == filter.PersonnelId.Value);
    if (filter.ProjetId.HasValue)
        query = query.Where(t => t.ProjetId == filter.ProjetId.Value);

    // Filtre par plage de dates
    if (filter.Start.HasValue)
        query = query.Where(t => t.CreatedAt >= filter.Start.Value.Date);
    if (filter.End.HasValue)
        query = query.Where(t => t.CreatedAt < filter.End.Value.Date.AddDays(1));

    // Agrégation selon la granularité demandée
    switch (filter.Granularity.ToLowerInvariant())
    {
        case "daily":
        {
            var dailyRaw = query
                .GroupBy(t => t.CreatedAt.Date)
                .Select(g => new { Date = g.Key, Value = g.Sum(t => (double?)t.HoursSpent + t.MinutesSpent/60.0) ?? 0.0 })
                .ToList();

            return GenerateStats(dailyRaw, g => new TicketStatDto
            {
                Key = g.Date.ToString("yyyy-MM-dd"),
                Value = Math.Round(g.Value, 2)
            });
        }

        case "weekly":
        {
            var weeklyRaw = query
                .AsEnumerable()
                .GroupBy(t => CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(
                    t.CreatedAt.Date,
                    CalendarWeekRule.FirstDay,
                    DayOfWeek.Monday))
                .Select(g => new
                {
                    Year = g.First().CreatedAt.Year,
                    Week = g.Key,
                    Value = g.Sum(t => (double?)t.HoursSpent + t.MinutesSpent/60.0) ?? 0.0
                })
                .ToList();

            return GenerateStats(weeklyRaw, g => new TicketStatDto
            {
                Key = $"S{g.Week} {g.Year}",
                Value = Math.Round(g.Value, 2)
            });
        }

        case "monthly":
        {
            var monthlyRaw = query
                .GroupBy(t => new { t.CreatedAt.Year, t.CreatedAt.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Value = g.Sum(t => (double?)t.HoursSpent + t.MinutesSpent/60.0) ?? 0.0
                })
                .ToList();

            return GenerateStats(monthlyRaw, g => new TicketStatDto
            {
                Key = $"{g.Year}-{g.Month:D2}",
                Value = Math.Round(g.Value, 2)
            });
        }

        default:
            throw new ArgumentException("Granularity must be 'daily', 'weekly' or 'monthly'.", nameof(filter.Granularity));
    }
}
  }
}