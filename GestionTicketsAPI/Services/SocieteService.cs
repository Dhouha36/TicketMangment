using AutoMapper;
using GestionTicketsAPI.DTOs;
using GestionTicketsAPI.Entities;
using GestionTicketsAPI.Helpers;
using GestionTicketsAPI.Interfaces;
using GestionTicketsAPI.Repositories;

namespace GestionTicketsAPI.Services
{
  public class SocieteService : ISocieteService
  {
    private readonly ISocieteRepository _societeRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly IMapper _mapper;

    public SocieteService(ISocieteRepository societeRepository, IAccountRepository accountRepository, IMapper mapper)
    {
      _societeRepository = societeRepository;
      _accountRepository = accountRepository;
      _mapper = mapper;
    }

    public async Task<IEnumerable<SocieteDto>> GetAllSocietesAsync(string? searchTerm = null, string? pays = null)
    {
      var societes = await _societeRepository.GetAllSocietesAsync(searchTerm, pays);
      return _mapper.Map<IEnumerable<SocieteDto>>(societes);
    }

    public async Task<PagedList<SocieteDto>> GetSocietesPagedAsync(UserParams userParams)
    {
      var societesPaged = await _societeRepository.GetSocietesPagedAsync(userParams);
      var societesDto = _mapper.Map<IEnumerable<SocieteDto>>(societesPaged);
      var pagedSocieteDtos = new PagedList<SocieteDto>(
          societesDto.ToList(),
          societesPaged.TotalCount,
          societesPaged.CurrentPage,
          societesPaged.PageSize
      );
      return pagedSocieteDtos;
    }


    public async Task<SocieteDto?> GetSocieteByIdAsync(int id)
    {
      var societe = await _societeRepository.GetSocieteByIdAsync(id);
      return societe == null ? null : _mapper.Map<SocieteDto>(societe);
    }

    public async Task<SocieteDetailsDto?> GetSocieteWithDetailsByIdAsync(int id)
    {
      var societe = await _societeRepository.GetSocieteWithDetailsByIdAsync(id);
      return societe == null ? null : _mapper.Map<SocieteDetailsDto>(societe);
    }

    public async Task<SocieteDto> AddSocieteAsync(SocieteDto societeDto)
    {
      // 1) Projection du DTO vers l'entité Societe
      var societe = _mapper.Map<Societe>(societeDto);

      // 2) Ajout de la société
      await _societeRepository.AddSocieteAsync(societe);
      await _societeRepository.SaveAllAsync(); // Génère societe.Id

      // 3) Retourner le DTO mis à jour
      return _mapper.Map<SocieteDto>(societe);
    }



    public async Task<bool> UpdateSocieteAsync(int id, SocieteDto societeDto)
    {
      var existingSociete = await _societeRepository.GetSocieteByIdAsync(id);
      if (existingSociete == null)
        return false;

      // Conserver l'ancien pays pour comparaison
      var originalPaysId = existingSociete.PaysId;

      // Appliquer les modifications depuis le DTO
      _mapper.Map(societeDto, existingSociete);

      // Si le pays a changé, mettre à jour les projets et utilisateurs associés
      if (existingSociete.PaysId != originalPaysId)
      {
        await _societeRepository.UpdateRelatedEntitiesForSocietePaysChangeAsync(id, existingSociete.PaysId);
      }

      _societeRepository.UpdateSociete(existingSociete);
      return await _societeRepository.SaveAllAsync();
    }


    public async Task<bool> DeleteSocieteAsync(int id)
    {
      return await _societeRepository.DeleteSocieteWithAssociationsAsync(id);
    }


    public async Task<bool> DeleteSocietesAsync(List<int> ids)
    {
      foreach (var id in ids)
      {
        var deleted = await DeleteSocieteAsync(id);
        if (!deleted)
          return false;
      }
      return true;
    }


    public async Task<PagedList<UserDto>> GetSocieteUsersPagedAsync(int societeId, UserParams userParams)
    {
      var usersPaged = await _societeRepository.GetSocieteUsersPagedAsync(societeId, userParams);
      var usersDto = _mapper.Map<IEnumerable<UserDto>>(usersPaged);
      return new PagedList<UserDto>(
          usersDto.ToList(),
          usersPaged.TotalCount,
          usersPaged.CurrentPage,
          usersPaged.PageSize
      );
    }
    public async Task<bool> AttachUserToSocieteAsync(int societeId, int userId)
    {
      return await _societeRepository.AttachUserToSocieteAsync(societeId, userId);
    }

    public async Task<bool> DetachUserFromSocieteAsync(int societeId, int userId)
    {
      return await _societeRepository.DetachUserFromSocieteAsync(societeId, userId);
    }

    public async Task<bool> SocieteExists(string nom)
    {
      return await _societeRepository.SocieteExists(nom);
    }
  }
}
