namespace GestionTicketsAPI.Helpers
{
    public class TicketFilterParams
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public string? Id { get; set; }
        public string? Titre { get; set; }
        public string? Client { get; set; }
         public int? ClientId { get; set; }
        public string? Categorie { get; set; }
        public string? Priorite { get; set; }
        public string? Statut { get; set; }
        public string? Qualification { get; set; }
        public string? Projet { get; set; }
        public string? Societe { get; set; }
        public int UserId { get; set; }
        public string? Role { get; set; }

        public string? FilterType { get; set; }
        public int? SocieteId { get; set; }
        public DateTime? StartDate { get; set; } // Date de début (par exemple pour CreatedAt)
        public DateTime? EndDate { get; set; }
    }
}
