// ClassSchedulingSys/Models/Building
using System.ComponentModel.DataAnnotations;

namespace ClassSchedulingSys.Models
{
    public class Building
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public ICollection<Room>? Rooms { get; set; }
    }
}
