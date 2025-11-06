using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FarmGame.Models;
using System.Linq; // Keep this for other LINQ uses

namespace FarmGame.Services
{
    public class DatabaseService
    {
        private SQLiteAsyncConnection _database;

        public DatabaseService()
        {
            string dbPath = Path.Combine(FileSystem.AppDataDirectory, "FarmGame.db3");
            _database = new SQLiteAsyncConnection(dbPath);
        }

        public async Task InitializeAsync()
        {
            await _database.CreateTableAsync<PlayerState>();
            await _database.CreateTableAsync<Plot>();
            await _database.CreateTableAsync<SeedDefinition>();
            await _database.CreateTableAsync<ProduceDefinition>();
            await _database.CreateTableAsync<InventoryItem>();
            await _database.CreateTableAsync<ToolDefinition>();
            await _database.CreateTableAsync<PlayerOwnedTool>();
            await _database.CreateTableAsync<MachineDefinition>();
            await _database.CreateTableAsync<PlayerOwnedMachine>();

            await SeedInitialData();
        }

        private async Task SeedInitialData()
        {
            if (await _database.Table<ProduceDefinition>().CountAsync() == 0)
            {
                await _database.InsertAllAsync(new[]
                {
                    new ProduceDefinition { Id = 1, Name = "Carrot", BaseSellPrice = 5.0, CanBeProcessed = true },
                    new ProduceDefinition { Id = 2, Name = "Tomato", BaseSellPrice = 10.0, CanBeProcessed = true },
                    new ProduceDefinition { Id = 3, Name = "Wheat", BaseSellPrice = 3.0, CanBeProcessed = true },
                    new ProduceDefinition { Id = 4, Name = "Blueberry", BaseSellPrice = 15.0, CanBeProcessed = true },
                    new ProduceDefinition { Id = 5, Name = "Flour", BaseSellPrice = 6.0, CanBeProcessed = true },
                    new ProduceDefinition { Id = 6, Name = "Peeled Carrot", BaseSellPrice = 7.0, CanBeProcessed = false },
                    new ProduceDefinition { Id = 7, Name = "Peeled Tomato", BaseSellPrice = 12.0, CanBeProcessed = false },
                    new ProduceDefinition { Id = 8, Name = "Blueberry Mix", BaseSellPrice = 20.0, CanBeProcessed = true },
                    new ProduceDefinition { Id = 9, Name = "Blueberry Pie", BaseSellPrice = 35.0, CanBeProcessed = false }
                });
            }

            // Seed Seed Definitions (if not already present)
            if (await _database.Table<SeedDefinition>().CountAsync() == 0)
            {
                await _database.InsertAllAsync(new[]
                {
                    new SeedDefinition { Id = 101, Name = "Carrot Seeds", ShopCost = 2.0, GrowTimeSeconds = 5, HarvestsProduceDefinitionId = 1 },
                    new SeedDefinition { Id = 102, Name = "Tomato Seeds", ShopCost = 5.0, GrowTimeSeconds = 10, HarvestsProduceDefinitionId = 2 },
                    new SeedDefinition { Id = 103, Name = "Wheat Seeds", ShopCost = 3.0, GrowTimeSeconds = 7, HarvestsProduceDefinitionId = 3 },
                    new SeedDefinition { Id = 104, Name = "Blueberry Seeds", ShopCost = 8.0, GrowTimeSeconds = 15, HarvestsProduceDefinitionId = 4 }
                });
            }

            // Seed Tool Definitions (if not already present)
            if (await _database.Table<ToolDefinition>().CountAsync() == 0)
            {
                await _database.InsertAllAsync(new[]
                {
                    new ToolDefinition { Id = 201, Name = "Rusty Hoe", Type = "Hoe", Range = 1, ShopCost = 0.0 },
                    new ToolDefinition { Id = 202, Name = "Normal Hoe", Type = "Hoe", Range = 3, ShopCost = 50.0 },
                    new ToolDefinition { Id = 203, Name = "Advanced Hoe", Type = "Hoe", Range = 5, ShopCost = 200.0 },
                    new ToolDefinition { Id = 204, Name = "Super Hoe", Type = "Hoe", Range = 10, ShopCost = 800.0 },

                    new ToolDefinition { Id = 205, Name = "Rusty Watering Can", Type = "WateringCan", Range = 1, ShopCost = 0.0, MaxWaterCapacity = 50, WaterRefillRate = 1.0 },
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
                                            InputProduceDefinitionId = 3, InputQuantity = 1,
                                            OutputProduceDefinitionId = 5, OutputQuantity = 1 },
                    new MachineDefinition { Id = 302, Name = "Veggie Peeler", ShopCost = 100.0, ProcessingTimeSeconds = 5,
                                            InputProduceDefinitionId = 1, InputQuantity = 1,
                                            OutputProduceDefinitionId = 6, OutputQuantity = 1 },
                    new MachineDefinition { Id = 303, Name = "Mixer", ShopCost = 300.0, ProcessingTimeSeconds = 15,
                                            InputProduceDefinitionId = 5, InputQuantity = 1,
                                            OutputProduceDefinitionId = 8, OutputQuantity = 1 },
                    new MachineDefinition { Id = 304, Name = "Oven", ShopCost = 500.0, ProcessingTimeSeconds = 20,
                                            InputProduceDefinitionId = 8, InputQuantity = 1,
                                            OutputProduceDefinitionId = 9, OutputQuantity = 1 }
                });
            }


