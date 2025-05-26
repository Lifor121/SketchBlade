using System;
using System.Collections.Generic;
using SketchBlade.Models;
using System.Windows.Forms;
using System.IO;
using SketchBlade.Services;
using SketchBlade.Utilities;

namespace SketchBlade.Services
{
    public class GameBalanceService
    {
        private const double BossHealthMultiplier = 2.5;
        private const double BossAttackMultiplier = 1.8;
        private const double BossDefenseMultiplier = 1.5;
        
        private const double EnemyStatRandomFactor = 0.15;
        
        private static readonly Dictionary<Difficulty, double> DifficultyMultipliers = new Dictionary<Difficulty, double>
        {
            { Difficulty.Easy, 0.75 },
            { Difficulty.Normal, 1.0 },
            { Difficulty.Hard, 1.4 }
        };
        
        private static readonly Dictionary<LocationType, double> LocationDifficultyMultiplier = new Dictionary<LocationType, double>
        {
            { LocationType.Village, 1.0 },
            { LocationType.Forest, 1.3 },
            { LocationType.Cave, 1.7 },
            { LocationType.Ruins, 2.2 },
            { LocationType.Castle, 3.0 }
        };
        
        private readonly Random _random = new Random();
        
        private static readonly Dictionary<LocationType, EnemyBaseStats> BaseEnemyStats = new Dictionary<LocationType, EnemyBaseStats>
        {
            { LocationType.Village, new EnemyBaseStats { Health = 25, Attack = 4, Defense = 2 } },
            { LocationType.Forest, new EnemyBaseStats { Health = 40, Attack = 7, Defense = 3 } },
            { LocationType.Cave, new EnemyBaseStats { Health = 60, Attack = 10, Defense = 6 } },
            { LocationType.Ruins, new EnemyBaseStats { Health = 80, Attack = 14, Defense = 8 } },
            { LocationType.Castle, new EnemyBaseStats { Health = 100, Attack = 18, Defense = 10 } }
        };
        
        private static readonly Dictionary<LocationType, string> EnemySpriteNames = new Dictionary<LocationType, string>
        {
            { LocationType.Village, "village_enemy" },
            { LocationType.Forest, "forest_enemy" },
            { LocationType.Cave, "cave_enemy" },
            { LocationType.Ruins, "ruins_enemy" },
            { LocationType.Castle, "castle_enemy" }
        };
        
        private static readonly Dictionary<LocationType, string> HeroSpriteNames = new Dictionary<LocationType, string>
        {
            { LocationType.Village, "village_hero" },
            { LocationType.Forest, "forest_hero" },
            { LocationType.Cave, "cave_hero" },
            { LocationType.Ruins, "ruins_hero" },
            { LocationType.Castle, "castle_hero" }
        };
        
        public LocationDifficultyLevel GetLocationDifficulty(LocationType locationType)
        {
            switch (locationType)
            {
                case LocationType.Village:
                    return LocationDifficultyLevel.Easy;
                case LocationType.Forest:
                    return LocationDifficultyLevel.Medium;
                case LocationType.Cave:
                    return LocationDifficultyLevel.Hard;
                case LocationType.Ruins:
                    return LocationDifficultyLevel.VeryHard;
                case LocationType.Castle:
                    return LocationDifficultyLevel.Extreme;
                default:
                    return LocationDifficultyLevel.Medium;
            }
        }
        
        public static Character GenerateEnemy(LocationType locationType, bool isBoss = false, Difficulty? difficulty = null)
        {           
            var balanceService = new GameBalanceService();           
            int baseHealth, baseAttack, baseDefense;          
            string spriteName = balanceService.GetEnemySpriteName(locationType, isBoss);
            string localizedName = balanceService.GetLocalizedEnemyName(locationType, isBoss);
            
            if (BaseEnemyStats.TryGetValue(locationType, out var stats))
            {
                baseHealth = stats.Health;
                baseAttack = stats.Attack;
                baseDefense = stats.Defense;
            }
            else
            {
                baseHealth = 30;
                baseAttack = 5;
                baseDefense = 2;
            }
            
            double locationMultiplier = 1.0;
            if (LocationDifficultyMultiplier.TryGetValue(locationType, out double multiplier))
            {
                locationMultiplier = multiplier;
            }
            
            double difficultyMultiplier = 1.0;
            if (difficulty.HasValue && DifficultyMultipliers.TryGetValue(difficulty.Value, out double diffMult))
            {
                difficultyMultiplier = diffMult;
            }
            
            baseHealth = (int)(baseHealth * locationMultiplier * difficultyMultiplier);
            baseAttack = (int)(baseAttack * locationMultiplier * difficultyMultiplier);
            baseDefense = (int)(baseDefense * locationMultiplier * difficultyMultiplier);
            
            if (isBoss)
            {
                baseHealth = (int)(baseHealth * BossHealthMultiplier);
                baseAttack = (int)(baseAttack * BossAttackMultiplier);
                baseDefense = (int)(baseDefense * BossDefenseMultiplier);
            }
            
            var enemy = new Character
            {
                Name = localizedName,
                MaxHealth = baseHealth,
                CurrentHealth = baseHealth,
                Attack = baseAttack,
                Defense = baseDefense,
                Level = CalculateEnemyLevel(locationType, isBoss),
                Type = isBoss ? "Boss" : "Enemy",
                ImagePath = AssetPaths.Enemies.GetEnemyPathByName(spriteName),
                IsPlayer = false
            };
            
            return enemy;
        }
        
