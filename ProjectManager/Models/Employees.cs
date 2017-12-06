using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectManager.Models
{
    public partial class Employees
    {
        public Employees()
        {
            ProjectEmployees = new HashSet<ProjectEmployees>();
        }

        public int EmployeeId { get; set; }
        [Required(ErrorMessage = "Frist Name field is required")]
        [DisplayName("First Name")]
        public string FirstName { get; set; }
        [Required(ErrorMessage = "Last Name field is required")]
        [DisplayName("Last Name")]
        public string LastName { get; set; }
        [DataType(DataType.Date)]
        public DateTime Birthday { get; set; }
        [Required(ErrorMessage = "PhoneNumber field is required")]
        [DisplayName("Phone Number")]
        public string PhoneNumber { get; set; }
        public short Position { get; set; }
        public int Salary { get; set; }
        public int Experiance { get; set; }
        [DataType(DataType.Date)]
        [DisplayName("Beginning Of Work")]
        public DateTime BeginningOfWork { get; set; }
        public virtual byte[] Photo { get; set; }
        public string Description { get; set; }
        public byte Vacation { get; set; }
        public byte Overtime { get; set; }
        public DateTime CalculateMonthPaymentFrom { get; set; }

        [DisplayName("Photo")]
        [NotMapped]
        public IFormFile Image { get; set; }

        public string FullName
        {
            get
            {
                return $"{FirstName} {LastName}";
            }
        }

        public ICollection<ProjectEmployees> ProjectEmployees { get; set; }
    }
}
