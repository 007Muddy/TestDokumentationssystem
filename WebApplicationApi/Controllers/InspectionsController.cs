
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebApplicationApi.Data;
using WebApplicationApi.Model;
using System.IO;
using iTextSharp.text;
using iTextSharp.text.pdf;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;

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
                .Include(i => i.Photos) // Ensure photos are included in the response
                .Where(i => i.CreatedBy == userId)
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
        //Methods for download
        [HttpPost("download-inspections-pdf")]
        [Authorize]
        public async Task<IActionResult> DownloadMultipleInspectionsAsPdf([FromBody] List<int> inspectionIds)
        {
            if (inspectionIds == null || !inspectionIds.Any())
            {
                return BadRequest(new { message = "No inspections selected." });
            }

            var inspections = await _context.Inspections
                                             .Where(i => inspectionIds.Contains(i.Id))
                                             .ToListAsync();

            if (!inspections.Any())
            {
                return NotFound(new { message = "No valid inspections found." });
            }

            using var stream = new MemoryStream();

            try
            {
                var document = new iTextSharp.text.Document();
                PdfWriter writer = PdfWriter.GetInstance(document, stream);
                writer.CloseStream = false;
                document.Open();

                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18);
                var textFont = FontFactory.GetFont(FontFactory.HELVETICA, 12);

                foreach (var inspection in inspections)
                {
                    var photos = await _context.Photos.Where(p => p.InspectionId == inspection.Id).ToListAsync();

                    document.Add(new iTextSharp.text.Paragraph($"Inspection: {inspection.InspectionName}", titleFont));
                    document.Add(new iTextSharp.text.Paragraph($"Address: {inspection.Address}", textFont));
                    document.Add(new iTextSharp.text.Paragraph($"Date: {inspection.Date:yyyy-MM-dd HH:mm:ss}", textFont));
                    document.Add(new iTextSharp.text.Paragraph(" "));

                    foreach (var photo in photos)
                    {
                        document.Add(new iTextSharp.text.Paragraph(photo.PhotoName, FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14)));
                        document.Add(new iTextSharp.text.Paragraph($"Rating: {photo.Rating}", textFont));
                        document.Add(new iTextSharp.text.Paragraph(photo.Description, textFont));

                        if (photo.PhotoData != null && photo.PhotoData.Length > 0)
                        {
                            try
                            {
                                var image = iTextSharp.text.Image.GetInstance(photo.PhotoData);
                                image.ScaleToFit(300f, 300f);
                                image.Alignment = iTextSharp.text.Element.ALIGN_CENTER;
                                document.Add(image);
                            }
                            catch
                            {
                                document.Add(new iTextSharp.text.Paragraph("Error loading image", textFont));
                            }
                        }

                        document.Add(new iTextSharp.text.Paragraph(" "));
                    }

                    document.Add(new iTextSharp.text.Paragraph("------------------------------------------------------", textFont));
                }

                document.Close();
                stream.Position = 0;

                var pdfFileName = "Selected_Inspections.pdf";
                return File(stream.ToArray(), "application/pdf", pdfFileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error generating PDF", details = ex.Message });
            }
        }



        [HttpPost("download-inspections-word")]
        [Authorize]
        public async Task<IActionResult> DownloadMultipleInspectionsAsWord([FromBody] List<int> inspectionIds)
        {
            if (inspectionIds == null || !inspectionIds.Any())
            {
                return BadRequest(new { message = "No inspections selected." });
            }

            // Fetch the inspections and their associated photos
            var inspections = await _context.Inspections
                                             .Where(i => inspectionIds.Contains(i.Id))
                                             .ToListAsync();

            if (!inspections.Any())
            {
                return NotFound(new { message = "No valid inspections found." });
            }

            using (var stream = new MemoryStream())
            {
                using (WordprocessingDocument doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document, true))
                {
                    MainDocumentPart mainPart = doc.AddMainDocumentPart();
                    mainPart.Document = new DocumentFormat.OpenXml.Wordprocessing.Document();
                    Body body = mainPart.Document.AppendChild(new Body());

                    foreach (var inspection in inspections)
                    {
                        // Get photos for each inspection
                        var photos = await _context.Photos.Where(p => p.InspectionId == inspection.Id).ToListAsync();

                        var timeZone = TimeZoneInfo.Local;
                        var localDate = TimeZoneInfo.ConvertTimeFromUtc(inspection.Date, timeZone);

                        // Add inspection details
                        body.Append(CreateParagraph($"Inspection: {inspection.InspectionName}", 24, true, JustificationValues.Center));
                        body.Append(CreateParagraph($"Address: {inspection.Address}", 14, false, JustificationValues.Center));
                        body.Append(CreateParagraph($"Date: {localDate:yyyy-MM-dd HH:mm:ss}", 14, false, JustificationValues.Center));
                        body.Append(new DocumentFormat.OpenXml.Wordprocessing.Paragraph(new DocumentFormat.OpenXml.Wordprocessing.Run(new DocumentFormat.OpenXml.Wordprocessing.Text("")))); // Spacer

                        foreach (var photo in photos)
                        {
                            // Add photo details
                            body.Append(CreateParagraph(photo.PhotoName, 18, true, JustificationValues.Left));
                            body.Append(CreateParagraph($"Rating: {photo.Rating}", 14, false, JustificationValues.Left));
                            body.Append(CreateParagraph(photo.Description, 12, false, JustificationValues.Left));

                            if (photo.PhotoData != null && photo.PhotoData.Length > 0)
                            {
                                AddImageToBody(mainPart, body, photo.PhotoData);
                            }

                            body.Append(new DocumentFormat.OpenXml.Wordprocessing.Paragraph(new DocumentFormat.OpenXml.Wordprocessing.Run(new DocumentFormat.OpenXml.Wordprocessing.Text("")))); // Spacer
                        }

                        // Add a separator between inspections
                        body.Append(CreateParagraph("------------------------------------------------------", 12, false, JustificationValues.Center));
                        body.Append(new DocumentFormat.OpenXml.Wordprocessing.Paragraph(new DocumentFormat.OpenXml.Wordprocessing.Run(new DocumentFormat.OpenXml.Wordprocessing.Text(""))));
                    }

                    mainPart.Document.Save();
                }

                stream.Seek(0, SeekOrigin.Begin);
                return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "Selected_Inspections.docx");
            }
        }


        // Helper method to create a formatted paragraph
        private DocumentFormat.OpenXml.Wordprocessing.Paragraph CreateParagraph(string text, int fontSize, bool bold, JustificationValues alignment)
        {
            RunProperties runProperties = new RunProperties();
            runProperties.Append(new FontSize() { Val = (fontSize * 2).ToString() });
            if (bold)
            {
                runProperties.Append(new Bold());
            }

            Run run = new Run();
            run.Append(runProperties);
            run.Append(new DocumentFormat.OpenXml.Wordprocessing.Text(text) { Space = SpaceProcessingModeValues.Preserve });

            ParagraphProperties paragraphProperties = new ParagraphProperties();
            paragraphProperties.Append(new Justification() { Val = alignment });

            DocumentFormat.OpenXml.Wordprocessing.Paragraph paragraph = new DocumentFormat.OpenXml.Wordprocessing.Paragraph();
            paragraph.Append(paragraphProperties);
            paragraph.Append(run);

            return paragraph;
        }


        // Helper method to add an image to the Word document
        private void AddImageToBody(MainDocumentPart mainPart, Body body, byte[] imageData)
        {
            // Add image part to the document
            ImagePart imagePart = mainPart.AddImagePart(ImagePartType.Jpeg);

            // Feed the image data into the ImagePart
            using (MemoryStream stream = new MemoryStream(imageData))
            {
                imagePart.FeedData(stream);
            }

            // Get the ID of the image part
            string imageId = mainPart.GetIdOfPart(imagePart);

            // Define the image drawing properties and add it to the document body
            var element = new Drawing(
                new DocumentFormat.OpenXml.Drawing.Wordprocessing.Inline(
                    new DocumentFormat.OpenXml.Drawing.Wordprocessing.Extent() { Cx = 990000L, Cy = 792000L },
                    new DocumentFormat.OpenXml.Drawing.Wordprocessing.EffectExtent()
                    {
                        LeftEdge = 0L,
                        TopEdge = 0L,
                        RightEdge = 0L,
                        BottomEdge = 0L
                    },
                    new DocumentFormat.OpenXml.Drawing.Wordprocessing.DocProperties()
                    {
                        Id = (UInt32Value)1U,
                        Name = "Picture"
                    },
                    new DocumentFormat.OpenXml.Drawing.Wordprocessing.NonVisualGraphicFrameDrawingProperties(
                        new DocumentFormat.OpenXml.Drawing.GraphicFrameLocks() { NoChangeAspect = true }),
                    new DocumentFormat.OpenXml.Drawing.Graphic(
                        new DocumentFormat.OpenXml.Drawing.GraphicData(
                            new DocumentFormat.OpenXml.Drawing.Pictures.Picture(
                                new DocumentFormat.OpenXml.Drawing.Pictures.NonVisualPictureProperties(
                                    new DocumentFormat.OpenXml.Drawing.Pictures.NonVisualDrawingProperties()
                                    {
                                        Id = (UInt32Value)0U,
                                        Name = "Image.jpg"
                                    },
                                    new DocumentFormat.OpenXml.Drawing.Pictures.NonVisualPictureDrawingProperties()),
                                new DocumentFormat.OpenXml.Drawing.Pictures.BlipFill(
                                    new DocumentFormat.OpenXml.Drawing.Blip()
                                    {
                                        Embed = imageId,
                                        CompressionState = DocumentFormat.OpenXml.Drawing.BlipCompressionValues.Print
                                    },
                                    new DocumentFormat.OpenXml.Drawing.Stretch(new DocumentFormat.OpenXml.Drawing.FillRectangle())),
                                new DocumentFormat.OpenXml.Drawing.Pictures.ShapeProperties(
                                    new DocumentFormat.OpenXml.Drawing.Transform2D(
                                        new DocumentFormat.OpenXml.Drawing.Offset() { X = 0L, Y = 0L },
                                        new DocumentFormat.OpenXml.Drawing.Extents() { Cx = 990000L, Cy = 792000L }),
                                    new DocumentFormat.OpenXml.Drawing.PresetGeometry(new DocumentFormat.OpenXml.Drawing.AdjustValueList())
                                    { Preset = DocumentFormat.OpenXml.Drawing.ShapeTypeValues.Rectangle })))))
                {
                    DistanceFromTop = (UInt32Value)0U,
                    DistanceFromBottom = (UInt32Value)0U,
                    DistanceFromLeft = (UInt32Value)0U,
                    DistanceFromRight = (UInt32Value)0U,
                    EditId = "50D07946"
                });

            // Add the image to a paragraph and then add the paragraph to the body
            var paragraph = new DocumentFormat.OpenXml.Wordprocessing.Paragraph(new DocumentFormat.OpenXml.Wordprocessing.Run(element));
            body.Append(paragraph);
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
        public async Task<IActionResult> CreateInspection([FromForm] Inspection model)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User is not authenticated." });
            }

            model.CreatedBy = userId;
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

        [HttpPost("{id}/photos")]
        [Authorize]
        public async Task<IActionResult> AddPhotosToInspection(int id, [FromForm] List<IFormFile> photos, [FromForm] List<string> photoNames, [FromForm] List<string> descriptions, [FromForm] List<int> ratings)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User is not authenticated." });
            }

            var inspection = await _context.Inspections
                .Include(i => i.Photos)
                .FirstOrDefaultAsync(i => i.Id == id && i.CreatedBy == userId);

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


        [HttpPut("{inspectionId}/photos/{photoId}")]
        [Authorize]
        public async Task<IActionResult> UpdatePhoto(int inspectionId, int photoId, [FromBody] Photo updatedPhoto)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User is not authenticated." });
            }

            var inspection = await _context.Inspections
                .Include(i => i.Photos)
                .FirstOrDefaultAsync(i => i.Id == inspectionId && i.CreatedBy == userId);

            if (inspection == null)
            {
                return NotFound(new { message = "Inspection not found or access denied." });
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


        // GET: api/inspections/{id}/photos - Get photos with names and descriptions for a specific inspection
        [HttpGet("{id}/photos")]
        [Authorize]
        public async Task<IActionResult> GetPhotosForInspection(int id)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User is not authenticated." });
            }

            // Find the inspection by ID
            var inspection = await _context.Inspections
                .Include(i => i.Photos)  // Ensure Photos are included in the query
                .FirstOrDefaultAsync(i => i.Id == id && i.CreatedBy == userId);

            if (inspection == null)
            {
                return NotFound(new { message = "Inspection not found or access denied." });
            }

            // Prepare photo data to return (Base64-encoded photos, names, and descriptions)
            var photoDtos = inspection.Photos.Select(p => new
            {
                p.Id,
                p.PhotoName,
                p.Description,
                p.Rating,
                PhotoData = Convert.ToBase64String(p.PhotoData) // Convert byte[] to Base64 string
            }).ToList();

            // Log the photo IDs to verify they're being sent correctly
            foreach (var photo in photoDtos)
            {
                Console.WriteLine($"Photo ID sent to client: {photo.Id}");
            }

            return Ok(photoDtos);
        }

        // DELETE: api/inspections/{inspectionId}/photos/{photoId} - Delete a specific photo from an inspection
        [HttpDelete("{inspectionId}/photos/{photoId}")]
        [Authorize]
        public async Task<IActionResult> DeletePhoto(int inspectionId, int photoId)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User is not authenticated." });
            }

            // Find the inspection by ID and ensure it belongs to the authenticated user
            var inspection = await _context.Inspections
                .Include(i => i.Photos)
                .FirstOrDefaultAsync(i => i.Id == inspectionId && i.CreatedBy == userId);

            if (inspection == null)
            {
                return NotFound(new { message = "Inspection not found or access denied." });
            }

            // Find the specific photo by ID
            var photo = inspection.Photos.FirstOrDefault(p => p.Id == photoId);
            if (photo == null)
            {
                return NotFound(new { message = "Photo not found." });
            }

            // Remove the photo from the inspection
            inspection.Photos.Remove(photo);
            _context.Photos.Remove(photo);

            // Save changes to the database
            await _context.SaveChangesAsync();

            return Ok(new { message = "Photo deleted successfully!" });
        }


    }
}
