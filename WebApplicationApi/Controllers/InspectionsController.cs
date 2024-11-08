using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        // GET: api/inspections - Fetch all inspections
        [HttpGet]
        public async Task<IActionResult> GetInspections()
        {
            var inspections = await _context.Inspections
                .Include(i => i.Photos) // Ensure photos are included in the response
                .ToListAsync();

            // Transform the inspections to include Base64 encoded photos
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
                    PhotoData = Convert.ToBase64String(p.PhotoData) // Convert photo to Base64 string
                }).ToList()
            });

            return Ok(inspectionDtos);
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
        public async Task<IActionResult> CreateInspection([FromForm] Inspection model)
        {
            _context.Inspections.Add(model);
            await _context.SaveChangesAsync();

            return Ok(model);
        }

        // PUT: api/inspections/{id} - Update an existing inspection with optional photo uploads
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateInspection(int id, [FromBody] Inspection model)
        {
            var existingInspection = await _context.Inspections
                .Include(i => i.Photos)
                .FirstOrDefaultAsync(i => i.Id == id);

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

        // POST: api/inspections/{id}/photos - Add photos to an inspection
        [HttpPost("{id}/photos")]
        public async Task<IActionResult> AddPhotosToInspection(int id, [FromForm] List<IFormFile> photos, [FromForm] List<string> photoNames, [FromForm] List<string> descriptions, [FromForm] List<int> ratings)
        {
            var inspection = await _context.Inspections
                .Include(i => i.Photos)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (inspection == null)
            {
                return NotFound(new { message = "Inspection not found." });
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

        // PUT: api/inspections/{inspectionId}/photos/{photoId} - Update a specific photo
        [HttpPut("{inspectionId}/photos/{photoId}")]
        public async Task<IActionResult> UpdatePhoto(int inspectionId, int photoId, [FromBody] Photo updatedPhoto)
        {
            var inspection = await _context.Inspections
                .Include(i => i.Photos)
                .FirstOrDefaultAsync(i => i.Id == inspectionId);

            if (inspection == null)
            {
                return NotFound(new { message = "Inspection not found." });
            }

            var photo = inspection.Photos.FirstOrDefault(p => p.Id == photoId);
            if (photo == null)
            {
                return NotFound(new { message = "Photo not found." });
            }

            if (!string.IsNullOrEmpty(updatedPhoto.PhotoName))
            {
                photo.PhotoName = updatedPhoto.PhotoName;
            }

            if (!string.IsNullOrEmpty(updatedPhoto.Description))
            {
                photo.Description = updatedPhoto.Description;
            }

            if (updatedPhoto.PhotoData != null && updatedPhoto.PhotoData.Length > 0)
            {
                photo.PhotoData = updatedPhoto.PhotoData;
            }

            if (updatedPhoto.Rating >= 1 && updatedPhoto.Rating <= 10)
            {
                photo.Rating = updatedPhoto.Rating;
            }

            _context.Photos.Update(photo);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Photo updated successfully!" });
        }

        // GET: api/inspections/{id}/photos - Get photos for a specific inspection
        [HttpGet("{id}/photos")]
        public async Task<IActionResult> GetPhotosForInspection(int id)
        {
            var inspection = await _context.Inspections
                .Include(i => i.Photos)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (inspection == null)
            {
                return NotFound(new { message = "Inspection not found." });
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

        // DELETE: api/inspections/{inspectionId}/photos/{photoId} - Delete a specific photo
        [HttpDelete("{inspectionId}/photos/{photoId}")]
        public async Task<IActionResult> DeletePhoto(int inspectionId, int photoId)
        {
            var inspection = await _context.Inspections
                .Include(i => i.Photos)
                .FirstOrDefaultAsync(i => i.Id == inspectionId);

            if (inspection == null)
            {
                return NotFound(new { message = "Inspection not found." });
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