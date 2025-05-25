using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using SketchBlade.Services;
using SketchBlade.Models;
using SketchBlade.Utilities;

namespace SketchBlade.Models
{
    [Serializable]
    public class BattleManager
    {
        private readonly Random _random = new Random();
        [NonSerialized]
        private readonly GameData _gameState;
        private bool _isVictory = false;
        private List<Character> _enemies = new List<Character>();
        private int _goldReward = 0;

        public int DamageValue { get; private set; }
        public bool IsCriticalHit { get; private set; }
        public int? GoldReward => _goldReward;
        public int? ExpReward => null;

        public BattleManager(GameData GameData)
        {
            _gameState = GameData;
        }

        public int CalculateDamage(Character attacker, Character defender)
        {
            int baseAttack = attacker.GetTotalAttack();
            int baseDefense = defender.GetTotalDefense();
            
            double randomFactor = 0.8 + (_random.NextDouble() * 0.4); // 0.8 - 1.2
            
            int damage = Math.Max(1, (int)((baseAttack - (baseDefense / 2)) * randomFactor));
            
            return damage;
        }

        public bool IsAttackCritical()
        {
            return _random.Next(10) == 0;
        }

        public int ApplyCriticalHit(int damage)
        {
            return (int)(damage * 1.5);
        }

        public Character SelectEnemyForAttack(IEnumerable<Character> enemies)
        {
            List<Character> enemyList = enemies.ToList();
            
            if (enemyList.Count == 1)
                return enemyList[0];
            
            foreach (var enemy in enemyList)
            {
                if (enemy.IsHero)
                    return enemy;
            }
            
            Character strongest = enemyList[0];
            foreach (var enemy in enemyList)
            {
                if (enemy.GetTotalAttack() > strongest.GetTotalAttack())
                    strongest = enemy;
            }
            
            if (_random.Next(10) == 0)
            {
                return enemyList[_random.Next(enemyList.Count)];
            }
            
            return strongest;
        }

        public bool ShouldUseSpecialAbility(Character enemy)
        {
            int threshold = enemy.IsHero ? 3 : 5;
            
            if (enemy.CurrentHealth < enemy.MaxHealth / 2)
            {
                threshold += 2;
            }
            
            return _random.Next(10) < threshold;
        }

        public (string abilityName, int damage, bool isAreaEffect) GetEnemySpecialAbility(Character enemy, Character target)
        {
            string[] abilities;
            int baseDamage = enemy.GetTotalAttack();
            bool isAreaEffect = false;
            
            if (enemy.IsHero)
            {
                abilities = new[] { "Мощный удар", "Свирепый рывок", "Сокрушающий замах", "Критический разрез", "Массовая атака" };
                isAreaEffect = _random.Next(5) == 0;
                
                if (isAreaEffect && _random.Next(2) == 0)
                {
                    return ("Массовая атака", (int)(baseDamage * 0.8), true);
                }
            }
            else
            {
                abilities = new[] { "Внезапный выпад", "Яростная атака", "Сильный удар", "Серия ударов", "Круговой удар" };
                isAreaEffect = _random.Next(10) == 0;
                
                if (isAreaEffect)
                {
                    return ("Круговой удар", (int)(baseDamage * 0.7), true);
                }
            }
            
            string abilityName = abilities[_random.Next(abilities.Length)];
            
            double damageMultiplier;
            
            if (isAreaEffect)
            {
                damageMultiplier = 0.6 + (_random.NextDouble() * 0.3);
            }
            else
            {
                damageMultiplier = 1.2 + (_random.NextDouble() * 0.4);
            }
            
            int damage = (int)(baseDamage * damageMultiplier);
            
            if (_random.Next(4) == 0)
            {
                damage = (int)(damage * 1.3);
                abilityName = "Критический" + abilityName.ToLower();
            }
            
            return (abilityName, damage, isAreaEffect);
        }

