using SQLite;

namespace FarmGame.Models
{
    [Table("InventoryItems")]
    public class InventoryItem
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        // Foreign Key to ProduceDefinition (represents both seeds and harvested/processed goods)
        public int ProduceDefinitionId { get; set; }
        public int Quantity { get; set; }

        // This flag helps differentiate if the ProduceDefinitionId refers to a seed type
        // or a harvestable/processed item in the inventory.
        public bool IsSeed { get; set; }
    }
}