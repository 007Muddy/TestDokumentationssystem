using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebApplicationApi.Data;
using WebApplicationApi.Model;
using System.IO;

namespace WebApplicationApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InspectionsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;  // Injecting the environment for file saving

        public InspectionsController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: api/inspections - Fetch all inspections for the logged-in user
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


        // POST: api/inspections/createinspection - Create a new inspection with photo upload
        [HttpPost("createinspection")]
        [Authorize]
        public async Task<IActionResult> CreateInspection([FromForm] Inspection model, [FromForm] List<string> photos)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User is not authenticated." });
            }

            // Ensure the PhotoPaths list is initialized
            if (model.PhotoPaths == null)
            {
                model.PhotoPaths = new List<string>();
            }

            // Add the Base64-encoded photos to the PhotoPaths list
            if (photos != null && photos.Count > 0)
            {
                model.PhotoPaths.AddRange(photos);  // Store Base64-encoded strings
            }

            model.CreatedBy = userId;  // Save the User ID
            _context.Inspections.Add(model);
            await _context.SaveChangesAsync();

            return Ok(model);
        }




        // PUT: api/inspections/{id} - Update an existing inspection with optional photo uploads
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateInspection(int id, [FromForm] Inspection model, [FromForm] List<IFormFile> photos)
        {
            var existingInspection = await _context.Inspections.FindAsync(id);
            if (existingInspection == null)
            {
                return NotFound();
            }

            existingInspection.InspectionName = model.InspectionName;
            existingInspection.Address = model.Address;
            existingInspection.Date = model.Date;

            // Handle photo upload for update
            if (photos != null && photos.Count > 0)
            {
                foreach (var photo in photos)
                {
                    if (photo.Length > 0)
                    {
                        var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        var uniqueFileName = Guid.NewGuid().ToString() + "_" + photo.FileName;
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await photo.CopyToAsync(stream);
                        }

                        // Save the new file path to the model's PhotoPaths property
                        existingInspection.PhotoPaths.Add(filePath);
                    }
                }
            }

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
