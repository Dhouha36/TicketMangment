using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using GestionTicketsAPI.DTOs;
using GestionTicketsAPI.Interfaces;
using GestionTicketsAPI.Services;
using Hangfire; // N'oubliez pas d'ajouter la référence à Hangfire
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionTicketsAPI.Controllers;

[ApiController]
public class AccountController : BaseApiController
{
  private readonly IAccountService _accountService;
  private readonly IAccountRepository _accountRepository;
  private readonly IUserService _userService;
  private readonly EmailService _emailService; // Injection du service email

  public AccountController(
      IAccountRepository accountRepository,
      IAccountService accountService,
      IUserService userService,
      EmailService emailService)
  {
    _accountService = accountService;
    _accountRepository = accountRepository;
    _userService = userService;
    _emailService = emailService;
  }

  [HttpPost("register")]
  public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto)
  {
    try
    {
      var userDto = await _accountService.RegisterAsync(registerDto);

      var body = $@"
<html>
  <body style='font-family: Arial, sans-serif; color: #333;'>
    <p>Bonjour {userDto.FirstName} {userDto.LastName},</p>
    <p>Votre compte a été créé avec succès.</p>
    <p><strong>Vos identifiants de connexion :</strong></p>
    <ul>
      <li><strong>Email :</strong> {userDto.Email}</li>
      <li><strong>Mot de passe :</strong> {userDto.InitialPassword}</li>
      <li><strong>Rôle :</strong> {userDto.Role}</li>
    </ul>
    <p><a href='https://simsoft-gt.tn/' style='color: #007BFF;' target='_blank'>Accéder à l'application</a></p>
    <p>Cordialement,<br>L'équipe Simsoft</p>
  </body>
</html>";

      BackgroundJob.Enqueue(() => _emailService.SendEmailAsync(
          $"{userDto.FirstName} {userDto.LastName}",
          userDto.Email,
          "Bienvenue dans notre application",
          body
      ));

      return Ok(userDto);
    }
    catch (Exception ex)
    {
      var inner = ex.InnerException?.Message;
      return BadRequest(new { message = ex.Message, inner });
    }
  }


  [HttpPost("login")]
  public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
  {
    try
    {
      var userDto = await _accountService.LoginAsync(loginDto);
      return Ok(userDto);
    }
    catch (Exception ex)
    {
      return Unauthorized(new { message = ex.Message });
    }
  }

  [HttpGet("validate")]
  public async Task<ActionResult> Validate()
  {
    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
    {
      return Unauthorized();
    }

    var user = await _userService.GetUserByIdAsync(userId);
    if (user == null)
      return Unauthorized();

    return Ok();
  }

  [HttpPost("forgot-password")]
  public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto forgotPasswordDto)
  {
    // Recherche de l’utilisateur par e-mail
    var user = await _accountRepository.GetUserByEmailAsync(forgotPasswordDto.Email);

    // Ne pas révéler si l'e-mail existe ou non pour des raisons de sécurité
    if (user == null)
    {
      return Ok(new { message = "Si cet e-mail est enregistré, vous recevrez un lien de réinitialisation." });
    }

    // Génération d’un token sécurisé
    var token = GeneratePasswordResetToken();  // Implémentez une méthode pour générer un token aléatoire sécurisé.

    // Enregistrer le token et sa date d'expiration (exemple : en 1 heure) dans une table dédiée ou dans le modèle User
    await _accountService.SaveResetTokenAsync(user.Id, token, DateTime.UtcNow.AddHours(1));

    // Construction du lien de réinitialisation (adapter l’URL à votre configuration)
    var resetLink = $"http://localhost:4200/reset-password?token={Uri.EscapeDataString(token)}";

    // Envoi de l’email en tâche de fond avec Hangfire
    BackgroundJob.Enqueue(() => _emailService.SendEmailAsync(
        $"{user.FirstName} {user.LastName}",
        user.Email,
        "Réinitialisation du mot de passe",
        $"Cliquez sur le lien pour réinitialiser votre mot de passe : <a href='{resetLink}'>Réinitialiser mon mot de passe</a>"
    ));

    return Ok(new { message = "Si cet e-mail est enregistré, vous recevrez un lien de réinitialisation." });
  }

  [HttpPost("reset-password")]
  public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto resetPasswordDto)
  {
    // Trim and decode the token if needed
    var token = resetPasswordDto.Token?.Trim();

    // Récupérer l’utilisateur à partir du token
    var user = await _accountService.GetUserByResetTokenAsync(token);
    if (user == null)
    {
      return BadRequest(new { message = "Token invalide ou expiré." });
    }

    // Vérifier que le token n'est pas expiré
    if (user.PasswordResetTokenExpires < DateTime.UtcNow)
    {
      return BadRequest(new { message = "Le token a expiré." });
    }

    // Mettre à jour le mot de passe de l’utilisateur
    using var hmac = new HMACSHA512();
    user.PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(resetPasswordDto.NewPassword));
    user.PasswordSalt = hmac.Key;

    // Invalider le token (par exemple, en le supprimant ou en le marquant comme utilisé)
    user.PasswordResetToken = null;
    user.PasswordResetTokenExpires = null;

    // Sauvegarder les modifications dans la base
    if (!await _accountRepository.SaveAllAsync())
    {
      return StatusCode(500, new { message = "Une erreur est survenue lors de la mise à jour du mot de passe." });
    }

    return Ok(new { message = "Votre mot de passe a été mis à jour avec succès." });
  }


  private string GeneratePasswordResetToken()
  {
    byte[] randomBytes = new byte[32];
    using (var rng = RandomNumberGenerator.Create())
    {
      rng.GetBytes(randomBytes);
    }
    return Convert.ToBase64String(randomBytes);
  }


}
