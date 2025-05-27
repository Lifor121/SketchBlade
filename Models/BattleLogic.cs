using System;
using System.Collections.Generic;
using System.Linq;
using SketchBlade.Models;
using SketchBlade.Services;

namespace SketchBlade.Models
{
    public class BattleLogic
    {
        private readonly Random _random = new();
        private readonly GameData _gameState;

        public BattleLogic(GameData GameData)
        {
            _gameState = GameData;
        }

        public int CalculateDamage(Character attacker, Character defender)
        {
            int baseDamage = attacker.GetTotalAttack() - (defender.GetTotalDefense() / 2);
            
            double variation = 0.8 + (_random.NextDouble() * 0.4);
            int finalDamage = Math.Max(1, (int)(baseDamage * variation));
            
            return finalDamage;
        }

        public bool IsCriticalHit()
        {
            return _random.NextDouble() < 0.1;
        }

        public bool ShouldEnemyUseSpecialAbility(Character enemy)
        {
            double baseChance = enemy.IsHero ? 0.3 : 0.2;
            
            if (enemy.CurrentHealth < enemy.MaxHealth / 2)
            {
                baseChance += 0.2;
            }
            
            return _random.NextDouble() < baseChance;
        }

        public void ApplyDamage(Character target, int damage)
        {
            target.TakeDamage(damage);
        }

        public bool IsCharacterDefeated(Character character)
        {
            return character.CurrentHealth <= 0;
        }

        public void UseHealingPotion(Character character, int healAmount)
        {
            character.Heal(healAmount);
        }

        public void ApplyRagePotion(Character character, int attackBonus, int duration)
        {
            character.SetTemporaryAttackBonus(attackBonus, duration);
        }

        public void ApplyDefensePotion(Character character, int defenseBonus, int duration)
        {
            character.SetTemporaryDefenseBonus(defenseBonus, duration);
        }

        public int CalculateBombDamage()
        {
            return _random.Next(15, 26);
        }

        public List<Item> GenerateBattleRewards(GameData gameData, bool isHeroDefeated = false)
        {
            var rewardItems = new List<Item>();
            
            LoggingService.LogInfo($"GenerateBattleRewards: Начинаем генерацию наград, isHeroDefeated: {isHeroDefeated}");
            
            if (gameData.CurrentLocation == null) 
            {
                LoggingService.LogWarning("GenerateBattleRewards: CurrentLocation is null");
                return rewardItems;
            }

            var location = gameData.CurrentLocation;
            LoggingService.LogInfo($"GenerateBattleRewards: Локация: {location.Name}");
            
            int baseItemCount = isHeroDefeated ? _random.Next(3, 6) : _random.Next(1, 4);
            int bonusItemCount = _random.Next(0, 3);
            int totalItems = baseItemCount + bonusItemCount;
            
            LoggingService.LogInfo($"GenerateBattleRewards: Планируем создать {totalItems} предметов (base: {baseItemCount}, bonus: {bonusItemCount})");

            if (location.LootTable == null || location.LootTable.Count == 0)
            {
                LoggingService.LogWarning($"GenerateBattleRewards: LootTable пуста для локации {location.Name}");
                rewardItems = CreateFallbackRewards(totalItems, location.LocationType);
            }
            else
            {
                LoggingService.LogInfo($"GenerateBattleRewards: LootTable содержит {location.LootTable.Count} материалов: [{string.Join(", ", location.LootTable)}]");
                
                for (int i = 0; i < totalItems; i++)
                {
                    string materialName = GetRandomMaterialWithWeights(location.LootTable, location.LocationType);
                    int quantity = GetMaterialQuantity(materialName, isHeroDefeated);
                    
                    LoggingService.LogInfo($"GenerateBattleRewards: Попытка создать {materialName} x{quantity}");
                    
                    var item = CreateItemByName(materialName, quantity);
                    if (item != null)
                    {
                        rewardItems.Add(item);
                        LoggingService.LogInfo($"GenerateBattleRewards: Создан предмет: {item.Name} x{item.StackSize}");
                    }
                    else
                    {
                        LoggingService.LogWarning($"GenerateBattleRewards: Не удалось создать предмет {materialName}");
                    }
                }
            }

            LoggingService.LogInfo($"GenerateBattleRewards: Итого создано {rewardItems.Count} предметов");
            
            gameData.BattleRewardItems = rewardItems;
            gameData.BattleRewardGold = CalculateGoldReward(isHeroDefeated);
            
            LoggingService.LogInfo($"GenerateBattleRewards: Установлено золота: {gameData.BattleRewardGold}");
            
            return rewardItems;
        }

