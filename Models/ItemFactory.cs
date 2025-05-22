using System;
using System.Collections.Generic;
using SketchBlade.Services;

namespace SketchBlade.Models
{
    /// <summary>
    /// Централизованная фабрика для создания всех предметов в игре.
    /// Все предметы должны создаваться через эту фабрику для обеспечения единообразия.
    /// </summary>
    public static class ItemFactory
    {
        #region Weapons

        public static Item CreateWoodenWeapon()
        {
            var weapon = new Item
            {
                Name = LanguageService.GetTranslation("Items.WoodenSword.Name"),
                Description = LanguageService.GetTranslation("Items.WoodenSword.Description"),
                Type = ItemType.Weapon,
                Material = ItemMaterial.Wood,
                Rarity = ItemRarity.Common,
                Damage = 4,
                StatBonuses = new Dictionary<string, int>()
            };
            
            weapon.StatBonuses.Add("CriticalChance", 5); // +5% critical chance
            weapon.UpdateSpritePath();
            return weapon;
        }
        
        public static Item CreateIronWeapon()
        {
            var weapon = new Item
            {
                Name = LanguageService.GetTranslation("Items.IronSword.Name"),
                Description = LanguageService.GetTranslation("Items.IronSword.Description"),
                Type = ItemType.Weapon,
                Material = ItemMaterial.Iron,
                Rarity = ItemRarity.Uncommon,
                Damage = 8,
                StatBonuses = new Dictionary<string, int>()
            };
            
            weapon.StatBonuses.Add("CriticalChance", 8); // +8% critical chance
            weapon.UpdateSpritePath();
            return weapon;
        }
        
        public static Item CreateGoldWeapon()
        {
            var weapon = new Item
            {
                Name = LanguageService.GetTranslation("Items.GoldenSword.Name"),
                Description = LanguageService.GetTranslation("Items.GoldenSword.Description"),
                Type = ItemType.Weapon,
                Material = ItemMaterial.Gold,
                Rarity = ItemRarity.Rare,
                Damage = 14,
                StatBonuses = new Dictionary<string, int>()
            };
            
            weapon.StatBonuses.Add("CriticalChance", 10); // +10% critical chance
            weapon.UpdateSpritePath();
            return weapon;
        }
        
        public static Item CreateLuminiteWeapon()
        {
            var weapon = new Item
            {
                Name = LanguageService.GetTranslation("Items.LuminiteSword.Name"),
                Description = LanguageService.GetTranslation("Items.LuminiteSword.Description"),
                Type = ItemType.Weapon,
                Material = ItemMaterial.Luminite,
                Rarity = ItemRarity.Epic,
                Damage = 22,
                StatBonuses = new Dictionary<string, int>()
            };
            
            weapon.StatBonuses.Add("CriticalChance", 15); // +15% critical chance
            weapon.StatBonuses.Add("Attack", 5); // +5 attack for swords
                
            weapon.UpdateSpritePath();
            return weapon;
        }

        #endregion

        #region Armor

        public static Item CreateWoodenArmor(ItemSlotType slot)
        {
            string itemNameKey = "";
            string itemDescriptionKey = "";
            ItemType itemType = ItemType.Chestplate;
            string imageSuffix = "chest";
            
            switch (slot)
            {
                case ItemSlotType.Head:
                    itemNameKey = "Items.WoodenHelmet.Name";
                    itemDescriptionKey = "Items.WoodenHelmet.Description";
                    itemType = ItemType.Helmet;
                    imageSuffix = "helmet";
                    break;
                case ItemSlotType.Chest:
                    itemNameKey = "Items.WoodenChestplate.Name";
                    itemDescriptionKey = "Items.WoodenChestplate.Description";
                    itemType = ItemType.Chestplate;
                    imageSuffix = "chest";
                    break;
                case ItemSlotType.Legs:
                    itemNameKey = "Items.WoodenLeggings.Name";
                    itemDescriptionKey = "Items.WoodenLeggings.Description";
                    itemType = ItemType.Leggings;
                    imageSuffix = "legs";
                    break;
                default:
                    itemNameKey = "Items.WoodenChestplate.Name";
                    itemDescriptionKey = "Items.WoodenChestplate.Description";
                    itemType = ItemType.Chestplate;
                    imageSuffix = "chest";
                    break;
            }
            
            return new Item
            {
                Name = LanguageService.GetTranslation(itemNameKey),
                Description = LanguageService.GetTranslation(itemDescriptionKey),
                Type = itemType,
                Material = ItemMaterial.Wood,
                Rarity = ItemRarity.Common,
                Defense = 3,
                Value = 15,
                Weight = 2.0f,
                SpritePath = $"Assets/Images/items/armor/wooden_{imageSuffix}.png"
            };
        }
        
