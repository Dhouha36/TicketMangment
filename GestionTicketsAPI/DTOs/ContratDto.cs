using System;
using GestionTicketsAPI.Entities;

namespace GestionTicketsAPI.DTOs;

public class ContratDto
    {
        public int Id { get; set; }
        public DateTime DateDebut { get; set; }
        public DateTime? DateFin { get; set; }
        public TypeContrat Type { get; set; }
        
        // Ces champs seront renseignés en fonction du type de contrat
        public int? SocietePartenaireId { get; set; }
        public int? ClientId { get; set; }
    }