        public (int Gold, List<Item> Items) CalculateBattleRewards(bool isBossHeroBattle, int enemyCount)
        {
            int goldReward = CalculateGoldReward(isBossHeroBattle);
            
            List<Item> loot = new List<Item>();
            
            if (isBossHeroBattle)
            {
                loot = GenerateHeroLoot();
            }
            else
            {
                loot = GenerateNormalLoot(enemyCount);
            }
            
            return (goldReward, loot);
        }

        private List<Item> GenerateNormalLoot(int enemyCount)
        {
            List<Item> loot = new List<Item>();
            
            int itemCount = Math.Min(4, 2 + (_random.Next(enemyCount + 1)));
            
            for (int i = 0; i < itemCount; i++)
            {
                Item material = GenerateLocationBasedMaterial(_gameState.CurrentLocation?.Type ?? LocationType.Village);
                if (material != null)
                {
                    int stackSize = material.Rarity switch
                    {
                        ItemRarity.Common => _random.Next(1, 6),
                        ItemRarity.Uncommon => _random.Next(1, 4),
                        ItemRarity.Rare => _random.Next(1, 3),
                        ItemRarity.Epic => 1,
                        ItemRarity.Legendary => 1,
                        _ => 1
                    };
                    
                    material.StackSize = stackSize;
                    loot.Add(material);
                }
            }
            
            return loot;
        }

        private List<Item> GenerateHeroLoot()
        {
            List<Item> loot = new List<Item>();
            
            int itemCount = 4 + _random.Next(3);
            bool hasRareItem = false;
            
            for (int i = 0; i < itemCount; i++)
            {
                bool forceRare = (i == itemCount - 1 && !hasRareItem);
                
                Item material = GenerateLocationBasedMaterial(_gameState.CurrentLocation?.Type ?? LocationType.Village, forceRare);
                if (material != null)
                {
                    if (material.Rarity >= ItemRarity.Rare)
                    {
                        hasRareItem = true;
                    }
                    
                    int stackSize = material.Rarity switch
                    {
                        ItemRarity.Common => _random.Next(2, 8),
                        ItemRarity.Uncommon => _random.Next(2, 6),
                        ItemRarity.Rare => _random.Next(1, 4),
                        ItemRarity.Epic => _random.Next(1, 3),
                        ItemRarity.Legendary => 1,
                        _ => 1
                    };
                    
                    material.StackSize = stackSize;
                    loot.Add(material);
                }
            }
            
            return loot;
        }

        private Item GenerateLocationBasedMaterial(LocationType locationType, bool forceRare = false)
        {
            ItemRarity rarity = CalculateItemRarity(locationType, forceRare);
            
            List<Item> possibleMaterials = GetLocationMaterials(locationType, rarity);
            
            if (possibleMaterials.Count > 0)
            {
                return possibleMaterials[_random.Next(possibleMaterials.Count)].Clone();
            }
            
            return ItemFactory.CreateWood();
        }

