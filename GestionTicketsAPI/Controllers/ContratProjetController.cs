using GestionTicketsAPI.DTOs;
using GestionTicketsAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GestionTicketsAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContratProjetController : ControllerBase
    {
        private readonly IContratProjetService _service;

        public ContratProjetController(IContratProjetService service)
        {
            _service = service;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ContratProjetDto>> GetById(int id)
        {
            var dto = await _service.GetByIdAsync(id);
            if (dto == null) return NotFound();
            return Ok(dto);
        }

        [HttpPost]
        public async Task<ActionResult<ContratProjetDto>> Create([FromBody] ContratProjetDto dto)
        {
            var created = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] ContratProjetDto dto)
        {
            if (id != dto.Id)
                return BadRequest("L'ID de l'URL doit correspondre à l'ID du corps de la requête.");

            var ok = await _service.UpdateAsync(id, dto);
            if (!ok) return NotFound();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var ok = await _service.DeleteAsync(id);
            if (!ok) return NotFound();
            return NoContent();
        }
    }
}
