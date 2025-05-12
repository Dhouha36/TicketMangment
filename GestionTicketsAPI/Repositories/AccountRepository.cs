using GestionTicketsAPI.Data;
using GestionTicketsAPI.Entities;
using GestionTicketsAPI.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GestionTicketsAPI.Repositories
{
  public class AccountRepository : IAccountRepository
  {
    private readonly DataContext _context;
    public AccountRepository(DataContext context)
    {
      _context = context;
    }

    public async Task<bool> UserExistsAsync(string firstname, string lastname, string email)
    {
      return await _context.Users.AnyAsync(x =>
          (x.FirstName.ToLower() == firstname.ToLower() &&
           x.LastName.ToLower() == lastname.ToLower()) ||
           x.Email.ToLower() == email.ToLower());
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
      return await _context.Users
          .Include(u => u.PaysNavigation)
          .Include(u => u.Role)
          .Include(u => u.SocieteUsers)
              .ThenInclude(su => su.Societe)
          .FirstOrDefaultAsync(x => x.Email.ToLower() == email.ToLower());
    }

    public async Task AddUserAsync(User user)
    {
      await _context.Users.AddAsync(user);
    }

    // Ajout de la méthode pour ajouter un contrat
    public async Task AddContractAsync(Contrat contrat)
    {
      await _context.Contrats.AddAsync(contrat);
    }

    public async Task<bool> SaveAllAsync()
    {
      return await _context.SaveChangesAsync() > 0;
    }

    public async Task<Pays?> GetPaysByIdAsync(int paysId)
    {
      return await _context.Pays.FindAsync(paysId);
    }

    public async Task<int> GetRoleIdByNameAsync(string roleName)
    {
      var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name.ToLower() == roleName.ToLower());
      if (role == null)
        throw new Exception("Role not found");
      return role.Id;
    }

    public async Task<User?> GetUserByResetTokenAsync(string token)
    {
      // Adaptez ce code à votre contexte d'accès aux données et au stockage du token.
      return await _context.Users.FirstOrDefaultAsync(u => u.PasswordResetToken == token);
    }

    public async Task<bool> ClientExistsAsync(string email, string firstname, string lastname)
        {
            return await _context.Client.AnyAsync(c =>
                c.Email.ToLower() == email.ToLower() ||
                (c.FirstName.ToLower() == firstname.ToLower() && c.LastName.ToLower() == lastname.ToLower())
            );
        }

        public async Task<Client?> GetClientByEmailAsync(string email)
        {
            return await _context.Client
                .Include(c => c.Societe)
                .Include(c => c.PaysNavigation)
                .FirstOrDefaultAsync(c => c.Email.ToLower() == email.ToLower());
        }

        public async Task AddClientAsync(Client client)
        {
            await _context.Client.AddAsync(client);
        }

  }
}
