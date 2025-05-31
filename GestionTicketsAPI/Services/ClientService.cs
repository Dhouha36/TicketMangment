
using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using GestionTicketsAPI.Data;
using GestionTicketsAPI.DTOs;
using GestionTicketsAPI.Entities;
using GestionTicketsAPI.Helpers;
using GestionTicketsAPI.Interfaces;
using GestionTicketsAPI.Repositories;
namespace GestionTicketsAPI.Services
{
  public interface IClientService
  {
    Task<PagedList<ClientDto>> GetAllClientsAsync(ClientParams clientParams);
    Task<IEnumerable<ClientDto>> GetAllClientsNoPaginationAsync();
    Task<ClientDto?> GetClientByIdAsync(int id);
    Task<bool> DeleteClientAsync(int id);
    Task<ClientDto?> UpdateClientAsync(ClientUpdateDto dto);
    Task<IEnumerable<ClientDto>> GetClientsBySocieteAsync(int societeId);
    Task<IEnumerable<TicketDto>> GetClientTicketsAsync(int clientId);
    Task<IEnumerable<ProjetDto>> GetClientProjectsAsync(int clientId);
    Task<bool> AddClientToProjectAsync(int clientId, int projetId);
    Task<bool> DetachProjectFromClientAsync(int clientId, int projetId);
  }

  public class ClientService : IClientService
  {
    private readonly IClientRepository _clientRepository;
    private readonly IMapper _mapper;
    private readonly IPhotoService _photoService;
    private readonly DataContext _context;

    public ClientService(IClientRepository clientRepository, IMapper mapper,
        IPhotoService photoService, DataContext context)
    {
      _clientRepository = clientRepository;
      _mapper = mapper;
      _photoService = photoService;
      _context = context;
    }

    public async Task<PagedList<ClientDto>> GetAllClientsAsync(ClientParams clientParams)
    {
      var clientsPaged = await _clientRepository.GetClientsAsync(clientParams);
      var dtos = _mapper.Map<IEnumerable<ClientDto>>(clientsPaged.ToList());
      return new PagedList<ClientDto>(dtos.ToList(), clientsPaged.TotalCount, clientsPaged.CurrentPage, clientsPaged.PageSize);
    }

    public async Task<IEnumerable<ClientDto>> GetAllClientsNoPaginationAsync()
    {
      var clients = await _clientRepository.GetClientsNoPaginationAsync();
      return _mapper.Map<IEnumerable<ClientDto>>(clients);
    }

    public async Task<ClientDto?> GetClientByIdAsync(int id)
    {
      var client = await _clientRepository.GetClientByIdAsync(id);
      return client == null ? null : _mapper.Map<ClientDto>(client);
    }

    public async Task<bool> DeleteClientAsync(int id)
    {
      var client = await _clientRepository.GetClientWithProjectsAsync(id);
      if (client == null) return false;
      return await _clientRepository.DeleteClientAsync(client);
    }

    public async Task<ClientDto?> UpdateClientAsync(ClientUpdateDto dto)
{
    // 1) Récupérer l’entité
    var client = await _clientRepository.GetClientByIdAsync(dto.Id);
    if (client == null)
        return null;

    // 2) Mapper les champs simples du DTO vers l’entité
    _mapper.Map(dto, client);

    // 3) Gérer la photo si elle existe
    if (dto.PhotoFile != null && dto.PhotoFile.Length > 0)
    {
        // 3a) Si l’entité avait déjà une photo, la supprimer d’abord
        if (client.Photo != null && !string.IsNullOrEmpty(client.Photo.PublicId))
        {
            // 3a.1) 1er : supprimer du Cloudinary
            try
            {
                var deletionResult = await _photoService.DeletePhotoAsync(client.Photo.PublicId);
                // (Optionnel : vérifier deletionResult.Result == "ok")
            }
            catch (Exception ex)
            {
                // Vous pouvez logger l’erreur, mais ne bloquez pas la suite de l’upload
                // _logger.LogWarning($"Impossible de supprimer l’ancienne photo sur Cloudinary : {ex.Message}");
            }

            // 3a.2) 2ᵉ : supprimer l’enregistrement dans la table Photo
             _context.Photos.Remove(client.Photo);
        }

        // 3b) Uploader la nouvelle photo sur Cloudinary
        var uploadResult = await _photoService.AddPhotoAsync(dto.PhotoFile);

        // 3c) Créer une entité Photo et l’attacher à Client
        var newPhoto = new Photo
        {
            Url = uploadResult.SecureUrl.ToString(),
            PublicId = uploadResult.PublicId
        };
        client.Photo = newPhoto;

        // Remarque : si votre Client possède une propriété PhotoId, EF se chargera de la mettre à jour automatiquement.
    }

    // 4) Gérer le changement de mot de passe
    if (!string.IsNullOrWhiteSpace(dto.NouveauPassword))
    {
        if (dto.NouveauPassword != dto.ConfirmNouveauPassword)
            throw new Exception("Les mots de passe ne correspondent pas.");

        using var hmac = new HMACSHA512();
        client.PasswordSalt = hmac.Key;
        client.PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(dto.NouveauPassword));
    }

    // 5) Appliquer la mise à jour en base
    _clientRepository.Update(client);
    var success = await _clientRepository.SaveAllAsync();
    if (!success)
        return null; // ou throw une exception si vous préférez

    // 6) Lire à nouveau, ou simplement mapper l’entité déjà modifiée
    //    Si votre entité 'client' est bien suivie par EF, elle a déjà la version à jour.
    //    On crée donc le DTO à partir de cette entité.
    var clientDto = _mapper.Map<ClientDto>(client);
    return clientDto;
}


    public async Task<IEnumerable<ClientDto>> GetClientsBySocieteAsync(int societeId)
    {
      var clients = await _clientRepository.GetClientsBySocieteAsync(societeId);
      return _mapper.Map<IEnumerable<ClientDto>>(clients);
    }

    public async Task<IEnumerable<TicketDto>> GetClientTicketsAsync(int clientId)
    {
      var tickets = await _clientRepository.GetTicketsByClientAsync(clientId);
      return _mapper.Map<IEnumerable<TicketDto>>(tickets);
    }

    public async Task<IEnumerable<ProjetDto>> GetClientProjectsAsync(int clientId)
    {
      var projets = await _clientRepository.GetProjectsByClientAsync(clientId);
      return _mapper.Map<IEnumerable<ProjetDto>>(projets);
    }

    public async Task<bool> AddClientToProjectAsync(int clientId, int projetId)
    {
      return await _clientRepository.AddClientToProjectAsync(clientId, projetId);
    }

    public async Task<bool> DetachProjectFromClientAsync(int clientId, int projetId)
    {
      return await _clientRepository.RemoveClientFromProjectAsync(clientId, projetId);
    }
  }
}
