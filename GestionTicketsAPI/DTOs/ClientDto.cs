using System;

namespace GestionTicketsAPI.DTOs;

public class ClientDto
    {
        public int Id { get; set; }
        public required string Email { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required string NumTelephone { get; set; }
        public int PaysId { get; set; }
        public required string Pays { get; set; }
        public required bool Actif { get; set; }
        public string PhotoUrl { get; set; }
        public string? Token { get; set; }
        public string InitialPassword { get; set; } = string.Empty;
        public int SocieteId { get; set; }  
        public SocieteDto Societe { get; set; } = default!;
        public ICollection<ProjetMiniDto> Projets { get; set; } = new List<ProjetMiniDto>();
    }
