using System;
using AutoMapper;
using GestionTicketsAPI.DTOs;
using GestionTicketsAPI.Entities;
using GestionTicketsAPI.Helpers;
using GestionTicketsAPI.Interfaces;

namespace GestionTicketsAPI.Services;

public class ProjetService : IProjetService
{
  private readonly IProjetRepository _projetRepository;
  private readonly ISocieteRepository _societeRepository;
  private readonly IAccountRepository _accountRepository;
  private readonly IMapper _mapper;

  public ProjetService(IProjetRepository projetRepository, IMapper mapper, ISocieteRepository societeRepository, IAccountRepository accountRepository)
  {
    _projetRepository = projetRepository;
    _societeRepository = societeRepository;
    _accountRepository = accountRepository;
    _mapper = mapper;
  }

  public async Task<IEnumerable<ProjetDto>> GetProjetsAsync()
  {
    var projets = await _projetRepository.GetProjetsAsync();
    return _mapper.Map<IEnumerable<ProjetDto>>(projets);
  }

  public async Task<PagedList<ProjetDto>> GetProjetsPagedAsync(ProjectFilterParams filterParams)
  {
    var projetsPaged = await _projetRepository.GetProjetsPagedAsync(filterParams);
    var projetsDto = _mapper.Map<IEnumerable<ProjetDto>>(projetsPaged);

    var pagedProjetDtos = new PagedList<ProjetDto>(
        projetsDto.ToList(),
        projetsPaged.TotalCount,
        projetsPaged.CurrentPage,
        projetsPaged.PageSize
    );

    return pagedProjetDtos;
  }

  public async Task<IEnumerable<ProjetDto>> GetProjetsFilteredAsync(ProjectFilterParams filterParams)
  {
    var projets = await _projetRepository.GetProjetsFilteredAsync(filterParams);
    return _mapper.Map<IEnumerable<ProjetDto>>(projets);
  }

  public async Task<ProjetDto?> GetProjetByIdAsync(int id)
  {
    var projet = await _projetRepository.GetProjetByIdAsync(id);
    if (projet == null) return null;
    return _mapper.Map<ProjetDto>(projet);
  }

  public async Task<ProjetDto> AddProjetAsync(ProjetDto projetDto)
  {
    if (projetDto.SocieteId == null)
      throw new ArgumentException("Un projet doit être associé à une société.");

    // 1. Mapper et charger la société
    var projet = _mapper.Map<Projet>(projetDto);
    var societe = await _societeRepository.GetSocieteByIdAsync(projet.SocieteId.Value);
    if (societe == null)
      throw new Exception("La société associée n'a pas été trouvée.");
    projet.Societe = societe;

    // 2. Enregistrer le projet
    await _projetRepository.AddProjetAsync(projet);
    if (!await _projetRepository.SaveAllAsync())
      throw new Exception("Erreur lors de l'enregistrement du projet.");
    // À ce stade, projet.Id a été généré

    // 3. Gérer le contrat de projet s’il est fourni
    if (projetDto.Contrat != null)
    {
      var dto = projetDto.Contrat;
      if (dto.Type != TypeContrat.Projet)
        throw new Exception("Pour AddProjetAsync, le contrat doit être de type Projet.");

      var contrat = new Contrat
      {
        DateDebut = dto.DateDebut,
        DateFin = dto.DateFin,
        Type = dto.Type,
        ProjetId = projet.Id,    // on utilise l’ID fraîchement généré
        UserId = null          // pas de user ici
      };
      await _accountRepository.AddContractAsync(contrat);
      if (!await _projetRepository.SaveAllAsync())
        throw new Exception("Erreur lors de l'enregistrement du contrat de projet.");
    }

    // 4. Retourner le DTO mis à jour
    var resultDto = _mapper.Map<ProjetDto>(projet);
    return resultDto;
  }



  public async Task<bool> UpdateProjetAsync(int id, ProjetUpdateDto projetDto)
  {
    if (id != projetDto.Id)
      return false;

    var projet = _mapper.Map<Projet>(projetDto);
    _projetRepository.UpdateProjet(projet);
    try
    {
      return await _projetRepository.SaveAllAsync();
    }
    catch (Exception)
    {
      if (!await _projetRepository.ProjetExistsAsync(id))
      {
        return false;
      }
      else
      {
        throw;
      }
    }
  }

  public async Task<bool> DeleteProjetAsync(int id)
  {
    // Vérifier si le projet possède des tickets associés
    bool hasTickets = await _projetRepository.ProjetHasTicketsAsync(id);
    if (hasTickets)
    {
      throw new InvalidOperationException("Impossible de supprimer le projet car il contient des tickets associés.");
    }

    var projet = await _projetRepository.GetProjetByIdAsync(id);
    if (projet == null)
      return false;

    // Supprimer les associations ProjetUser s'il y en a
    if (projet.ProjetUsers != null && projet.ProjetUsers.Any())
    {
      foreach (var pu in projet.ProjetUsers)
      {
        _projetRepository.RemoveProjetUser(pu);
      }
    }

    _projetRepository.RemoveProjet(projet);
    return await _projetRepository.SaveAllAsync();
  }


  public async Task<bool> DeleteProjetsAsync(List<int> ids)
  {
    foreach (var id in ids)
    {
      var result = await DeleteProjetAsync(id);
      if (!result) return false;
    }
    return true;
  }

  public async Task<bool> AjouterUtilisateurAuProjetAsync(int projetId, ProjetUserDto projetUserDto)
  {
    var projet = await _projetRepository.GetProjetByIdAsync(projetId);
    if (projet == null) return false;

    // Vérifier si l'utilisateur est déjà associé au projet
    var projetUserExists = await _projetRepository.GetProjetUserAsync(projetId, projetUserDto.UserId);
    if (projetUserExists != null)
    {
      throw new InvalidOperationException("Cet utilisateur est déjà associé à ce projet.");
    }

    var projetUser = new ProjetUser
    {
      ProjetId = projetId,
      UserId = projetUserDto.UserId
    };

    await _projetRepository.AddProjetUserAsync(projetUser);
    return await _projetRepository.SaveAllAsync();
  }


  // La méthode AssignerRoleAsync a été supprimée car le rôle n'est plus utilisé

  public async Task<IEnumerable<dynamic>> GetMembresProjetAsync(int projetId)
  {
    return await _projetRepository.GetMembresProjetAsync(projetId);
  }

  public async Task<bool> SupprimerUtilisateurDuProjetAsync(int projetId, int userId)
  {
    var projetUser = await _projetRepository.GetProjetUserAsync(projetId, userId);
    if (projetUser == null) return false;
    _projetRepository.RemoveProjetUser(projetUser);
    return await _projetRepository.SaveAllAsync();
  }

  public async Task<IEnumerable<Projet>> GetProjetsForUserAsync(int userId)
  {
    return await _projetRepository.GetProjetsForUserAsync(userId);
  }
  public async Task<bool> ProjetExists(string nom)
  {
    return await _projetRepository.ProjetExists(nom);
  }

  public async Task<IEnumerable<ProjetDto>> GetProjetsBySocieteIdAsync(int societeId)
  {
    var projets = await _projetRepository.GetProjetsBySocieteIdAsync(societeId);
    return _mapper.Map<IEnumerable<ProjetDto>>(projets);
  }

  public async Task<IEnumerable<ClientDto>> GetClientsByProjetIdAsync(int projetId)
  {
    var clients = await _projetRepository.GetClientsByProjetIdAsync(projetId);
    return _mapper.Map<IEnumerable<ClientDto>>(clients);
  }

}
