using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using GestionTicketsAPI.DTOs;
using GestionTicketsAPI.Entities;
using GestionTicketsAPI.Interfaces;

namespace GestionTicketsAPI.Services
{
  public class AccountService : IAccountService
  {
    private readonly IAccountRepository _accountRepository;
    private readonly ISocieteRepository _societeRepository;
    private readonly ITokenService _tokenService;
    private readonly IMapper _mapper;

    public AccountService(
        IAccountRepository accountRepository,
        ISocieteRepository societeRepository,
        ITokenService tokenService,
        IMapper mapper)
    {
      _accountRepository = accountRepository;
      _societeRepository = societeRepository;
      _tokenService = tokenService;
      _mapper = mapper;
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
  }
}