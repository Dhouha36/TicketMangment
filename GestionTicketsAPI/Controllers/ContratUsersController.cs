using GestionTicketsAPI.DTOs;
using GestionTicketsAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GestionTicketsAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContratUsersController : ControllerBase
    {
        private readonly IContratUserService _contratUserService;

        public ContratUsersController(IContratUserService contratUserService)
        {
            _contratUserService = contratUserService;
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] ContratUserDto dto)
        {
            if (id != dto.Id)
            {
                return BadRequest(new { Message = "L’ID de l’URL doit correspondre à l’ID du DTO." });
            }

            // Optionnel : vérifications supplémentaires (dates, type, salaire, etc.)
            if (dto.DateFin.HasValue && dto.DateFin < dto.DateDebut)
            {
                ModelState.AddModelError(nameof(dto.DateFin), "La date de fin doit être postérieure à la date de début.");
            }
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var updated = await _contratUserService.UpdateAsync(id, dto);
            if (!updated)
            {
                return NotFound(new { Message = $"Contrat avec l’ID {id} introuvable." });
            }

            return NoContent();
        }
    }
}
