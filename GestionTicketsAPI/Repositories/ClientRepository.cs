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
            var query = _context.Client
                .Include(c => c.Societe)
                .Include(c => c.PaysNavigation)
                .Include(c => c.ProjetClients)
                    .ThenInclude(pc => pc.Projet)
                .AsQueryable();

            if (!string.IsNullOrEmpty(clientParams.SearchTerm))
            {
                var term = clientParams.SearchTerm.ToLower();
                query = query.Where(c => c.FirstName.ToLower().Contains(term) || c.LastName.ToLower().Contains(term));
            }

            if (clientParams.Actif.HasValue)
                query = query.Where(c => c.Actif == clientParams.Actif.Value);

            return await PagedList<Client>.CreateAsync(query.OrderByDescending(c => c.CreatedAt), clientParams.PageNumber, clientParams.PageSize);
        }

        public async Task<IEnumerable<Client>> GetClientsNoPaginationAsync()
        {
            return await _context.Client
                .Include(c => c.Societe)
                .Include(c => c.PaysNavigation)
                .ToListAsync();
        }

        public async Task<Client?> GetClientByIdAsync(int id)
        {
            return await _context.Client
                .Include(c => c.Societe)
                .Include(c => c.PaysNavigation)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Client?> GetClientWithProjectsAsync(int id)
        {
            return await _context.Client
                .Include(c => c.ProjetClients)
                    .ThenInclude(pc => pc.Projet)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<IEnumerable<Client>> GetClientsBySocieteAsync(int societeId)
        {
            return await _context.Client
                .Where(c => c.SocieteId == societeId)
                .ToListAsync();
        }

        public async Task<bool> DeleteClientAsync(Client client)
        {
            // Optionally check constraints (e.g. tickets)
            _context.Client.Remove(client);
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
    }
}
