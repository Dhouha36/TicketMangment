namespace GestionTicketsAPI.DTOs
{
    public class ProjetDto
    {
        public int Id { get; set; }
        public required string Nom { get; set; }
        public string? Description { get; set; }
        
        // Association à la société
        public int? SocieteId { get; set; }
        public string? NomSociete { get; set; }

        public int? IdPays { get; set; }
        public string? NomPays { get; set; }

         public int? ChefProjetId { get; set; }
        public UserDto? ChefProjet { get; set; }
         public ContratDto? Contrat { get; set; }

    }
}
