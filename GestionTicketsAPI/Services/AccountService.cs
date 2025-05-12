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


    public async Task<object> LoginAsync(LoginDto dto)
    {
      // 1) Try authenticate as User
      var user = await _accountRepository.GetUserByEmailAsync(dto.Email);
      if (user != null)
      {
        if (!user.Actif)
          throw new Exception("Compte utilisateur désactivé.");

        using var hmac = new HMACSHA512(user.PasswordSalt);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(dto.Password));
        for (int i = 0; i < hash.Length; i++)
          if (hash[i] != user.PasswordHash[i])
            throw new Exception("Email ou mot de passe incorrect.");

        var userDto = _mapper.Map<UserDto>(user);
        userDto.Token = _tokenService.CreateToken(user);
        return userDto;
      }

      // 2) Fallback: authenticate as Client
      var client = await _accountRepository.GetClientByEmailAsync(dto.Email);
      if (client == null || !client.Actif)
        throw new Exception("Email ou mot de passe incorrect.");

      using var hmacClient = new HMACSHA512(client.PasswordSalt);
      var clientHash = hmacClient.ComputeHash(Encoding.UTF8.GetBytes(dto.Password));
      for (int i = 0; i < clientHash.Length; i++)
        if (clientHash[i] != client.PasswordHash[i])
          throw new Exception("Email ou mot de passe incorrect.");

      // Return ClientDto directly
      var clientResult = _mapper.Map<ClientDto>(client);
      clientResult.Token = _tokenService.CreateToken(client);
      return clientResult;
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

    public async Task<ClientDto> RegisterClientAsync(RegisterClientDto dto)
    {
      if (await _accountRepository.ClientExistsAsync(dto.Email, dto.FirstName, dto.LastName))
        throw new Exception("Le client existe déjà.");

      var pays = await _accountRepository.GetPaysByIdAsync(dto.Pays);
      if (pays == null)
        throw new Exception("Pays introuvable.");

      var societe = await _societeRepository.GetSocieteByIdAsync(dto.SocieteId);
      if (societe == null)
        throw new Exception("Société introuvable.");

      using var hmac = new HMACSHA512();
      var pwdBytes = Encoding.UTF8.GetBytes(dto.Password);
      var client = new Client
      {
        Email = dto.Email,
        FirstName = dto.FirstName,
        LastName = dto.LastName,
        NumTelephone = dto.NumTelephone,
        Pays = dto.Pays,
        PaysNavigation = pays,
        SocieteId = dto.SocieteId,
        Societe = societe,
        PasswordHash = hmac.ComputeHash(pwdBytes),
        PasswordSalt = hmac.Key
      };

      await _accountRepository.AddClientAsync(client);
      if (!await _accountRepository.SaveAllAsync())
        throw new Exception("Erreur lors de l'enregistrement du client.");

      var clientDto = _mapper.Map<ClientDto>(client);
      clientDto.Token = _tokenService.CreateToken(client);
      return clientDto;
    }


  }
}
