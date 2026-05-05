using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PropertyManagement.API.DTOs;
using PropertyManagement.API.Services;

namespace PropertyManagement.API.Controllers
{
    // API controller responsible for issuing JWT tokens
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly TokenService _tokenService;
        private readonly IConfiguration _configuration;

        public AuthController(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            TokenService tokenService,
            IConfiguration configuration)
        {
            _userManager   = userManager;
            _signInManager = signInManager;
            _tokenService  = tokenService;
            _configuration = configuration;
        }

        // validates credentials via Identity and returns a signed JWT on success
        // both unknown email and wrong password return 401 to avoid leaking registered addresses
        [HttpPost("login")]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
                return Unauthorized(new { message = "Invalid email or password." });

            // CheckPasswordSignInAsync validates the password hash without creating a cookie
            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);
            if (!result.Succeeded)
                return Unauthorized(new { message = "Invalid email or password." });

            var roles      = await _userManager.GetRolesAsync(user);
            var token      = _tokenService.GenerateToken(user, roles);
            var expiryMins = Convert.ToDouble(_configuration["Jwt:ExpiryInMinutes"] ?? "60");

            return Ok(new LoginResponseDto
            {
                Token      = token,
                Email      = user.Email ?? string.Empty,
                UserId     = user.Id,
                Roles      = roles.ToList(),
                Expiration = DateTime.UtcNow.AddMinutes(expiryMins)
            });
        }
    }
}
