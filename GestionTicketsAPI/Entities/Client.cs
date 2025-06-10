using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestionTicketsAPI.Entities
{
  public class Client
  {
    [Key]
    public int Id { get; set; }

    [Required, EmailAddress]
    public string Email { get; set; } = null!;

    // Pour l’authentification client si besoin
    public byte[] PasswordHash { get; set; } = new byte[0];
    public byte[] PasswordSalt { get; set; } = new byte[0];

    [Required]
    public string FirstName { get; set; } = null!;

    [Required]
    public string LastName { get; set; } = null!;

    [Required, Phone]
    public string NumTelephone { get; set; } = null!;

    [Required]
    public bool Actif { get; set; } = true;

    [Required]
    public int Pays { get; set; }

    public Pays PaysNavigation { get; set; } = null!;

    // Société **obligatoire** pour un client
    [Required, ForeignKey(nameof(Societe))]
    public int SocieteId { get; set; }
    public Societe Societe { get; set; } = null!;
    public ICollection<ProjetClient> ProjetClients { get; set; } = new List<ProjetClient>();
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    public int? PhotoId { get; set; }
    public Photo? Photo { get; set; }
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpires { get; set; }
    // Dates de suivi
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }
  }
}
