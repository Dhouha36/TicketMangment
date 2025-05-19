using GestionTicketsAPI.Entities;

namespace GestionTicketsAPI.DTOs
{
    public class TicketDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty; // Anciennement Titre
        public string Description { get; set; } = string.Empty;
        public int PriorityId { get; set; }              // Anciennement Priorite
        public Priorite? Priority { get; set; }
        public int StatutId { get; set; }                // Anciennement Statuts
        public StatutDesTicket? Statut { get; set; }
        public DateTime CreatedAt { get; set; }          // Anciennement DateCreation
        public DateTime? UpdatedAt { get; set; }           // Anciennement DateModification
        public int OwnerId { get; set; }                 // Anciennement UtilisateurId
        public ClientDto? Owner { get; set; }              // Anciennement Utilisateur
        public int ProblemCategoryId { get; set; }       // Anciennement CategorieProblemeId
        public CategorieProbleme? ProblemCategory { get; set; } // Anciennement CategorieProbleme
        public int QualificationId { get; set; }         // Anciennement Qualification
        public Qualification? Qualification { get; set; }
        public string? Attachments { get; set; }         // Anciennement Attachement
        public int ProjetId { get; set; }
        public ProjetDto? Projet { get; set; }
        public int? ResponsibleId { get; set; }          // Anciennement DeveloppeurId
        public UserDto? Responsible { get; set; }        // Anciennement Developpeur
        public DateTime? ApprovedAt { get; set; }
        public DateTime? SolvedAt { get; set; }

    }
}
