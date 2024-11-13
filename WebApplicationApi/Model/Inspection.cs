using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace WebApplicationApi.Model
{
    public class Inspection
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string InspectionName { get; set; }

        [Required]
        public string Address { get; set; }

        public DateTime Date { get; set; }

        [Required]
        public string UserId { get; set; } // Foreign key to the user

        [ForeignKey("UserId")]
        public IdentityUser User { get; set; } // Navigation property to User

        public string? CreatedBy { get; set; } 

        // Use ICollection instead of ObservableCollection for EF compatibility
        public ICollection<Photo> Photos { get; set; } = new List<Photo>();
    }

    public class Photo
    {
        [Key]
        public int Id { get; set; }

        public byte[]? PhotoData { get; set; }

        public string? PhotoName { get; set; }

        public string? Description { get; set; }

        public int InspectionId { get; set; }

        [Range(1, 10, ErrorMessage = "Rating must be between 1 and 10.")]
        public int Rating { get; set; }

        [JsonIgnore]
        [ForeignKey("InspectionId")]
        public Inspection? Inspection { get; set; } // Navigation property to Inspection
    }
}