        public static Item CreateIronArmor(ItemSlotType slot)
        {
            string itemNameKey = "";
            string itemDescriptionKey = "";
            ItemType itemType = ItemType.Chestplate;
            string imageSuffix = "chest";
            
            switch (slot)
            {
                case ItemSlotType.Head:
                    itemNameKey = "Items.IronHelmet.Name";
                    itemDescriptionKey = "Items.IronHelmet.Description";
                    itemType = ItemType.Helmet;
                    imageSuffix = "helmet";
                    break;
                case ItemSlotType.Chest:
                    itemNameKey = "Items.IronChestplate.Name";
                    itemDescriptionKey = "Items.IronChestplate.Description";
                    itemType = ItemType.Chestplate;
                    imageSuffix = "chest";
                    break;
                case ItemSlotType.Legs:
                    itemNameKey = "Items.IronLeggings.Name";
                    itemDescriptionKey = "Items.IronLeggings.Description";
                    itemType = ItemType.Leggings;
                    imageSuffix = "legs";
                    break;
                default:
                    itemNameKey = "Items.IronChestplate.Name";
                    itemDescriptionKey = "Items.IronChestplate.Description";
                    itemType = ItemType.Chestplate;
                    imageSuffix = "chest";
                    break;
            }
            
            return new Item
            {
                Name = LanguageService.GetTranslation(itemNameKey),
                Description = LanguageService.GetTranslation(itemDescriptionKey),
                Type = itemType,
                Material = ItemMaterial.Iron,
                Rarity = ItemRarity.Uncommon,
                Defense = 6,
                Value = 60,
                Weight = 4.0f,
                SpritePath = $"Assets/Images/items/armor/iron_{imageSuffix}.png"
            };
        }
        
        public static Item CreateGoldArmor(ItemSlotType slot)
        {
            string itemNameKey = "";
            string itemDescriptionKey = "";
            ItemType itemType = ItemType.Chestplate;
            string imageSuffix = "chest";
            
            switch (slot)
            {
                case ItemSlotType.Head:
                    itemNameKey = "Items.GoldenHelmet.Name";
                    itemDescriptionKey = "Items.GoldenHelmet.Description";
                    itemType = ItemType.Helmet;
                    imageSuffix = "helmet";
                    break;
                case ItemSlotType.Chest:
                    itemNameKey = "Items.GoldenChestplate.Name";
                    itemDescriptionKey = "Items.GoldenChestplate.Description";
                    itemType = ItemType.Chestplate;
                    imageSuffix = "chest";
                    break;
                case ItemSlotType.Legs:
                    itemNameKey = "Items.GoldenLeggings.Name";
                    itemDescriptionKey = "Items.GoldenLeggings.Description";
                    itemType = ItemType.Leggings;
                    imageSuffix = "legs";
                    break;
                default:
                    itemNameKey = "Items.GoldenChestplate.Name";
                    itemDescriptionKey = "Items.GoldenChestplate.Description";
                    itemType = ItemType.Chestplate;
                    imageSuffix = "chest";
                    break;
            }
            
            return new Item
            {
                Name = LanguageService.GetTranslation(itemNameKey),
                Description = LanguageService.GetTranslation(itemDescriptionKey),
                Type = itemType,
                Material = ItemMaterial.Gold,
                Rarity = ItemRarity.Rare,
                Defense = 9,
                Value = 180,
                Weight = 3.5f,
                SpritePath = $"Assets/Images/items/armor/golden_{imageSuffix}.png"
            };
        }
        
