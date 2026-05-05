using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace PropertyManagement.API.Services
{
    public class TokenService
    {
        private readonly IConfiguration _configuration;

        public TokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // takes the authenticated IdentityUser and their roles and returns a signed JWT string
        public string GenerateToken(IdentityUser user, IList<string> roles)
        {
            // build the claims that will be embedded inside the token payload
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),              // user GUID used throughout the app
                new Claim(ClaimTypes.Name,           user.UserName ?? ""),  // email stored as username
                new Claim(ClaimTypes.Email,          user.Email    ?? ""),  // explicit email claim
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) // unique token ID prevents replay
            };

            // add one Role claim per role so [Authorize(Roles = "...")] works
            foreach (var role in roles)
                claims.Add(new Claim(ClaimTypes.Role, role));

            var jwtKey = _configuration["Jwt:Key"]
                ?? throw new InvalidOperationException(
                    "Jwt:Key is not configured. Set the Jwt__Key environment variable.");

            var key         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            // fall back to 60 minutes if ExpiryInMinutes is missing from config
            var expiryMinutes = double.TryParse(_configuration["Jwt:ExpiryInMinutes"], out var parsed)
                ? parsed : 60.0;
            var expiry = DateTime.UtcNow.AddMinutes(expiryMinutes);

            var token = new JwtSecurityToken(
                issuer:            _configuration["Jwt:Issuer"],
                audience:          _configuration["Jwt:Audience"],
                claims:            claims,
                expires:           expiry,
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
