using System;
using System.Collections.Generic;
using System.Linq;

namespace SketchBlade.Utilities
{
    /// <summary>
    /// Централизованное управление путями к ресурсам игры.
    /// Предоставляет константы и методы для получения путей к спрайтам, изображениям и другим ресурсам.
    /// </summary>
    public static class AssetPaths
    {
        #region Base Paths
        
        /// <summary>
        /// Базовый путь к директории с ресурсами
        /// </summary>
        public const string BASE_ASSETS_PATH = "Resources/Assets/Images/";
        
        /// <summary>
        /// Путь к изображению по умолчанию (используется при отсутствии других ресурсов)
        /// </summary>
        public const string DEFAULT_IMAGE = BASE_ASSETS_PATH + "def.png";
        
        #endregion

        #region Characters
        
        /// <summary>
        /// Пути к спрайтам персонажей
        /// </summary>
        public static class Characters
        {
            private const string BASE_PATH = BASE_ASSETS_PATH + "Characters/";
            
            /// <summary>
            /// Основной спрайт игрока
            /// </summary>
            public const string PLAYER = BASE_PATH + "player.png";
            
            /// <summary>
            /// Спрайт NPC
            /// </summary>
            public const string NPC = BASE_PATH + "npc.png";
            
            /// <summary>
            /// Спрайт героя
            /// </summary>
            public const string HERO = BASE_PATH + "hero.png";

            /// <summary>
            /// Получить путь к спрайту персонажа по типу
            /// </summary>
            /// <param name="characterType">Тип персонажа (player, npc, hero)</param>
            /// <returns>Путь к спрайту персонажа</returns>
            public static string GetCharacterPath(string characterType)
            {
                return characterType?.ToLower() switch
                {
                    "player" => PLAYER,
                    "npc" => NPC,
                    "hero" => HERO,
                    _ => PLAYER
                };
            }
        }
        
        #endregion

        #region Weapons
        
        /// <summary>
        /// Пути к спрайтам оружия
        /// </summary>
        public static class Weapons
        {
            private const string BASE_PATH = BASE_ASSETS_PATH + "items/weapons/";
            
            // Мечи по материалам
            public const string WOODEN_SWORD = BASE_PATH + "wooden_sword.png";
            public const string IRON_SWORD = BASE_PATH + "iron_sword.png";
            public const string GOLDEN_SWORD = BASE_PATH + "golden_sword.png";
            public const string LUMINITE_SWORD = BASE_PATH + "luminite_sword.png";
            
            /// <summary>
            /// Получить путь к мечу по префиксу материала
            /// </summary>
            /// <param name="materialPrefix">Префикс материала (wooden, iron, golden, luminite)</param>
            /// <returns>Путь к спрайту меча</returns>
            public static string GetSwordPath(string materialPrefix)
            {
                return materialPrefix?.ToLower() switch
                {
                    "wooden" => WOODEN_SWORD,
                    "iron" => IRON_SWORD,
                    "golden" => GOLDEN_SWORD,
                    "luminite" => LUMINITE_SWORD,
                    _ => WOODEN_SWORD
                };
            }
            
            /// <summary>
            /// Получить путь к оружию по типу и материалу
            /// </summary>
            /// <param name="weaponType">Тип оружия (sword, bow, staff и т.д.)</param>
            /// <param name="material">Материал оружия</param>
            /// <returns>Путь к спрайту оружия</returns>
            public static string GetWeaponPath(string weaponType, string material)
            {
                var fileName = $"{material?.ToLower() ?? "wooden"}_{weaponType?.ToLower() ?? "sword"}.png";
                return BASE_PATH + fileName;
            }
        }
        
        #endregion

        #region Armor
        
        /// <summary>
        /// Пути к спрайтам брони и экипировки
        /// </summary>
        public static class Armor
        {
            private const string BASE_PATH = BASE_ASSETS_PATH + "items/armor/";
            
            // Шлемы
            public const string WOODEN_HELMET = BASE_PATH + "wooden_helmet.png";
            public const string IRON_HELMET = BASE_PATH + "iron_helmet.png";
            public const string GOLDEN_HELMET = BASE_PATH + "golden_helmet.png";
            public const string LUMINITE_HELMET = BASE_PATH + "luminite_helmet.png";
            
