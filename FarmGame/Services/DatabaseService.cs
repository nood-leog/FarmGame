using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FarmGame.Models; // Make sure to include your Models namespace

namespace FarmGame.Services
{
    public class DatabaseService
    {
        private SQLiteAsyncConnection _database;

        public DatabaseService()
        {
            // The database file will be stored in the app's local data folder.
            // This path is platform-specific and correctly resolved by FileSystem.AppDataDirectory.
            string dbPath = Path.Combine(FileSystem.AppDataDirectory, "FarmGame.db3");
            _database = new SQLiteAsyncConnection(dbPath);
        }

        public async Task InitializeAsync()
        {
            // Create all tables if they don't exist
            await _database.CreateTableAsync<PlayerState>();
            await _database.CreateTableAsync<Plot>();
            await _database.CreateTableAsync<SeedDefinition>();
            await _database.CreateTableAsync<ProduceDefinition>();
            await _database.CreateTableAsync<InventoryItem>();
            await _database.CreateTableAsync<ToolDefinition>();
            await _database.CreateTableAsync<PlayerOwnedTool>();
            await _database.CreateTableAsync<MachineDefinition>();
            await _database.CreateTableAsync<PlayerOwnedMachine>();

            // --- Initial Data Seeding (for definitions and initial player state) ---
            await SeedInitialData();
        }

