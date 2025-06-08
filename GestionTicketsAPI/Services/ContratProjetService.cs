using System;
using AutoMapper;
using GestionTicketsAPI.DTOs;
using GestionTicketsAPI.Entities;
using GestionTicketsAPI.Repositories;

namespace GestionTicketsAPI.Services;

public interface IContratProjetService
    {
        Task<ContratProjetDto?> GetByIdAsync(int id);
        Task<ContratProjetDto> CreateAsync(ContratProjetDto dto);
        Task<bool> UpdateAsync(int id, ContratProjetDto dto);
        Task<bool> DeleteAsync(int id);
    }

    public class ContratProjetService : IContratProjetService
    {
        private readonly IContratProjetRepository _repo;
        private readonly IMapper _mapper;

        public ContratProjetService(IContratProjetRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        public async Task<ContratProjetDto?> GetByIdAsync(int id)
        {
            var entity = await _repo.GetByIdAsync(id);
            return entity == null
                ? null
                : _mapper.Map<ContratProjetDto>(entity);
        }

        public async Task<ContratProjetDto> CreateAsync(ContratProjetDto dto)
        {
            var entity = _mapper.Map<ContratProjet>(dto);
            await _repo.AddAsync(entity);
            await _repo.SaveAllAsync();
            return _mapper.Map<ContratProjetDto>(entity);
        }

        public async Task<bool> UpdateAsync(int id, ContratProjetDto dto)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) return false;

            // On peut interdire de changer le ProjetId si besoin
            _mapper.Map(dto, existing);
            _repo.Update(existing);
            return await _repo.SaveAllAsync();
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) return false;
            return await _repo.DeleteAsync(existing);
        }
    }
