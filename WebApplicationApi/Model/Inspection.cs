

using SQLite;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace WebApplicationApi.Model
    {
    public class Inspection
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string InspectionName { get; set; }
        public string Address { get; set; }
        public DateTime Date { get; set; }

        //public string PhotoDescription { get; set; }

        //[JsonIgnore]
        public string? CreatedBy { get; set; }  // This will be set by the server
        public ObservableCollection<Photo> Photos { get; set; } = new ObservableCollection<Photo>();

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
        public Inspection? Inspection { get; set; }


    }



}