        private List<Item> GetLocationMaterials(LocationType locationType, ItemRarity rarity)
        {
            List<Item> materials = new List<Item>();
            
            if (rarity == ItemRarity.Common)
            {
                materials.Add(ItemFactory.CreateWood());
                materials.Add(ItemFactory.CreateCloth());
                materials.Add(ItemFactory.CreateFlask());
            }
            
            switch (locationType)
            {
                case LocationType.Village:
                    if (rarity == ItemRarity.Common)
                    {
                        materials.Add(ItemFactory.CreateHerb());
                    }
                    else if (rarity == ItemRarity.Uncommon)
                    {
                        materials.Add(ItemFactory.CreateFeather());
                    }
                    break;
                    
                case LocationType.Forest:
                    if (rarity == ItemRarity.Common)
                    {
                        materials.Add(ItemFactory.CreateHerb());
                    }
                    else if (rarity == ItemRarity.Uncommon)
                    {
                        materials.Add(ItemFactory.CreateCrystalDust());
                        materials.Add(ItemFactory.CreateIronOre());
                    }
                    break;
                    
                case LocationType.Cave:
                    if (rarity == ItemRarity.Common)
                    {
                        materials.Add(ItemFactory.CreateIronOre());
                    }
                    else if (rarity == ItemRarity.Uncommon)
                    {
                        materials.Add(ItemFactory.CreateIronIngot());
                        materials.Add(ItemFactory.CreateGunpowder());
                    }
                    else if (rarity == ItemRarity.Rare)
                    {
                        materials.Add(ItemFactory.CreateGoldOre());
                    }
                    break;
                    
                case LocationType.Ruins:
                    if (rarity == ItemRarity.Uncommon)
                    {
                        materials.Add(ItemFactory.CreateGoldOre());
                    }
                    else if (rarity == ItemRarity.Rare)
                    {
                        materials.Add(ItemFactory.CreateGoldIngot());
                        materials.Add(ItemFactory.CreatePoisonExtract());
                    }
                    else if (rarity == ItemRarity.Epic)
                    {
                        materials.Add(ItemFactory.CreateLuminiteFragment());
                    }
                    break;
                    
                case LocationType.Castle:
                    if (rarity == ItemRarity.Rare)
                    {
                        materials.Add(ItemFactory.CreateGoldIngot());
                        materials.Add(ItemFactory.CreateLuminiteFragment());
                    }
                    else if (rarity == ItemRarity.Epic)
                    {
                        materials.Add(ItemFactory.CreateLuminite());
                    }
                    break;
            }
            
            return materials;
        }

        private ItemRarity CalculateItemRarity(LocationType locationType, bool forceRare)
        {
            int commonChance = 50;
            int uncommonChance = 30;
            int rareChance = 15;
            int epicChance = 4;
            
            switch (locationType)
            {
                case LocationType.Village:
                    commonChance += 10;
                    uncommonChance -= 5;
                    rareChance -= 3;
                    epicChance -= 2;
                    break;
                    
                case LocationType.Forest:
                    break;
                    
                case LocationType.Cave:
                    commonChance -= 5;
                    uncommonChance += 3;
                    rareChance += 1;
                    epicChance += 1;
                    break;
                    
                case LocationType.Ruins:
                    commonChance -= 10;
                    uncommonChance -= 5;
                    rareChance += 10;
                    epicChance += 3;
                    break;
                    
                case LocationType.Castle:
                    commonChance -= 15;
                    uncommonChance -= 5;
                    rareChance += 10;
                    epicChance += 7;
                    break;
            }
            
            if (forceRare)
            {
                commonChance = 0;
                uncommonChance = 20;
                rareChance = 50;
                epicChance = 25;
            }
            
            int roll = _random.Next(100);
            
            if (roll < commonChance)
                return ItemRarity.Common;
            roll -= commonChance;
            
            if (roll < uncommonChance)
                return ItemRarity.Uncommon;
            roll -= uncommonChance;
            
            if (roll < rareChance)
                return ItemRarity.Rare;
            roll -= rareChance;
            
            if (roll < epicChance)
                return ItemRarity.Epic;
            
            return ItemRarity.Legendary;
        }

