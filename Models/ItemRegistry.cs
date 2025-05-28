using System;
using System.Collections.Generic;
using SketchBlade.Services;

namespace SketchBlade.Models
{
    /// <summary>
    /// Реестр предметов для управления предметами по ID
    /// </summary>
    public static class ItemRegistry
    {
        private static readonly Dictionary<string, Func<int, Item?>> _itemCreators = new();
        
        static ItemRegistry()
        {
            InitializeRegistry();
        }
        
        private static void InitializeRegistry()
        {
            // Материалы
            _itemCreators["wood"] = quantity => ItemFactory.CreateWood(quantity);
            _itemCreators["herbs"] = quantity => ItemFactory.CreateHerb(quantity);
            _itemCreators["cloth"] = quantity => ItemFactory.CreateCloth(quantity);
            _itemCreators["flask"] = quantity => ItemFactory.CreateFlask(quantity);
            _itemCreators["stick"] = quantity => ItemFactory.CreateStick(quantity);
            _itemCreators["iron_ore"] = quantity => ItemFactory.CreateIronOre(quantity);
            _itemCreators["iron_ingot"] = quantity => ItemFactory.CreateIronIngot(quantity);
            _itemCreators["gold_ore"] = quantity => ItemFactory.CreateGoldOre(quantity);
            _itemCreators["gold_ingot"] = quantity => ItemFactory.CreateGoldIngot(quantity);
            _itemCreators["crystal_dust"] = quantity => ItemFactory.CreateCrystalDust(quantity);
            _itemCreators["feather"] = quantity => ItemFactory.CreateFeather(quantity);
            _itemCreators["gunpowder"] = quantity => ItemFactory.CreateGunpowder(quantity);
            _itemCreators["poison_extract"] = quantity => ItemFactory.CreatePoisonExtract(quantity);
            _itemCreators["luminite_fragment"] = quantity => ItemFactory.CreateLuminiteFragment(quantity);
            _itemCreators["luminite"] = quantity => ItemFactory.CreateLuminite(quantity);
            
            // Расходуемые предметы
            _itemCreators["healing_potion"] = quantity => ItemFactory.CreateHealingPotion(quantity);
            _itemCreators["rage_potion"] = quantity => ItemFactory.CreateRagePotion(quantity);
            _itemCreators["invulnerability_potion"] = quantity => ItemFactory.CreateInvulnerabilityPotion(quantity);
            _itemCreators["bomb"] = quantity => ItemFactory.CreateBomb(quantity);
            _itemCreators["pillow"] = quantity => ItemFactory.CreatePillow(quantity);
            _itemCreators["poisoned_shuriken"] = quantity => ItemFactory.CreatePoisonedShuriken(quantity);
            
            // Оружие
            _itemCreators["wooden_sword"] = _ => ItemFactory.CreateWoodenWeapon();
            _itemCreators["iron_sword"] = _ => ItemFactory.CreateIronWeapon();
            _itemCreators["gold_sword"] = _ => ItemFactory.CreateGoldWeapon();
            _itemCreators["luminite_sword"] = _ => ItemFactory.CreateLuminiteWeapon();
            
            // Броня
            _itemCreators["wooden_helmet"] = _ => ItemFactory.CreateWoodenArmor(ItemSlotType.Head);
            _itemCreators["iron_helmet"] = _ => ItemFactory.CreateIronArmor(ItemSlotType.Head);
            _itemCreators["wooden_chestplate"] = _ => ItemFactory.CreateWoodenArmor(ItemSlotType.Chest);
            _itemCreators["iron_chestplate"] = _ => ItemFactory.CreateIronArmor(ItemSlotType.Chest);
            _itemCreators["wooden_leggings"] = _ => ItemFactory.CreateWoodenArmor(ItemSlotType.Legs);
            _itemCreators["iron_leggings"] = _ => ItemFactory.CreateIronArmor(ItemSlotType.Legs);
            _itemCreators["iron_shield"] = _ => ItemFactory.CreateIronShield();
        }
        
