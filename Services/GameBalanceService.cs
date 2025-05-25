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
        // Множители для характеристик боссов
        private const double BossHealthMultiplier = 2.5;
        private const double BossAttackMultiplier = 1.8;
        private const double BossDefenseMultiplier = 1.5;
        
        // Случайная вариация для характеристик
        private const double EnemyStatRandomFactor = 0.15; // ±15%
        
        // Множители сложности игры
        private static readonly Dictionary<Difficulty, double> DifficultyMultipliers = new Dictionary<Difficulty, double>
        {
            { Difficulty.Easy, 0.75 },    // -25% сложность
            { Difficulty.Normal, 1.0 },   // Базовая сложность
            { Difficulty.Hard, 1.4 }      // +40% сложность
        };
        
        // Множители сложности по локациям
        private static readonly Dictionary<LocationType, double> LocationDifficultyMultiplier = new Dictionary<LocationType, double>
        {
            { LocationType.Village, 1.0 },  // Базовая сложность
            { LocationType.Forest, 1.3 },   // +30% сложность
            { LocationType.Cave, 1.7 },     // +70% сложность
            { LocationType.Ruins, 2.2 },    // +120% сложность
            { LocationType.Castle, 3.0 }    // +200% сложность
        };
        
        private readonly Random _random = new Random();
        
        // База для расчета базовых характеристик врагов для разных локаций
        private static readonly Dictionary<LocationType, EnemyBaseStats> BaseEnemyStats = new Dictionary<LocationType, EnemyBaseStats>
        {
            { LocationType.Village, new EnemyBaseStats { Health = 25, Attack = 4, Defense = 2 } },
            { LocationType.Forest, new EnemyBaseStats { Health = 40, Attack = 7, Defense = 3 } },
            { LocationType.Cave, new EnemyBaseStats { Health = 60, Attack = 10, Defense = 6 } },
            { LocationType.Ruins, new EnemyBaseStats { Health = 80, Attack = 14, Defense = 8 } },
            { LocationType.Castle, new EnemyBaseStats { Health = 100, Attack = 18, Defense = 10 } }
        };
        
        // База для названий врагов (используем для путей к спрайтам)
        private static readonly Dictionary<LocationType, string> EnemySpriteNames = new Dictionary<LocationType, string>
        {
            { LocationType.Village, "village_enemy" },
            { LocationType.Forest, "forest_enemy" },
            { LocationType.Cave, "cave_enemy" },
            { LocationType.Ruins, "ruins_enemy" },
            { LocationType.Castle, "castle_enemy" }
        };
        
        // База для названий героев (используем для путей к спрайтам)
        private static readonly Dictionary<LocationType, string> HeroSpriteNames = new Dictionary<LocationType, string>
        {
            { LocationType.Village, "village_hero" },
            { LocationType.Forest, "forest_hero" },
            { LocationType.Cave, "cave_hero" },
            { LocationType.Ruins, "ruins_hero" },
            { LocationType.Castle, "castle_hero" }
        };
        
        // Получение уровня сложности для локации
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
        
        // Генерация врага
        public static Character GenerateEnemy(LocationType locationType, bool isBoss = false, Difficulty? difficulty = null)
        {
            File.AppendAllText("error_log.txt", 
                $"[{DateTime.Now}] Генерируем {(isBoss ? "босса" : "обычного")} врага для {locationType}\r\n");
            
            // Создаем экземпляр для вызова нестатических методов
            var balanceService = new GameBalanceService();
            
            // Базовые параметры в зависимости от локации
            int baseHealth, baseAttack, baseDefense;
            
            // Получаем имя для спрайта
            string spriteName = balanceService.GetEnemySpriteName(locationType, isBoss);
            
            // Получаем локализованное имя
            string localizedName = balanceService.GetLocalizedEnemyName(locationType, isBoss);
            
            // Получаем базовые характеристики из словаря
            if (BaseEnemyStats.TryGetValue(locationType, out var stats))
            {
                baseHealth = stats.Health;
                baseAttack = stats.Attack;
                baseDefense = stats.Defense;
            }
            else
            {
                // Значения по умолчанию
                baseHealth = 30;
                baseAttack = 5;
                baseDefense = 2;
            }
            
            // Применяем множитель сложности локации
            double locationMultiplier = 1.0;
            if (LocationDifficultyMultiplier.TryGetValue(locationType, out double multiplier))
            {
                locationMultiplier = multiplier;
            }
            
            // Применяем множитель сложности игры
            double difficultyMultiplier = 1.0;
            if (difficulty.HasValue && DifficultyMultipliers.TryGetValue(difficulty.Value, out double diffMult))
            {
                difficultyMultiplier = diffMult;
            }
            
            // Применяем все множители
            baseHealth = (int)(baseHealth * locationMultiplier * difficultyMultiplier);
            baseAttack = (int)(baseAttack * locationMultiplier * difficultyMultiplier);
            baseDefense = (int)(baseDefense * locationMultiplier * difficultyMultiplier);
            
            // Дополнительные бонусы для боссов
            if (isBoss)
            {
                baseHealth = (int)(baseHealth * BossHealthMultiplier);
                baseAttack = (int)(baseAttack * BossAttackMultiplier);
                baseDefense = (int)(baseDefense * BossDefenseMultiplier);
            }
            
            // Создаем персонажа
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
        
        // Метод для вычисления уровня врага
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
            
            // Боссы на 2-3 уровня выше обычных мобов
            if (isBoss)
            {
                baseLevel += 2;
            }
            
            return baseLevel;
        }
        
        // Получение имени для спрайта
        private string GetEnemySpriteName(LocationType locationType, bool isBoss)
        {
            if (isBoss)
            {
                if (HeroSpriteNames.TryGetValue(locationType, out var name))
                {
                    return name;
                }
                return "village_hero"; // fallback
            }
            else
            {
                if (EnemySpriteNames.TryGetValue(locationType, out var name))
                {
                    return name;
                }
                return "village_enemy"; // fallback
            }
        }
        
        // Получение локализованного имени
        private string GetLocalizedEnemyName(LocationType locationType, bool isBoss)
        {
            if (isBoss)
            {
                // Для боссов используем имена из секции Heroes
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
                // Для обычных врагов используем имена из секции Enemies
                string key = $"Characters.Enemies.{locationType}.Regular";
                string localizedName = LocalizationService.Instance.GetTranslation(key);
                return localizedName;
            }
        }
        
        // Публичный метод для получения случайного имени врага
        public string GetRandomEnemyName(LocationType locationType)
        {
            return GetLocalizedEnemyName(locationType, false);
        }
        
        // Публичный метод для получения случайного имени героя
        public string GetRandomHeroName(LocationType locationType)
        {
            return GetLocalizedEnemyName(locationType, true);
        }
        
        // Калькуляция характеристик предметов
        public (int Damage, int Defense) CalculateItemStats(ItemType itemType, ItemMaterial material, ItemRarity rarity)
        {
            int baseStat = 0;
            double rarityMultiplier = 1.0;
            
            // Базовая характеристика по материалу
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
            
            // Множитель по редкости
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
            
            // Для оружия возвращаем в damage, для брони в defense
            return (itemType == ItemType.Weapon) ? (finalStat, 0) : (0, finalStat);
        }
    }
    
    // Вспомогательный класс для хранения базовых характеристик врагов
    public class EnemyBaseStats
    {
        public int Health { get; set; }
        public int Attack { get; set; }
        public int Defense { get; set; }
    }
} 