            // Нагрудники
            public const string WOODEN_CHESTPLATE = BASE_PATH + "wooden_chest.png";
            public const string IRON_CHESTPLATE = BASE_PATH + "iron_chest.png";
            public const string GOLDEN_CHESTPLATE = BASE_PATH + "golden_chest.png";
            public const string LUMINITE_CHESTPLATE = BASE_PATH + "luminite_chest.png";
            
            // Поножи
            public const string WOODEN_LEGGINGS = BASE_PATH + "wooden_legs.png";
            public const string IRON_LEGGINGS = BASE_PATH + "iron_legs.png";
            public const string GOLDEN_LEGGINGS = BASE_PATH + "golden_legs.png";
            public const string LUMINITE_LEGGINGS = BASE_PATH + "luminite_legs.png";
            
            // Щиты
            public const string WOODEN_SHIELD = BASE_PATH + "wooden_shield.png";
            public const string IRON_SHIELD = BASE_PATH + "iron_shield.png";
            public const string GOLDEN_SHIELD = BASE_PATH + "golden_shield.png";
            public const string LUMINITE_SHIELD = BASE_PATH + "luminite_shield.png";
            
            /// <summary>
            /// Получить путь к броне по материалу и типу
            /// </summary>
            /// <param name="materialPrefix">Префикс материала (wooden, iron, golden, luminite)</param>
            /// <param name="armorType">Тип брони (helmet, chest, legs, shield)</param>
            /// <returns>Путь к спрайту брони</returns>
            public static string GetArmorPath(string materialPrefix, string armorType)
            {
                var material = materialPrefix?.ToLower() ?? "wooden";
                var type = armorType?.ToLower() ?? "chest";
                
                return $"{BASE_PATH}{material}_{type}.png";
            }
        }
        
        #endregion

        #region Consumables
        
        /// <summary>
        /// Пути к спрайтам расходуемых предметов
        /// </summary>
        public static class Consumables
        {
            private const string BASE_PATH = BASE_ASSETS_PATH + "items/consumables/";
            
            // Зелья
            public const string HEALING_POTION = BASE_PATH + "healing_potion.png";
            public const string RAGE_POTION = BASE_PATH + "rage_potion.png";
            public const string INVULNERABILITY_POTION = BASE_PATH + "invulnerability_potion.png";
            
            // Инструменты и разное
            public const string BOMB = BASE_PATH + "bomb.png";
            public const string PILLOW = BASE_PATH + "pillow.png";
            public const string POISONED_SHURIKEN = BASE_PATH + "poisoned_shuriken.png";
            
            /// <summary>
            /// Получить путь к расходуемому предмету по названию
            /// </summary>
            /// <param name="itemName">Название предмета</param>
            /// <returns>Путь к спрайту предмета</returns>
            public static string GetConsumablePath(string itemName)
            {
                var normalizedName = itemName?.ToLower()?.Replace(" ", "_") ?? "";
                
                return normalizedName switch
                {
                    "healing_potion" or "зелье_лечения" => HEALING_POTION,
                    "rage_potion" or "зелье_ярости" => RAGE_POTION,
                    "invulnerability_potion" or "зелье_неуязвимости" => INVULNERABILITY_POTION,
                    "bomb" or "бомба" => BOMB,
                    "pillow" or "подушка" => PILLOW,
                    "poisoned_shuriken" or "отравленная_звездочка" => POISONED_SHURIKEN,
                    _ => BASE_PATH + normalizedName + ".png"
                };
            }
        }
        
        #endregion

        #region Materials
        
        /// <summary>
        /// Пути к спрайтам материалов и ресурсов
        /// </summary>
        public static class Materials
        {
            private const string BASE_PATH = BASE_ASSETS_PATH + "items/materials/";
            
            // Базовые материалы
            public const string WOOD = BASE_PATH + "wood.png";
            public const string STICK = BASE_PATH + "stick.png";
            public const string CLOTH = BASE_PATH + "cloth.png";
            public const string HERB = BASE_PATH + "herb.png";
            public const string FEATHER = BASE_PATH + "feather.png";
            public const string FLASK = BASE_PATH + "flask.png";
            
