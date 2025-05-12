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
        private readonly ExcelExportServiceClosedXML _excelExportService;
        private readonly IMapper _mapper;

        public ClientsController(
            IClientService clientService,
            IAccountService accountService,
            ExcelExportServiceClosedXML excelExportService,
            IMapper mapper)
        {
            _clientService = clientService;
            _accountService = accountService;
            _excelExportService = excelExportService;
            _mapper = mapper;
        }

        // Enregistrement d'un nouveau client (public)
        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<ActionResult<ClientDto>> RegisterClient([FromBody] RegisterClientDto dto)
        {
            var clientDto = await _accountService.RegisterClientAsync(dto);
            return CreatedAtAction(nameof(GetClient), new { id = clientDto.Id }, clientDto);
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
    }
}
