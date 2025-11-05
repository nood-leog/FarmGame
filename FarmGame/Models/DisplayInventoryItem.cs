namespace FarmGame.Models
{
    public class DisplayInventoryItem
    {
        public int Id { get; set; } // InventoryItem's Id
        public string Name { get; set; } // ProduceDefinition's Name
        public int Quantity { get; set; } // InventoryItem's Quantity
        public bool IsSeed { get; set; } // InventoryItem's IsSeed flag
        public double BaseSellPrice { get; set; } // ProduceDefinition's BaseSellPrice
    }
}