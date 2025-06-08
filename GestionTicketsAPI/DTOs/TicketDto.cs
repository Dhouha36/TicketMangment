using GestionTicketsAPI.Entities;

namespace GestionTicketsAPI.DTOs
{
    public class TicketDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty; 
        public string Description { get; set; } = string.Empty;
        public int PriorityId { get; set; }              
        public Priorite? Priority { get; set; }
        public int StatutId { get; set; }                
        public StatutDesTicket? Statut { get; set; }
        public DateTime CreatedAt { get; set; }          
        public DateTime? UpdatedAt { get; set; }           
        public int OwnerId { get; set; }                 
        public ClientDto? Owner { get; set; }              
        public int ProblemCategoryId { get; set; }       
        public CategorieProbleme? ProblemCategory { get; set; } 
        public int QualificationId { get; set; }         
        public Qualification? Qualification { get; set; }
        public string? Attachments { get; set; }         
        public int ProjetId { get; set; }
        public ProjetDto? Projet { get; set; }
        public int? ResponsibleId { get; set; }         
        public UserDto? Responsible { get; set; }        
        public DateTime? ApprovedAt { get; set; }
        public DateTime? SolvedAt { get; set; }

    }
}