            // Initialize PlayerState (if not already present)
            if (await _database.Table<PlayerState>().CountAsync() == 0)
            {
                var rustyHoe = await GetItemAsync<ToolDefinition>(201);
                var rustyWateringCan = await GetItemAsync<ToolDefinition>(205);

                await _database.InsertAsync(new PlayerState
                {
                    Id = 1,
                    Money = 1000000.0, //STARTING MONEY
                    CurrentWater = rustyWateringCan?.MaxWaterCapacity ?? 0,
                    MaxWater = rustyWateringCan?.MaxWaterCapacity ?? 0,
                    WaterRefillRate = rustyWateringCan?.WaterRefillRate ?? 0,
                    SelectedHoeToolId = rustyHoe?.Id,
                    SelectedWaterToolId = rustyWateringCan?.Id,
                    LastSaveTime = DateTime.UtcNow
                });

                if (rustyHoe != null)
                {
                    await _database.InsertAsync(new PlayerOwnedTool { ToolDefinitionId = rustyHoe.Id });
                }
                if (rustyWateringCan != null)
                {
                    await _database.InsertAsync(new PlayerOwnedTool { ToolDefinitionId = rustyWateringCan.Id });
                }

                await _database.InsertAsync(new Plot { PlotNumber = 0, IsTilled = false, GrowthProgress = 0, IsWatered = false });
            }
        }


        public Task<List<T>> GetItemsAsync<T>() where T : new()
        {
            return _database.Table<T>().ToListAsync();
        }

        public Task<T?> GetItemAsync<T>(int id) where T : class, new()
        {
            return _database.FindAsync<T>(id);
        }

        public Task<int> SaveItemAsync<T>(T item) where T : class, new()
        {
            var props = typeof(T).GetProperties();
            var pkProp = Array.Find(props, p => p.GetCustomAttributes(typeof(PrimaryKeyAttribute), true).Length > 0);

            if (pkProp != null)
            {
                var pkValue = (int)pkProp.GetValue(item)!;
                if (pkValue != 0)
                {
                    return _database.UpdateAsync(item);
                }
            }
            return _database.InsertAsync(item);
        }

        public Task<int> DeleteItemAsync<T>(T item) where T : class, new()
        {
            return _database.DeleteAsync(item);
        }
    }
}