            // Руды и слитки
            public const string IRON_ORE = BASE_PATH + "iron_ore.png";
            public const string IRON_INGOT = BASE_PATH + "iron_ingot.png";
            public const string GOLD_ORE = BASE_PATH + "gold_ore.png";
            public const string GOLD_INGOT = BASE_PATH + "gold_ingot.png";
            
            // Редкие материалы
            public const string CRYSTAL_DUST = BASE_PATH + "crystal_dust.png";
            public const string GUNPOWDER = BASE_PATH + "gunpowder.png";
            public const string POISON_EXTRACT = BASE_PATH + "poison_extract.png";
            public const string LUMINITE_FRAGMENT = BASE_PATH + "luminite_fragment.png";
            public const string LUMINITE = BASE_PATH + "luminite.png";
            
            /// <summary>
            /// Получить путь к материалу по названию
            /// </summary>
            /// <param name="materialName">Название материала</param>
            /// <returns>Путь к спрайту материала</returns>
            public static string GetMaterialPath(string materialName)
            {
                var normalizedName = materialName?.ToLower()?.Replace(" ", "_") ?? "";
                
                return normalizedName switch
                {
                    "wood" or "дерево" => WOOD,
                    "stick" or "палка" => STICK,
                    "cloth" or "ткань" => CLOTH,
                    "herb" or "трава" => HERB,
                    "feather" or "перо" => FEATHER,
                    "flask" or "фляга" => FLASK,
                    "iron_ore" or "железная_руда" => IRON_ORE,
                    "iron_ingot" or "железный_слиток" => IRON_INGOT,
                    "gold_ore" or "золотая_руда" => GOLD_ORE,
                    "gold_ingot" or "золотой_слиток" => GOLD_INGOT,
                    "crystal_dust" or "кристальная_пыль" => CRYSTAL_DUST,
                    "gunpowder" or "порох" => GUNPOWDER,
                    "poison_extract" or "ядовитый_экстракт" => POISON_EXTRACT,
                    "luminite_fragment" or "фрагмент_люминита" => LUMINITE_FRAGMENT,
                    "luminite" or "люминит" => LUMINITE,
                    _ => BASE_PATH + normalizedName + ".png"
                };
            }
        }
        
        #endregion

        #region Locations
        
        /// <summary>
        /// Пути к спрайтам локаций
        /// </summary>
        public static class Locations
        {
            private const string BASE_PATH = BASE_ASSETS_PATH + "Locations/";
            
            public const string VILLAGE = BASE_PATH + "village.png";
            public const string FOREST = BASE_PATH + "forest.png";
            public const string CAVE = BASE_PATH + "cave.png";
            public const string RUINS = BASE_PATH + "ruins.png";
            public const string CASTLE = BASE_PATH + "castle.png";
            
            /// <summary>
            /// Получить путь к локации по типу
            /// </summary>
            /// <param name="locationType">Тип локации</param>
            /// <returns>Путь к спрайту локации</returns>
            public static string GetLocationPath(string locationType)
            {
                return locationType?.ToLower() switch
                {
                    "village" => VILLAGE,
                    "forest" => FOREST,
                    "cave" => CAVE,
                    "ruins" => RUINS,
                    "castle" => CASTLE,
                    _ => VILLAGE
                };
            }
            
            /// <summary>
            /// Получить путь к локации по названию
            /// </summary>
            /// <param name="locationName">Название локации</param>
            /// <returns>Путь к спрайту локации</returns>
            public static string GetLocationPathByName(string locationName)
            {
                return locationName?.ToLower() switch
                {
                    "village" or "деревня" => VILLAGE,
                    "forest" or "лес" => FOREST,
                    "cave" or "пещера" => CAVE,
                    "ancient ruins" or "руины" => RUINS,
                    "dark castle" or "замок" => CASTLE,
                    _ => VILLAGE
                };
            }
        }
        
        #endregion

        #region Enemies
        
        /// <summary>
        /// Пути к спрайтам врагов
        /// </summary>
        public static class Enemies
        {
            private const string BASE_PATH = BASE_ASSETS_PATH + "Enemies/";
            
