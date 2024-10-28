

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
        [Key] // Or use [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public byte[]? PhotoData { get; set; }
        public string? PhotoName { get; set; }
        public string? Description { get; set; }
        public int InspectionId { get; set; }

        [JsonIgnore]
        public Inspection? Inspection { get; set; }
    }



}

