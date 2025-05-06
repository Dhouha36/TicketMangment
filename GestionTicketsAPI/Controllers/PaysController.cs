using AutoMapper;
using GestionTicketsAPI.DTOs;
using GestionTicketsAPI.Interfaces;
using GestionTicketsAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionTicketsAPI.Controllers
{

  [ApiController]
  public class PaysController : BaseApiController
  {
    private readonly IPaysService _paysService;

    public PaysController(IPaysService paysService)
    {
      _paysService = paysService;
    }

    [Authorize]
    [HttpPost("getPays")]
    public async Task<ActionResult<IEnumerable<PaysDto>>> GetPays([FromBody] Dictionary<string, string?> data)
    {
      data.TryGetValue("searchTerm", out var searchTerm);
      if (!string.IsNullOrWhiteSpace(searchTerm))
      {
        var filteredPays = await _paysService.GetPaysAsync(searchTerm);
        return Ok(filteredPays);
      }
      else
      {
        var allPays = await _paysService.GetPaysAsync();
        return Ok(allPays);
      }
    }


    [Authorize]
    [HttpGet("{idPays}")]
    public async Task<ActionResult<PaysDto>> GetPaysById(int idPays)
    {
      var pays = await _paysService.GetPaysByIdAsync(idPays);
      if (pays == null) return NotFound("Le pays spécifié n'existe pas.");
      return Ok(pays);
    }

    [Authorize]
    [HttpPut("ModifierPays/{idPays}")]
    public async Task<ActionResult> UpdatePays(int idPays, [FromBody] Dictionary<string, string> data)
    {
      if (!data.TryGetValue("nom", out var nom))
        return BadRequest("Le nom est requis.");

      data.TryGetValue("codeTel", out var codeTel);
      data.TryGetValue("file", out var fileBase64);

      // Création du DTO de mise à jour
      var paysUpdateDto = new PaysUpdateDto
      {
        Nom = nom,
        CodeTel = codeTel
      };

      IFormFile? file = null;
      if (!string.IsNullOrEmpty(fileBase64))
      {
        try
        {
          // Supposons que fileBase64 est sous la forme "data:<mimeType>;base64,<data>"
          var base64Data = fileBase64.Substring(fileBase64.IndexOf(',') + 1);
          var bytes = Convert.FromBase64String(base64Data);
          var stream = new MemoryStream(bytes);
          // Le nom et le type MIME sont arbitraires ici, à adapter selon vos besoins
          file = new FormFile(stream, 0, stream.Length, "file", "uploadedFile.jpg");
        }
        catch (Exception ex)
        {
          return BadRequest("Erreur lors de la conversion du fichier: " + ex.Message);
        }
      }

      var updated = await _paysService.UpdatePaysAsync(idPays, paysUpdateDto, file);
      if (!updated)
        return BadRequest("Erreur lors de la mise à jour du pays.");

      return NoContent();
    }



    [Authorize]
    [HttpPost("ajouterPays")]
    public async Task<ActionResult<PaysDto>> AddPays([FromBody] Dictionary<string, string> data)
    {
      if (!data.TryGetValue("nom", out var nom))
        return BadRequest("Le nom est requis.");

      data.TryGetValue("codeTel", out var codeTel);
      data.TryGetValue("file", out var fileBase64);

      if (await _paysService.PaysExists(nom))
        return BadRequest("Le pays existe déjà");

      try
      {
        // Adaptez votre service pour traiter fileBase64 en lieu et place d'un IFormFile
        var paysDto = await _paysService.AddPaysAsync(nom, codeTel, fileBase64);
        return CreatedAtAction(nameof(GetPays), new { idPays = paysDto.IdPays }, paysDto);
      }
      catch (Exception ex)
      {
        return BadRequest(ex.Message);
      }
    }

    [Authorize]
    [HttpGet("supprimerPays/{idPays}")]
    public async Task<IActionResult> DeletePays(int idPays)
    {
        try
        {
            var deleted = await _paysService.DeletePaysAsync(idPays);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            // 409 Conflict pour traitement dans l'intercepteur
            return Conflict(ex.Message);
        }
        catch (Exception)
        {
            return StatusCode(500, "Erreur serveur inattendue.");
        }
    }

  }
}
