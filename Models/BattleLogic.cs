using System;
using System.Collections.Generic;
using System.Linq;
using SketchBlade.Models;

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
            target.CurrentHealth = Math.Max(0, target.CurrentHealth - damage);
        }

        public bool IsCharacterDefeated(Character character)
        {
            return character.CurrentHealth <= 0;
        }

        public void UseHealingPotion(Character character, int healAmount)
        {
            character.CurrentHealth = Math.Min(character.MaxHealth, character.CurrentHealth + healAmount);
        }

        public void ApplyRagePotion(Character character, int attackBonus, int duration)
        {
            character.Attack += attackBonus;
        }

        public void ApplyDefensePotion(Character character, int defenseBonus, int duration)
        {
            character.Defense += defenseBonus;
        }

        public int CalculateBombDamage()
        {
            return _random.Next(15, 26);
        }

        public List<Item> GenerateBattleRewards(GameData gameData, bool isHeroDefeated = false)
        {
            var rewardItems = new List<Item>();
            
            if (gameData.CurrentLocation == null) 
                return rewardItems;

            var location = gameData.CurrentLocation;
            
            int baseItemCount = isHeroDefeated ? 3 : 2;
            int bonusItemCount = _random.Next(0, 3);
            int totalItems = baseItemCount + bonusItemCount;

            for (int i = 0; i < totalItems; i++)
            {
                if (location.LootTable != null && location.LootTable.Count > 0)
                {
                    string materialName = location.LootTable[_random.Next(location.LootTable.Count)];
                    int quantity = GetMaterialQuantity(materialName, isHeroDefeated);
                    
                    var item = CreateItemByName(materialName, quantity);
                    if (item != null)
                    {
                        rewardItems.Add(item);
                    }
                }
            }

            gameData.BattleRewardItems = rewardItems;
            gameData.BattleRewardGold = CalculateGoldReward(isHeroDefeated);
            
            return rewardItems;
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
            return battleState.Enemies.FirstOrDefault(e => !e.IsDefeated);
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
            double multiplier = 1.2 + (_random.NextDouble() * 0.4); // 20-60% Р±РѕР»СЊС€Рµ СѓСЂРѕРЅР°
            return (int)(baseDamage * multiplier);
        }

        private int GetMaterialQuantity(string materialName, bool isHero)
        {
            int baseQuantity = materialName switch
            {
                "Luminite" => 1,
                "Luminite Fragment" => _random.Next(1, 3),
                "Gold Ingot" => _random.Next(1, 4),
                "Gold Ore" => _random.Next(2, 6),
                "Iron Ingot" => _random.Next(2, 5),
                "Iron Ore" => _random.Next(3, 8),
                "Poison Extract" => _random.Next(1, 3),
                "Crystal Dust" => _random.Next(2, 5),
                "Gunpowder" => _random.Next(1, 4),
                _ => _random.Next(2, 6)
            };

            return isHero ? (int)(baseQuantity * 1.5) : baseQuantity;
        }

        private int CalculateGoldReward(bool isHero)
        {
            int baseGold = _random.Next(10, 30);
            return isHero ? baseGold * 2 : baseGold;
        }
    }
} 
