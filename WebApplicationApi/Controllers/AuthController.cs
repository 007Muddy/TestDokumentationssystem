
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
            if (ModelState.IsValid)
            {
                var user = new IdentityUser { UserName = model.Username, Email = model.Email };
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // Return a success message without generating a JWT token
                    return Ok(new { Result = "User registered successfully!" });
                }
                else
                {
                    return BadRequest(result.Errors);
                }
            }

            return BadRequest(ModelState);
        }


        //[HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(model.Username, model.Password, false, false);

                if (result.Succeeded)
                {
                    var user = await _userManager.FindByNameAsync(model.Username);
                    var token = GenerateJwtToken(user);

                    _logger.LogInformation("User {Username} logged in successfully.", model.Username);
                    return Ok(new { Message = "Login successful!", Token = token });
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

            _logger.LogWarning("Login attempt failed due to invalid request.");
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
                audience: _configuration["Jwt:Issuer"],
                claims: claims,
                expires: DateTime.Now.AddHours(1),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }








    }
}
