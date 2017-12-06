using System.ComponentModel.DataAnnotations;

namespace ProjectManager.Models
{
    public partial class ProjectEmployees
    {
        public int ProjectEmployeesId { get; set; }
        public int ProjectId { get; set; }
        [Required(ErrorMessage ="Choose the employee")]
        public int EmployeeId { get; set; }
        [Required]
        [RegularExpression(@"(100|0|[1-9]\d|[1-9])", ErrorMessage = "Enter the correct value")]
        public short Percent { get; set; }

        public Employees Employee { get; set; }
        public Projects Project { get; set; }
    }
}
