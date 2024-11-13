using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WebApplicationApi.Model;
using Microsoft.EntityFrameworkCore;
using WebApplicationApi.Data;

namespace WebApplicationApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context; // Add DbContext

        public AuthController(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            IConfiguration configuration,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context) // Inject DbContext
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _roleManager = roleManager;
            _context = context; // Initialize DbContext

            // Initialize the admin user and role only once
            Task.Run(() => CreateAdminUser()).Wait();
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        private async Task CreateAdminUser()
        {
            try
            {
                string adminUsername = "adminUser";
                string adminPassword = "AdminPassword123!";

                // Check if the Admin role exists; if not, create it
                if (!await _roleManager.RoleExistsAsync("Admin"))
                {
                    var roleResult = await _roleManager.CreateAsync(new IdentityRole("Admin"));
                    Console.WriteLine($"Admin role created: {roleResult.Succeeded}");
                }

                // Check if the admin user exists
                var adminUser = await _userManager.FindByNameAsync(adminUsername);
                if (adminUser == null)
                {
                    // Create the admin user
                    adminUser = new IdentityUser { UserName = adminUsername, Email = "admin@example.com" };
                    var result = await _userManager.CreateAsync(adminUser, adminPassword);

                    if (result.Succeeded)
                    {
                        // Assign Admin role to the user
                        var roleAssignmentResult = await _userManager.AddToRoleAsync(adminUser, "Admin");
                        Console.WriteLine($"Admin role assigned to user: {roleAssignmentResult.Succeeded}");
                    }
                    else
                    {
                        Console.WriteLine("Failed to create admin user: " + string.Join(", ", result.Errors.Select(e => e.Description)));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred while creating the admin user: " + ex.Message);
            }
        }


        [Authorize(Roles = "Admin")]
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _userManager.Users
                .Select(user => new
                {
                    user.UserName,
                    user.Email,
                })
                .ToListAsync();

            return Ok(users);
        }


        // Assign "Admin" role to a specific user (Admin only)
        [Authorize(Roles = "Admin")]
        [HttpPost("assign-admin-role")]
        public async Task<IActionResult> AssignAdminRole([FromBody] string username)
        {
            var user = await _userManager.FindByNameAsync(username);
            if (user == null) return NotFound("User not found");

            if (!await _roleManager.RoleExistsAsync("Admin"))
            {
                await _roleManager.CreateAsync(new IdentityRole("Admin"));
            }

            var result = await _userManager.AddToRoleAsync(user, "Admin");

            return result.Succeeded ? Ok("Admin role assigned successfully") : BadRequest("Failed to assign admin role");
        }

        // Register a new user (Admin only)
        [Authorize(Roles = "Admin")]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new IdentityUser { UserName = model.Username, Email = model.Email };
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    var token = GenerateJwtToken(user);
                    return Ok(new { Token = token, Result = "User registered successfully!" });
                }
                else
                {
                    return BadRequest(result.Errors);
                }
            }

            return BadRequest(ModelState);
        }

        // Log in user
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(model.Username, model.Password, false, false);

                if (result.Succeeded)
                {
                    var user = await _userManager.FindByNameAsync(model.Username);
                    var token = GenerateJwtToken(user);

                    // Get the user's roles
                    var roles = await _userManager.GetRolesAsync(user);
                    var role = roles.FirstOrDefault() ?? "User"; // Default role to "User" if none assigned

                    return Ok(new { Token = token, Role = role });
                }
                else
                {
                    return Unauthorized();
                }
            }

            return BadRequest(ModelState);
        }

        // Delete a user (Admin only)
        [Authorize(Roles = "Admin")]
        [HttpDelete("delete/{username}")]
        public async Task<IActionResult> DeleteUser(string username)
        {
            var user = await _userManager.FindByNameAsync(username);
            if (user == null) return NotFound("User not found");

            var result = await _userManager.DeleteAsync(user);
            return result.Succeeded ? Ok("User deleted successfully") : BadRequest("Failed to delete user");
        }

        // Generate JWT Token with roles
        private string GenerateJwtToken(IdentityUser user)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id)
            };

            // Add role claims
            var roles = _userManager.GetRolesAsync(user).Result;
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(30),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
