using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using WebApplicationApi.Data;
using WebApplicationApi.Model;

namespace WebApplicationApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InspectionsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public InspectionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetInspections()
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User is not authenticated." });
            }

            var inspections = await _context.Inspections
                .Where(i => i.CreatedBy == userId)  // Fetch only the inspections created by the logged-in user
                .ToListAsync();

            return Ok(inspections);
        }



        // GET: api/inspections/{id} - Fetch a specific inspection by ID
        [HttpGet("{id}")]

        public async Task<IActionResult> GetInspection(int id)
        {
            var inspection = await _context.Inspections.FindAsync(id);
            if (inspection == null)
            {
                return NotFound();
            }

            return Ok(inspection);
        }


        [HttpPost("createinspection")]
        [Authorize]
        public async Task<IActionResult> CreateInspection([FromBody] Inspection model)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            Console.WriteLine($"Authenticated User ID: {userId}");

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User is not authenticated." });
            }

            model.CreatedBy = userId;  // Save the User ID
            _context.Inspections.Add(model);
            await _context.SaveChangesAsync();

            return Ok(model);
        }


        // PUT: api/inspections/{id} - Update an existing inspection
        [HttpPut("{id}")]

        public async Task<IActionResult> UpdateInspection(int id, [FromBody] Inspection model)
        {
            var existingInspection = await _context.Inspections.FindAsync(id);
            if (existingInspection == null)
            {
                return NotFound();
            }

            existingInspection.InspectionName = model.InspectionName;
            existingInspection.Address = model.Address;
            existingInspection.Date = model.Date;


            await _context.SaveChangesAsync();

            return Ok(existingInspection);
        }

        // DELETE: api/inspections/{id} - Delete an inspection
        [HttpDelete("{id}")]

        public async Task<IActionResult> DeleteInspection(int id)
        {
            var inspection = await _context.Inspections.FindAsync(id);
            if (inspection == null)
            {
                return NotFound();
            }

            _context.Inspections.Remove(inspection);
            await _context.SaveChangesAsync();

            return Ok();
        }
    }
}
