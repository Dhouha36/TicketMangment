using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GestionTicketsAPI.DTOs;
using GestionTicketsAPI.Extensions;
using GestionTicketsAPI.Helpers;
using GestionTicketsAPI.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionTicketsAPI.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  [Authorize]
  public class SocieteController : ControllerBase
  {
    private readonly ISocieteService _societeService;

    public SocieteController(ISocieteService societeService)
    {
      _societeService = societeService;
    }

    // GET: api/Societe?searchTerm=...
    [HttpGet]
    public async Task<IActionResult> GetSocietes([FromQuery] string? searchTerm)
    {
      var societes = await _societeService.GetAllSocietesAsync(searchTerm);
      return Ok(societes);
    }

    // GET: api/Societe/paged?PageNumber=1&PageSize=10&SearchTerm=...
    [HttpGet("paged")]
    public async Task<ActionResult<PagedList<SocieteDto>>> GetSocietesPaged([FromQuery] UserParams userParams)
    {
      var societesPaged = await _societeService.GetSocietesPagedAsync(userParams);
      Response.AddPaginationHeader(societesPaged); // Ajoute les métadonnées de pagination dans l'en-tête HTTP
      return Ok(societesPaged);
    }

    // GET: api/Societe/5
    [HttpGet("{id}")]
    public async Task<IActionResult> GetSociete(int id)
    {
      var societe = await _societeService.GetSocieteByIdAsync(id);
      if (societe == null)
        return NotFound();
      return Ok(societe);
    }

    // GET: api/Societe/details/5
    [HttpGet("details/{id}")]
    public async Task<IActionResult> GetSocieteDetails(int id)
    {
      var societeDetails = await _societeService.GetSocieteWithDetailsByIdAsync(id);
      if (societeDetails == null)
        return NotFound();
      return Ok(societeDetails);
    }

    [HttpPost]
    public async Task<IActionResult> AddSociete(SocieteDto societeDto)
    {
      // Vérifier si la société existe déjà (par exemple, sur la base du nom)
      if (await _societeService.SocieteExists(societeDto.Nom))
        return BadRequest("La société existe déjà");

      var newSociete = await _societeService.AddSocieteAsync(societeDto);
      return CreatedAtAction(nameof(GetSociete), new { id = newSociete.Id }, newSociete);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateSociete(int id, SocieteDto societeDto)
    {
      var updated = await _societeService.UpdateSocieteAsync(id, societeDto);
      if (!updated)
        return NotFound("Société introuvable ou mise à jour échouée");
      return NoContent();
    }


    // DELETE: api/Societe/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSociete(int id)
    {
      var deleted = await _societeService.DeleteSocieteAsync(id);
      if (!deleted)
        return NotFound();
      return NoContent();
    }

    // DELETE: api/Societe/supprimerSocietes
    [HttpDelete("supprimerSocietes")]
    public async Task<IActionResult> DeleteSocietes([FromBody] List<int> ids)
    {
      if (ids == null || !ids.Any())
        return BadRequest("Aucun identifiant fourni.");

      var result = await _societeService.DeleteSocietesAsync(ids);
      if (!result)
        return NotFound("Une ou plusieurs sociétés n'ont pas été trouvées.");
      return NoContent();
    }

    // GET: api/Societe/5/users/paged?PageNumber=1&PageSize=10&SearchTerm=...
    [HttpGet("{societeId}/users/paged")]
    public async Task<ActionResult<PagedList<UserDto>>> GetSocieteUsersPaged(
        int societeId,
        [FromQuery] UserParams userParams)
    {
      var usersPaged = await _societeService.GetSocieteUsersPagedAsync(societeId, userParams);
      Response.AddPaginationHeader(usersPaged);
      return Ok(usersPaged);
    }

    [HttpPost("{societeId}/users/{userId}")]
    public async Task<IActionResult> AttachUser(int societeId, int userId)
    {
      bool attached = await _societeService.AttachUserToSocieteAsync(societeId, userId);
      if (attached)
      {
        return Ok(new { message = "Utilisateur attaché à la société avec succès." });
      }
      else
      {
        return Conflict(new { message = "Cet utilisateur est déjà attaché à la société." });
      }
    }

    [HttpDelete("{societeId}/users/{userId}")]
    public async Task<IActionResult> DetachUser(int societeId, int userId)
    {
      if (await _societeService.DetachUserFromSocieteAsync(societeId, userId))
        return Ok("Utilisateur détaché de la société avec succès.");
      return BadRequest("Aucune association trouvée ou une erreur est survenue.");
    }
  }
}
