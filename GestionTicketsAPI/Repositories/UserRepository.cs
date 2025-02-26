using System;
using GestionTicketsAPI.Data;
using GestionTicketsAPI.Entities;
using GestionTicketsAPI.Helpers;
using GestionTicketsAPI.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GestionTicketsAPI.Repositories;

public class UserRepository : IUserRepository
{
  private readonly DataContext _context;

  public UserRepository(DataContext context)
  {
    _context = context;
  }

  public async Task<PagedList<User>> GetUsersAsync(UserParams userParams)
  {
    var query = _context.Users.AsQueryable();

    if (!string.IsNullOrEmpty(userParams.SearchTerm))
    {
      var lowerSearchTerm = userParams.SearchTerm.ToLower();
      query = query.Where(u => u.FirstName.ToLower().Contains(lowerSearchTerm)
                            || u.LastName.ToLower().Contains(lowerSearchTerm));
    }

    return await PagedList<User>.CreateAsync(query, userParams.PageNumber, userParams.PageSize);
  }


  public async Task<IEnumerable<User>> GetUsersNoPaginationAsync()
  {
    return await _context.Users.ToListAsync();
  }


  public async Task<User?> GetUserByIdAsync(int id)
  {
    return await _context.Users.FindAsync(id);
  }

  public async Task<User?> GetUserWithProjetUsersAsync(int id)
  {
    return await _context.Users
        .Include(u => u.ProjetUsers) // Vérifie que la propriété ProjetUsers existe dans ton entité User
        .FirstOrDefaultAsync(u => u.Id == id);
  }

  public async Task<bool> DeleteUserAsync(User user)
{
    // Charger explicitement les contrats associés (ici, en supposant que la relation est bien configurée via [InverseProperty])
    await _context.Entry(user)
        .Collection(u => u.Contrats)
        .LoadAsync();

    // Supprimer manuellement tous les contrats liés
    if (user.Contrats?.Any() == true)
    {
        _context.Contrats.RemoveRange(user.Contrats);
    }

    // Supprimer l'utilisateur
    _context.Users.Remove(user);

    // Sauvegarder les modifications
    return await _context.SaveChangesAsync() > 0;
}




  public async Task<bool> SaveAllAsync()
  {
    return await _context.SaveChangesAsync() > 0;
  }
}
