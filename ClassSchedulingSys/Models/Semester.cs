using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ClassSchedulingSys.Models
{
    public class Semester
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty; // e.g. "1st Semester", "2nd Semester"

        public int SchoolYearId { get; set; }
        [ForeignKey("SchoolYearId")]
        public SchoolYear? SchoolYear { get; set; }

        public ICollection<Schedule>? Schedules { get; set; }

    }
}
