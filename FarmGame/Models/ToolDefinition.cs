using SQLite;

namespace FarmGame.Models
{
    [Table("ToolDefinitions")]
    public class ToolDefinition
    {
        [PrimaryKey] // Not AutoIncrement, pre-defined IDs
        public int Id { get; set; }
        public string Name { get; set; } // E.g., "Rusty Hoe", "Advanced Watering Can"
        public string Type { get; set; } // "Hoe" or "WateringCan"
        public int Range { get; set; } // Area of effect (1 for 1x1, 3 for 3x3, etc.)
        public double ShopCost { get; set; }

        public double? MaxWaterCapacity { get; set; } // Nullable, only for Watering Cans
        public double? WaterRefillRate { get; set; } // Nullable, only for Watering Cans
    }
}