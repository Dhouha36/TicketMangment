using System;
using System.ComponentModel.DataAnnotations;
using GestionTicketsAPI.Entities;

namespace GestionTicketsAPI.DTOs;
public class ContractRegistrationDto
{
    [Required]
    public DateTime DateDebut { get; set; }

    public DateTime? DateFin { get; set; }

    [Required]
    public TypeContrat Type { get; set; }  // CDD, CDI ou Projet

    // Obligatoire si Type == CDD ou CDI
    public int? UserId { get; set; }

    // Obligatoire si Type == Projet
    public int? ProjetId { get; set; }
}