// DTO/RoomCreateDto.cs
using System.ComponentModel.DataAnnotations;

namespace ClassSchedulingSys.DTO
{
    public class RoomDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class RoomCreateDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        public int Capacity { get; set; }

        public string? Type { get; set; } // Lecture, Lab, etc.

        [Required]
        public int BuildingId { get; set; }
    }

    public class RoomUpdateDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        public int Capacity { get; set; }

        public string? Type { get; set; }

        [Required]
        public int BuildingId { get; set; }
    }

    public class RoomReadDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public int Capacity { get; set; }

        public string? Type { get; set; }

        public int BuildingId { get; set; }

        public string? BuildingName { get; set; }
    }
}
