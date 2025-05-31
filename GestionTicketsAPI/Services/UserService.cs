using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using GestionTicketsAPI.Data;
using GestionTicketsAPI.DTOs;
using GestionTicketsAPI.Entities;
using GestionTicketsAPI.Helpers;
using GestionTicketsAPI.Interfaces;

namespace GestionTicketsAPI.Services
{
  public class UserService : IUserService
  {
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;
    private readonly IPhotoService _photoService;
    private readonly DataContext _context;

    public UserService(IUserRepository userRepository, IMapper mapper,
        IPhotoService photoService, DataContext context)
    {
      _userRepository = userRepository;
      _mapper = mapper;
      _photoService = photoService;
      _context = context;
    }

    public async Task<PagedList<UserDto>> GetAllUsersAsync(UserParams userParams)
    {
      var usersPaged = await _userRepository.GetUsersAsync(userParams);
      var userDtos = _mapper.Map<IEnumerable<UserDto>>(usersPaged.ToList());
      var pagedUserDtos = new PagedList<UserDto>(
          userDtos.ToList(),
          usersPaged.TotalCount,
          usersPaged.CurrentPage,
          usersPaged.PageSize
      );
      return pagedUserDtos;
    }

    public async Task<IEnumerable<UserDto>> GetAllUsersNoPaginationAsync()
    {
      var users = await _userRepository.GetUsersNoPaginationAsync();
      return _mapper.Map<IEnumerable<UserDto>>(users);
    }

    public async Task<UserDto?> GetUserByIdAsync(int id)
    {
      var user = await _userRepository.GetUserByIdAsync(id);
      return user == null ? null : _mapper.Map<UserDto>(user);
    }

    public async Task<bool> DeleteUserAsync(int id)
    {
      var user = await _userRepository.GetUserWithProjetUsersAsync(id);
      if (user == null)
        return false;
      return await _userRepository.DeleteUserAsync(user);
    }

    public async Task<PagedList<ProjetDto>> GetUserProjectsPagedAsync(int userId, UserParams userParams)
    {
      var projetsPaged = await _userRepository.GetUserProjectsAsync(userId, userParams);
      var projetDtos = _mapper.Map<IEnumerable<ProjetDto>>(projetsPaged.ToList());
      var pagedProjetDtos = new PagedList<ProjetDto>(
          projetDtos.ToList(),
          projetsPaged.TotalCount,
          projetsPaged.CurrentPage,
          projetsPaged.PageSize
      );
      return pagedProjetDtos;
    }

    public async Task<PagedList<TicketDto>> GetUserTicketsPagedAsync(int userId, UserParams userParams)
    {
      var ticketsPaged = await _userRepository.GetUserTicketsAsync(userId, userParams);
      var ticketDtos = _mapper.Map<IEnumerable<TicketDto>>(ticketsPaged.ToList());
      var pagedTicketDtos = new PagedList<TicketDto>(
          ticketDtos.ToList(),
          ticketsPaged.TotalCount,
          ticketsPaged.CurrentPage,
          ticketsPaged.PageSize
      );
      return pagedTicketDtos;
    }


    // Ajout de la fonctionnalité de mise à jour d'un utilisateur
    public async Task<UserDto?> UpdateUserAsync(UserUpdateDto userUpdateDto)
{
    // 1) Chargement de l’utilisateur existant
    var userFromRepo = await _userRepository.GetUserByIdAsync(userUpdateDto.Id);
    if (userFromRepo == null)
        return null;

    // 2) Mapper les champs simples
    _mapper.Map(userUpdateDto, userFromRepo);

    // 3) Gérer la photo si présente
    if (userUpdateDto.PhotoFile != null && userUpdateDto.PhotoFile.Length > 0)
    {
        // 3a) Si l’utilisateur avait déjà une photo, la supprimer
        if (userFromRepo.Photo != null && !string.IsNullOrEmpty(userFromRepo.Photo.PublicId))
        {
            try
            {
                // Supprimer côté Cloudinary
                var deletionResult = await _photoService.DeletePhotoAsync(userFromRepo.Photo.PublicId);
                // (Optionnel : vérifier que deletionResult.Result == "ok")
            }
            catch (Exception ex)
            {
                // Loguez ou ignorez l’erreur de suppression Cloudinary
                // _logger.LogWarning($"Échec suppression ancienne photo Cloudinary : {ex.Message}");
            }

            // Supprimer l’entité Photo en base
            _context.Photos.Remove(userFromRepo.Photo);
        }

        // 3b) Uploader la nouvelle photo
        var uploadResult = await _photoService.AddPhotoAsync(userUpdateDto.PhotoFile);

        // 3c) Créer et lier la nouvelle entité Photo
        var newPhoto = new Photo
        {
            Url = uploadResult.SecureUrl.ToString(),
            PublicId = uploadResult.PublicId
        };
        userFromRepo.Photo = newPhoto;
    }

    // 4) Gérer le changement de mot de passe si demandé
    if (!string.IsNullOrWhiteSpace(userUpdateDto.NouveauPassword))
    {
        CreatePasswordHash(userUpdateDto.NouveauPassword, out byte[] passwordHash, out byte[] passwordSalt);
        userFromRepo.PasswordHash = passwordHash;
        userFromRepo.PasswordSalt = passwordSalt;
    }

    // 5) Sauvegarder la mise à jour
    _userRepository.Update(userFromRepo);
    var success = await _userRepository.SaveAllAsync();
    if (!success)
        return null;

    // 6) Mapper l’entité modifiée vers le DTO
    var userDto = _mapper.Map<UserDto>(userFromRepo);
    return userDto;
}


    private void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
    {
      using (var hmac = new HMACSHA512())
      {
        passwordSalt = hmac.Key;
        passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
      }
    }



    public async Task<IEnumerable<UserDto>> GetUsersByRoleAsync(string roleName)
    {
      var users = await _userRepository.GetUsersByRoleAsync(roleName);
      return _mapper.Map<IEnumerable<UserDto>>(users);
    }

    public async Task<IEnumerable<UserDto>> GetUsersFilteredAsync(UserParams userParams)
    {
      var users = await _userRepository.GetUsersFilteredAsync(userParams);
      return _mapper.Map<IEnumerable<UserDto>>(users);
    }


  }
}
