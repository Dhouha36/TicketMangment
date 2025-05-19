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
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CommentairesController(ICommentService commentService,
        IHttpContextAccessor httpContextAccessor)
    {
      _commentService = commentService;
      _httpContextAccessor = httpContextAccessor;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<CommentDto>> CreateComment(
    [FromForm] CommentCreateDto dto)
    {

      // Récupère l’ID utilisateur depuis le JWT
      var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
      if (userIdClaim == null)
        return Unauthorized("Utilisateur non authentifié.");

      int userId = int.Parse(userIdClaim.Value);

      Console.WriteLine($"DEBUG: dto.ClientId = {dto.ClientId}");

      // Validation basique
      if (string.IsNullOrWhiteSpace(dto.Contenu)
          && (dto.Files == null || dto.Files.Length == 0))
      {
        ModelState.AddModelError(nameof(dto.Contenu),
            "Vous devez fournir du texte ou au moins un fichier.");
        return ValidationProblem(ModelState);
      }

      // >>> Passer *toujours* les deux paramètres :
      //     - userId vaut l’ID du caller
      //     - clientId vaut dto.ClientId (null si ce n’est pas un client)
      var createdComment = await _commentService.CreateCommentAsync(
          dto,
          userId: userId,
          clientId: dto.ClientId
      );

      if (createdComment == null)
        return BadRequest("Erreur lors de la création du commentaire.");

      return CreatedAtAction(
          nameof(CreateComment),
          new { id = createdComment.Id },
          createdComment
      );
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
