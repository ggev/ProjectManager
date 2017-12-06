using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProjectManager.Models
{
    public partial class Projects
    {
        public Projects()
        {
            ProjectEmployees = new HashSet<ProjectEmployees>();
        }

        public int ProjectId { get; set; }
        public string Name { get; set; }
        [DataType(DataType.Date)]
        public DateTime Start { get; set; }
        [DataType(DataType.Date)]
        public DateTime Deadline { get; set; }
        public double Budget { get; set; }
        public short? Deposit { get; set; }
        public short Status { get; set; }

        public ICollection<ProjectEmployees> ProjectEmployees { get; set; }
    }
}
