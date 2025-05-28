using System;
using System.Collections.Generic;
using System.Globalization;
using SketchBlade.Services;
using SketchBlade.Utilities;

namespace SketchBlade.Models
{
    public static class ItemFactory
    {
        #region Weapons

        public static Item CreateWoodenWeapon()
        {
            return new Item
            {
                Name = LocalizationService.Instance.GetTranslation("Items.WoodenSword.Name"),
                Description = LocalizationService.Instance.GetTranslation("Items.WoodenSword.Description"),
                Type = ItemType.Weapon,
                Material = ItemMaterial.Wood,
                Rarity = ItemRarity.Common,
                Damage = 5,
                Value = 10,
                Weight = 1.5f,
                SpritePath = AssetPaths.Weapons.WOODEN_SWORD
            };
        }
        
        public static Item CreateIronWeapon()
        {
            return new Item
            {
                Name = LocalizationService.Instance.GetTranslation("Items.IronSword.Name"),
                Description = LocalizationService.Instance.GetTranslation("Items.IronSword.Description"),
                Type = ItemType.Weapon,
                Material = ItemMaterial.Iron,
                Rarity = ItemRarity.Uncommon,
                Damage = 10,
                Value = 50,
                Weight = 2.5f,
                SpritePath = AssetPaths.Weapons.IRON_SWORD
            };
        }
        
        public static Item CreateGoldWeapon()
        {
            return new Item
            {
                Name = LocalizationService.Instance.GetTranslation("Items.GoldenSword.Name"),
                Description = LocalizationService.Instance.GetTranslation("Items.GoldenSword.Description"),
                Type = ItemType.Weapon,
                Material = ItemMaterial.Gold,
                Rarity = ItemRarity.Rare,
                Damage = 15,
                Value = 150,
                Weight = 2.0f,
                SpritePath = AssetPaths.Weapons.GOLDEN_SWORD
            };
        }
        
        public static Item CreateLuminiteWeapon()
        {
            return new Item
            {
                Name = LocalizationService.Instance.GetTranslation("Items.LuminiteSword.Name"),
                Description = LocalizationService.Instance.GetTranslation("Items.LuminiteSword.Description"),
                Type = ItemType.Weapon,
                Material = ItemMaterial.Luminite,
                Rarity = ItemRarity.Epic,
                Damage = 25,
                Value = 400,
                Weight = 1.0f,
                SpritePath = AssetPaths.Weapons.LUMINITE_SWORD
            };
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
                Name = LocalizationService.Instance.GetTranslation(itemNameKey),
                Description = LocalizationService.Instance.GetTranslation(itemDescriptionKey),
                Type = itemType,
                Material = ItemMaterial.Wood,
                Rarity = ItemRarity.Common,
                Defense = 3,
                Value = 15,
                Weight = 2.0f,
                SpritePath = AssetPaths.Armor.GetArmorPath("wooden", imageSuffix)
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
                Name = LocalizationService.Instance.GetTranslation(itemNameKey),
                Description = LocalizationService.Instance.GetTranslation(itemDescriptionKey),
                Type = itemType,
                Material = ItemMaterial.Iron,
                Rarity = ItemRarity.Uncommon,
                Defense = 6,
                Value = 60,
                Weight = 4.0f,
                SpritePath = AssetPaths.Armor.GetArmorPath("iron", imageSuffix)
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
                Name = LocalizationService.Instance.GetTranslation(itemNameKey),
                Description = LocalizationService.Instance.GetTranslation(itemDescriptionKey),
                Type = itemType,
                Material = ItemMaterial.Gold,
                Rarity = ItemRarity.Rare,
                Defense = 9,
                Value = 180,
                Weight = 3.5f,
                SpritePath = AssetPaths.Armor.GetArmorPath("golden", imageSuffix)
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
                Name = LocalizationService.Instance.GetTranslation(itemNameKey),
                Description = LocalizationService.Instance.GetTranslation(itemDescriptionKey),
                Type = itemType,
                Material = ItemMaterial.Luminite,
                Rarity = ItemRarity.Epic,
                Defense = 15,
                Value = 450,
                Weight = 2.5f,
                SpritePath = AssetPaths.Armor.GetArmorPath("luminite", imageSuffix)
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
                Name = LocalizationService.Instance.GetTranslation("Items.IronShield.Name"),
                Description = LocalizationService.Instance.GetTranslation("Items.IronShield.Description"),
                Type = ItemType.Shield,
                Material = ItemMaterial.Iron,
                Rarity = ItemRarity.Uncommon,
                Defense = 5,
                Value = 70,
                Weight = 3.5f,
                SpritePath = AssetPaths.Armor.IRON_SHIELD
            };
        }
        
