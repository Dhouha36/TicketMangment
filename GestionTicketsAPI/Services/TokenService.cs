using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GestionTicketsAPI.Entities;
using GestionTicketsAPI.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace GestionTicketsAPI.Services
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _config;
        public TokenService(IConfiguration config)
        {
            _config = config;
        }

        public string CreateToken(User user)
            => BuildToken(
                subjectId: user.Id.ToString(),
                subjectName: user.Email,
                role: user.Role?.Name ?? "User"
            );

        public string CreateToken(Client client)
            => BuildToken(
                subjectId: client.Id.ToString(),
                subjectName: client.Email,
                role: "Client"
            );

        private string BuildToken(string subjectId, string subjectName, string role)
        {
            var keyString = _config["TokenKey"]
                ?? throw new Exception("Cannot access tokenKey from appsettings");
            if (keyString.Length < 64)
                throw new Exception("Your tokenKey needs to be at least 64 characters");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, subjectId),
                new Claim(JwtRegisteredClaimNames.UniqueName, subjectName),
                new Claim(ClaimTypes.Role, role)
            };

            var tokenDesc = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = creds
            };

            var handler = new JwtSecurityTokenHandler();
            var token    = handler.CreateToken(tokenDesc);
            return handler.WriteToken(token);
        }
    }
}