            // Враги по локациям
            public const string VILLAGE_ENEMY = BASE_PATH + "village_enemy.png";
            public const string FOREST_ENEMY = BASE_PATH + "forest_enemy.png";
            public const string CAVE_ENEMY = BASE_PATH + "cave_enemy.png";
            public const string RUINS_ENEMY = BASE_PATH + "ruins_enemy.png";
            public const string CASTLE_ENEMY = BASE_PATH + "castle_enemy.png";
            
            // Герои локаций
            public const string VILLAGE_HERO = BASE_PATH + "village_hero.png";
            public const string FOREST_HERO = BASE_PATH + "forest_hero.png";
            public const string CAVE_HERO = BASE_PATH + "cave_hero.png";
            public const string RUINS_HERO = BASE_PATH + "ruins_hero.png";
            public const string CASTLE_HERO = BASE_PATH + "castle_hero.png";
            
            /// <summary>
            /// Получить путь к спрайту врага по типу
            /// </summary>
            /// <param name="enemyType">Тип врага</param>
            /// <returns>Путь к спрайту врага</returns>
            public static string GetEnemyPath(string enemyType)
            {
                return enemyType?.ToLower() switch
                {
                    "bandit" or "thief" => VILLAGE_ENEMY,
                    "wolf" or "bear" or "goblin" => FOREST_ENEMY,
                    "orc" or "troll" or "spider" => CAVE_ENEMY,
                    "skeleton" or "wraith" or "golem" => RUINS_ENEMY,
                    "knight" or "demon" or "dragon" => CASTLE_ENEMY,
                    _ => VILLAGE_ENEMY
                };
            }
            
            /// <summary>
            /// Получить путь к спрайту героя по названию локации
            /// </summary>
            /// <param name="locationName">Название локации</param>
            /// <returns>Путь к спрайту героя</returns>
            public static string GetHeroPath(string locationName)
            {
                return locationName?.ToLower() switch
                {
                    "village" => VILLAGE_HERO,
                    "forest" => FOREST_HERO,
                    "cave" => CAVE_HERO,
                    "ruins" => RUINS_HERO,
                    "castle" => CASTLE_HERO,
                    _ => VILLAGE_HERO
                };
            }
            
            /// <summary>
            /// Получить путь к врагу по типу локации
            /// </summary>
            /// <param name="locationType">Тип локации</param>
            /// <param name="isHero">Является ли враг героем локации</param>
            /// <returns>Путь к спрайту врага</returns>
            public static string GetEnemyByLocationType(string locationType, bool isHero = false)
            {
                var baseType = locationType?.ToLower() ?? "village";
                
                if (isHero)
                {
                    return GetHeroPath(baseType);
                }
                
                return baseType switch
                {
                    "village" => VILLAGE_ENEMY,
                    "forest" => FOREST_ENEMY,
                    "cave" => CAVE_ENEMY,
                    "ruins" => RUINS_ENEMY,
                    "castle" => CASTLE_ENEMY,
                    _ => VILLAGE_ENEMY
                };
            }
            
            /// <summary>
            /// Получить путь к спрайту врага по имени
            /// </summary>
            /// <param name="enemyName">Имя врага</param>
            /// <returns>Путь к спрайту врага</returns>
            public static string GetEnemyPathByName(string enemyName)
            {
                var normalizedName = enemyName?.ToLower() ?? "";
                
                // Карта имен врагов к спрайтам
                return normalizedName switch
                {
                    "village_enemy" or "village_hero" => VILLAGE_ENEMY,
                    "forest_enemy" or "forest_hero" => FOREST_ENEMY,
                    "cave_enemy" or "cave_hero" => CAVE_ENEMY,
                    "ruins_enemy" or "ruins_hero" => RUINS_ENEMY,
                    "castle_enemy" or "castle_hero" => CASTLE_ENEMY,
                    _ => BASE_PATH + normalizedName + ".png"
                };
            }
        }
        
        #endregion

        #region UI
        
        /// <summary>
        /// Пути к элементам интерфейса
        /// </summary>
        public static class UI
        {
            private const string BASE_PATH = BASE_ASSETS_PATH + "UI/";
            