        public static int CalculateEnemyLevel(LocationType locationType, bool isBoss)
        {
            int baseLevel = locationType switch
            {
                LocationType.Village => 1,
                LocationType.Forest => 3,
                LocationType.Cave => 5,
                LocationType.Ruins => 7,
                LocationType.Castle => 10,
                _ => 1
            };
            
            if (isBoss)
            {
                baseLevel += 2;
            }
            
            return baseLevel;
        }
        
        private string GetEnemySpriteName(LocationType locationType, bool isBoss)
        {
            if (isBoss)
            {
                if (HeroSpriteNames.TryGetValue(locationType, out var name))
                {
                    return name;
                }
                return "village_hero";
            }
            else
            {
                if (EnemySpriteNames.TryGetValue(locationType, out var name))
                {
                    return name;
                }
                return "village_enemy";
            }
        }
        
        private string GetLocalizedEnemyName(LocationType locationType, bool isBoss)
        {
            if (isBoss)
            {
                string heroKey = locationType switch
                {
                    LocationType.Village => "Characters.Heroes.VillageElder",
                    LocationType.Forest => "Characters.Heroes.ForestGuardian",
                    LocationType.Cave => "Characters.Heroes.CaveTroll",
                    LocationType.Ruins => "Characters.Heroes.GuardianGolem",
                    LocationType.Castle => "Characters.Heroes.DarkKing",
                    _ => "Characters.Heroes.VillageElder"
                };
                string localizedName = LocalizationService.Instance.GetTranslation(heroKey);
                return localizedName;
            }
            else
            {
                string key = $"Characters.Enemies.{locationType}.Regular";
                string localizedName = LocalizationService.Instance.GetTranslation(key);
                return localizedName;
            }
        }
        
        public string GetRandomEnemyName(LocationType locationType)
        {
            return GetLocalizedEnemyName(locationType, false);
        }
        
        public string GetRandomHeroName(LocationType locationType)
        {
            return GetLocalizedEnemyName(locationType, true);
        }
        
        public (int Damage, int Defense) CalculateItemStats(ItemType itemType, ItemMaterial material, ItemRarity rarity)
        {
            int baseStat = 0;
            double rarityMultiplier = 1.0;
            
            switch (material)
            {
                case ItemMaterial.Wood:
                    baseStat = 3;
                    break;
                case ItemMaterial.Iron:
                    baseStat = 6;
                    break;
                case ItemMaterial.Gold:
                    baseStat = 9;
                    break;
                case ItemMaterial.Luminite:
                    baseStat = 15;
                    break;
                default:
                    baseStat = 3;
                    break;
            }
            
            switch (rarity)
            {
                case ItemRarity.Common:
                    rarityMultiplier = 1.0;
                    break;
                case ItemRarity.Uncommon:
                    rarityMultiplier = 1.3;
                    break;
                case ItemRarity.Rare:
                    rarityMultiplier = 1.6;
                    break;
                case ItemRarity.Epic:
                    rarityMultiplier = 2.0;
                    break;
                case ItemRarity.Legendary:
                    rarityMultiplier = 3.0;
                    break;
            }
            
            int finalStat = (int)(baseStat * rarityMultiplier);
            return (itemType == ItemType.Weapon) ? (finalStat, 0) : (0, finalStat);
        }
    }
    
    public class EnemyBaseStats
    {
        public int Health { get; set; }
        public int Attack { get; set; }
        public int Defense { get; set; }
    }
} 
