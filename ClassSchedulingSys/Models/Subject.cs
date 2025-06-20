using System.ComponentModel.DataAnnotations;

namespace ClassSchedulingSys.Models
{
    public class Subject
    {
        public int Id { get; set; }

        [Required]
        public string Code { get; set; } = null!; // e.g., CS101

        [Required]
        public string Title { get; set; } = null!; // e.g., Introduction to Programming

        public string? Description { get; set; }

        public int Units { get; set; }

        public ICollection<Schedule>? Schedules { get; set; } // inverse property

    }
}
