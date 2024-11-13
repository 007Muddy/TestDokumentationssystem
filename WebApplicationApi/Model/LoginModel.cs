using SQLite;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace WebApplicationApi.Model
{
    public class LoginModel
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

    
    }

    public class UserDto
    {
        public string UserName { get; set; }
        public string Email { get; set; }
        public int InspectionCount { get; set; }
    }

}
