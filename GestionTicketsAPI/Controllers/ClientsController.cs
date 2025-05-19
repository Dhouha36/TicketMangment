using AutoMapper;
using GestionTicketsAPI.DTOs;
using GestionTicketsAPI.Extensions;
using GestionTicketsAPI.Helpers;
using GestionTicketsAPI.Interfaces;
using GestionTicketsAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionTicketsAPI.Controllers
{
  [ApiController]
  [Authorize]
  [Route("api/[controller]")]
  public class ClientsController : BaseApiController
  {
    private readonly IClientService _clientService;
    private readonly IAccountService _accountService;
    private readonly IAccountRepository _accountRepository;
    private readonly ISocieteRepository _societeRepository;
    private readonly ExcelExportServiceClosedXML _excelExportService;
    private readonly IMapper _mapper;

    public ClientsController(
        IClientService clientService,
        IAccountService accountService,
        ExcelExportServiceClosedXML excelExportService,
        IAccountRepository accountRepository,
        ISocieteRepository societeRepository,
        IMapper mapper)
    {
      _clientService = clientService;
      _accountService = accountService;
      _accountRepository = accountRepository;
      _societeRepository = societeRepository;
      _excelExportService = excelExportService;
      _mapper = mapper;
    }

    // Enregistrement d'un nouveau client 
    [HttpPost("register")]
    public async Task<ActionResult<ClientDto>> RegisterClient([FromBody] RegisterClientDto dto)
    {
      try
      {
        var clientDto = await _accountService.RegisterClientAsync(dto);
        return CreatedAtAction(nameof(GetClient), new { id = clientDto.Id }, clientDto);
      }
      catch (Exception ex)
      {
        // Pour plus de finesse, vous pouvez filtrer sur des types d'exception custom
        // ou sur le message, mais en l'état on renvoie simplement BadRequest.
        return BadRequest(ex.Message);
      }
    }


    [HttpPost("validateClient")]
    public async Task<IActionResult> ValidateClient([FromBody] RegisterClientDto dto)
    {
      if (await _accountRepository.ClientExistsAsync(dto.Email, dto.FirstName, dto.LastName))
        return BadRequest("Le client existe déjà.");
      if (await _accountRepository.GetPaysByIdAsync(dto.Pays) == null)
        return BadRequest("Le pays spécifié est introuvable.");
      if (await _societeRepository.GetSocieteByIdAsync(dto.SocieteId) == null)
        return BadRequest("La société spécifiée est introuvable.");
      // ... éventuellement d’autres checks ...
      return Ok();
    }

    // Récupérer les clients paginés
    [HttpPost("paged")]
    public async Task<ActionResult<PagedList<ClientDto>>> GetClients([FromBody] ClientParams clientParams)
    {
      var clients = await _clientService.GetAllClientsAsync(clientParams);
      Response.AddPaginationHeader(clients);
      return Ok(clients);
    }

    // Récupérer tous les clients (sans pagination)
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ClientDto>>> GetAllClients()
    {
      var clients = await _clientService.GetAllClientsNoPaginationAsync();
      return Ok(clients);
    }

    // Récupérer un client par ID
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ClientDto>> GetClient(int id)
    {
      var client = await _clientService.GetClientByIdAsync(id);
      if (client == null)
        return NotFound();
      return Ok(client);
    }

    // Supprimer un client
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteClient(int id)
    {
      try
      {
        var result = await _clientService.DeleteClientAsync(id);
        if (!result)
          return NotFound();
        return NoContent();
      }
      catch (Exception ex)
      {
        return BadRequest(new { errors = new string[] { ex.Message } });
      }
    }

    // Mettre à jour un client
    [HttpPut("{id:int}")]
    public async Task<ActionResult> UpdateClient(int id, [FromBody] ClientUpdateDto clientUpdateDto)
    {
      if (id != clientUpdateDto.Id)
        return BadRequest("L'ID de l'URL ne correspond pas à celui du body.");

      var result = await _clientService.UpdateClientAsync(clientUpdateDto);
      if (!result)
        return NotFound("Client non trouvé.");

      return NoContent();
    }

    // Récupérer les clients par société
    [HttpGet("societe/{societeId:int}")]
    public async Task<ActionResult<IEnumerable<ClientDto>>> GetClientsBySociete(int societeId)
    {
      var clients = await _clientService.GetClientsBySocieteAsync(societeId);
      return Ok(clients);
    }

    // Exporter les clients vers Excel
    [HttpPost("export")]
    public async Task<IActionResult> ExportClients([FromBody] ClientParams clientParams)
    {
      var clients = await _clientService.GetAllClientsAsync(clientParams);
      var clientExportDtos = _mapper.Map<IEnumerable<ClientExportDto>>(clients);
      var content = _excelExportService.ExportToExcel(clientExportDtos, "Clients");
      return File(content,
          "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
          $"ClientsExport_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
    }

    // GET /api/clients/{id}/tickets
    [HttpGet("{id:int}/tickets")]
    public async Task<ActionResult<IEnumerable<TicketDto>>> GetClientTickets(int id)
    {
      try
      {
        var tickets = await _clientService.GetClientTicketsAsync(id) ?? Enumerable.Empty<TicketDto>();
        return Ok(tickets);
      }
      catch (Exception ex)
      {
        // Log ex.Message si nécessaire
        // Retourne une liste vide plutôt qu'une erreur 500
        return Ok(Enumerable.Empty<TicketDto>());
      }
    }

    // GET /api/clients/{id}/projects
    [HttpGet("{id:int}/projects")]
    public async Task<ActionResult<IEnumerable<ProjetDto>>> GetClientProjects(int id)
    {
      try
      {
        var projets = await _clientService.GetClientProjectsAsync(id) ?? Enumerable.Empty<ProjetDto>();
        return Ok(projets);
      }
      catch (Exception ex)
      {
        // Log ex.Message si nécessaire
        return Ok(Enumerable.Empty<ProjetDto>());
      }
    }

    [HttpPost("{clientId:int}/projects/{projetId:int}")]
    public async Task<IActionResult> AddClientToProject(int clientId, int projetId)
    {
      var success = await _clientService.AddClientToProjectAsync(clientId, projetId);
      if (!success)
        return Conflict("Ce client est déjà associé à ce projet."); // Changé de BadRequest à Conflict
      return NoContent();
    }

    [HttpDelete("{clientId:int}/projects/{projectId:int}")]
    public async Task<IActionResult> DetachProjectFromClient(int clientId, int projectId)
    {
      var result = await _clientService.DetachProjectFromClientAsync(clientId, projectId);
      if (!result)
        return NotFound();
      return NoContent();
    }
  }
}
