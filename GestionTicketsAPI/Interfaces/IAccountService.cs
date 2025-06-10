using System;
using GestionTicketsAPI.DTOs;
using GestionTicketsAPI.Entities;

namespace GestionTicketsAPI.Interfaces;

 public interface IAccountService
    {
        Task<UserDto> RegisterAsync(RegisterDto registerDto);
        Task<ClientDto> RegisterClientAsync(RegisterClientDto dto);
        Task<object> LoginAsync(LoginDto dto);
        Task SaveResetTokenForUserAsync(int userId, string token, DateTime expires);

        Task SaveResetTokenForClientAsync(int clientId, string token, DateTime expires);
        Task<User> GetUserByResetTokenAsync(string token);
        Task<Client> GetClientByResetTokenAsync(string token);
    }
