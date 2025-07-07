// ClassSchedulingSys/Models/Schedule
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClassSchedulingSys.Models
{
    public class Schedule
    {
        public int Id { get; set; }

        [Required]
        public DayOfWeek Day { get; set; } // Enum ensures consistency (Monday–Sunday)

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        [NotMapped]
        public double Duration => (EndTime - StartTime).TotalHours;

        [Required]
        public string FacultyId { get; set; } // Tied to ApplicationUser

        [Required]
        public int RoomId { get; set; }

        [Required]
        public int SubjectId { get; set; }

        [Required]
        public int ClassSectionId { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation Properties
        [ForeignKey("FacultyId")]
        public ApplicationUser Faculty { get; set; } = null!;

        [ForeignKey("RoomId")]
        public Room Room { get; set; } = null!;

        [ForeignKey("SubjectId")]
        public Subject Subject { get; set; } = null!;

        [ForeignKey("ClassSectionId")]
        public ClassSection ClassSection { get; set; } = null!;
    }
}
