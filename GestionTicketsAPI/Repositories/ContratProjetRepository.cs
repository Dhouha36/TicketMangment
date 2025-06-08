using System;
using GestionTicketsAPI.Data;
using GestionTicketsAPI.Entities;
using Microsoft.EntityFrameworkCore;

namespace GestionTicketsAPI.Repositories;

 public interface IContratProjetRepository
    {
        Task<ContratProjet?> GetByIdAsync(int id);
        Task AddAsync(ContratProjet contratProjet);
        void Update(ContratProjet contratProjet);
        Task<bool> DeleteAsync(ContratProjet contratProjet);
        Task<bool> SaveAllAsync();
    }

    public class ContratProjetRepository : IContratProjetRepository
    {
        private readonly DataContext _context;
        public ContratProjetRepository(DataContext context)
        {
            _context = context;
        }

        public async Task<ContratProjet?> GetByIdAsync(int id) =>
            await _context.ContratsProjets
                          .Include(cp => cp.Projet)
                          .FirstOrDefaultAsync(cp => cp.Id == id);

        public async Task AddAsync(ContratProjet contratProjet) =>
            await _context.ContratsProjets.AddAsync(contratProjet);

        public void Update(ContratProjet contratProjet) =>
            _context.ContratsProjets.Update(contratProjet);

        public async Task<bool> DeleteAsync(ContratProjet contratProjet)
        {
            _context.ContratsProjets.Remove(contratProjet);
            return await SaveAllAsync();
        }

        public async Task<bool> SaveAllAsync() =>
            await _context.SaveChangesAsync() > 0;
    }