        public static Item CreateGoldShield()
        {
            return new Item
            {
                Name = LocalizationService.Instance.GetTranslation("Items.GoldenShield.Name"),
                Description = LocalizationService.Instance.GetTranslation("Items.GoldenShield.Description"),
                Type = ItemType.Shield,
                Material = ItemMaterial.Gold,
                Rarity = ItemRarity.Rare,
                Defense = 8,
                Value = 210,
                Weight = 3.0f,
                SpritePath = AssetPaths.Armor.GOLDEN_SHIELD
            };
        }
        
        public static Item CreateLuminiteShield()
        {
            return new Item
            {
                Name = LocalizationService.Instance.GetTranslation("Items.LuminiteShield.Name"),
                Description = LocalizationService.Instance.GetTranslation("Items.LuminiteShield.Description"),
                Type = ItemType.Shield,
                Material = ItemMaterial.Luminite,
                Rarity = ItemRarity.Epic,
                Defense = 12,
                Value = 525,
                Weight = 2.0f,
                SpritePath = AssetPaths.Armor.LUMINITE_SHIELD
            };
        }
        
        public static Item CreateShieldForMaterial(ItemMaterial material)
        {
            return material switch
            {
                ItemMaterial.Iron => CreateIronShield(),
                ItemMaterial.Gold => CreateGoldShield(),
                ItemMaterial.Luminite => CreateLuminiteShield(),
                _ => CreateIronShield()
            };
        }

        #endregion

        #region Consumables

        public static Item CreateHealingPotion(int amount = 1)
        {
            return new Item
            {
                Name = LocalizationService.Instance.GetTranslation("Items.HealingPotion.Name"),
                Description = LocalizationService.Instance.GetTranslation("Items.HealingPotion.Description"),
                Type = ItemType.Consumable,
                Rarity = ItemRarity.Common,
                Value = 25,
                EffectPower = 30,
                Weight = 0.5f,
                MaxStackSize = 10,
                StackSize = amount,
                SpritePath = AssetPaths.Consumables.HEALING_POTION
            };
        }
        
        public static Item CreatePillow(int amount = 1)
        {
            return new Item
            {
                Name = LocalizationService.Instance.GetTranslation("Items.Pillow.Name"),
                Description = LocalizationService.Instance.GetTranslation("Items.Pillow.Description"),
                Type = ItemType.Consumable,
                Rarity = ItemRarity.Uncommon,
                Value = 35,
                Weight = 0.5f,
                MaxStackSize = 5,
                StackSize = amount,
                SpritePath = AssetPaths.Consumables.PILLOW
            };
        }
        
        public static Item CreatePoisonedShuriken(int amount = 1)
        {
            return new Item
            {
                Name = LocalizationService.Instance.GetTranslation("Items.PoisonedShuriken.Name"),
                Description = LocalizationService.Instance.GetTranslation("Items.PoisonedShuriken.Description"),
                Type = ItemType.Consumable,
                Rarity = ItemRarity.Uncommon,
                Value = 40,
                Damage = 8,
                EffectPower = 5,
                Weight = 0.2f,
                MaxStackSize = 20,
                StackSize = amount,
                SpritePath = AssetPaths.Consumables.POISONED_SHURIKEN
            };
        }
        
        public static Item CreateBomb(int amount = 1)
        {
            return new Item
            {
                Name = LocalizationService.Instance.GetTranslation("Items.Bomb.Name"),
                Description = LocalizationService.Instance.GetTranslation("Items.Bomb.Description"),
                Type = ItemType.Consumable,
                Rarity = ItemRarity.Rare,
                Value = 75,
                Damage = 15,
                Weight = 1.0f,
                MaxStackSize = 5,
                StackSize = amount,
                SpritePath = AssetPaths.Consumables.BOMB
            };
        }
        
