using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestionTicketsAPI.Entities
{
  public class User
  {
    public int Id { get; set; }

    [EmailAddress]
    public required string Email { get; set; }

    public byte[] PasswordHash { get; set; } = new byte[0];
    public byte[] PasswordSalt { get; set; } = new byte[0];

    [ForeignKey("RoleId")]
    public int RoleId { get; set; }
    public Role Role { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }

    [Phone]
    public required string NumTelephone { get; set; }
    public required bool Actif { get; set; }

    [ForeignKey("id_pays")]
    public required int Pays { get; set; }
    public Pays? PaysNavigation { get; set; }
    public ICollection<SocieteUser> SocieteUsers { get; set; } = new List<SocieteUser>();

    // Autres relations (tickets, commentaires, etc.)
    public ICollection<ProjetUser> ProjetUsers { get; set; } = new List<ProjetUser>();

    [InverseProperty("User")]
    public ICollection<Contrat>? Contrats { get; set; }
    public int? PhotoId { get; set; }
    public Photo? Photo { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpires { get; set; }
  }
}
