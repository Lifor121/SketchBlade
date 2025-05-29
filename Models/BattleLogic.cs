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
            
            // Уменьшены количества предметов для более сбалансированного лута
            int baseItemCount = isHeroDefeated ? _random.Next(2, 4) : _random.Next(1, 3);
            int bonusItemCount = _random.Next(0, 2);
            int totalItems = baseItemCount + bonusItemCount;
            
            LoggingService.LogInfo($"GenerateBattleRewards: Планируем создать {totalItems} предметов (base: {baseItemCount}, bonus: {bonusItemCount})");

            // Словарь для группировки предметов по названию
            var itemQuantities = new Dictionary<string, int>();

            if (location.LootTable == null || location.LootTable.Count == 0)
            {
                LoggingService.LogWarning($"GenerateBattleRewards: LootTable пуста для локации {location.Name}");
                rewardItems = CreateFallbackRewards(totalItems, location.LocationType);
            }
            else
            {
                LoggingService.LogInfo($"GenerateBattleRewards: LootTable содержит {location.LootTable.Count} материалов: [{string.Join(", ", location.LootTable)}]");
                
                // Собираем все предметы и их количества
                for (int i = 0; i < totalItems; i++)
                {
                    string materialName = GetRandomMaterialWithWeights(location.LootTable, location.LocationType);
                    int quantity = GetMaterialQuantity(materialName, isHeroDefeated);
                    
                    LoggingService.LogInfo($"GenerateBattleRewards: Попытка создать {materialName} x{quantity}");
                    
                    // Добавляем количество к уже существующему или создаем новую запись
                    if (itemQuantities.ContainsKey(materialName))
                    {
                        itemQuantities[materialName] += quantity;
                        LoggingService.LogInfo($"GenerateBattleRewards: Добавлено к существующему стеку {materialName}, общее количество: {itemQuantities[materialName]}");
                    }
                    else
                    {
                        itemQuantities[materialName] = quantity;
                        LoggingService.LogInfo($"GenerateBattleRewards: Создан новый стек {materialName} x{quantity}");
                    }
                }

                // Создаем предметы на основе сгруппированных количеств
                foreach (var kvp in itemQuantities)
                {
                    var item = CreateItemByName(kvp.Key, kvp.Value);
                    if (item != null)
                    {
                        rewardItems.Add(item);
                        LoggingService.LogInfo($"GenerateBattleRewards: Создан предмет: {item.Name} x{item.StackSize}");
                    }
                    else
                    {
                        LoggingService.LogWarning($"GenerateBattleRewards: Не удалось создать предмет {kvp.Key}");
                    }
                }
            }

            LoggingService.LogInfo($"GenerateBattleRewards: Итого создано {rewardItems.Count} различных типов предметов");
            
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
                "Wood" => locationType == LocationType.Village ? 30 : 20,
                "Herbs" => locationType == LocationType.Village ? 30 : 25,
                "Cloth" => locationType == LocationType.Village ? 20 : 5,
                "Water Flask" => locationType == LocationType.Village ? 20 : 5,
                
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
            
            // Словарь для группировки предметов по названию
            var itemQuantities = new Dictionary<string, int>();
            
            for (int i = 0; i < count; i++)
            {
                var randomItem = possibleItems[_random.Next(possibleItems.Count)];
                Item? item = randomItem();
                
                if (item != null)
                {
                    int quantity = item.IsStackable ? _random.Next(1, Math.Min(item.MaxStackSize, 5) + 1) : 1;
                    
                    // Добавляем количество к уже существующему или создаем новую запись
                    if (itemQuantities.ContainsKey(item.Name))
                    {
                        itemQuantities[item.Name] += quantity;
                        LoggingService.LogInfo($"CreateFallbackRewards: Добавлено к существующему стеку {item.Name}, общее количество: {itemQuantities[item.Name]}");
                    }
                    else
                    {
                        itemQuantities[item.Name] = quantity;
                        LoggingService.LogInfo($"CreateFallbackRewards: Создан новый стек {item.Name} x{quantity}");
                    }
                }
            }
            
            // Создаем предметы на основе сгруппированных количеств
            foreach (var kvp in itemQuantities)
            {
                var item = CreateItemByName(kvp.Key, kvp.Value);
                if (item != null)
                {
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
            // Уменьшенная система количества (уменьшено на четверть от предыдущих значений)
            int baseQuantity = materialName switch
            {
                // Редкие материалы - малое количество
                "Luminite" => _random.Next(1, 2),
                "Luminite Fragment" => _random.Next(1, 3),
                "Poison Extract" => _random.Next(1, 3),
                
                // Ценные материалы - среднее количество
                "Gold Ingot" => _random.Next(1, 4),
                "Gold Ore" => _random.Next(1, 5),
                "Iron Ingot" => _random.Next(1, 4),
                "Crystal Dust" => _random.Next(1, 4),
                "Gunpowder" => _random.Next(1, 4),
                "Feathers" => _random.Next(1, 4),
                
                // Обычные материалы - уменьшенное количество
                "Iron Ore" => _random.Next(2, 7),
                "Wood" => _random.Next(2, 7),
                "Herbs" => _random.Next(1, 6),
                "Cloth" => _random.Next(1, 4),
                "Water Flask" => _random.Next(1, 3),
                
                _ => _random.Next(1, 5)
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

        /// <summary>
        /// Рассчитывает опыт, который игрок получает за победу в битве
        /// </summary>
        /// <param name="enemyCount">Количество побежденных врагов</param>
        /// <param name="isHero">Была ли битва с героем</param>
        /// <returns>Количество опыта</returns>
        public int CalculateXPReward(int enemyCount, bool isHero = false)
        {
            int baseXP = enemyCount * _random.Next(8, 15); // Базовый опыт за каждого врага
            
            if (isHero)
            {
                // Герои дают больше опыта
                baseXP *= 3;
                baseXP += _random.Next(20, 40); // Дополнительный бонус за героя
            }
            
            return Math.Max(1, baseXP);
        }

        /// <summary>
        /// Применяет награды к игроку (опыт, золото, предметы)
        /// </summary>
        /// <param name="gameData">Данные игры</param>
        /// <param name="enemyCount">Количество побежденных врагов</param>
        /// <param name="isHeroDefeated">Был ли побежден герой</param>
        public void ApplyBattleRewards(GameData gameData, int enemyCount, bool isHeroDefeated = false)
        {
            if (gameData.Player == null) return;

            // Начисляем опыт
            int xpReward = CalculateXPReward(enemyCount, isHeroDefeated);
            gameData.Player.XP += xpReward;
            LoggingService.LogInfo($"Player gained {xpReward} XP. Total XP: {gameData.Player.XP}");

            // Проверяем повышение уровня
            CheckLevelUp(gameData.Player);

            // Поднимаем здоровье до 20, если у игрока меньше
            if (gameData.Player.CurrentHealth < 20)
            {
                gameData.Player.CurrentHealth = 20;
                LoggingService.LogInfo($"Player health restored to 20 after victory");
            }

            // Начисляем золото
            int goldReward = gameData.BattleRewardGold;
            gameData.Gold += goldReward;
            LoggingService.LogInfo($"Player gained {goldReward} gold. Total gold: {gameData.Gold}");

            // Добавляем предметы в инвентарь
            if (gameData.BattleRewardItems != null && gameData.BattleRewardItems.Count > 0)
            {
                foreach (var item in gameData.BattleRewardItems)
                {
                    // Используем StackSize предмета как количество для добавления в инвентарь
                    gameData.Inventory.AddItem(item, item.StackSize);
                    LoggingService.LogInfo($"Added reward item to inventory: {item.Name} x{item.StackSize}");
                }
                
                // Очищаем награды после обработки
                gameData.BattleRewardItems.Clear();
            }
        }

        /// <summary>
        /// Проверяет и обрабатывает повышение уровня игрока
        /// </summary>
        /// <param name="player">Игрок</param>
        private void CheckLevelUp(Character player)
        {
            while (player.XP >= player.XPToNextLevel)
            {
                player.XP -= player.XPToNextLevel;
                player.Level++;
                
                // Увеличиваем характеристики при повышении уровня
                player.MaxHealth += 10;
                player.CurrentHealth = player.MaxHealth; // Полностью восстанавливаем здоровье
                player.Attack += 2;
                player.Defense += 1;
                
                // Рассчитываем опыт для следующего уровня
                player.XPToNextLevel = CalculateXPToNextLevel(player.Level);
                
                LoggingService.LogInfo($"LEVEL UP! Player is now level {player.Level}. New stats: HP:{player.MaxHealth}, ATK:{player.Attack}, DEF:{player.Defense}");
            }
        }

        /// <summary>
        /// Рассчитывает количество опыта, необходимое для следующего уровня
        /// </summary>
        /// <param name="currentLevel">Текущий уровень</param>
        /// <returns>Опыт для следующего уровня</returns>
        private int CalculateXPToNextLevel(int currentLevel)
        {
            return 100 + (currentLevel - 1) * 25; // Прогрессивное увеличение
        }
    }
} 