        public static Item CreateLuminiteArmor(ItemSlotType slot)
        {
            string itemNameKey = "";
            string itemDescriptionKey = "";
            ItemType itemType = ItemType.Chestplate;
            string imageSuffix = "chest";
            
            switch (slot)
            {
                case ItemSlotType.Head:
                    itemNameKey = "Items.LuminiteHelmet.Name";
                    itemDescriptionKey = "Items.LuminiteHelmet.Description";
                    itemType = ItemType.Helmet;
                    imageSuffix = "helmet";
                    break;
                case ItemSlotType.Chest:
                    itemNameKey = "Items.LuminiteChestplate.Name";
                    itemDescriptionKey = "Items.LuminiteChestplate.Description";
                    itemType = ItemType.Chestplate;
                    imageSuffix = "chest";
                    break;
                case ItemSlotType.Legs:
                    itemNameKey = "Items.LuminiteLeggings.Name";
                    itemDescriptionKey = "Items.LuminiteLeggings.Description";
                    itemType = ItemType.Leggings;
                    imageSuffix = "legs";
                    break;
                default:
                    itemNameKey = "Items.LuminiteChestplate.Name";
                    itemDescriptionKey = "Items.LuminiteChestplate.Description";
                    itemType = ItemType.Chestplate;
                    imageSuffix = "chest";
                    break;
            }
            
            return new Item
            {
                Name = LanguageService.GetTranslation(itemNameKey),
                Description = LanguageService.GetTranslation(itemDescriptionKey),
                Type = itemType,
                Material = ItemMaterial.Luminite,
                Rarity = ItemRarity.Epic,
                Defense = 15,
                Value = 450,
                Weight = 2.5f,
                SpritePath = $"Assets/Images/items/armor/luminite_{imageSuffix}.png"
            };
        }
        
        public static Item CreateArmorForSlot(ItemMaterial material, ItemSlotType slot)
        {
            switch (material)
            {
                case ItemMaterial.Wood:
                    return CreateWoodenArmor(slot);
                case ItemMaterial.Iron:
                    return CreateIronArmor(slot);
                case ItemMaterial.Gold:
                    return CreateGoldArmor(slot);
                case ItemMaterial.Luminite:
                    return CreateLuminiteArmor(slot);
                default:
                    return CreateWoodenArmor(slot);
            }
        }

        #endregion

        #region Shields

        public static Item CreateIronShield()
        {
            return new Item
            {
                Name = LanguageService.GetTranslation("Items.IronShield.Name"),
                Description = LanguageService.GetTranslation("Items.IronShield.Description"),
                Type = ItemType.Shield,
                Material = ItemMaterial.Iron,
                Rarity = ItemRarity.Uncommon,
                Defense = 5,
                Value = 70,
                Weight = 3.5f,
                SpritePath = "Assets/Images/items/armor/iron_shield.png"
            };
        }
        
        public static Item CreateGoldShield()
        {
            return new Item
            {
                Name = LanguageService.GetTranslation("Items.GoldenShield.Name"),
                Description = LanguageService.GetTranslation("Items.GoldenShield.Description"),
                Type = ItemType.Shield,
                Material = ItemMaterial.Gold,
                Rarity = ItemRarity.Rare,
                Defense = 8,
                Value = 200,
                Weight = 3.0f,
                SpritePath = "Assets/Images/items/armor/golden_shield.png"
            };
        }
        
        public static Item CreateLuminiteShield()
        {
            return new Item
            {
                Name = LanguageService.GetTranslation("Items.LuminiteShield.Name"),
                Description = LanguageService.GetTranslation("Items.LuminiteShield.Description"),
                Type = ItemType.Shield,
                Material = ItemMaterial.Luminite,
                Rarity = ItemRarity.Epic,
                Defense = 12,
                Value = 500,
                Weight = 2.0f,
                SpritePath = "Assets/Images/items/armor/luminite_shield.png"
            };
        }
        
