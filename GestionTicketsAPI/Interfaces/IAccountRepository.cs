using System;
using GestionTicketsAPI.Entities;

namespace GestionTicketsAPI.Interfaces;

public interface IAccountRepository
    {
        // Vérifie si un utilisateur existe (par prénom, nom ou email)
        Task<bool> UserExistsAsync(string firstname, string lastname, string email);

        // Récupère un utilisateur par son email (en incluant PaysNavigation si besoin)
        Task<User?> GetUserByEmailAsync(string email);

        // Ajoute un nouvel utilisateur
        Task AddUserAsync(User user);
        Task AddContractAsync(Contrat contrat);

        // Sauvegarde les modifications
        Task<bool> SaveAllAsync();

        // Récupère un pays par son identifiant
        Task<Pays?> GetPaysByIdAsync(int paysId);
        Task<int> GetRoleIdByNameAsync(string roleName);
        Task<User?> GetUserByResetTokenAsync(string token);
        Task<bool> ClientExistsAsync(string email, string firstname, string lastname);
        Task<Client?> GetClientByEmailAsync(string email);
        Task AddClientAsync(Client client);


    }
