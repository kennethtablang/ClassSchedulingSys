// ClassSchedulingSys/DTO/BuildingCreateDto.cs
using System.ComponentModel.DataAnnotations;

namespace ClassSchedulingSys.DTO
{
    public class BuildingCreateDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }
    }

    public class BuildingUpdateDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public bool IsActive { get; set; }
    }

    public class BuildingReadDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public bool IsActive { get; set; }

        public List<RoomDto>? Rooms { get; set; }
    }
}