        private string GetRandomMaterialWithWeights(List<string> lootTable, LocationType locationType)
        {
            var weights = new Dictionary<string, int>();
            
            foreach (var material in lootTable)
            {
                weights[material] = GetMaterialWeight(material, locationType);
            }
            
            int totalWeight = weights.Values.Sum();
            int randomValue = _random.Next(totalWeight);
            int currentWeight = 0;
            
            foreach (var kvp in weights)
            {
                currentWeight += kvp.Value;
                if (randomValue < currentWeight)
                {
                    return kvp.Key;
                }
            }
            
            return lootTable[_random.Next(lootTable.Count)];
        }
        
        private int GetMaterialWeight(string material, LocationType locationType)
        {
            return material switch
            {
                "Wood" => locationType == LocationType.Village ? 40 : 20,
                "Herbs" => locationType == LocationType.Village ? 35 : 25,
                "Cloth" => locationType == LocationType.Village ? 15 : 5,
                "Water Flask" => locationType == LocationType.Village ? 10 : 5,
                
                "Feathers" => locationType == LocationType.Forest ? 25 : 10,
                "Iron Ore" => locationType == LocationType.Forest ? 20 : 15,
                "Crystal Dust" => locationType == LocationType.Forest ? 15 : 10,
                
                "Iron Ingot" => locationType == LocationType.Cave ? 30 : 15,
                "Gunpowder" => locationType == LocationType.Cave ? 20 : 10,
                "Gold Ore" => locationType == LocationType.Cave ? 25 : 15,
                
                "Gold Ingot" => locationType == LocationType.Ruins ? 30 : (locationType == LocationType.Castle ? 25 : 10),
                "Poison Extract" => locationType == LocationType.Ruins ? 20 : 5,
                "Luminite Fragment" => locationType == LocationType.Ruins ? 15 : (locationType == LocationType.Castle ? 25 : 5),
                "Luminite" => locationType == LocationType.Castle ? 20 : 5,
                
                _ => 15
            };
        }

        private List<Item> CreateFallbackRewards(int count, LocationType locationType)
        {
            var items = new List<Item>();
            LoggingService.LogInfo($"CreateFallbackRewards: Создаем {count} fallback предметов для {locationType}");
            
            var possibleItems = GetFallbackItemsForLocation(locationType);
            
            for (int i = 0; i < count; i++)
            {
                var randomItem = possibleItems[_random.Next(possibleItems.Count)];
                Item? item = randomItem();
                
                if (item != null)
                {
                    if (item.IsStackable)
                    {
                        item.StackSize = _random.Next(1, Math.Min(item.MaxStackSize, 5) + 1);
                    }
                    
                    items.Add(item);
                    LoggingService.LogInfo($"CreateFallbackRewards: Создан fallback предмет: {item.Name} x{item.StackSize}");
                }
            }
            
            return items;
        }
        
        private List<Func<Item?>> GetFallbackItemsForLocation(LocationType locationType)
        {
            return locationType switch
            {
                LocationType.Village => new List<Func<Item?>>
                {
                    () => ItemFactory.CreateWood(),
                    () => ItemFactory.CreateHerb(),
                    () => ItemFactory.CreateCloth(),
                    () => ItemFactory.CreateFlask(),
                    () => ItemFactory.CreateStick()
                },
                LocationType.Forest => new List<Func<Item?>>
                {
                    () => ItemFactory.CreateWood(),
                    () => ItemFactory.CreateHerb(),
                    () => ItemFactory.CreateIronOre(),
                    () => ItemFactory.CreateFeather(),
                    () => ItemFactory.CreateCrystalDust()
                },
                LocationType.Cave => new List<Func<Item?>>
                {
                    () => ItemFactory.CreateIronOre(),
                    () => ItemFactory.CreateIronIngot(),
                    () => ItemFactory.CreateGoldOre(),
                    () => ItemFactory.CreateGunpowder(),
                    () => ItemFactory.CreateCrystalDust()
                },
                LocationType.Ruins => new List<Func<Item?>>
                {
                    () => ItemFactory.CreateGoldOre(),
                    () => ItemFactory.CreateGoldIngot(),
                    () => ItemFactory.CreatePoisonExtract(),
                    () => ItemFactory.CreateLuminiteFragment(),
                    () => ItemFactory.CreateCrystalDust()
                },
                LocationType.Castle => new List<Func<Item?>>
                {
                    () => ItemFactory.CreateGoldIngot(),
                    () => ItemFactory.CreateLuminiteFragment(),
                    () => ItemFactory.CreateLuminite(),
                    () => ItemFactory.CreatePoisonExtract(),
                    () => ItemFactory.CreateCrystalDust()
                },
                _ => new List<Func<Item?>>
                {
                    () => ItemFactory.CreateWood(),
                    () => ItemFactory.CreateHerb()
                }
            };
        }

