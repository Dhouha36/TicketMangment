using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using GestionTicketsAPI.Data;
using GestionTicketsAPI.DTOs;
using GestionTicketsAPI.Entities;
using GestionTicketsAPI.Helpers;
using GestionTicketsAPI.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GestionTicketsAPI.Services
{
  public class AccountService : IAccountService
  {
    private readonly IAccountRepository _accountRepository;
    private readonly ISocieteRepository _societeRepository;
    private readonly ITokenService _tokenService;
    private readonly IMapper _mapper;
    private readonly DataContext _context;

    public AccountService(
        DataContext context,
        IAccountRepository accountRepository,
        ISocieteRepository societeRepository,
        ITokenService tokenService,
        IMapper mapper)
    {
      _accountRepository = accountRepository;
      _societeRepository = societeRepository;
      _tokenService = tokenService;
      _mapper = mapper;
      _context = context;
    }

    public async Task<UserDto> RegisterAsync(RegisterDto registerDto)
    {
      // Vérifier si l'utilisateur existe déjà
      if (await _accountRepository.UserExistsAsync(registerDto.Firstname, registerDto.Lastname, registerDto.Email))
        throw new Exception("L'utilisateur existe déjà.");

      // Récupération du pays
      var pays = await _accountRepository.GetPaysByIdAsync(registerDto.Pays);
      if (pays == null)
        throw new Exception("Le pays spécifié est introuvable.");

      // Si une société est spécifiée pour l'utilisateur, vérifier son existence
      if (registerDto.SocieteId.HasValue)
      {
        var societe = await _societeRepository.GetSocieteByIdAsync(registerDto.SocieteId.Value);
        if (societe == null)
          throw new Exception("La société spécifiée est introuvable.");
      }

      // Création de l'utilisateur avec hachage du mot de passe
      using var hmac = new HMACSHA512();
      var user = new User
      {
        FirstName = registerDto.Firstname,
        LastName = registerDto.Lastname,
        RoleId = await _accountRepository.GetRoleIdByNameAsync(registerDto.Role),
        Email = registerDto.Email,
        NumTelephone = registerDto.Numtelephone,
        Pays = registerDto.Pays,
        PaysNavigation = pays,
        Actif = registerDto.Actif,
        PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDto.Password)),
        PasswordSalt = hmac.Key,
      };

      // Si une société est spécifiée, créer l'association via SocieteUser
      if (registerDto.SocieteId.HasValue)
      {
        user.SocieteUsers.Add(new SocieteUser
        {
          SocieteId = registerDto.SocieteId.Value
          // La liaison avec l'utilisateur sera gérée automatiquement lors de l'ajout en base
        });
      }

      // Ajout de l'utilisateur en base
      await _accountRepository.AddUserAsync(user);

      // Sauvegarder pour générer l'ID utilisateur
      if (!await _accountRepository.SaveAllAsync())
        throw new Exception("Erreur lors de l'enregistrement de l'utilisateur.");

      // Gestion du contrat (optionnel) pour l'utilisateur, même s'il est lié à une société
      if (registerDto.Contract != null)
      {
        var contrat = new Contrat
        {
          DateDebut = registerDto.Contract.DateDebut,
          DateFin = registerDto.Contract.DateFin,
          TypeContrat = "Client-Societe",
          ClientId = user.Id
        };

        await _accountRepository.AddContractAsync(contrat);
        await _accountRepository.SaveAllAsync();
      }

      var userDto = _mapper.Map<UserDto>(user);
      userDto.Token = _tokenService.CreateToken(user);

      return userDto;
    }



    public async Task<UserDto> LoginAsync(LoginDto loginDto)
    {
      var user = await _accountRepository.GetUserByEmailAsync(loginDto.Email);
      if (user == null)
        throw new Exception("L'adresse e-mail est incorrecte");

      using var hmac = new HMACSHA512(user.PasswordSalt);
      var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDto.Password));

      for (int i = 0; i < computedHash.Length; i++)
      {
        if (computedHash[i] != user.PasswordHash[i])
          throw new Exception("Le mot de passe est incorrect");
      }

      var userDto = _mapper.Map<UserDto>(user);
      userDto.Token = _tokenService.CreateToken(user);

      return userDto;
    }

    public async Task<User> GetUserByEmailAsync(string email)
    {
      // Par exemple, utiliser le repository pour récupérer l'utilisateur
      return await _accountRepository.GetUserByEmailAsync(email);
    }

    public async Task SavePasswordResetToken(User user, string token)
    {
      // Supprimer un éventuel token existant pour l'utilisateur
      var existingToken = await _context.PasswordResetTokens.FirstOrDefaultAsync(p => p.UserId == user.Id);
      if (existingToken != null)
      {
        _context.PasswordResetTokens.Remove(existingToken);
      }

      var resetToken = new PasswordResetToken
      {
        UserId = user.Id,
        Token = token,
        Expiration = DateTime.UtcNow.AddHours(1) // par exemple 1 heure de validité
      };

      _context.PasswordResetTokens.Add(resetToken);
      await _context.SaveChangesAsync();
    }

    public async Task<PasswordResetToken> GetPasswordResetToken(User user)
    {
      return await _context.PasswordResetTokens.FirstOrDefaultAsync(p => p.UserId == user.Id);
    }

    public async Task RemovePasswordResetToken(User user)
    {
      var tokenEntity = await _context.PasswordResetTokens.FirstOrDefaultAsync(p => p.UserId == user.Id);
      if (tokenEntity != null)
      {
        _context.PasswordResetTokens.Remove(tokenEntity);
        await _context.SaveChangesAsync();
      }
    }

    public async Task<bool> SaveAllAsync()
    {
      return await _context.SaveChangesAsync() > 0;
    }
  }
}