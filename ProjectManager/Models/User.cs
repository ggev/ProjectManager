using System.ComponentModel.DataAnnotations;

namespace ProjectManager.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required(ErrorMessage ="Enter the login")]
        public string Login { get; set; }

        [Required(ErrorMessage = "Enter the password")]
        [StringLength(255, ErrorMessage = "Must be between 5 and 255 characters", MinimumLength = 5)]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}
