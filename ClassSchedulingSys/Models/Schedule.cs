using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClassSchedulingSys.Models
{
    public class Schedule
    {
        public int Id { get; set; }

        [Required]
        public int SubjectId { get; set; }

        [ForeignKey("SubjectId")]
        public Subject? Subject { get; set; }

        [Required]
        public string FacultyId { get; set; } = string.Empty;

        [ForeignKey("FacultyId")]
        public ApplicationUser? Faculty { get; set; }

        [Required]
        public int RoomId { get; set; }

        [ForeignKey("RoomId")]
        public Room? Room { get; set; }

        [Required]
        public int ClassSectionId { get; set; }

        [ForeignKey("ClassSectionId")]
        public ClassSection? ClassSection { get; set; }

        [Required]
        public DayOfWeek Day { get; set; } // Enum from System

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