        private async Task SeedInitialData()
        {
            // Seed Produce Definitions (if not already present)
            if (await _database.Table<ProduceDefinition>().CountAsync() == 0)
            {
                await _database.InsertAllAsync(new[]
                {
                    new ProduceDefinition { Id = 1, Name = "Carrot", BaseSellPrice = 5.0, CanBeProcessed = true },
                    new ProduceDefinition { Id = 2, Name = "Tomato", BaseSellPrice = 10.0, CanBeProcessed = true },
                    new ProduceDefinition { Id = 3, Name = "Wheat", BaseSellPrice = 3.0, CanBeProcessed = true },
                    new ProduceDefinition { Id = 4, Name = "Blueberry", BaseSellPrice = 15.0, CanBeProcessed = true },
                    new ProduceDefinition { Id = 5, Name = "Flour", BaseSellPrice = 6.0, CanBeProcessed = true }, // Wheat -> Flour
                    new ProduceDefinition { Id = 6, Name = "Peeled Carrot", BaseSellPrice = 7.0, CanBeProcessed = false }, // Carrot -> Peeled Carrot
                    new ProduceDefinition { Id = 7, Name = "Peeled Tomato", BaseSellPrice = 12.0, CanBeProcessed = false }, // Tomato -> Peeled Tomato
                    new ProduceDefinition { Id = 8, Name = "Blueberry Mix", BaseSellPrice = 20.0, CanBeProcessed = true }, // Flour + Blueberry -> Blueberry Mix
                    new ProduceDefinition { Id = 9, Name = "Blueberry Pie", BaseSellPrice = 35.0, CanBeProcessed = false } // Blueberry Mix -> Blueberry Pie
                });
            }

            // Seed Seed Definitions (if not already present)
            if (await _database.Table<SeedDefinition>().CountAsync() == 0)
            {
                await _database.InsertAllAsync(new[]
                {
                    new SeedDefinition { Id = 101, Name = "Carrot Seeds", ShopCost = 2.0, GrowTimeSeconds = 5, HarvestsProduceDefinitionId = 1 }, // Yields Carrot (ProduceDefId 1)
                    new SeedDefinition { Id = 102, Name = "Tomato Seeds", ShopCost = 5.0, GrowTimeSeconds = 10, HarvestsProduceDefinitionId = 2 }, // Yields Tomato (ProduceDefId 2)
                    new SeedDefinition { Id = 103, Name = "Wheat Seeds", ShopCost = 3.0, GrowTimeSeconds = 7, HarvestsProduceDefinitionId = 3 }, // Yields Wheat (ProduceDefId 3)
                    new SeedDefinition { Id = 104, Name = "Blueberry Seeds", ShopCost = 8.0, GrowTimeSeconds = 15, HarvestsProduceDefinitionId = 4 } // Yields Blueberry (ProduceDefId 4)
                });
            }

            // Seed Tool Definitions (if not already present)
            if (await _database.Table<ToolDefinition>().CountAsync() == 0)
            {
                await _database.InsertAllAsync(new[]
                {
                    new ToolDefinition { Id = 201, Name = "Rusty Hoe", Type = "Hoe", Range = 1, ShopCost = 0.0 }, // Starting tool
                    new ToolDefinition { Id = 202, Name = "Normal Hoe", Type = "Hoe", Range = 3, ShopCost = 50.0 },
                    new ToolDefinition { Id = 203, Name = "Advanced Hoe", Type = "Hoe", Range = 5, ShopCost = 200.0 },
                    new ToolDefinition { Id = 204, Name = "Super Hoe", Type = "Hoe", Range = 10, ShopCost = 800.0 },

                    new ToolDefinition { Id = 205, Name = "Rusty Watering Can", Type = "WateringCan", Range = 1, ShopCost = 0.0, MaxWaterCapacity = 50, WaterRefillRate = 1.0 }, // Starting tool
                    new ToolDefinition { Id = 206, Name = "Normal Watering Can", Type = "WateringCan", Range = 3, ShopCost = 75.0, MaxWaterCapacity = 100, WaterRefillRate = 2.0 },
                    new ToolDefinition { Id = 207, Name = "Advanced Watering Can", Type = "WateringCan", Range = 5, ShopCost = 300.0, MaxWaterCapacity = 250, WaterRefillRate = 5.0 },
                    new ToolDefinition { Id = 208, Name = "Super Watering Can", Type = "WateringCan", Range = 10, ShopCost = 1000.0, MaxWaterCapacity = 500, WaterRefillRate = 10.0 }
                });
            }

            // Seed Machine Definitions (if not already present)
            if (await _database.Table<MachineDefinition>().CountAsync() == 0)
            {
                await _database.InsertAllAsync(new[]
                {
                    new MachineDefinition { Id = 301, Name = "Mill", ShopCost = 150.0, ProcessingTimeSeconds = 10,
                                            InputProduceDefinitionId = 3, InputQuantity = 1, // Wheat
                                            OutputProduceDefinitionId = 5, OutputQuantity = 1 }, // Flour
                    new MachineDefinition { Id = 302, Name = "Veggie Peeler", ShopCost = 100.0, ProcessingTimeSeconds = 5,
                                            InputProduceDefinitionId = 1, InputQuantity = 1, // Carrot (can be extended for tomato)
                                            OutputProduceDefinitionId = 6, OutputQuantity = 1 }, // Peeled Carrot
                    new MachineDefinition { Id = 303, Name = "Mixer", ShopCost = 300.0, ProcessingTimeSeconds = 15,
                                            InputProduceDefinitionId = 5, InputQuantity = 1, // Flour
                                            OutputProduceDefinitionId = 8, OutputQuantity = 1 }, // Blueberry Mix (needs a second ingredient logic, for this simple case, just primary input)
                    new MachineDefinition { Id = 304, Name = "Oven", ShopCost = 500.0, ProcessingTimeSeconds = 20,
                                            InputProduceDefinitionId = 8, InputQuantity = 1, // Blueberry Mix
                                            OutputProduceDefinitionId = 9, OutputQuantity = 1 } // Blueberry Pie
                });
            }


            // Initialize PlayerState (if not already present)
            if (await _database.Table<PlayerState>().CountAsync() == 0)
            {
                // Get starting tools from definitions
                var rustyHoe = await _database.GetAsync<ToolDefinition>(201); // Assuming Id 201 for Rusty Hoe
                var rustyWateringCan = await _database.GetAsync<ToolDefinition>(205); // Assuming Id 205 for Rusty Watering Can

                await _database.InsertAsync(new PlayerState
                {
                    Id = 1, // Ensure it's the singleton ID
                    Money = 100.0, // Starting money
                    CurrentWater = rustyWateringCan?.MaxWaterCapacity ?? 0,
                    MaxWater = rustyWateringCan?.MaxWaterCapacity ?? 0,
                    WaterRefillRate = rustyWateringCan?.WaterRefillRate ?? 0,
                    SelectedHoeToolId = rustyHoe?.Id,
                    SelectedWaterToolId = rustyWateringCan?.Id,
                    LastSaveTime = DateTime.UtcNow
                });

                // Give player starting tools
                await _database.InsertAsync(new PlayerOwnedTool { ToolDefinitionId = rustyHoe.Id });
                await _database.InsertAsync(new PlayerOwnedTool { ToolDefinitionId = rustyWateringCan.Id });

                // Give player initial plot
                await _database.InsertAsync(new Plot { PlotNumber = 0, IsTilled = false, GrowthProgress = 0, IsWatered = false });
            }
        }


        // --- Generic CRUD Operations (you'll add more specific ones later) ---

        public Task<List<T>> GetItemsAsync<T>() where T : new()
        {
            return _database.Table<T>().ToListAsync();
        }

        public Task<T> GetItemAsync<T>(int id) where T : class, new()
        {
            return _database.GetAsync<T>(id);
        }

        public Task<int> SaveItemAsync<T>(T item) where T : class, new()
        {
            // Check if item has a PrimaryKey Id property and if it's set (for updates)
            var props = typeof(T).GetProperties();
            var pkProp = Array.Find(props, p => p.GetCustomAttributes(typeof(PrimaryKeyAttribute), true).Length > 0);

            if (pkProp != null)
            {
                var pkValue = (int)pkProp.GetValue(item);
                if (pkValue != 0) // If PK is set, it's an update
                {
                    return _database.UpdateAsync(item);
                }
            }
            // Otherwise, it's an insert
            return _database.InsertAsync(item);
        }

        public Task<int> DeleteItemAsync<T>(T item) where T : class, new()
        {
            return _database.DeleteAsync(item);
        }
    }
}