using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
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

    public UserService(IUserRepository userRepository, IMapper mapper)
    {
      _userRepository = userRepository;
      _mapper = mapper;
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
    public async Task<bool> UpdateUserAsync(UserUpdateDto userUpdateDto)
{
    // Récupération complète de l'utilisateur (avec Role et SocieteUsers)
    var userFromRepo = await _userRepository.GetUserByIdAsync(userUpdateDto.Id);
    if (userFromRepo == null)
        return false;

    // Mapper les propriétés simples du DTO vers l'entité existante
    _mapper.Map(userUpdateDto, userFromRepo);

    // Mise à jour du mot de passe s'il y a une nouvelle valeur
    if (!string.IsNullOrWhiteSpace(userUpdateDto.NouveauPassword))
    {
        CreatePasswordHash(userUpdateDto.NouveauPassword, out byte[] passwordHash, out byte[] passwordSalt);
        userFromRepo.PasswordHash = passwordHash;
        userFromRepo.PasswordSalt = passwordSalt;
    }

    _userRepository.Update(userFromRepo);
    return await _userRepository.SaveAllAsync();
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