        private Item? CreateItemByName(string name, int quantity)
        {
            return name switch
            {
                "Wood" => ItemFactory.CreateWood(quantity),
                "Herbs" => ItemFactory.CreateHerb(quantity),
                "Cloth" => ItemFactory.CreateCloth(quantity),
                "Water Flask" => ItemFactory.CreateFlask(quantity),
                "Iron Ore" => ItemFactory.CreateIronOre(quantity),
                "Crystal Dust" => ItemFactory.CreateCrystalDust(quantity),
                "Feathers" => ItemFactory.CreateFeather(quantity),
                "Iron Ingot" => ItemFactory.CreateIronIngot(quantity),
                "Gunpowder" => ItemFactory.CreateGunpowder(quantity),
                "Gold Ore" => ItemFactory.CreateGoldOre(quantity),
                "Gold Ingot" => ItemFactory.CreateGoldIngot(quantity),
                "Poison Extract" => ItemFactory.CreatePoisonExtract(quantity),
                "Luminite Fragment" => ItemFactory.CreateLuminiteFragment(quantity),
                "Luminite" => ItemFactory.CreateLuminite(quantity),
                _ => null
            };
        }

        public void RemoveDefeatedEnemies(BattleState battleState)
        {
            var toRemove = battleState.Enemies.Where(e => e.IsDefeated).ToList();
            foreach (var enemy in toRemove)
            {
                battleState.Enemies.Remove(enemy);
            }
        }

        public bool IsAllEnemiesDefeated(BattleState battleState)
        {
            return battleState.Enemies.All(e => e.IsDefeated);
        }

        public Character? GetNextActiveEnemy(BattleState battleState)
        {
            var aliveEnemies = battleState.Enemies.Where(e => !e.IsDefeated).ToList();
            if (aliveEnemies.Count == 0) return null;

            if (battleState.CurrentEnemyIndex >= aliveEnemies.Count)
            {
                battleState.CurrentEnemyIndex = 0;
            }

            var nextEnemy = aliveEnemies[battleState.CurrentEnemyIndex];
            
            battleState.CurrentEnemyIndex = (battleState.CurrentEnemyIndex + 1) % aliveEnemies.Count;
            
            return nextEnemy;
        }

        public string GetEnemySpecialAbilityName(Character enemy)
        {
            if (enemy.IsHero)
            {
                return enemy.LocationType.ToString().ToLower() switch
                {
                    "village" => "Крестьянская ярость",
                    "forest" => "Лесная засада", 
                    "cave" => "Каменный удар",
                    "ruins" => "Древнее проклятие",
                    "castle" => "Королевский гнев",
                    _ => "Особая атака"
                };
            }
            else
            {
                return "Дикая атака";
            }
        }

        public int CalculateSpecialAbilityDamage(Character attacker, Character defender)
        {
            int baseDamage = CalculateDamage(attacker, defender);
            double multiplier = 1.2 + (_random.NextDouble() * 0.4);
            return (int)(baseDamage * multiplier);
        }

        private int GetMaterialQuantity(string materialName, bool isHero)
        {
            // Улучшенная система количества с большим разнообразием
            int baseQuantity = materialName switch
            {
                // Редкие материалы - малое количество
                "Luminite" => _random.Next(1, 3),
                "Luminite Fragment" => _random.Next(1, 4),
                "Poison Extract" => _random.Next(1, 4),
                
                // Ценные материалы - среднее количество
                "Gold Ingot" => _random.Next(1, 5),
                "Gold Ore" => _random.Next(2, 7),
                "Iron Ingot" => _random.Next(2, 6),
                "Crystal Dust" => _random.Next(2, 6),
                "Gunpowder" => _random.Next(1, 5),
                "Feathers" => _random.Next(2, 6),
                
                // Обычные материалы - большее количество
                "Iron Ore" => _random.Next(3, 9),
                "Wood" => _random.Next(3, 10),
                "Herbs" => _random.Next(2, 8),
                "Cloth" => _random.Next(1, 5),
                "Water Flask" => _random.Next(1, 4),
                
                _ => _random.Next(2, 7)
            };

            // Бонус за победу над героем
            if (isHero)
            {
                baseQuantity = (int)(baseQuantity * _random.NextDouble() * (1.5 - 1.2) + 1.2); // 1.2x - 1.5x множитель
                baseQuantity = Math.Max(1, baseQuantity); // Минимум 1
            }

            return baseQuantity;
        }

        private int CalculateGoldReward(bool isHero)
        {
            // Улучшенная система золотых наград с большим разнообразием
            int baseGold = _random.Next(8, 35);
            
            if (isHero)
            {
                // Герои дают больше золота с некоторой случайностью
                double multiplier = 1.8 + (_random.NextDouble() * 0.6); // 1.8x - 2.4x множитель
                baseGold = (int)(baseGold * multiplier);
                baseGold += _random.Next(10, 25); // Дополнительный бонус
            }
            
            return baseGold;
        }
    }
} 
