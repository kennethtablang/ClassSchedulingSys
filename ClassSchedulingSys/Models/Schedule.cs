using System.ComponentModel.DataAnnotations;

namespace ClassSchedulingSys.Models
{
    public class Schedule
    {
        public int Id { get; set; }

        //navigation
        [Required]
        public int SubjectId { get; set; }
        public Subject? Subject { get; set; }

        //navigation
        [Required]
        public string FacultyId { get; set; } = null!;
        public ApplicationUser? Faculty { get; set; }

        //navigation
        [Required]
        public int RoomId { get; set; }
        public Room? Room { get; set; }

        [Required]
        public DayOfWeek Day { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        //navigation
        [Required]
        public int SemesterId { get; set; }
        public Semester? Semester { get; set; }

        //navigation
        [Required]
        public int? ClassId { get; set; }
        public Class? Class { get; set; }
    }
}
