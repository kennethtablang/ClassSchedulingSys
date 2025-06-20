﻿using System.ComponentModel.DataAnnotations;

namespace ClassSchedulingSys.Models
{
    public class Building
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        public ICollection<Room>? Rooms { get; set; }
    }
}
