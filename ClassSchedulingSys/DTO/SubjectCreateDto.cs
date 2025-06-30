using System.ComponentModel.DataAnnotations;

namespace ClassSchedulingSys.DTO
{
    public class SubjectCreateDTO
    {
        [Required]
        public string SubjectCode { get; set; }

        [Required]
        public string SubjectTitle { get; set; }

        [Required]
        public int Units { get; set; }

        [Required]
        public string SubjectType { get; set; } // "Lecture", "Lab", "Lecture-Lab"

        public string? Color { get; set; }

        [Required]
        public string YearLevel { get; set; }   // "1st Year", etc.

        [Required]
        public int CollegeCourseId { get; set; }

        //[Required]
        //public int SemesterId { get; set; }
    }

    public class SubjectUpdateDTO
    {
        [Required]
        public string SubjectCode { get; set; }

        [Required]
        public string SubjectTitle { get; set; }

        [Required]
        public int Units { get; set; }

        [Required]
        public string SubjectType { get; set; }

        [Required]
        public string YearLevel { get; set; }

        [Required]
        public int CollegeCourseId { get; set; }

        public string? Color { get; set; }

        //[Required]
        //public int SemesterId { get; set; }
    }

    public class SubjectReadDTO
    {
        public int Id { get; set; }

        public string SubjectCode { get; set; }

        public string SubjectTitle { get; set; }

        public int Units { get; set; }

        public string SubjectType { get; set; }

        public string YearLevel { get; set; }

        public int CollegeCourseId { get; set; }

        public string CollegeCourseName { get; set; } // Optional for display



        public string? Color { get; set; }

        public bool IsActive { get; set; }
    }

}
