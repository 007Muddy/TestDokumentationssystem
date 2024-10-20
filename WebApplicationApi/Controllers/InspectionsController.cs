using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        // GET: api/inspections - Fetch all inspections
        [HttpGet]
    
        public async Task<IActionResult> GetInspections()
        {
            var inspections = await _context.Inspections.ToListAsync();
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
        //[Authorize]  // Ensure only authenticated users can access this method
        public async Task<IActionResult> CreateInspection([FromBody] Inspection model)
        {
            // Get the logged-in user's username from the JWT claims
            var userName = User?.Identity?.Name;

            //if (string.IsNullOrEmpty(userName))
            //{
            //    return Unauthorized(new { message = "User is not authenticated." });
            //}

            // Assign the created inspection to the logged-in user
            model.CreatedBy = userName;
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

            existingInspection.InspectionName   = model.InspectionName;
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
