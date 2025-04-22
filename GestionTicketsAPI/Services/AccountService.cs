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
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly IMapper _mapper;

    public AccountService(
        IUserRepository userRepository,
        IAccountRepository accountRepository,
        ISocieteRepository societeRepository,
        ITokenService tokenService,
        IMapper mapper)
    {
      _accountRepository = accountRepository;
      _societeRepository = societeRepository;
      _userRepository = userRepository;
      _tokenService = tokenService;
      _mapper = mapper;
    }

    public async Task<UserDto> RegisterAsync(RegisterDto registerDto)
    {
      // 1. Vérifier si l'utilisateur existe déjà
      if (await _accountRepository.UserExistsAsync(registerDto.Firstname, registerDto.Lastname, registerDto.Email))
        throw new Exception("L'utilisateur existe déjà.");

      // 2. Récupérer le pays
      var pays = await _accountRepository.GetPaysByIdAsync(registerDto.Pays);
      if (pays == null)
        throw new Exception("Le pays spécifié est introuvable.");

      // 3. Si une société est spécifiée, vérifier son existence
      if (registerDto.SocieteId.HasValue)
      {
        var societe = await _societeRepository.GetSocieteByIdAsync(registerDto.SocieteId.Value);
        if (societe == null)
          throw new Exception("La société spécifiée est introuvable.");
      }

      // 4. Générer un mot de passe aléatoire de 8 caractères
      string generatedPassword = GenerateRandomPassword(8);

      // 5. Créer l'utilisateur et hacher son mot de passe
      using var hmac = new HMACSHA512();
      var passwordBytes = Encoding.UTF8.GetBytes(generatedPassword);

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
        PasswordHash = hmac.ComputeHash(passwordBytes),
        PasswordSalt = hmac.Key
      };

      // 6. Associer à une société si nécessaire
      if (registerDto.SocieteId.HasValue)
      {
        user.SocieteUsers.Add(new SocieteUser
        {
          SocieteId = registerDto.SocieteId.Value
        });
      }

      // 7. Enregistrer l'utilisateur en base
      await _accountRepository.AddUserAsync(user);
      if (!await _accountRepository.SaveAllAsync())
        throw new Exception("Erreur lors de l'enregistrement de l'utilisateur.");

      // 8. Ajouter un contrat si fourni
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
        if (!await _accountRepository.SaveAllAsync())
          throw new Exception("Erreur lors de l'enregistrement du contrat.");
      }

      // 9. Mapper vers UserDto, créer le token et exposer le mot de passe initial
      var userDto = _mapper.Map<UserDto>(user);
      userDto.Token = _tokenService.CreateToken(user);
      userDto.InitialPassword = generatedPassword;

      return userDto;
    }

    public async Task<UserDto> LoginAsync(LoginDto loginDto)
    {
      // 1) Récupérer l'utilisateur par e-mail (avec Actif)
      var user = await _accountRepository.GetUserByEmailAsync(loginDto.Email);
      if (user == null)
        throw new Exception("L'adresse e-mail est incorrecte.");

      // 2) Vérifier que l'utilisateur est actif
      if (!user.Actif)
        throw new Exception("Votre compte n'est pas activé. Veuillez contacter un administrateur.");

      // 3) Calcul du hash du mot de passe fourni
      using var hmac = new HMACSHA512(user.PasswordSalt);
      var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDto.Password));

      // 4) Comparaison des hash
      for (int i = 0; i < computedHash.Length; i++)
      {
        if (computedHash[i] != user.PasswordHash[i])
          throw new Exception("Le mot de passe est incorrect.");
      }

      // 5) Création du DTO et du token
      var userDto = _mapper.Map<UserDto>(user);
      userDto.Token = _tokenService.CreateToken(user);

      return userDto;
    }


    public async Task SaveResetTokenAsync(int userId, string token, DateTime expires)
    {
      // Récupérer l'utilisateur concerné
      var user = await _userRepository.GetUserByIdAsync(userId);
      if (user == null)
        throw new Exception("Utilisateur non trouvé.");

      user.PasswordResetToken = token;
      user.PasswordResetTokenExpires = expires;

      // Sauvegarder les modifications dans la base de données.
      if (!await _accountRepository.SaveAllAsync())
        throw new Exception("Erreur lors de la sauvegarde du token.");
    }
    public async Task<User> GetUserByResetTokenAsync(string token)
    {
      // Vous devez ajouter une méthode dans votre repository pour rechercher un utilisateur par token.
      var user = await _accountRepository.GetUserByResetTokenAsync(token);

      return user;
    }

    private static string GenerateRandomPassword(int length)
    {
      const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
      var data = new byte[length];
      using var rng = RandomNumberGenerator.Create();
      rng.GetBytes(data);

      var sb = new StringBuilder(length);
      foreach (var b in data)
        sb.Append(chars[b % chars.Length]);

      return sb.ToString();
    }


  }
}
