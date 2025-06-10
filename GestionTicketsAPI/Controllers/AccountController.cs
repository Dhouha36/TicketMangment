using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using GestionTicketsAPI.DTOs;
using GestionTicketsAPI.Interfaces;
using GestionTicketsAPI.Services;
using Hangfire; 
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace GestionTicketsAPI.Controllers;

[ApiController]
public class AccountController : BaseApiController
{
  private readonly IAccountService _accountService;
  private readonly IAccountRepository _accountRepository;
  private readonly IClientService _clientService;
  private readonly IUserService _userService;
  private readonly EmailService _emailService; // Injection du service email

  public AccountController(
      IAccountRepository accountRepository,
      IAccountService accountService,
      IClientService clientService,
      IUserService userService,
      EmailService emailService)
  {
    _accountService = accountService;
    _accountRepository = accountRepository;
    _clientService = clientService;
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
    // 1. Récupérer l'ID dans le claim
    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var id))
      return Unauthorized();

    // 2. Chercher un utilisateur interne
    var user = await _userService.GetUserByIdAsync(id);
    if (user != null)
      return Ok();

    // 3. Si pas trouvé, chercher un client
    var client = await _clientService.GetClientByIdAsync(id);
    if (client != null)
      return Ok();

    // 4. Aucun des deux → non autorisé
    return Unauthorized();
  }

  [HttpPost("forgot-password")]
  public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto forgotPasswordDto)
  {
    // Recherche
    var user = await _accountRepository.GetUserByEmailAsync(forgotPasswordDto.Email);
    var client = await _accountRepository.GetClientByEmailAsync(forgotPasswordDto.Email);

    if (user == null && client == null)
    {
      return Ok(new { message = "Si cet e-mail est enregistré, vous recevrez un lien de réinitialisation." });
    }

    // Génère un token et un lien de réinitialisation
    var token = GeneratePasswordResetToken();
    var resetLink = $"http://localhost:4200/reset-password?token={token}";
    if (user != null)
    {
      await _accountService.SaveResetTokenForUserAsync(user.Id, token, DateTime.UtcNow.AddHours(1));
      await SendResetEmailAsync(user.FirstName, user.LastName, user.Email, resetLink);
    }
    else if (client != null)
    {
      await _accountService.SaveResetTokenForClientAsync(client.Id, token, DateTime.UtcNow.AddHours(1));
      await SendResetEmailAsync(client.FirstName, client.LastName, client.Email, resetLink);
    }

    return Ok(new { message = "Si cet e-mail est enregistré, vous recevrez un lien de réinitialisation." });
  }
  private async Task SendResetEmailAsync(string firstName, string lastName, string email, string resetLink)
  {
    var body = $@"
    <html>
        <body style='font-family: Arial, sans-serif; color: #333;'>
            <p>Bonjour {firstName} {lastName},</p>
            <p>Cliquez sur le lien ci‑dessous pour réinitialiser votre mot de passe :</p>
            <p>
                <a href='{resetLink}' target='_blank' style='color: #007BFF; text-decoration: none;'>
                    Réinitialiser mon mot de passe
                </a>
            </p>
            <p>Si vous n’avez pas demandé ce changement, vous pouvez ignorer ce message.</p>
            <p>Cordialement,<br />L'équipe Simsoft</p>
        </body>
    </html>";

    BackgroundJob.Enqueue(() => _emailService.SendEmailAsync(
        $"{firstName} {lastName}",
        email,
        "Réinitialisation du mot de passe",
        body
    ));
  }

  [HttpPost("reset-password")]
  public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto resetPasswordDto)
  {
    var token = resetPasswordDto.Token?.Trim();

    // Vérifie si le token correspond à un User
    var user = await _accountService.GetUserByResetTokenAsync(token);
    if (user != null)
    {
      if (user.PasswordResetTokenExpires < DateTime.UtcNow)
        return BadRequest(new { message = "Le token a expiré." });

      // Met à jour le mot de passe de l'utilisateur
      using var hmac = new HMACSHA512();
      user.PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(resetPasswordDto.NewPassword));
      user.PasswordSalt = hmac.Key;
      user.PasswordResetToken = null;
      user.PasswordResetTokenExpires = null;

      if (!await _accountRepository.SaveAllAsync())
        return StatusCode(500, new { message = "Erreur lors de la mise à jour du mot de passe." });

      return Ok(new { message = "Mot de passe mis à jour avec succès." });
    }

    // Vérifie si le token correspond à un Client
    var client = await _accountService.GetClientByResetTokenAsync(token);
    if (client != null)
    {
      if (client.PasswordResetTokenExpires < DateTime.UtcNow)
        return BadRequest(new { message = "Le token a expiré." });

      // Met à jour le mot de passe du client
      using var hmac = new HMACSHA512();
      client.PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(resetPasswordDto.NewPassword));
      client.PasswordSalt = hmac.Key;
      client.PasswordResetToken = null;
      client.PasswordResetTokenExpires = null;

      if (!await _accountRepository.SaveAllAsync())
        return StatusCode(500, new { message = "Erreur lors de la mise à jour du mot de passe." });

      return Ok(new { message = "Mot de passe mis à jour avec succès." });
    }

    return BadRequest(new { message = "Token invalide ou expiré." });
  }
  private string GeneratePasswordResetToken()
  {
    // 32 octets aléatoires
    var bytes = new byte[32];
    using var rng = RandomNumberGenerator.Create();
    rng.GetBytes(bytes);

    // Base64 URL‑safe -> uniquement [A–Z a–z 0–9 _ -]
    return WebEncoders.Base64UrlEncode(bytes);
  }


}
