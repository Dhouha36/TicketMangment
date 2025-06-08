using System;
using AutoMapper;
using GestionTicketsAPI.DTOs;
using GestionTicketsAPI.Entities;
using GestionTicketsAPI.Repositories;

namespace GestionTicketsAPI.Services;

public interface IContratUserService
{
    Task<ContratUserDto?> GetByIdAsync(int id);
    Task<ContratUserDto> CreateAsync(ContratUserDto dto);
    Task<bool> UpdateAsync(int id, ContratUserDto dto);
    Task<bool> DeleteAsync(int id);
}

public class ContratUserService : IContratUserService
{
    private readonly IContratUserRepository _repo;
    private readonly IMapper _mapper;

    public ContratUserService(IContratUserRepository repo, IMapper mapper)
    {
        _repo = repo;
        _mapper = mapper;
    }

    public async Task<ContratUserDto?> GetByIdAsync(int id)
    {
        var entity = await _repo.GetByIdAsync(id);
        return entity == null
            ? null
            : _mapper.Map<ContratUserDto>(entity);
    }

    public async Task<ContratUserDto> CreateAsync(ContratUserDto dto)
    {
        var entity = _mapper.Map<ContratUser>(dto);
        await _repo.AddAsync(entity);
        await _repo.SaveAllAsync();
        return _mapper.Map<ContratUserDto>(entity);
    }

    public async Task<bool> UpdateAsync(int id, ContratUserDto dto)
    {
        var existing = await _repo.GetByIdAsync(id);
        if (existing == null) return false;
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