        public static Item CreateRagePotion(int amount = 1)
        {
            return new Item
            {
                Name = LocalizationService.Instance.GetTranslation("Items.RagePotion.Name"),
                Description = LocalizationService.Instance.GetTranslation("Items.RagePotion.Description"),
                Type = ItemType.Consumable,
                Rarity = ItemRarity.Rare,
                Value = 80,
                EffectPower = 10,
                Weight = 0.5f,
                MaxStackSize = 5,
                StackSize = amount,
                SpritePath = AssetPaths.Consumables.RAGE_POTION
            };
        }
        
        public static Item CreateInvulnerabilityPotion(int amount = 1)
        {
            return new Item
            {
                Name = LocalizationService.Instance.GetTranslation("Items.InvulnerabilityPotion.Name"),
                Description = LocalizationService.Instance.GetTranslation("Items.InvulnerabilityPotion.Description"),
                Type = ItemType.Consumable,
                Rarity = ItemRarity.Epic,
                Value = 150,
                EffectPower = 15,
                Weight = 0.5f,
                MaxStackSize = 3,
                StackSize = amount,
                SpritePath = AssetPaths.Consumables.INVULNERABILITY_POTION
            };
        }

        #endregion

        #region Materials

        public static Item CreateWood(int amount = 1)
        {
            return new Item
            {
                Name = LocalizationService.Instance.GetTranslation("Items.Wood.Name"),
                Description = LocalizationService.Instance.GetTranslation("Items.Wood.Description"),
                Type = ItemType.Material,
                Rarity = ItemRarity.Common,
                Value = 5,
                Weight = 1.0f,
                MaxStackSize = 50,
                StackSize = amount,
                SpritePath = AssetPaths.Materials.WOOD
            };
        }

        public static Item CreateStick(int amount = 1)
        {
            return new Item
            {
                Name = LocalizationService.Instance.GetTranslation("Items.Stick.Name"),
                Description = LocalizationService.Instance.GetTranslation("Items.Stick.Description"),
                Type = ItemType.Material,
                Rarity = ItemRarity.Common,
                Value = 2,
                Weight = 0.2f,
                MaxStackSize = 99,
                StackSize = amount,
                SpritePath = AssetPaths.Materials.STICK
            };
        }

        public static Item CreateCloth(int amount = 1)
        {
            return new Item
            {
                Name = LocalizationService.Instance.GetTranslation("Items.Cloth.Name"),
                Description = LocalizationService.Instance.GetTranslation("Items.Cloth.Description"),
                Type = ItemType.Material,
                Rarity = ItemRarity.Common,
                Value = 8,
                Weight = 0.3f,
                MaxStackSize = 50,
                StackSize = amount,
                SpritePath = AssetPaths.Materials.CLOTH
            };
        }

        public static Item CreateHerb(int amount = 1)
        {
            return new Item
            {
                Name = LocalizationService.Instance.GetTranslation("Items.Herb.Name"),
                Description = LocalizationService.Instance.GetTranslation("Items.Herb.Description"),
                Type = ItemType.Material,
                Rarity = ItemRarity.Common,
                Value = 10,
                Weight = 0.1f,
                MaxStackSize = 30,
                StackSize = amount,
                SpritePath = AssetPaths.Materials.HERB
            };
        }

        public static Item CreateFeather(int amount = 1)
        {
            return new Item
            {
                Name = LocalizationService.Instance.GetTranslation("Items.Feather.Name"),
                Description = LocalizationService.Instance.GetTranslation("Items.Feather.Description"),
                Type = ItemType.Material,
                Rarity = ItemRarity.Common,
                Value = 7,
                Weight = 0.1f,
                MaxStackSize = 50,
                StackSize = amount,
                SpritePath = AssetPaths.Materials.FEATHER
            };
        }

        public static Item CreateFlask(int amount = 1)
        {
            return new Item
            {
                Name = LocalizationService.Instance.GetTranslation("Items.Flask.Name"),
                Description = LocalizationService.Instance.GetTranslation("Items.Flask.Description"),
                Type = ItemType.Material,
                Rarity = ItemRarity.Common,
                Value = 15,
                Weight = 0.3f,
                MaxStackSize = 20,
                StackSize = amount,
                SpritePath = AssetPaths.Materials.FLASK
            };
        }

        public static Item CreateIronOre(int amount = 1)
        {
            return new Item
            {
                Name = LocalizationService.Instance.GetTranslation("Items.IronOre.Name"),
                Description = LocalizationService.Instance.GetTranslation("Items.IronOre.Description"),
                Type = ItemType.Material,
                Rarity = ItemRarity.Common,
                Value = 20,
                Weight = 2.0f,
                MaxStackSize = 30,
                StackSize = amount,
                SpritePath = AssetPaths.Materials.IRON_ORE
            };
        }

