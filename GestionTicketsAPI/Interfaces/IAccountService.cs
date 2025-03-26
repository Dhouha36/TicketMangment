using System;
using GestionTicketsAPI.DTOs;
using GestionTicketsAPI.Entities;

namespace GestionTicketsAPI.Interfaces;

 public interface IAccountService
    {
        Task<UserDto> RegisterAsync(RegisterDto registerDto);
        Task<UserDto> LoginAsync(LoginDto loginDto);
        Task<User> GetUserByEmailAsync(string email);
        Task SavePasswordResetToken(User user, string token);
        Task<PasswordResetToken> GetPasswordResetToken(User user);
        Task RemovePasswordResetToken(User user);
        Task<bool> SaveAllAsync();
    }
