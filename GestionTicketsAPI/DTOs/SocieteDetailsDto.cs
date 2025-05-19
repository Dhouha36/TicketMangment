namespace GestionTicketsAPI.DTOs
{
    public class SocieteDetailsDto
    {
        public int Id { get; set; }
        public required string Nom { get; set; }
        public required string Adresse { get; set; }
        public required string Telephone { get; set; }
        public string Ville { get; set; } = string.Empty;
        public string CodePostal { get; set; } = string.Empty;
        public int PaysId { get; set; }  
        public PaysDto? Pays { get; set; }
        
        public ICollection<UserDto> Utilisateurs { get; set; } = new List<UserDto>();
        
        public ICollection<ProjetDto> Projets { get; set; } = new List<ProjetDto>();

    }
}
