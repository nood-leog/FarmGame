using SQLite;

namespace FarmGame.Models
{
    [Table("PlayerOwnedTools")]
    public class PlayerOwnedTool
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        // Foreign Key to ToolDefinition for the type of tool owned
        public int ToolDefinitionId { get; set; }

        // You might have a flag like IsEquipped here if you want to track multiple owned tools
        // and which one is active, but PlayerState already covers 'SelectedHoeToolId'.
        // For simplicity, owning a tool might imply it's available for selection.
    }
}