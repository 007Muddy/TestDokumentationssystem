// InspectionsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebApplicationApi.Data;
using WebApplicationApi.Model;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplicationApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InspectionsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public InspectionsController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: api/inspections - Fetch all inspections for the logged-in user
        [HttpGet]
        public async Task<IActionResult> GetInspections()
        {
            var userName = User.Identity?.Name;

            if (string.IsNullOrEmpty(userName))
            {
                return Unauthorized(new { message = "User is not authenticated." });
            }

            var inspections = await _context.Inspections
                .Include(i => i.Photos)
                .Where(i => i.CreatedBy == userName)
                .ToListAsync();

            var inspectionDtos = inspections.Select(i => new
            {
                i.Id,
                i.CreatedBy,
                i.InspectionName,
                i.Address,
                i.Date,
                Photos = i.Photos.Select(p => new
                {
                    p.Id,
                    p.PhotoName,
                    p.Description,
                    PhotoData = Convert.ToBase64String(p.PhotoData)
                }).ToList()
            });

            return Ok(inspectionDtos);
        }

        // GET: api/inspections/{id} - Fetch a specific inspection by ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetInspection(int id)
        {
            var userName = User.Identity?.Name;

            if (string.IsNullOrEmpty(userName))
            {
                return Unauthorized(new { message = "User is not authenticated." });
            }

            var inspection = await _context.Inspections
                .FirstOrDefaultAsync(i => i.Id == id && i.CreatedBy == userName);

            if (inspection == null)
            {
                return NotFound(new { message = "Inspection not found or access denied." });
            }

            return Ok(inspection);
        }

        // POST: api/inspections/createinspection - Create a new inspection with photo upload
        [HttpPost("createinspection")]
        public async Task<IActionResult> CreateInspection([FromForm] Inspection model)
        {
            var userName = User.Identity?.Name;

            if (string.IsNullOrEmpty(userName))
            {
                return Unauthorized(new { message = "User is not authenticated." });
            }

            model.CreatedBy = userName;
            _context.Inspections.Add(model);
            await _context.SaveChangesAsync();

            return Ok(model);
        }

        // PUT: api/inspections/{id} - Update an existing inspection
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateInspection(int id, [FromBody] Inspection model)
        {
            var userName = User.Identity?.Name;

            if (string.IsNullOrEmpty(userName))
            {
                return Unauthorized(new { message = "User is not authenticated." });
            }

            var existingInspection = await _context.Inspections
                .Include(i => i.Photos)
                .FirstOrDefaultAsync(i => i.Id == id && i.CreatedBy == userName);

            if (existingInspection == null)
            {
                return NotFound(new { message = "Inspection not found or access denied." });
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
            var userName = User.Identity?.Name;

            if (string.IsNullOrEmpty(userName))
            {
                return Unauthorized(new { message = "User is not authenticated." });
            }

            var inspection = await _context.Inspections
                .FirstOrDefaultAsync(i => i.Id == id && i.CreatedBy == userName);

            if (inspection == null)
            {
                return NotFound(new { message = "Inspection not found or access denied." });
            }

            _context.Inspections.Remove(inspection);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Inspection deleted successfully!" });
        }

        // POST: api/inspections/{id}/photos - Add photos to an inspection
        [HttpPost("{id}/photos")]
        public async Task<IActionResult> AddPhotosToInspection(int id, [FromForm] List<IFormFile> photos, [FromForm] List<string> photoNames, [FromForm] List<string> descriptions, [FromForm] List<int> ratings)
        {
            var userName = User.Identity?.Name;

            if (string.IsNullOrEmpty(userName))
            {
                return Unauthorized(new { message = "User is not authenticated." });
            }

            var inspection = await _context.Inspections
                .Include(i => i.Photos)
                .FirstOrDefaultAsync(i => i.Id == id && i.CreatedBy == userName);

            if (inspection == null)
            {
                return NotFound(new { message = "Inspection not found or access denied." });
            }

            if (photos == null || photos.Count == 0 || photoNames.Count != photos.Count || descriptions.Count != photos.Count || ratings.Count != photos.Count)
            {
                return BadRequest(new { message = "Photos, names, descriptions, and ratings are not properly aligned." });
            }

            for (int i = 0; i < photos.Count; i++)
            {
                var photo = photos[i];
                if (photo.Length > 0)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await photo.CopyToAsync(memoryStream);
                        var photoBytes = memoryStream.ToArray();

                        inspection.Photos.Add(new Photo
                        {
                            PhotoData = photoBytes,
                            PhotoName = photoNames[i],
                            Description = descriptions[i],
                            Rating = ratings[i]
                        });
                    }
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Photos added successfully!" });
        }

        // GET: api/inspections/{id}/photos - Get photos for a specific inspection
        [HttpGet("{id}/photos")]
        public async Task<IActionResult> GetPhotosForInspection(int id)
        {
            var userName = User.Identity?.Name;

            if (string.IsNullOrEmpty(userName))
            {
                return Unauthorized(new { message = "User is not authenticated." });
            }

            var inspection = await _context.Inspections
                .Include(i => i.Photos)
                .FirstOrDefaultAsync(i => i.Id == id && i.CreatedBy == userName);

            if (inspection == null)
            {
                return NotFound(new { message = "Inspection not found or access denied." });
            }

            var photoDtos = inspection.Photos.Select(p => new
            {
                p.Id,
                p.PhotoName,
                p.Description,
                p.Rating,
                PhotoData = Convert.ToBase64String(p.PhotoData)
            }).ToList();

            return Ok(photoDtos);
        }

        // DELETE: api/inspections/{inspectionId}/photos/{photoId} - Delete a photo
        [HttpDelete("{inspectionId}/photos/{photoId}")]
        public async Task<IActionResult> DeletePhoto(int inspectionId, int photoId)
        {
            var userName = User.Identity?.Name;

            if (string.IsNullOrEmpty(userName))
            {
                return Unauthorized(new { message = "User is not authenticated." });
            }

            var inspection = await _context.Inspections
                .Include(i => i.Photos)
                .FirstOrDefaultAsync(i => i.Id == inspectionId && i.CreatedBy == userName);

            if (inspection == null)
            {
                return NotFound(new { message = "Inspection not found or access denied." });
            }

            var photo = inspection.Photos.FirstOrDefault(p => p.Id == photoId);
            if (photo == null)
            {
                return NotFound(new { message = "Photo not found." });
            }

            inspection.Photos.Remove(photo);
            _context.Photos.Remove(photo);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Photo deleted successfully!" });
        }
    }
}
