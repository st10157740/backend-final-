using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MbabaneHighlandersBackend2.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IConfiguration _config;

        public AccountController(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            IConfiguration config)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _config = config;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
                return BadRequest(new { message = "Email and password are required." });

            var user = await _userManager.FindByEmailAsync(req.Email);
            if (user == null)
                return Unauthorized(new { message = "Invalid credentials." });

            var result = await _signInManager.CheckPasswordSignInAsync(user, req.Password, false);
            if (!result.Succeeded)
                return Unauthorized(new { message = "Invalid credentials." });

            var roles = await _userManager.GetRolesAsync(user);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? ""),
                new Claim("name", user.UserName ?? "")
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? throw new InvalidOperationException("JWT key missing")));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds);

            return Ok(new
            {
                access_token = new JwtSecurityTokenHandler().WriteToken(token),
                expires_in = 3600,
                user = new
                {
                    user.Id,
                    user.Email,
                    user.UserName,
                    roles
                }
            });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest req)
        {
            var errors = new Dictionary<string, string[]>();

            if (string.IsNullOrWhiteSpace(req.Email))
                errors["email"] = new[] { "Email is required." };
            if (string.IsNullOrWhiteSpace(req.Password))
                errors["password"] = new[] { "Password is required." };
            if (req.Password != req.ConfirmPassword)
                errors["confirmPassword"] = new[] { "Passwords do not match." };

            if (errors.Count > 0)
                return BadRequest(new { errors });

            var user = new IdentityUser
            {
                UserName = req.Email,
                Email = req.Email
            };

            var result = await _userManager.CreateAsync(user, req.Password);

            if (!result.Succeeded)
            {
                foreach (var err in result.Errors)
                {
                    if (!errors.ContainsKey(err.Code))
                        errors[err.Code] = Array.Empty<string>();
                    errors[err.Code] = errors[err.Code].Append(err.Description).ToArray();
                }
                return BadRequest(new { errors });
            }

            // Assign default role
            await _userManager.AddToRoleAsync(user, "user");

            return Ok(new { message = "Registration successful" });
        }
    }

    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterRequest
    {
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? ConfirmPassword { get; set; }
        public string? Name { get; set; }
    }
}