        public (int Gold, List<Item> Items) CalculateRewards(bool isVictory, List<Character> enemies)
        {
            int goldReward = 0;
            List<Item> loot = new List<Item>();
            
            LoggingService.LogDebug($"CalculateRewards: Starting reward calculation for battle with isVictory={isVictory}, enemies={enemies.Count}");
            
            if (isVictory)
            {
                int baseGold = 10;
                
                // Adjust rewards based on enemy count and type
                goldReward = baseGold * enemies.Count;
                
                // Increase gold for boss enemies
                foreach (var enemy in enemies)
                {
                    if (enemy.IsHero)
                    {
                        goldReward += 50; // Дополнительное золото за героя
                        LoggingService.LogDebug($"CalculateRewards: Adding extra 50 gold for hero enemy {enemy.Name}");
                    }
                }
                
                // Store gold reward
                _goldReward = goldReward;
                
                // Generate loot if there's a current location
                if (_gameState.CurrentLocation != null)
                {
                    // Use the CalculateBattleRewards method to get appropriate loot
                    bool isBossHeroBattle = enemies.Any(e => e.IsHero);
                    (_, loot) = CalculateBattleRewards(isBossHeroBattle, enemies.Count);
                    
                    // Make sure we have at least some loot items
                    if (loot.Count == 0)
                    {
                        LoggingService.LogError("CalculateRewards: CRITICAL - No loot was generated, forcing fallback loot generation");
                        
                        // Fallback loot generation - minimum items based on battle type
                        int minLootCount = isBossHeroBattle ? 3 : 2;
                        
                        try
                        {
                            // Try to generate loot from location directly
                            if (_gameState.CurrentLocation.PossibleLoot != null && _gameState.CurrentLocation.PossibleLoot.Length > 0)
                            {
                                loot = _gameState.CurrentLocation.GenerateLoot(minLootCount);
                                LoggingService.LogDebug($"CalculateRewards: Generated {loot.Count} fallback loot items from location directly");
                            }
                            else
                            {
                                // If we still can't generate loot, create some basic items
                                loot = GenerateFallbackLoot(minLootCount, _gameState.CurrentLocation.Type);
                                LoggingService.LogDebug($"CalculateRewards: Generated {loot.Count} basic fallback loot items");
                            }
                        }
                        catch (Exception ex)
                        {
                            LoggingService.LogError($"CalculateRewards: Error in fallback loot generation: {ex.Message}", ex);
                        }
                    }
                    
                    // Log the final loot items
                    LoggingService.LogDebug($"CalculateRewards: Generated {loot.Count} total loot items");
                    
                    foreach (var item in loot)
                    {
                        LoggingService.LogDebug($"CalculateRewards: Loot item: {item.Name} x{item.StackSize}");
                    }
                }
                else
                {
                    LoggingService.LogError("CalculateRewards: WARNING: No current location to generate loot from");
                }
            }
            else
            {
                LoggingService.LogDebug("CalculateRewards: Battle lost, no rewards generated");
            }
            
            LoggingService.LogDebug($"CalculateRewards: Returning gold={goldReward}, items={loot.Count}");
                
            return (goldReward, loot);
        }
        
        // Generate some basic fallback loot when other methods fail
        private List<Item> GenerateFallbackLoot(int count, LocationType locationType)
        {
            List<Item> items = new List<Item>();
            Random random = new Random();
            
            // Define basic item templates for each location
            for (int i = 0; i < count; i++)
            {
                Item item = new Item
                {
                    MaxStackSize = 10,
                    StackSize = random.Next(1, 4), // 1-3 items
                    Type = ItemType.Material
                };
                
                // Set properties based on location
                switch (locationType)
                {
                    case LocationType.Village:
                        item.Name = random.Next(4) switch
                        {
                            0 => "Дерево",
                            1 => "Трава",
                            2 => "Ткань",
                            _ => "Фляга"
                        };
                        break;
                        
                    case LocationType.Forest:
                        item.Name = random.Next(4) switch
                        {
                            0 => "Дерево",
                            1 => "Трава",
                            2 => "Перо",
                            _ => "Железная руда"
                        };
                        break;
                        
                    case LocationType.Cave:
                        item.Name = random.Next(4) switch
                        {
                            0 => "Железная руда",
                            1 => "Золотая руда",
                            2 => "Порох",
                            _ => "Кристальная пыль"
                        };
                        break;
                        
                    case LocationType.Ruins:
                        item.Name = random.Next(4) switch
                        {
                            0 => "Золотая руда",
                            1 => "Ядовитый экстракт",
                            2 => "Фрагмент люминита",
                            _ => "Древний артефакт"
                        };
                        break;
                        
                    case LocationType.Castle:
                        item.Name = random.Next(4) switch
                        {
                            0 => "Золотой слиток",
                            1 => "Фрагмент люминита",
                            2 => "Люминит",
                            _ => "Королевская ткань"
                        };
                        break;
                }
                
                // Set sprite path based on name
                item.SpritePath = AssetPaths.Materials.GetMaterialPath(item.Name);
                
                // Set rarity and value based on location
                switch (locationType)
                {
                    case LocationType.Village:
                        item.Rarity = ItemRarity.Common;
                        item.Value = 5;
                        break;
                    case LocationType.Forest:
                        item.Rarity = random.Next(10) < 7 ? ItemRarity.Common : ItemRarity.Uncommon;
                        item.Value = 8;
                        break;
                    case LocationType.Cave:
                        item.Rarity = random.Next(10) < 5 ? ItemRarity.Common : ItemRarity.Uncommon;
                        item.Value = 12;
                        break;
                    case LocationType.Ruins:
                        item.Rarity = random.Next(10) < 5 ? ItemRarity.Uncommon : ItemRarity.Rare;
                        item.Value = 20;
                        break;
                    case LocationType.Castle:
                        item.Rarity = random.Next(10) < 5 ? ItemRarity.Rare : ItemRarity.Epic;
                        item.Value = 30;
                        break;
                }
                
                // Add description
                item.Description = $"{item.Name} - {item.Rarity} материал из локации {locationType}";
                
                items.Add(item);
            }
            
            return items;
        }

