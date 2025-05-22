using System;
using System.Collections.Generic;
using System.Linq;

namespace SketchBlade.Models
{
    [Serializable]
    public class BattleManager
    {
        private readonly Random _random = new Random();
        [NonSerialized]
        private readonly GameState _gameState;
        private bool _isVictory = false;
        private List<Character> _enemies = new List<Character>();
        private int _goldReward = 0;

        public int DamageValue { get; private set; }
        public bool IsCriticalHit { get; private set; }
        public int? GoldReward => _goldReward;
        public int? ExpReward => null;

        public BattleManager(GameState gameState)
        {
            _gameState = gameState;
        }

        // Calculate damage for an attack
        public int CalculateDamage(Character attacker, Character defender)
        {
            // Базовая формула расчета урона: атака - (защита/2)
            int baseAttack = attacker.GetTotalAttack();
            int baseDefense = defender.GetTotalDefense();
            
            // Добавляем случайный элемент (±20%)
            double randomFactor = 0.8 + (_random.NextDouble() * 0.4); // 0.8 - 1.2
            
            // Рассчитываем итоговый урон, минимум 1
            int damage = Math.Max(1, (int)((baseAttack - (baseDefense / 2)) * randomFactor));
            
            return damage;
        }

        // Calculate if attack is critical (10% chance)
        public bool IsAttackCritical()
        {
            return _random.Next(10) == 0;
        }

        // Apply critical hit multiplier
        public int ApplyCriticalHit(int damage)
        {
            return (int)(damage * 1.5);
        }

        // Determine enemy for AI attack
        public Character SelectEnemyForAttack(IEnumerable<Character> enemies)
        {
            List<Character> enemyList = enemies.ToList();
            
            // If there's only one enemy, return it
            if (enemyList.Count == 1)
                return enemyList[0];
            
            // If there's a hero, it has priority to attack
            foreach (var enemy in enemyList)
            {
                if (enemy.IsHero)
                    return enemy;
            }
            
            // Find the strongest enemy (highest attack value)
            Character strongest = enemyList[0];
            foreach (var enemy in enemyList)
            {
                if (enemy.GetTotalAttack() > strongest.GetTotalAttack())
                    strongest = enemy;
            }
            
            // Small chance to pick a random enemy for unpredictability
            if (_random.Next(10) == 0)
            {
                return enemyList[_random.Next(enemyList.Count)];
            }
            
            return strongest;
        }

        // Decide if enemy should use special ability
        public bool ShouldUseSpecialAbility(Character enemy)
        {
            // Heroes have higher chance to use special abilities
            int threshold = enemy.IsHero ? 3 : 5; // 30% for hero, 20% for regular
            
            // Increase chance when health is lower
            if (enemy.CurrentHealth < enemy.MaxHealth / 2)
            {
                threshold += 2; // +20% when below half health
            }
            
            return _random.Next(10) < threshold;
        }

        // Choose a special ability for the enemy
        public (string abilityName, int damage, bool isAreaEffect) GetEnemySpecialAbility(Character enemy, Character target)
        {
            string[] abilities;
            int baseDamage = enemy.GetTotalAttack();
            bool isAreaEffect = false;
            
            // Different abilities based on enemy type
            if (enemy.IsHero)
            {
                abilities = new[] { "Мощный удар", "Свирепый рывок", "Сокрушающий замах", "Критический разрез", "Массовая атака" };
                // 20% chance for area effect ability for heroes
                isAreaEffect = _random.Next(5) == 0;
                
                // Ensure the "Массовая атака" is always area effect
                if (isAreaEffect && _random.Next(2) == 0)
                {
                    return ("Массовая атака", (int)(baseDamage * 0.8), true);
                }
            }
            else
            {
                abilities = new[] { "Внезапный выпад", "Яростная атака", "Сильный удар", "Серия ударов", "Круговой удар" };
                // 10% chance for area effect ability for regular enemies
                isAreaEffect = _random.Next(10) == 0;
                
                // Ensure the "Круговой удар" is always area effect
                if (isAreaEffect)
                {
                    return ("Круговой удар", (int)(baseDamage * 0.7), true);
                }
            }
            
            string abilityName = abilities[_random.Next(abilities.Length)];
            
            // Ability damage based on ability type
            double damageMultiplier;
            
            if (isAreaEffect)
            {
                // Area attacks do less damage per target
                damageMultiplier = 0.6 + (_random.NextDouble() * 0.3); // 0.6 - 0.9
            }
            else
            {
                // Single target special abilities do more damage
                damageMultiplier = 1.2 + (_random.NextDouble() * 0.4); // 1.2 - 1.6
            }
            
            int damage = (int)(baseDamage * damageMultiplier);
            
            // If ability is critical, increase damage further
            if (_random.Next(4) == 0) // 25% critical chance for abilities
            {
                damage = (int)(damage * 1.3);
                abilityName = "Критический " + abilityName.ToLower();
            }
            
            return (abilityName, damage, isAreaEffect);
        }

        // Calculate battle rewards
        public (int Gold, List<Item> Items) CalculateBattleRewards(bool isBossHeroBattle, int enemyCount)
        {
            // Get base reward from game state
            int goldReward = CalculateGoldReward(isBossHeroBattle);
            
            // Generate appropriate loot
            List<Item> loot = new List<Item>();
            
            if (isBossHeroBattle)
            {
                // Hero battles give better loot
                loot = GenerateHeroLoot();
            }
            else
            {
                // Regular battles
                loot = GenerateNormalLoot(enemyCount);
            }
            
            return (goldReward, loot);
        }

