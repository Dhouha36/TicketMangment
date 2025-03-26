using System;
using GestionTicketsAPI.DTOs;
using GestionTicketsAPI.Entities;
using GestionTicketsAPI.Helpers;

namespace GestionTicketsAPI.Interfaces;

public interface IUserService
    {
        // Récupère tous les utilisateurs et les transforme en DTOs
        Task<PagedList<UserDto>> GetAllUsersAsync(UserParams userParams); 

        // Nouvelle méthode sans pagination
        Task<IEnumerable<UserDto>> GetAllUsersNoPaginationAsync();
        Task<PagedList<ProjetDto>> GetUserProjectsPagedAsync(int userId, UserParams userParams);
        Task<PagedList<TicketDto>> GetUserTicketsPagedAsync(int userId, UserParams userParams);
        
        // Récupère un utilisateur par son identifiant et le transforme en DTO
        Task<UserDto?> GetUserByIdAsync(int id);
        
        // Supprime un utilisateur via son identifiant
        Task<bool> DeleteUserAsync(int id);

        Task<bool> UpdateUserAsync(UserUpdateDto userUpdateDto);
        Task<IEnumerable<UserDto>> GetUsersByRoleAsync(string roleName);
    }