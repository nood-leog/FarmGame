using SQLite;
using System; // For DateTime

namespace FarmGame.Models
{
    [Table("PlayerState")]
    public class PlayerState
    {
        [PrimaryKey] // There will only be one row, so Id=1 will be its primary key.
        public int Id { get; set; } = 1; // Default to 1, ensuring a single record

        public double Money { get; set; }
        public double CurrentWater { get; set; }
        public double MaxWater { get; set; }
        public double WaterRefillRate { get; set; } // e.g., water units per second

        // Foreign Keys for equipped tools - these will reference the Id from ToolDefinition
        public int? SelectedHoeToolId { get; set; } // Nullable, as player might not have one equipped
        public int? SelectedWaterToolId { get; set; } // Nullable

        // Storing DateTime as TEXT (ISO 8601 format) is a common practice for SQLite
        public DateTime LastSaveTime { get; set; } = DateTime.UtcNow;
    }
}