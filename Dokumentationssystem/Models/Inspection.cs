using SQLite;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Dokumentationssystem.Models
{

    public class Inspection
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string InspectionName { get; set; }
        public string Address { get; set; }
        public DateTime Date { get; set; }

  
        public string? CreatedBy { get; set; } 
        public ObservableCollection<Photo> Photos { get; set; } = new ObservableCollection<Photo>();

    }

    public class Photo
    {
        [Key] // Or use [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public byte[]? PhotoData { get; set; }
        public string? PhotoName { get; set; }
        public string? Description { get; set; }
        public int InspectionId { get; set; }
        [Range(1, 10, ErrorMessage = "Rating must be between 1 and 10.")]
        public int Rating { get; set; }
        [JsonIgnore]
        public Inspection? Inspection { get; set; }

   
    }


}
