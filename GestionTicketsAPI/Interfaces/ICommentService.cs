using System;
using GestionTicketsAPI.DTOs;

namespace GestionTicketsAPI.Interfaces;

public interface ICommentService
{
  Task<CommentDto> CreateCommentAsync(CommentCreateDto dto, int? userId = null, int? clientId = null);
  Task<CommentDto> GetCommentByIdAsync(int id);
  Task<IEnumerable<CommentDto>> GetCommentsByTicketAsync(int ticketId);
}
