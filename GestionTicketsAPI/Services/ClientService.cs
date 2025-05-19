
using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using GestionTicketsAPI.DTOs;
using GestionTicketsAPI.Helpers;
using GestionTicketsAPI.Repositories;
namespace GestionTicketsAPI.Services
{
  public interface IClientService
  {
    Task<PagedList<ClientDto>> GetAllClientsAsync(ClientParams clientParams);
    Task<IEnumerable<ClientDto>> GetAllClientsNoPaginationAsync();
    Task<ClientDto?> GetClientByIdAsync(int id);
    Task<bool> DeleteClientAsync(int id);
    Task<bool> UpdateClientAsync(ClientUpdateDto clientUpdateDto);
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

    public ClientService(IClientRepository clientRepository, IMapper mapper)
    {
      _clientRepository = clientRepository;
      _mapper = mapper;
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

    public async Task<bool> UpdateClientAsync(ClientUpdateDto dto)
{
  var client = await _clientRepository.GetClientByIdAsync(dto.Id);
  if (client == null) return false;

  // Mapper les champs simples
  _mapper.Map(dto, client);

  // Gestion du changement de mot de passe
  if (!string.IsNullOrWhiteSpace(dto.NouveauPassword))
  {
    if (dto.NouveauPassword != dto.ConfirmNouveauPassword)
      throw new Exception("Les mots de passe ne correspondent pas.");

    using var hmac = new HMACSHA512();
    client.PasswordSalt = hmac.Key;
    client.PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(dto.NouveauPassword));
  }

  _clientRepository.Update(client);
  return await _clientRepository.SaveAllAsync();
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
