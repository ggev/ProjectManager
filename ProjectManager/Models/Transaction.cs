using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectManager.Models
{
    public class Transaction
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public DateTime Date { get; set; }
        public int Payment { get; set; }
        public byte Status { get; set; }
        public int PaymentCounter { get; set; }

        public Employees Employee { get; set; }
    }
}
