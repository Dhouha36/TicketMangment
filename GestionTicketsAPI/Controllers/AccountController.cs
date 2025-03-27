using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using GestionTicketsAPI.DTOs;
using GestionTicketsAPI.Helpers;
using GestionTicketsAPI.Interfaces;
using GestionTicketsAPI.Services;
using Hangfire; // N'oubliez pas d'ajouter la référence à Hangfire
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionTicketsAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountController : BaseApiController
{
  private readonly IAccountService _accountService;
  private readonly IUserService _userService;
  private readonly IMapper _mapper;
  private readonly EmailService _emailService;
  private readonly IAccountRepository _accountRepository;

  public AccountController(
      IAccountRepository accountRepository,
      IAccountService accountService,
      IUserService userService,
      EmailService emailService,
      IMapper mapper)
  {
    _accountRepository = accountRepository;
    _accountService = accountService;
    _userService = userService;
    _emailService = emailService;
    _mapper = mapper;
  }

  [HttpPost("register")]
  public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto)
  {
    try
    {
      // Appel au service d'inscription
      var userDto = await _accountService.RegisterAsync(registerDto);

      // Préparation du corps de l'e-mail en HTML
      var body = $"Bonjour {userDto.FirstName} {userDto.LastName},<br><br>" +
                 "Votre compte a été créé avec succès.<br><br>" +
                 $"Email : {userDto.Email}<br>" +
                 $"Mot de passe : {registerDto.Password}<br>" +
                 $"Rôle : {userDto.Role}<br><br>" +
                 "Merci de votre confiance.";

      // Envoi de l'e-mail en tâche de fond via Hangfire.
      BackgroundJob.Enqueue(() => _emailService.SendEmailAsync(
          $"{userDto.FirstName} {userDto.LastName}",
          userDto.Email,
          "Bienvenue dans notre application",
          body // Utilisation du corps en HTML
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
  public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
  {
    // Vérifier si l'utilisateur existe
    var user = await _accountService.GetUserByEmailAsync(dto.Email);
    if (user == null)
    {
      // Pour éviter de divulguer l'existence d'un compte, renvoyer toujours un message générique
      return Ok(new { message = "Si cet e-mail est enregistré, vous recevrez un lien de réinitialisation." });
    }

    // Générer un token de réinitialisation (vous pouvez utiliser par exemple un GUID ou un JWT avec une validité courte)
    var resetToken = Guid.NewGuid().ToString();

    // Enregistrer le token dans la base de données associé à l'utilisateur avec une date d'expiration
    await _accountService.SavePasswordResetToken(user, resetToken);

    // Construire l'URL de réinitialisation (vous pouvez adapter selon votre front-end)
    var resetUrl = $"https://votre-front-url/reset-password?token={resetToken}&email={Uri.EscapeDataString(user.Email)}";

    // Envoyer l'e-mail
    var emailSent = await _emailService.SendEmailAsync(
        user.FirstName,
        user.Email,
        "Réinitialisation de votre mot de passe",
        $"Cliquez sur ce lien pour réinitialiser votre mot de passe : <a href='{resetUrl}'>Réinitialiser</a>");

    if (!emailSent)
    {
      return StatusCode(500, new { message = "Erreur lors de l'envoi de l'e-mail" });
    }

    return Ok(new { message = "Si cet e-mail est enregistré, vous recevrez un lien de réinitialisation." });
  }


   [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        // Récupérer l'utilisateur via l'e-mail
        var user = await _accountRepository.GetUserByEmailAsync(dto.Email);
        if (user == null)
        {
            return BadRequest(new { message = "Utilisateur non trouvé." });
        }

        // Récupérer le token enregistré pour cet utilisateur
        var storedToken = await _accountService.GetPasswordResetToken(user);
        if (storedToken == null ||
            storedToken.Token != dto.Token ||
            storedToken.Expiration < DateTime.UtcNow)
        {
            return BadRequest(new { message = "Le token est invalide ou expiré." });
        }

        // Mettre à jour le mot de passe de l'utilisateur
        using var hmac = new HMACSHA512();
        user.PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(dto.NewPassword));
        user.PasswordSalt = hmac.Key;

        // Mise à jour de l'utilisateur (selon votre méthode d'update)
        //_accountRepository.Update(user);
        await _accountService.SaveAllAsync();

        // Supprimer le token après usage
        await _accountService.RemovePasswordResetToken(user);

        return Ok(new { message = "Le mot de passe a été réinitialisé avec succès." });
    }

}