        public static Item CreateIronIngot(int amount = 1)
        {
            return new Item
            {
                Name = LocalizationService.Instance.GetTranslation("Items.IronIngot.Name"),
                Description = LocalizationService.Instance.GetTranslation("Items.IronIngot.Description"),
                Type = ItemType.Material,
                Rarity = ItemRarity.Uncommon,
                Value = 40,
                Weight = 1.0f,
                MaxStackSize = 20,
                StackSize = amount,
                SpritePath = AssetPaths.Materials.IRON_INGOT
            };
        }

        public static Item CreateCrystalDust(int amount = 1)
        {
            return new Item
            {
                Name = LocalizationService.Instance.GetTranslation("Items.CrystalDust.Name"),
                Description = LocalizationService.Instance.GetTranslation("Items.CrystalDust.Description"),
                Type = ItemType.Material,
                Rarity = ItemRarity.Uncommon,
                Value = 35,
                Weight = 0.1f,
                MaxStackSize = 25,
                StackSize = amount,
                SpritePath = AssetPaths.Materials.CRYSTAL_DUST
            };
        }

        public static Item CreateGunpowder(int amount = 1)
        {
            return new Item
            {
                Name = LocalizationService.Instance.GetTranslation("Items.Gunpowder.Name"),
                Description = LocalizationService.Instance.GetTranslation("Items.Gunpowder.Description"),
                Type = ItemType.Material,
                Rarity = ItemRarity.Uncommon,
                Value = 30,
                Weight = 0.2f,
                MaxStackSize = 25,
                StackSize = amount,
                SpritePath = AssetPaths.Materials.GUNPOWDER
            };
        }

        public static Item CreateGoldOre(int amount = 1)
        {
            return new Item
            {
                Name = LocalizationService.Instance.GetTranslation("Items.GoldOre.Name"),
                Description = LocalizationService.Instance.GetTranslation("Items.GoldOre.Description"),
                Type = ItemType.Material,
                Rarity = ItemRarity.Uncommon,
                Value = 50,
                Weight = 2.0f,
                MaxStackSize = 20,
                StackSize = amount,
                SpritePath = AssetPaths.Materials.GOLD_ORE
            };
        }

        public static Item CreateGoldIngot(int amount = 1)
        {
            return new Item
            {
                Name = LocalizationService.Instance.GetTranslation("Items.GoldIngot.Name"),
                Description = LocalizationService.Instance.GetTranslation("Items.GoldIngot.Description"),
                Type = ItemType.Material,
                Rarity = ItemRarity.Rare,
                Value = 100,
                Weight = 1.0f,
                MaxStackSize = 15,
                StackSize = amount,
                SpritePath = AssetPaths.Materials.GOLD_INGOT
            };
        }

        public static Item CreatePoisonExtract(int amount = 1)
        {
            return new Item
            {
                Name = LocalizationService.Instance.GetTranslation("Items.PoisonExtract.Name"),
                Description = LocalizationService.Instance.GetTranslation("Items.PoisonExtract.Description"),
                Type = ItemType.Material,
                Rarity = ItemRarity.Rare,
                Value = 85,
                Weight = 0.2f,
                MaxStackSize = 10,
                StackSize = amount,
                SpritePath = AssetPaths.Materials.POISON_EXTRACT
            };
        }

        public static Item CreateLuminiteFragment(int amount = 1)
        {
            return new Item
            {
                Name = LocalizationService.Instance.GetTranslation("Items.LuminiteFragment.Name"),
                Description = LocalizationService.Instance.GetTranslation("Items.LuminiteFragment.Description"),
                Type = ItemType.Material,
                Rarity = ItemRarity.Rare,
                Value = 120,
                Weight = 0.3f,
                MaxStackSize = 15,
                StackSize = amount,
                SpritePath = AssetPaths.Materials.LUMINITE_FRAGMENT
            };
        }

        public static Item CreateLuminite(int amount = 1)
        {
            return new Item
            {
                Name = LocalizationService.Instance.GetTranslation("Items.Luminite.Name"),
                Description = LocalizationService.Instance.GetTranslation("Items.Luminite.Description"),
                Type = ItemType.Material,
                Rarity = ItemRarity.Epic,
                Value = 250,
                Weight = 0.5f,
                MaxStackSize = 10,
                StackSize = amount,
                SpritePath = AssetPaths.Materials.LUMINITE
            };
        }

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
                SpritePath = AssetPaths.Materials.GetMaterialPath(name)
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

