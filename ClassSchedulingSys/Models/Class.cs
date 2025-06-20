using System.ComponentModel.DataAnnotations;

namespace ClassSchedulingSys.Models
{
    public class Class
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty; // e.g. BSIT 2A

        public int DepartmentId { get; set; } //navigation property
        public Department? Department { get; set; }

        public ICollection<Schedule>? Schedules { get; set; } //inverse property
    }
}
