

using SQLite;
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

        //[JsonIgnore]
        public string? CreatedBy { get; set; }  // This will be set by the server
    }

}

