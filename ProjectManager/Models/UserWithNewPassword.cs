using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace ProjectManager.Models
{
    public class UserWithNewPassword : User
    {
        [Required(ErrorMessage = "Enter the password")]
        [StringLength(255, ErrorMessage = "Must be between 5 and 255 characters", MinimumLength = 5)]
        [DataType(DataType.Password)]
        [DisplayName("New Password")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Enter the password")]
        [DataType(DataType.Password)]
        [DisplayName("Compare New Password")]
        [Compare("NewPassword", ErrorMessage = "Both password must be same")]
        public string ComparePassword { get; set; }
    }
}