        public static Item CreateShieldForMaterial(ItemMaterial material)
        {
            return material switch
            {
                ItemMaterial.Iron => CreateIronShield(),
                ItemMaterial.Gold => CreateGoldShield(),
                ItemMaterial.Luminite => CreateLuminiteShield(),
                _ => CreateIronShield() // Default to iron if material not supported
            };
        }

        #endregion

        #region Consumables

        public static Item CreateHealingPotion(int amount = 1)
        {
            return new Item
            {
                Name = LanguageService.GetTranslation("Items.HealingPotion.Name"),
                Description = LanguageService.GetTranslation("Items.HealingPotion.Description"),
                Type = ItemType.Consumable,
                Rarity = ItemRarity.Common,
                Value = 25,
                Weight = 0.5f,
                MaxStackSize = 10,
                StackSize = amount,
                SpritePath = "Assets/Images/items/consumables/healing_potion.png"
            };
        }
        
        public static Item CreatePillow(int amount = 1)
        {
            return new Item
            {
                Name = LanguageService.GetTranslation("Items.Pillow.Name"),
                Description = LanguageService.GetTranslation("Items.Pillow.Description"),
                Type = ItemType.Consumable,
                Rarity = ItemRarity.Uncommon,
                Value = 35,
                Weight = 0.5f,
                MaxStackSize = 5,
                StackSize = amount,
                SpritePath = "Assets/Images/items/consumables/pillow.png"
            };
        }
        
        public static Item CreatePoisonedShuriken(int amount = 1)
        {
            return new Item
            {
                Name = LanguageService.GetTranslation("Items.PoisonedShuriken.Name"),
                Description = LanguageService.GetTranslation("Items.PoisonedShuriken.Description"),
                Type = ItemType.Consumable,
                Rarity = ItemRarity.Uncommon,
                Value = 40,
                Damage = 8,
                Weight = 0.2f,
                MaxStackSize = 20,
                StackSize = amount,
                SpritePath = "Assets/Images/items/consumables/poisoned_shuriken.png"
            };
        }
        
        public static Item CreateBomb(int amount = 1)
        {
            return new Item
            {
                Name = LanguageService.GetTranslation("Items.Bomb.Name"),
                Description = LanguageService.GetTranslation("Items.Bomb.Description"),
                Type = ItemType.Consumable,
                Rarity = ItemRarity.Rare,
                Value = 75,
                Damage = 15,
                Weight = 1.0f,
                MaxStackSize = 5,
                StackSize = amount,
                SpritePath = "Assets/Images/items/consumables/bomb.png"
            };
        }
        
        public static Item CreateRagePotion(int amount = 1)
        {
            return new Item
            {
                Name = LanguageService.GetTranslation("Items.RagePotion.Name"),
                Description = LanguageService.GetTranslation("Items.RagePotion.Description"),
                Type = ItemType.Consumable,
                Rarity = ItemRarity.Rare,
                Value = 80,
                Weight = 0.5f,
                MaxStackSize = 5,
                StackSize = amount,
                SpritePath = "Assets/Images/items/consumables/rage_potion.png"
            };
        }
        
        public static Item CreateInvulnerabilityPotion(int amount = 1)
        {
            return new Item
            {
                Name = LanguageService.GetTranslation("Items.InvulnerabilityPotion.Name"),
                Description = LanguageService.GetTranslation("Items.InvulnerabilityPotion.Description"),
                Type = ItemType.Consumable,
                Rarity = ItemRarity.Epic,
                Value = 150,
                Weight = 0.5f,
                MaxStackSize = 3,
                StackSize = amount,
                SpritePath = "Assets/Images/items/consumables/invulnerability_potion.png"
            };
        }

        #endregion

        #region Materials

        // Common materials
        public static Item CreateWood(int amount = 1)
        {
            return new Item
            {
                Name = LanguageService.GetTranslation("Items.Wood.Name"),
                Description = LanguageService.GetTranslation("Items.Wood.Description"),
                Type = ItemType.Material,
                Rarity = ItemRarity.Common,
                Value = 5,
                Weight = 1.0f,
                MaxStackSize = 50,
                StackSize = amount,
                SpritePath = "Assets/Images/items/materials/wood.png"
            };
        }

