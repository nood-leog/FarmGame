namespace FarmGame.Models
{
    public class DisplayInventoryItem
    {
        public int Id { get; set; } // InventoryItem's Id (PK)
        public int ProduceDefinitionId { get; set; } // <--- ADD THIS LINE
        public string Name { get; set; } // ProduceDefinition's Name (or SeedDefinition's Name if IsSeed is true)
        public int Quantity { get; set; } // InventoryItem's Quantity
        public bool IsSeed { get; set; } // InventoryItem's IsSeed flag
        public double BaseSellPrice { get; set; } // ProduceDefinition's BaseSellPrice (or SeedDefinition's ShopCost if IsSeed is true)
    }
}