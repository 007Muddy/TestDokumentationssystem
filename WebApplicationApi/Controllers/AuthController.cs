using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WebApplicationApi.Model;

namespace WebApplicationApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        public AuthController(ILogger<AuthController> logger, UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager, IConfiguration configuration)
        {
            _logger = logger;
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
        }

        // Fetches all users from the UserManager
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            var users = _userManager.Users.ToList();
            var userList = users.Select(user => new
            {
                user.UserName,
                user.Email
            }).ToList();

            return Ok(userList);
        }

        // Register a new user and generate a JWT token
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            _logger.LogInformation("Starting user registration process...");

            if (ModelState.IsValid)
            {
                _logger.LogInformation("Model state is valid.");

                var user = new IdentityUser { UserName = model.Username, Email = model.Email };
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User {Username} registered successfully.", model.Username);
                    return Ok(new { Result = "User registered successfully!" });
                }
                else
                {
                    _logger.LogError("User registration failed: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
                    return BadRequest(result.Errors);
                }
            }

            _logger.LogWarning("Model state is invalid.");
            return BadRequest(ModelState);
        }


        // Login method with JWT token generation
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            _logger.LogInformation("Starting login process for user {Username}", model.Username);

            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(model.Username, model.Password, false, false);

                if (result.Succeeded)
                {
                    var user = await _userManager.FindByNameAsync(model.Username);
                    if (user == null)
                    {
                        _logger.LogWarning("User not found after successful sign-in for username {Username}", model.Username);
                        return Unauthorized(new { Message = "Invalid username or password." });
                    }

                    var token = GenerateJwtToken(user);

                    _logger.LogInformation("User {Username} logged in successfully.", model.Username);
                    return Ok(new { Token = token, Message = "Login successful." });
                }
                else
                {
                    if (result.IsLockedOut)
                    {
                        _logger.LogWarning("Login attempt failed for user {Username}: User is locked out.", model.Username);
                        return Unauthorized(new { Message = "User is locked out. Please try again later." });
                    }
                    else if (result.IsNotAllowed)
                    {
                        _logger.LogWarning("Login attempt failed for user {Username}: User is not allowed to login.", model.Username);
                        return Unauthorized(new { Message = "User is not allowed to log in." });
                    }
                    else
                    {
                        _logger.LogWarning("Login attempt failed for user {Username}: Invalid credentials.", model.Username);
                        return Unauthorized(new { Message = "Invalid username or password." });
                    }
                }
            }

            _logger.LogWarning("Login attempt failed due to invalid request for user {Username}.", model.Username);
            return BadRequest(new { Message = "Invalid request. Please check your input and try again." });
        }

        private string GenerateJwtToken(IdentityUser user)
        {
            var claims = new[]
            {
        new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new Claim(ClaimTypes.NameIdentifier, user.Id)
    };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(double.Parse(_configuration["Jwt:TokenExpiryMinutes"])),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

    }
}
