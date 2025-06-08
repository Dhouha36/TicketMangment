using System;

namespace GestionTicketsAPI.Repositories;

using System.Threading.Tasks;
using GestionTicketsAPI.Data;
using GestionTicketsAPI.Entities;
using Microsoft.EntityFrameworkCore;

public interface IContratUserRepository
{
    Task<ContratUser?> GetByIdAsync(int id);
    Task AddAsync(ContratUser contrat);
    void Update(ContratUser contrat);
    Task<bool> DeleteAsync(ContratUser contrat);
    Task<bool> SaveAllAsync();
}

public class ContratUserRepository : IContratUserRepository
{
    private readonly DataContext _context;
    public ContratUserRepository(DataContext context) => _context = context;

    public async Task<ContratUser?> GetByIdAsync(int id) =>
        await _context.ContratsUsers.FirstOrDefaultAsync(c => c.Id == id);

    public async Task AddAsync(ContratUser contrat) =>
        await _context.ContratsUsers.AddAsync(contrat);

    public void Update(ContratUser contrat) =>
        _context.ContratsUsers.Update(contrat);

    public async Task<bool> DeleteAsync(ContratUser contrat)
    {
        _context.ContratsUsers.Remove(contrat);
        return await SaveAllAsync();
    }

    public async Task<bool> SaveAllAsync() =>
        await _context.SaveChangesAsync() > 0;
}