        // Mark battle as complete
        public void MarkBattleComplete(bool isVictory, List<Character> enemies)
        {
            _isVictory = isVictory;
            _enemies = enemies;
            
            // Calculate rewards if the battle was won
            if (isVictory)
            {
                var (gold, items) = CalculateRewards(isVictory, enemies);
                
                // Apply rewards
                _gameState.Gold += gold;
                
                // Add items to inventory
                if (items != null)
                {
                    foreach (var item in items)
                    {
                        _gameState.Inventory.AddItem(item);
                    }
                }
            }
        }

        // Calculate specific rewards for battle based on enemy type
        public int CalculateGoldReward(bool isBossHeroBattle)
        {
            // Boss hero battles provide more rewards
            if (isBossHeroBattle)
            {
                // Base rewards for boss battles
                return 100;
            }
            else
            {
                // Regular battle rewards
                return 10 * _enemies.Count;
            }
        }

        // Настройка новой битвы с данными вргами
        public void SetupBattle(List<Character> enemies, bool isHeroBattle = false)
        {
            if (enemies == null || enemies.Count == 0)
            {
                LoggingService.LogError("Error: Cannot setup battle with empty enemy list");
                return;
            }
            
            _enemies = new List<Character>(enemies);
            _isVictory = false;
            
            try
            {
                // Инициализация свойств врагам
                foreach (var enemy in _enemies)
                {
                    // Восстанавливаем здоровье любого выжившего врага
                    if (enemy.IsDefeated)
                    {
                        enemy.CurrentHealth = enemy.MaxHealth;
                    }
                    
                    // Добавляем вариацию здоровья и атаки для больших боев
                    if (!enemy.IsHero && !isHeroBattle)
                    {
                        // Добавляем случайные вариации для обычных боев (±10%)
                        Random random = new Random();
                        double healthVariation = 0.9 + (random.NextDouble() * 0.2); // 0.9-1.1
                        double attackVariation = 0.9 + (random.NextDouble() * 0.2); // 0.9-1.1
                        double defenseVariation = 0.9 + (random.NextDouble() * 0.2); // 0.9-1.1
                        
                        enemy.MaxHealth = (int)(enemy.MaxHealth * healthVariation);
                        enemy.CurrentHealth = enemy.MaxHealth;
                        enemy.Attack = (int)(enemy.Attack * attackVariation);
                        enemy.Defense = (int)(enemy.Defense * defenseVariation);
                    }
                }
                
                LoggingService.LogDebug($"Battle setup complete with {_enemies.Count} enemies");
                foreach (var enemy in _enemies)
                {
                    LoggingService.LogDebug($"Enemy: {enemy.Name}, HP: {enemy.CurrentHealth}/{enemy.MaxHealth}, ATK: {enemy.Attack}, DEF: {enemy.Defense}, IsHero: {enemy.IsHero}");
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error in SetupBattle: {ex.Message}", ex);
                throw;
            }
        }
    }
} 