            public const string BUTTON_NORMAL = BASE_PATH + "button_normal.png";
            public const string BUTTON_HOVER = BASE_PATH + "button_hover.png";
            public const string BUTTON_PRESSED = BASE_PATH + "button_pressed.png";
            public const string BACKGROUND = BASE_PATH + "background.png";
            public const string PANEL = BASE_PATH + "panel.png";
            public const string INVENTORY_SLOT = BASE_PATH + "inventory_slot.png";
            
            /// <summary>
            /// Получить путь к элементу UI
            /// </summary>
            /// <param name="elementName">Название элемента UI</param>
            /// <returns>Путь к спрайту элемента</returns>
            public static string GetUIPath(string elementName)
            {
                return elementName?.ToLower() switch
                {
                    "button_normal" => BUTTON_NORMAL,
                    "button_hover" => BUTTON_HOVER,
                    "button_pressed" => BUTTON_PRESSED,
                    "background" => BACKGROUND,
                    "panel" => PANEL,
                    "inventory_slot" => INVENTORY_SLOT,
                    _ => BASE_PATH + elementName + ".png"
                };
            }
        }
        
        #endregion

        #region Utility Methods
        
        /// <summary>
        /// Получить список всех критически важных ресурсов для предзагрузки
        /// </summary>
        /// <returns>Список путей к критически важным ресурсам</returns>
        public static IEnumerable<string> GetCriticalAssetPaths()
        {
            return new[]
            {
                // Обязательные изображения
                DEFAULT_IMAGE,
                
                // Основные персонажи
                Characters.PLAYER,
                Characters.NPC,
                Characters.HERO,
                
                // Базовое оружие
                Weapons.WOODEN_SWORD,
                
                // Базовые расходники
                Consumables.HEALING_POTION,
                
                // Основные материалы
                Materials.WOOD,
                Materials.HERB,
                
                // Стартовая локация
                Locations.VILLAGE,
                
                // Базовые враги
                Enemies.VILLAGE_ENEMY,
                Enemies.VILLAGE_HERO
            };
        }
        
        /// <summary>
        /// Проверить, является ли ресурс критически важным
        /// </summary>
        /// <param name="assetPath">Путь к ресурсу</param>
        /// <returns>true, если ресурс критически важен</returns>
        public static bool IsCriticalAsset(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return false;
                
            return GetCriticalAssetPaths().Contains(assetPath);
        }
        
        /// <summary>
        /// Проверить существование пути к ресурсу в файловой системе
        /// </summary>
        /// <param name="assetPath">Путь к ресурсу</param>
        /// <returns>true, если файл существует</returns>
        public static bool AssetExists(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return false;
                
            try
            {
                var fullPath = System.IO.Path.Combine(
                    System.AppDomain.CurrentDomain.BaseDirectory, 
                    assetPath
                );
                return System.IO.File.Exists(fullPath);
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Получить альтернативный путь для ресурса (fallback)
        /// </summary>
        /// <param name="originalPath">Оригинальный путь</param>
        /// <returns>Альтернативный путь или путь по умолчанию</returns>
        public static string GetFallbackPath(string originalPath)
        {
            if (string.IsNullOrEmpty(originalPath))
                return DEFAULT_IMAGE;
                
            // Если оригинальный путь существует, возвращаем его
            if (AssetExists(originalPath))
                return originalPath;
                
            // Пытаемся найти альтернативы по типу
            if (originalPath.Contains("weapons/"))
                return Weapons.WOODEN_SWORD;
            else if (originalPath.Contains("armor/"))
                return Armor.WOODEN_HELMET;
            else if (originalPath.Contains("consumables/"))
                return Consumables.HEALING_POTION;
            else if (originalPath.Contains("materials/"))
                return Materials.WOOD;
            else if (originalPath.Contains("Characters/"))
                return Characters.PLAYER;
            else if (originalPath.Contains("Locations/"))
                return Locations.VILLAGE;
            else if (originalPath.Contains("Enemies/"))
                return Enemies.VILLAGE_ENEMY;
                
            // Возвращаем изображение по умолчанию
            return DEFAULT_IMAGE;
        }
        
        #endregion
    }
} 