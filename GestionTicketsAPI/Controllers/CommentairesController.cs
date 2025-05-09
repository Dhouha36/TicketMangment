using System.Security.Claims;
using GestionTicketsAPI.DTOs;
using GestionTicketsAPI.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GestionTicketsAPI.Controllers
{
  [ApiController]
  public class CommentairesController : BaseApiController
  {
    private readonly ICommentService _commentService;

    public CommentairesController(ICommentService commentService)
    {
      _commentService = commentService;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<CommentDto>> CreateComment(
        [FromForm] CommentCreateDto commentCreateDto)    // <-- [FromForm]
    {
        var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null) return Unauthorized("Utilisateur non authentifié.");

        int userId = int.Parse(userIdClaim.Value);
        if (string.IsNullOrWhiteSpace(commentCreateDto.Contenu) 
            && (commentCreateDto.Files == null || commentCreateDto.Files.Length == 0))
        {
            ModelState.AddModelError(nameof(commentCreateDto.Contenu),
                "Vous devez fournir du texte ou au moins un fichier.");
            return ValidationProblem(ModelState);
        }
        var createdComment = await _commentService.CreateCommentAsync(commentCreateDto, userId);
        if (createdComment == null)
            return BadRequest("Erreur lors de la création du commentaire.");

        return CreatedAtAction(nameof(GetCommentById),
                               new { id = createdComment.Id },
                               createdComment);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CommentDto>> GetCommentById(int id)
    {
      var comment = await _commentService.GetCommentByIdAsync(id);
      if (comment == null)
        return NotFound();
      return Ok(comment);
    }

    [HttpGet("ticket/{ticketId:int}")]
    public async Task<ActionResult<IEnumerable<CommentDto>>> GetCommentsByTicket(int ticketId)
    {
      var comments = await _commentService.GetCommentsByTicketAsync(ticketId);
      return Ok(comments);
    }

  }

}
