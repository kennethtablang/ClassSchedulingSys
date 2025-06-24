using System.ComponentModel.DataAnnotations;

namespace ClassSchedulingSys.DTO
{
    public class SemesterDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public bool IsCurrent { get; set; }

        public int SchoolYearId { get; set; }

        public string? SchoolYearLabel { get; set; } // Optional: for display

        public bool IsSchoolYearCurrent { get; set; }
    }

    public class CreateSemesterDto
    {
        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        public int SchoolYearId { get; set; }

        public bool IsCurrent { get; set; } = false;
    }

    public class UpdateSemesterDto : CreateSemesterDto
    {
        public int Id { get; set; }
    }
}