        public static Item CreateStick(int amount = 1)
        {
            return new Item
            {
                Name = LanguageService.GetTranslation("Items.Stick.Name"),
                Description = LanguageService.GetTranslation("Items.Stick.Description"),
                Type = ItemType.Material,
                Rarity = ItemRarity.Common,
                Value = 2,
                Weight = 0.2f,
                MaxStackSize = 99,
                StackSize = amount,
                SpritePath = "Assets/Images/items/materials/stick.png"
            };
        }

        public static Item CreateCloth(int amount = 1)
        {
            return new Item
            {
                Name = LanguageService.GetTranslation("Items.Cloth.Name"),
                Description = LanguageService.GetTranslation("Items.Cloth.Description"),
                Type = ItemType.Material,
                Rarity = ItemRarity.Common,
                Value = 8,
                Weight = 0.3f,
                MaxStackSize = 50,
                StackSize = amount,
                SpritePath = "Assets/Images/items/materials/cloth.png"
            };
        }

        public static Item CreateHerb(int amount = 1)
        {
            return new Item
            {
                Name = LanguageService.GetTranslation("Items.Herb.Name"),
                Description = LanguageService.GetTranslation("Items.Herb.Description"),
                Type = ItemType.Material,
                Rarity = ItemRarity.Common,
                Value = 10,
                Weight = 0.1f,
                MaxStackSize = 30,
                StackSize = amount,
                SpritePath = "Assets/Images/items/materials/herb.png"
            };
        }

        public static Item CreateFeather(int amount = 1)
        {
            return new Item
            {
                Name = LanguageService.GetTranslation("Items.Feather.Name"),
                Description = LanguageService.GetTranslation("Items.Feather.Description"),
                Type = ItemType.Material,
                Rarity = ItemRarity.Common,
                Value = 7,
                Weight = 0.1f,
                MaxStackSize = 50,
                StackSize = amount,
                SpritePath = "Assets/Images/items/materials/feather.png"
            };
        }

        public static Item CreateFlask(int amount = 1)
        {
            return new Item
            {
                Name = LanguageService.GetTranslation("Items.Flask.Name"),
                Description = LanguageService.GetTranslation("Items.Flask.Description"),
                Type = ItemType.Material,
                Rarity = ItemRarity.Common,
                Value = 15,
                Weight = 0.3f,
                MaxStackSize = 20,
                StackSize = amount,
                SpritePath = "Assets/Images/items/materials/flask.png"
            };
        }

        // Uncommon materials
        public static Item CreateIronOre(int amount = 1)
        {
            return new Item
            {
                Name = LanguageService.GetTranslation("Items.IronOre.Name"),
                Description = LanguageService.GetTranslation("Items.IronOre.Description"),
                Type = ItemType.Material,
                Rarity = ItemRarity.Common,
                Value = 20,
                Weight = 2.0f,
                MaxStackSize = 30,
                StackSize = amount,
                SpritePath = "Assets/Images/items/materials/iron_ore.png"
            };
        }

        public static Item CreateIronIngot(int amount = 1)
        {
            return new Item
            {
                Name = LanguageService.GetTranslation("Items.IronIngot.Name"),
                Description = LanguageService.GetTranslation("Items.IronIngot.Description"),
                Type = ItemType.Material,
                Rarity = ItemRarity.Uncommon,
                Value = 40,
                Weight = 1.0f,
                MaxStackSize = 20,
                StackSize = amount,
                SpritePath = "Assets/Images/items/materials/iron_ingot.png"
            };
        }

        public static Item CreateCrystalDust(int amount = 1)
        {
            return new Item
            {
                Name = LanguageService.GetTranslation("Items.CrystalDust.Name"),
                Description = LanguageService.GetTranslation("Items.CrystalDust.Description"),
                Type = ItemType.Material,
                Rarity = ItemRarity.Uncommon,
                Value = 35,
                Weight = 0.1f,
                MaxStackSize = 25,
                StackSize = amount,
                SpritePath = "Assets/Images/items/materials/crystal_dust.png"
            };
        }

