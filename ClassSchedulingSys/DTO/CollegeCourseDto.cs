// ClassSchedulingSys/DTO/CollegeCourseDto.cs
using System.ComponentModel.DataAnnotations;

namespace ClassSchedulingSys.DTO
{
    public class CollegeCourseDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class CreateCollegeCourseDto
    {
        [Required]
        public string Code { get; set; } = string.Empty;

        [Required]
        public string Name { get; set; } = string.Empty;
    }

    public class UpdateCollegeCourseDto
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public string Code { get; set; } = string.Empty;

        [Required]
        public string Name { get; set; } = string.Empty;
    }
}
