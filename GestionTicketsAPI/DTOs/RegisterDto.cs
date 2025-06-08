using System;
using System.ComponentModel.DataAnnotations;

namespace GestionTicketsAPI.DTOs
{
    public class RegisterDto
    {
        [Required]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        public string Role { get; set; } = string.Empty;
        
        [Required]
        public string Firstname { get; set; } = string.Empty;
        
        [Required]
        public string Lastname { get; set; } = string.Empty;
        
        [Required]
        public string Numtelephone { get; set; } = string.Empty;
        
        [Required]
        public int Pays { get; set; }
        
        public bool Actif { get; set; } = true;
        
        // Informations du contrat (optionnel)
        public ContratUserDto? ContratUser { get; set; }
    }
}
