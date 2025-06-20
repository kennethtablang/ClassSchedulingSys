using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ClassSchedulingSys.Models
{
    public class Room
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = null!;

        public int Capacity { get; set; }

        public string? Type { get; set; }  // Lecture, Lab, etc.

        public int BuildingId { get; set; }
        [ForeignKey("BuildingId")]
        public Building? Building { get; set; }

        public ICollection<Schedule>? Schedules { get; set; }

    }
}
