using SQLite;
using System;

namespace FarmGame.Models
{
    [Table("Plots")]
    public class Plot
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        // Logical position in the grid (e.g., for a 5x5 grid, 0-24)
        public int PlotNumber { get; set; }

        public bool IsTilled { get; set; }

        // Foreign Key to SeedDefinition for the type of seed planted
        public int? PlantedSeedDefinitionId { get; set; } // Nullable if nothing is planted

        public DateTime? PlantTime { get; set; } // Nullable if nothing is planted
        public double GrowthProgress { get; set; } // 0.0 to 1.0
        public bool IsWatered { get; set; }
    }
}