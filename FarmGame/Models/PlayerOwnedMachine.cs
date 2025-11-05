using SQLite;
using System;

namespace FarmGame.Models
{
    [Table("PlayerOwnedMachines")]
    public class PlayerOwnedMachine
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        // Foreign Key to MachineDefinition for the type of machine owned
        public int MachineDefinitionId { get; set; }

        public bool IsProcessing { get; set; }
        public DateTime? ProcessStartTime { get; set; } // Nullable if not currently processing

        // What produce is currently in the machine for processing
        public int? InputProduceInMachineId { get; set; } // Nullable
        public int? InputQuantityInMachine { get; set; } // Nullable
    }
}