using SQLite;

namespace FarmGame.Models
{
    [Table("MachineDefinitions")]
    public class MachineDefinition
    {
        [PrimaryKey] // Not AutoIncrement, pre-defined IDs
        public int Id { get; set; }
        public string Name { get; set; } // E.g., "Mill", "Oven"
        public double ShopCost { get; set; }
        public int ProcessingTimeSeconds { get; set; }

        // Foreign Key to ProduceDefinition for input
        public int InputProduceDefinitionId { get; set; }
        public int InputQuantity { get; set; }

        // Foreign Key to ProduceDefinition for output
        public int OutputProduceDefinitionId { get; set; }
        public int OutputQuantity { get; set; }
    }
}