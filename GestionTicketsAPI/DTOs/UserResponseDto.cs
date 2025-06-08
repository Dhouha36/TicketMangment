namespace GestionTicketsAPI.DTOs
{
    public class UserResponseDto
    {
        public int Id { get; set; }
        public required string Email { get; set; }
        public required string Role { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required string NumTelephone { get; set; }
        public required string Pays { get; set; }
        public required bool Actif { get; set; }
        public ICollection<SocieteDto> Societes { get; set; } = new List<SocieteDto>();
        
        public ContratUserDto? ContratUser { get; set; }
    }
}
