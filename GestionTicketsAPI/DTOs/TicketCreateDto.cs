using System.ComponentModel.DataAnnotations;

namespace GestionTicketsAPI.DTOs
{
    public class TicketCreateDto
    {
        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        public int PriorityId { get; set; }

        [Required]
        public int OwnerId { get; set; }

        [Required]
        public int ProblemCategoryId { get; set; }

        [Required]
        public int QualificationId { get; set; }

        [Required]
        public int ProjetId { get; set; }
    }
}