        // Generate loot for normal battles
        private List<Item> GenerateNormalLoot(int enemyCount)
        {
            List<Item> loot = new List<Item>();
            
            // Generate 2-4 materials based on number of enemies and randomness
            int itemCount = Math.Min(4, 2 + (_random.Next(enemyCount + 1)));
            
            for (int i = 0; i < itemCount; i++)
            {
                // Get appropriate materials based on location
                Item material = GenerateLocationBasedMaterial(_gameState.CurrentLocation?.Type ?? LocationType.Village);
                if (material != null)
                {
                    // Set appropriate stack size based on rarity (rarer materials come in smaller quantities)
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

        // Generate loot for hero battles
        private List<Item> GenerateHeroLoot()
        {
            List<Item> loot = new List<Item>();
            
            // Hero battles always drop 4-6 materials with at least one rare or better
            int itemCount = 4 + _random.Next(3);
            bool hasRareItem = false;
            
            for (int i = 0; i < itemCount; i++)
            {
                // Last item should be rare+ if none generated yet
                bool forceRare = (i == itemCount - 1 && !hasRareItem);
                
                // Get appropriate material based on location
                Item material = GenerateLocationBasedMaterial(_gameState.CurrentLocation?.Type ?? LocationType.Village, forceRare);
                if (material != null)
                {
                    if (material.Rarity >= ItemRarity.Rare)
                    {
                        hasRareItem = true;
                    }
                    
                    // Set appropriate stack size based on rarity (rarer materials come in smaller quantities)
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

        // Generate material appropriate for the location
        private Item GenerateLocationBasedMaterial(LocationType locationType, bool forceRare = false)
        {
            // Calculate rarity based on location and randomness
            ItemRarity rarity = CalculateItemRarity(locationType, forceRare);
            
            // Get list of materials for this location and rarity
            List<Item> possibleMaterials = GetLocationMaterials(locationType, rarity);
            
            // Choose a random material from the list
            if (possibleMaterials.Count > 0)
            {
                return possibleMaterials[_random.Next(possibleMaterials.Count)].Clone();
            }
            
            // Fallback to basic material if none found
            return ItemFactory.CreateWood();
        }

        // Get a list of materials for the location and rarity
        private List<Item> GetLocationMaterials(LocationType locationType, ItemRarity rarity)
        {
            List<Item> materials = new List<Item>();
            
            // Add basic materials that can be found anywhere
            if (rarity == ItemRarity.Common)
            {
                materials.Add(ItemFactory.CreateWood());
                materials.Add(ItemFactory.CreateCloth());
                materials.Add(ItemFactory.CreateFlask());
            }
            
            // Add location-specific materials
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

        // Calculate item rarity based on location and randomness
        private ItemRarity CalculateItemRarity(LocationType locationType, bool forceRare)
        {
            // Base rarity chances
            int commonChance = 50;      // 50%
            int uncommonChance = 30;    // 30%
            int rareChance = 15;        // 15%
            int epicChance = 4;         // 4%
            // Legendary is the remaining 1%
            
            // Adjust based on location type
            switch (locationType)
            {
                case LocationType.Village:
                    // Village has more common items
                    commonChance += 10;
                    uncommonChance -= 5;
                    rareChance -= 3;
                    epicChance -= 2;
                    break;
                    
                case LocationType.Forest:
                    // Forest has default distribution
                    break;
                    
                case LocationType.Cave:
                    // Cave has slightly better chances
                    commonChance -= 5;
                    uncommonChance += 3;
                    rareChance += 1;
                    epicChance += 1;
                    break;
                    
                case LocationType.Ruins:
                    // Ruins have even better chances
                    commonChance -= 10;
                    uncommonChance -= 5;
                    rareChance += 10;
                    epicChance += 3;
                    break;
                    
                case LocationType.Castle:
                    // Castle has best distribution
                    commonChance -= 15;
                    uncommonChance -= 5;
                    rareChance += 10;
                    epicChance += 7;
                    break;
            }
            
            // Force rarer items if specified
            if (forceRare)
            {
                commonChance = 0;
                uncommonChance = 20;
                rareChance = 50;
                epicChance = 25;
                // Legendary becomes 5%
            }
            
            // Roll for rarity
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

        // Calculate rewards based on battle results
        public (int Gold, List<Item> Items) CalculateRewards(bool isVictory, List<Character> enemies)
        {
            int goldReward = 0;
            List<Item> loot = new List<Item>();
            
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
                    }
                }
                
                // Store gold reward
                _goldReward = goldReward;
                
                // Generate loot if there's a current location
                if (_gameState.CurrentLocation != null)
                {
                    loot = _gameState.CurrentLocation.GenerateLoot(enemies.Count);
                }
            }
            
            return (goldReward, loot);
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

        // Set up a new battle with the given enemies
        public void SetupBattle(List<Character> enemies, bool isHeroBattle = false)
        {
            if (enemies == null || enemies.Count == 0)
            {
                Console.WriteLine("Error: Cannot setup battle with empty enemy list");
                return;
            }
            
            _enemies = new List<Character>(enemies);
            _isVictory = false;
            
            // Initialize enemy properties
            foreach (var enemy in _enemies)
            {
                // Reset any existing defeat status
                if (enemy.IsDefeated)
                {
                    enemy.CurrentHealth = enemy.MaxHealth;
                }
                
                // Add some variations to regular enemies to make battles more interesting
                if (!enemy.IsHero && !isHeroBattle)
                {
                    // Add random variations to stats for regular enemies (±10%)
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
            
            Console.WriteLine($"Battle setup complete with {_enemies.Count} enemies");
            foreach (var enemy in _enemies)
            {
                Console.WriteLine($"Enemy: {enemy.Name}, HP: {enemy.CurrentHealth}/{enemy.MaxHealth}, ATK: {enemy.Attack}, DEF: {enemy.Defense}, IsHero: {enemy.IsHero}");
            }
        }
    }
} 