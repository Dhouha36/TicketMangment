using System;
using System.ComponentModel.DataAnnotations;

namespace GestionTicketsAPI.DTOs;

public class ForgotPasswordDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; }
}
