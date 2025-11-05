using SQLite;

namespace FarmGame.Models
{
    [Table("ProduceDefinitions")]
    public class ProduceDefinition
    {
        [PrimaryKey] // Not AutoIncrement, pre-defined IDs
        public int Id { get; set; }
        public string Name { get; set; } // E.g., "Carrot", "Flour", "Blueberry Pie"
        public double BaseSellPrice { get; set; }
        public bool CanBeProcessed { get; set; } // True if this can be an input for a machine
    }
}