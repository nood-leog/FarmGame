using SQLite;

namespace FarmGame.Models
{
    [Table("SeedDefinitions")]
    public class SeedDefinition
    {
        [PrimaryKey] // Not AutoIncrement, as we'll pre-define these IDs
        public int Id { get; set; }
        public string Name { get; set; }
        public double ShopCost { get; set; }
        public int GrowTimeSeconds { get; set; } // Time to grow from planted to harvestable

        // Foreign Key to ProduceDefinition for what this seed yields when harvested
        public int HarvestsProduceDefinitionId { get; set; }
    }
}