using GestionTicketsAPI.Data;
using GestionTicketsAPI.Entities;
using GestionTicketsAPI.Helpers;
using Microsoft.EntityFrameworkCore;

namespace GestionTicketsAPI.Repositories
{
  public interface IClientRepository
  {
    Task<PagedList<Client>> GetClientsAsync(ClientParams clientParams);
    Task<IEnumerable<Client>> GetClientsNoPaginationAsync();
    Task<Client?> GetClientByIdAsync(int id);
    Task<Client?> GetClientWithProjectsAsync(int id);
    Task<bool> DeleteClientAsync(Client client);
    void Update(Client client);
    Task<bool> SaveAllAsync();
    Task<IEnumerable<Client>> GetClientsBySocieteAsync(int societeId);
    Task<IEnumerable<Ticket>> GetTicketsByClientAsync(int clientId);
    Task<IEnumerable<Projet>> GetProjectsByClientAsync(int clientId);
    Task<bool> AddClientToProjectAsync(int clientId, int projetId);
    Task<bool> RemoveClientFromProjectAsync(int clientId, int projetId);
  }

  public class ClientRepository : IClientRepository
  {
    private readonly DataContext _context;

    public ClientRepository(DataContext context)
    {
      _context = context;
    }

    public async Task<PagedList<Client>> GetClientsAsync(ClientParams clientParams)
    {
      var query = _context.Clients
          .Include(c => c.Societe)
          .Include(c => c.PaysNavigation)
          .Include(c => c.ProjetClients)
              .ThenInclude(pc => pc.Projet)
          .AsQueryable();

      // Filtre “contient” insensible à la casse
      if (!string.IsNullOrWhiteSpace(clientParams.SearchTerm))
      {
        // Séparer tous les segments non vides
        var parts = clientParams.SearchTerm
                       .Trim()
                       .ToLower()
                       .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // Pour chaque mot, on ajoute une condition (ici en AND, pour que tous soient trouvés)
        foreach (var part in parts)
        {
          query = query.Where(c =>
              c.FirstName.ToLower().Contains(part) ||
              c.LastName.ToLower().Contains(part) ||
              c.Email.ToLower().Contains(part)
          );
        }
      }

      // Autres filtres (Actif, SocieteId, ProjetId…)
      if (clientParams.Actif.HasValue)
        query = query.Where(c => c.Actif == clientParams.Actif.Value);

      if (clientParams.SocieteId.HasValue)
        query = query.Where(c => c.SocieteId == clientParams.SocieteId.Value);

      if (clientParams.ProjetId.HasValue)
        query = query.Where(c => c.ProjetClients.Any(pc => pc.ProjetId == clientParams.ProjetId.Value));

      // Tri (par date de création décroissante)
      query = query.OrderByDescending(c => c.CreatedAt);

      // Pagination
      return await PagedList<Client>.CreateAsync(
          query,
          clientParams.PageNumber,
          clientParams.PageSize
      );
    }

    public async Task<IEnumerable<Client>> GetClientsNoPaginationAsync()
    {
      return await _context.Clients
          .Include(c => c.Societe)
          .Include(c => c.PaysNavigation)
          .Include(c => c.ProjetClients)
            .ThenInclude(pc => pc.Projet)
          .ToListAsync();
    }

    public async Task<Client?> GetClientByIdAsync(int id)
    {
      return await _context.Clients
          .Include(c => c.Societe)
          .Include(c => c.PaysNavigation)
          .Include(c => c.ProjetClients)
            .ThenInclude(pc => pc.Projet)
          .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Client?> GetClientWithProjectsAsync(int id)
    {
      return await _context.Clients
          .Include(c => c.ProjetClients)
              .ThenInclude(pc => pc.Projet)
          .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<IEnumerable<Client>> GetClientsBySocieteAsync(int societeId)
    {
      return await _context.Clients
          .Include(c => c.Societe)
          .Include(c => c.ProjetClients)
              .ThenInclude(pc => pc.Projet)
                .ThenInclude(p => p.ContratProjet)
          .Where(c => c.SocieteId == societeId)
          .ToListAsync();
    }

    public async Task<bool> DeleteClientAsync(Client client)
    {
      // Optionally check constraints (e.g. tickets)
      _context.Clients.Remove(client);
      return await _context.SaveChangesAsync() > 0;
    }

    public void Update(Client client)
    {
      _context.Entry(client).State = EntityState.Modified;
    }

    public async Task<bool> SaveAllAsync()
    {
      return await _context.SaveChangesAsync() > 0;
    }

    public async Task<IEnumerable<Ticket>> GetTicketsByClientAsync(int clientId)
    {
      return await _context.Tickets
          .Where(t => t.OwnerId == clientId)
          .Include(t => t.Priority)
          .Include(t => t.Qualification)
          .Include(t => t.ProblemCategory)
          .Include(t => t.Statut)
          .Include(t => t.Projet)
          .Include(t => t.Owner)
          .Include(t => t.Responsible)
          .ToListAsync();
    }
    public async Task<IEnumerable<Projet>> GetProjectsByClientAsync(int clientId)
    {
      return await _context.ProjetClients
          .Where(pc => pc.ClientId == clientId)
          .Include(pc => pc.Projet)
              .ThenInclude(p => p.Pays)
          .Include(pc => pc.Projet)
              .ThenInclude(p => p.ContratProjet)
          .Include(pc => pc.Projet)
              .ThenInclude(p => p.Societe)
          .Include(pc => pc.Projet)
              .ThenInclude(p => p.ChefProjet)
          .Select(pc => pc.Projet)
          .ToListAsync();
    }

    public async Task<bool> AddClientToProjectAsync(int clientId, int projetId)
    {
      // Vérifier que l’association n’existe pas déjà
      var exists = await _context.ProjetClients
          .AnyAsync(pc => pc.ClientId == clientId && pc.ProjetId == projetId);
      if (exists) return false;

      // Créer l’association
      var pc = new ProjetClient
      {
        ClientId = clientId,
        ProjetId = projetId
      };
      _context.ProjetClients.Add(pc);
      return await _context.SaveChangesAsync() > 0;
    }
    public async Task<bool> RemoveClientFromProjectAsync(int clientId, int projetId)
    {
      var association = await _context.ProjetClients
          .FirstOrDefaultAsync(pc => pc.ClientId == clientId && pc.ProjetId == projetId);
      if (association == null) return false;

      _context.ProjetClients.Remove(association);
      return await _context.SaveChangesAsync() > 0;
    }
  }
}