        /// <summary>
        /// Создать предмет по ID
        /// </summary>
        /// <param name="itemId">ID предмета</param>
        /// <param name="quantity">Количество</param>
        /// <returns>Созданный предмет или null</returns>
        public static Item? CreateItem(string itemId, int quantity = 1)
        {
            if (string.IsNullOrEmpty(itemId) || !_itemCreators.ContainsKey(itemId))
            {
                LoggingService.LogWarning($"Неизвестный ID предмета: {itemId}");
                return null;
            }
            
            try
            {
                return _itemCreators[itemId](quantity);
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Ошибка создания предмета {itemId}: {ex.Message}", ex);
                return null;
            }
        }
        
        /// <summary>
        /// Получить ID предмета по его названию
        /// </summary>
        /// <param name="item">Предмет</param>
        /// <returns>ID предмета</returns>
        public static string GetItemId(Item item)
        {
            if (item == null) return "";
            
            // Определяем ID на основе типа, материала и названия
            return item.Type switch
            {
                ItemType.Weapon => GetWeaponId(item),
                ItemType.Helmet => GetArmorId(item, "helmet"),
                ItemType.Chestplate => GetArmorId(item, "chestplate"),
                ItemType.Leggings => GetArmorId(item, "leggings"),
                ItemType.Shield => GetShieldId(item),
                ItemType.Consumable => GetConsumableId(item),
                ItemType.Material => GetMaterialId(item),
                _ => ""
            };
        }
        
        private static string GetWeaponId(Item item)
        {
            return item.Material switch
            {
                ItemMaterial.Wood => "wooden_sword",
                ItemMaterial.Iron => "iron_sword",
                ItemMaterial.Gold => "gold_sword",
                ItemMaterial.Luminite => "luminite_sword",
                _ => "wooden_sword"
            };
        }
        
        private static string GetArmorId(Item item, string armorType)
        {
            return item.Material switch
            {
                ItemMaterial.Wood => $"wooden_{armorType}",
                ItemMaterial.Iron => $"iron_{armorType}",
                ItemMaterial.Gold => $"gold_{armorType}",
                ItemMaterial.Luminite => $"luminite_{armorType}",
                _ => $"wooden_{armorType}"
            };
        }
        
        private static string GetShieldId(Item item)
        {
            return item.Material switch
            {
                ItemMaterial.Wood => "wooden_shield",
                ItemMaterial.Iron => "iron_shield",
                ItemMaterial.Gold => "gold_shield",
                ItemMaterial.Luminite => "luminite_shield",
                _ => "iron_shield"
            };
        }
        
        private static string GetConsumableId(Item item)
        {
            return item.Name.ToLower() switch
            {
                "зелье лечения" or "healing potion" => "healing_potion",
                "зелье ярости" or "rage potion" => "rage_potion",
                "зелье неуязвимости" or "invulnerability potion" => "invulnerability_potion",
                "бомба" or "bomb" => "bomb",
                "подушка" or "pillow" => "pillow",
                "отравленный сюрикен" or "poisoned shuriken" => "poisoned_shuriken",
                _ => ""
            };
        }
        
        private static string GetMaterialId(Item item)
        {
            return item.Name.ToLower() switch
            {
                "дерево" or "wood" => "wood",
                "трава" or "herbs" => "herbs",
                "ткань" or "cloth" => "cloth",
                "фляга" or "flask" or "water flask" => "flask",
                "палка" or "stick" => "stick",
                "железная руда" or "iron ore" => "iron_ore",
                "железный слиток" or "iron ingot" => "iron_ingot",
                "золотая руда" or "gold ore" => "gold_ore",
                "золотой слиток" or "gold ingot" => "gold_ingot",
                "кристальная пыль" or "crystal dust" => "crystal_dust",
                "перо" or "feather" or "feathers" => "feather",
                "порох" or "gunpowder" => "gunpowder",
                "извлечение яда" or "poison extract" => "poison_extract",
                "фрагмент люминита" or "luminite fragment" => "luminite_fragment",
                "люминит" or "luminite" => "luminite",
                _ => ""
            };
        }
        
        /// <summary>
        /// Получить все доступные ID предметов
        /// </summary>
        /// <returns>Список ID предметов</returns>
        public static IEnumerable<string> GetAllItemIds()
        {
            return _itemCreators.Keys;
        }
    }
} 