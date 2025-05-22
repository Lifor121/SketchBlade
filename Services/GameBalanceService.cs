using System;
using System.Collections.Generic;
using SketchBlade.Models;

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
        
        // База для названий врагов
        private static readonly Dictionary<LocationType, string[]> EnemyNames = new Dictionary<LocationType, string[]>
        {
            { LocationType.Village, new[] { "Бандит", "Разбойник", "Вор", "Грабитель", "Мародёр" } },
            { LocationType.Forest, new[] { "Волк", "Медведь", "Лесной разбойник", "Кабан", "Лесной паук" } },
            { LocationType.Cave, new[] { "Пещерный паук", "Летучая мышь", "Гоблин", "Троглодит", "Слизень" } },
            { LocationType.Ruins, new[] { "Скелет", "Зомби", "Призрак", "Оживший труп", "Мумия" } },
            { LocationType.Castle, new[] { "Тёмный рыцарь", "Прислужник тьмы", "Вампир", "Скелет-воин", "Личинка тьмы" } }
        };
        
        // База для названий героев
        private static readonly Dictionary<LocationType, string[]> HeroNames = new Dictionary<LocationType, string[]>
        {
            { LocationType.Village, new[] { "Деревенский старейшина", "Глава деревни", "Сельский богатырь" } },
            { LocationType.Forest, new[] { "Хранитель леса", "Лесной дух", "Древний медведь" } },
            { LocationType.Cave, new[] { "Пещерный тролль", "Горный голем", "Владыка подземелья" } },
            { LocationType.Ruins, new[] { "Древний голем", "Повелитель нежити", "Древний лич" } },
            { LocationType.Castle, new[] { "Тёмный король", "Повелитель замка", "Тёмный рыцарь смерти" } }
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
        public Character GenerateEnemy(LocationType locationType, bool isBoss = false)
        {
            Console.WriteLine($"Generating {(isBoss ? "boss" : "regular")} enemy for {locationType}");
            
            // Базовые параметры в зависимости от локации
            int baseHealth, baseAttack, baseDefense;
            
            // Используем словари для выбора имени
            string enemyName = GetEnemyName(locationType, isBoss);
            
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
            
            baseHealth = (int)(baseHealth * locationMultiplier);
            baseAttack = (int)(baseAttack * locationMultiplier);
            baseDefense = (int)(baseDefense * locationMultiplier);
            
            // Применяем множитель для боссов
            if (isBoss)
            {
                baseHealth = (int)(baseHealth * BossHealthMultiplier);
                baseAttack = (int)(baseAttack * BossAttackMultiplier);
                baseDefense = (int)(baseDefense * BossDefenseMultiplier);
            }
            
            // Создаем врага
            Character enemy = new Character
            {
                Name = enemyName,
                MaxHealth = baseHealth,
                CurrentHealth = baseHealth,
                Attack = baseAttack,
                Defense = baseDefense,
                IsHero = isBoss,
                Type = isBoss ? "Hero" : "Enemy"
            };
            
            // Выбираем соответствующий спрайт
            string imagePath = $"Assets/Images/Characters/{(isBoss ? "boss" : "enemy")}_{locationType.ToString().ToLower()}.png";
            enemy.ImagePath = imagePath;
            
            Console.WriteLine($"Created enemy: {enemy.Name}, HP: {enemy.MaxHealth}, Attack: {enemy.Attack}, Defense: {enemy.Defense}");
            
            return enemy;
        }
        
        // Случайный выбор имени врага
        private string GetEnemyName(LocationType locationType, bool isBoss)
        {
            if (isBoss)
            {
                if (HeroNames.TryGetValue(locationType, out var names))
                {
                    return names[_random.Next(names.Length)];
                }
                return "Герой локации";
            }
            else
            {
                if (EnemyNames.TryGetValue(locationType, out var names))
                {
                    return names[_random.Next(names.Length)];
                }
                return "Враг";
            }
        }
        
        // Публичный метод для получения случайного имени врага
        public string GetRandomEnemyName(LocationType locationType)
        {
            return GetEnemyName(locationType, false);
        }
        
        // Публичный метод для получения случайного имени героя
        public string GetRandomHeroName(LocationType locationType)
        {
            return GetEnemyName(locationType, true);
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