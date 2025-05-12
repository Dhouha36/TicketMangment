
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
            _mapper.Map(dto, client);
            _clientRepository.Update(client);
            return await _clientRepository.SaveAllAsync();
        }

        public async Task<IEnumerable<ClientDto>> GetClientsBySocieteAsync(int societeId)
        {
            var clients = await _clientRepository.GetClientsBySocieteAsync(societeId);
            return _mapper.Map<IEnumerable<ClientDto>>(clients);
        }
    }
}