        public static Item CreateGunpowder(int amount = 1)
        {
            return new Item
            {
                Name = LanguageService.GetTranslation("Items.Gunpowder.Name"),
                Description = LanguageService.GetTranslation("Items.Gunpowder.Description"),
                Type = ItemType.Material,
                Rarity = ItemRarity.Uncommon,
                Value = 30,
                Weight = 0.2f,
                MaxStackSize = 25,
                StackSize = amount,
                SpritePath = "Assets/Images/items/materials/gunpowder.png"
            };
        }

        // Rare materials
        public static Item CreateGoldOre(int amount = 1)
        {
            return new Item
            {
                Name = LanguageService.GetTranslation("Items.GoldOre.Name"),
                Description = LanguageService.GetTranslation("Items.GoldOre.Description"),
                Type = ItemType.Material,
                Rarity = ItemRarity.Uncommon,
                Value = 50,
                Weight = 2.0f,
                MaxStackSize = 20,
                StackSize = amount,
                SpritePath = "Assets/Images/items/materials/gold_ore.png"
            };
        }

        public static Item CreateGoldIngot(int amount = 1)
        {
            return new Item
            {
                Name = LanguageService.GetTranslation("Items.GoldIngot.Name"),
                Description = LanguageService.GetTranslation("Items.GoldIngot.Description"),
                Type = ItemType.Material,
                Rarity = ItemRarity.Rare,
                Value = 100,
                Weight = 1.0f,
                MaxStackSize = 15,
                StackSize = amount,
                SpritePath = "Assets/Images/items/materials/gold_ingot.png"
            };
        }

        public static Item CreatePoisonExtract(int amount = 1)
        {
            return new Item
            {
                Name = LanguageService.GetTranslation("Items.PoisonExtract.Name"),
                Description = LanguageService.GetTranslation("Items.PoisonExtract.Description"),
                Type = ItemType.Material,
                Rarity = ItemRarity.Rare,
                Value = 85,
                Weight = 0.2f,
                MaxStackSize = 10,
                StackSize = amount,
                SpritePath = "Assets/Images/items/materials/poison_extract.png"
            };
        }

        // Epic materials
        public static Item CreateLuminiteFragment(int amount = 1)
        {
            return new Item
            {
                Name = LanguageService.GetTranslation("Items.LuminiteFragment.Name"),
                Description = LanguageService.GetTranslation("Items.LuminiteFragment.Description"),
                Type = ItemType.Material,
                Rarity = ItemRarity.Rare,
                Value = 120,
                Weight = 0.3f,
                MaxStackSize = 15,
                StackSize = amount,
                SpritePath = "Assets/Images/items/materials/luminite_fragment.png"
            };
        }

        public static Item CreateLuminite(int amount = 1)
        {
            return new Item
            {
                Name = LanguageService.GetTranslation("Items.Luminite.Name"),
                Description = LanguageService.GetTranslation("Items.Luminite.Description"),
                Type = ItemType.Material,
                Rarity = ItemRarity.Epic,
                Value = 250,
                Weight = 0.5f,
                MaxStackSize = 10,
                StackSize = amount,
                SpritePath = "Assets/Images/items/materials/luminite.png"
            };
        }

        // Helper method for generic material creation
        public static Item CreateMaterial(string name, string description, ItemRarity rarity, int amount = 1)
        {
            return new Item
            {
                Name = name,
                Description = description,
                Type = ItemType.Material,
                Rarity = rarity,
                Value = GetDefaultValueForRarity(rarity),
                Weight = 0.5f,
                MaxStackSize = 20,
                StackSize = amount,
                SpritePath = $"Assets/Images/items/materials/{name.ToLower().Replace(" ", "_")}.png"
            };
        }

        private static int GetDefaultValueForRarity(ItemRarity rarity)
        {
            return rarity switch
            {
                ItemRarity.Common => 10,
                ItemRarity.Uncommon => 35,
                ItemRarity.Rare => 80,
                ItemRarity.Epic => 200,
                ItemRarity.Legendary => 500,
                _ => 10
            };
        }

        #endregion
    }
} 