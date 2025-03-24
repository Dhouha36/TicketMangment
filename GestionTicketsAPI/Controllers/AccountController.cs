using System.Security.Claims;
using GestionTicketsAPI.DTOs;
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
  private readonly EmailService _emailService; // Injection du service email

  public AccountController(
      IAccountService accountService,
      IUserService userService,
      EmailService emailService)
  {
    _accountService = accountService;
    _userService = userService;
    _emailService = emailService;
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
          body,
          userDto.Id
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
}