        public static Item? CreateMaterialByName(string materialName)
        {
            if (string.IsNullOrEmpty(materialName))
                return null;

            return materialName switch
            {
                "Дерево" => CreateWood(),
                "Палка" => CreateStick(),
                "Железный слиток" => CreateIronIngot(),
                "Золотой слиток" => CreateGoldIngot(),
                "Люминит" => CreateLuminite(),
                "Фрагмент люминита" => CreateLuminiteFragment(),
                "Трава" => CreateHerb(),
                "Фляга" => CreateFlask(),
                "Кристаллическая пыль" => CreateCrystalDust(),
                "Ткань" => CreateCloth(),
                "Перо" => CreateFeather(),
                "Экстракт яда" => CreatePoisonExtract(),
                "Порох" => CreateGunpowder(),
                "Железная руда" => CreateIronOre(),
                "Золотая руда" => CreateGoldOre(),
                _ => null
            };
        }

        public static Item? CreateItem(string itemName, int quantity = 1)
        {
            return itemName switch
            {
                // Английские названия (оригинальные)
                "Wood" => CreateWood(quantity),
                "Herbs" => CreateHerb(quantity),
                "Cloth" => CreateCloth(quantity),
                "Water Flask" => CreateFlask(quantity),
                "Iron Ore" => CreateIronOre(quantity),
                "Crystal Dust" => CreateCrystalDust(quantity),
                "Feathers" => CreateFeather(quantity),
                "Iron Ingot" => CreateIronIngot(quantity),
                "Gunpowder" => CreateGunpowder(quantity),
                "Gold Ore" => CreateGoldOre(quantity),
                "Gold Ingot" => CreateGoldIngot(quantity),
                "Poison Extract" => CreatePoisonExtract(quantity),
                "Luminite Fragment" => CreateLuminiteFragment(quantity),
                "Luminite" => CreateLuminite(quantity),
                "Healing Potion" => CreateHealingPotion(quantity),
                "Rage Potion" => CreateRagePotion(quantity),
                "Invulnerability Potion" => CreateInvulnerabilityPotion(quantity),
                "Bomb" => CreateBomb(quantity),
                "Pillow" => CreatePillow(quantity),
                "Poisoned Shuriken" => CreatePoisonedShuriken(quantity),
                "Wooden Sword" => CreateWoodenWeapon(),
                "Iron Sword" => CreateIronWeapon(),
                "Gold Sword" => CreateGoldWeapon(),
                "Luminite Sword" => CreateLuminiteWeapon(),
                
                // Русские названия (для совместимости)
                "Дерево" => CreateWood(quantity),
                "Трава" => CreateHerb(quantity),
                "Ткань" => CreateCloth(quantity),
                "Фляга" => CreateFlask(quantity),
                "Железная руда" => CreateIronOre(quantity),
                "Кристальная пыль" => CreateCrystalDust(quantity),
                "Перо" => CreateFeather(quantity),
                "Железный слиток" => CreateIronIngot(quantity),
                "Порох" => CreateGunpowder(quantity),
                "Золотая руда" => CreateGoldOre(quantity),
                "Золотой слиток" => CreateGoldIngot(quantity),
                "Извлечение яда" => CreatePoisonExtract(quantity),
                "Фрагмент люминита" => CreateLuminiteFragment(quantity),
                "Люминит" => CreateLuminite(quantity),
                "Зелье лечения" => CreateHealingPotion(quantity),
                "Зелье ярости" => CreateRagePotion(quantity),
                "Зелье неуязвимости" => CreateInvulnerabilityPotion(quantity),
                "Бомба" => CreateBomb(quantity),
                "Подушка" => CreatePillow(quantity),
                "Отравленный сюрикен" => CreatePoisonedShuriken(quantity),
                "Деревянный меч" => CreateWoodenWeapon(),
                "Железный меч" => CreateIronWeapon(),
                "Золотой меч" => CreateGoldWeapon(),
                "Люминитовый меч" => CreateLuminiteWeapon(),
                "Палка" => CreateStick(quantity),
                
                _ => null
            };
        }
    }
} 
