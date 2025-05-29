using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

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
        /// Базовый путь к директории с ресурсами (внешняя папка Resources)
        /// </summary>
        public static string BASE_ASSETS_PATH => Path.Combine(ResourcePathManager.ImagesPath, "") + Path.DirectorySeparatorChar;
        
        /// <summary>
        /// Путь к изображению по умолчанию (используется при отсутствии других ресурсов)
        /// </summary>
        public static string DEFAULT_IMAGE => Path.Combine(ResourcePathManager.ImagesPath, "def.png");
        
        #endregion

        #region Characters
        
        /// <summary>
        /// Пути к спрайтам персонажей
        /// </summary>
        public static class Characters
        {
            private static string BASE_PATH => Path.Combine(ResourcePathManager.ImagesPath, "Characters") + Path.DirectorySeparatorChar;
            
            /// <summary>
            /// Основной спрайт игрока
            /// </summary>
            public static string PLAYER => Path.Combine(BASE_PATH, "player.png");
            
            /// <summary>
            /// Спрайт NPC
            /// </summary>
            public static string NPC => Path.Combine(BASE_PATH, "npc.png");
            
            /// <summary>
            /// Спрайт героя
            /// </summary>
            public static string HERO => Path.Combine(BASE_PATH, "hero.png");

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
            private static string BASE_PATH => Path.Combine(ResourcePathManager.ImagesPath, "items", "weapons") + Path.DirectorySeparatorChar;
            
            // Мечи по материалам
            public static string WOODEN_SWORD => Path.Combine(BASE_PATH, "wooden_sword.png");
            public static string IRON_SWORD => Path.Combine(BASE_PATH, "iron_sword.png");
            public static string GOLDEN_SWORD => Path.Combine(BASE_PATH, "golden_sword.png");
            public static string LUMINITE_SWORD => Path.Combine(BASE_PATH, "luminite_sword.png");
            
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
                return Path.Combine(BASE_PATH, fileName);
            }
        }
        
        #endregion

        #region Armor
        
        /// <summary>
        /// Пути к спрайтам брони и экипировки
        /// </summary>
        public static class Armor
        {
            private static string BASE_PATH => Path.Combine(ResourcePathManager.ImagesPath, "items", "armor") + Path.DirectorySeparatorChar;
            
            // Шлемы
            public static string WOODEN_HELMET => Path.Combine(BASE_PATH, "wooden_helmet.png");
            public static string IRON_HELMET => Path.Combine(BASE_PATH, "iron_helmet.png");
            public static string GOLDEN_HELMET => Path.Combine(BASE_PATH, "golden_helmet.png");
            public static string LUMINITE_HELMET => Path.Combine(BASE_PATH, "luminite_helmet.png");
            
            // Нагрудники
            public static string WOODEN_CHESTPLATE => Path.Combine(BASE_PATH, "wooden_chest.png");
            public static string IRON_CHESTPLATE => Path.Combine(BASE_PATH, "iron_chest.png");
            public static string GOLDEN_CHESTPLATE => Path.Combine(BASE_PATH, "golden_chest.png");
            public static string LUMINITE_CHESTPLATE => Path.Combine(BASE_PATH, "luminite_chest.png");
            
            // Поножи
            public static string WOODEN_LEGGINGS => Path.Combine(BASE_PATH, "wooden_legs.png");
            public static string IRON_LEGGINGS => Path.Combine(BASE_PATH, "iron_legs.png");
            public static string GOLDEN_LEGGINGS => Path.Combine(BASE_PATH, "golden_legs.png");
            public static string LUMINITE_LEGGINGS => Path.Combine(BASE_PATH, "luminite_legs.png");
            
            // Щиты
            public static string WOODEN_SHIELD => Path.Combine(BASE_PATH, "wooden_shield.png");
            public static string IRON_SHIELD => Path.Combine(BASE_PATH, "iron_shield.png");
            public static string GOLDEN_SHIELD => Path.Combine(BASE_PATH, "golden_shield.png");
            public static string LUMINITE_SHIELD => Path.Combine(BASE_PATH, "luminite_shield.png");
            
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
                
                return Path.Combine(BASE_PATH, $"{material}_{type}.png");
            }
        }
        
        #endregion

        #region Consumables
        
        /// <summary>
        /// Пути к спрайтам расходуемых предметов
        /// </summary>
        public static class Consumables
        {
            private static string BASE_PATH => Path.Combine(ResourcePathManager.ImagesPath, "items", "consumables") + Path.DirectorySeparatorChar;
            
            // Зелья
            public static string HEALING_POTION => Path.Combine(BASE_PATH, "healing_potion.png");
            public static string RAGE_POTION => Path.Combine(BASE_PATH, "rage_potion.png");
            public static string INVULNERABILITY_POTION => Path.Combine(BASE_PATH, "invulnerability_potion.png");
            
            // Инструменты и разное
            public static string BOMB => Path.Combine(BASE_PATH, "bomb.png");
            public static string PILLOW => Path.Combine(BASE_PATH, "pillow.png");
            public static string POISONED_SHURIKEN => Path.Combine(BASE_PATH, "poisoned_shuriken.png");
            
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
                    _ => Path.Combine(BASE_PATH, normalizedName + ".png")
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
            private static string BASE_PATH => Path.Combine(ResourcePathManager.ImagesPath, "items", "materials") + Path.DirectorySeparatorChar;
            
            // Базовые материалы
            public static string WOOD => Path.Combine(BASE_PATH, "wood.png");
            public static string STICK => Path.Combine(BASE_PATH, "stick.png");
            public static string CLOTH => Path.Combine(BASE_PATH, "cloth.png");
            public static string HERB => Path.Combine(BASE_PATH, "herb.png");
            public static string FEATHER => Path.Combine(BASE_PATH, "feather.png");
            public static string FLASK => Path.Combine(BASE_PATH, "flask.png");
            
            // Металлы
            public static string IRON_ORE => Path.Combine(BASE_PATH, "iron_ore.png");
            public static string IRON_INGOT => Path.Combine(BASE_PATH, "iron_ingot.png");
            public static string GOLD_ORE => Path.Combine(BASE_PATH, "gold_ore.png");
            public static string GOLD_INGOT => Path.Combine(BASE_PATH, "gold_ingot.png");
            
            // Специальные материалы
            public static string CRYSTAL_DUST => Path.Combine(BASE_PATH, "crystal_dust.png");
            public static string GUNPOWDER => Path.Combine(BASE_PATH, "gunpowder.png");
            public static string POISON_EXTRACT => Path.Combine(BASE_PATH, "poison_extract.png");
            public static string LUMINITE_FRAGMENT => Path.Combine(BASE_PATH, "luminite_fragment.png");
            public static string LUMINITE => Path.Combine(BASE_PATH, "luminite.png");
            
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
                    "flask" or "колба" => FLASK,
                    "iron_ore" or "железная_руда" => IRON_ORE,
                    "iron_ingot" or "железный_слиток" => IRON_INGOT,
                    "gold_ore" or "золотая_руда" => GOLD_ORE,
                    "gold_ingot" or "золотой_слиток" => GOLD_INGOT,
                    "crystal_dust" or "кристальная_пыль" => CRYSTAL_DUST,
                    "gunpowder" or "порох" => GUNPOWDER,
                    "poison_extract" or "ядовитый_экстракт" => POISON_EXTRACT,
                    "luminite_fragment" or "фрагмент_люминита" => LUMINITE_FRAGMENT,
                    "luminite" or "люминит" => LUMINITE,
                    _ => Path.Combine(BASE_PATH, normalizedName + ".png")
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
            private static string BASE_PATH => Path.Combine(ResourcePathManager.ImagesPath, "Locations") + Path.DirectorySeparatorChar;
            
            public static string VILLAGE => Path.Combine(BASE_PATH, "village.png");
            public static string FOREST => Path.Combine(BASE_PATH, "forest.png");
            public static string CAVE => Path.Combine(BASE_PATH, "cave.png");
            public static string RUINS => Path.Combine(BASE_PATH, "ruins.png");
            public static string CASTLE => Path.Combine(BASE_PATH, "castle.png");
            
            /// <summary>
            /// Получить путь к спрайту локации по типу
            /// </summary>
            /// <param name="locationType">Тип локации (village, forest, cave, ruins, castle)</param>
            /// <returns>Путь к спрайту локации</returns>
            public static string GetLocationPath(string locationType)
            {
                return locationType?.ToLower() switch
                {
                    "village" or "деревня" => VILLAGE,
                    "forest" or "лес" => FOREST,
                    "cave" or "пещера" => CAVE,
                    "ruins" or "руины" => RUINS,
                    "castle" or "замок" => CASTLE,
                    _ => VILLAGE
                };
            }
            
            /// <summary>
            /// Получить путь к спрайту локации по имени
            /// </summary>
            /// <param name="locationName">Имя локации</param>
            /// <returns>Путь к спрайту локации</returns>
            public static string GetLocationPathByName(string locationName)
            {
                var normalizedName = locationName?.ToLower() ?? "";
                
                if (normalizedName.Contains("village") || normalizedName.Contains("деревн"))
                    return VILLAGE;
                if (normalizedName.Contains("forest") || normalizedName.Contains("лес"))
                    return FOREST;
                if (normalizedName.Contains("cave") || normalizedName.Contains("пещер"))
                    return CAVE;
                if (normalizedName.Contains("ruins") || normalizedName.Contains("руин"))
                    return RUINS;
                if (normalizedName.Contains("castle") || normalizedName.Contains("замок"))
                    return CASTLE;
                    
                return VILLAGE;
            }
        }
        
        #endregion

        #region Enemies
        
        /// <summary>
        /// Пути к спрайтам врагов и героев
        /// </summary>
        public static class Enemies
        {
            private static string BASE_PATH => Path.Combine(ResourcePathManager.ImagesPath, "Enemies") + Path.DirectorySeparatorChar;
            
            // Враги по локациям
            public static string VILLAGE_ENEMY => Path.Combine(BASE_PATH, "village_enemy.png");
            public static string FOREST_ENEMY => Path.Combine(BASE_PATH, "forest_enemy.png");
            public static string CAVE_ENEMY => Path.Combine(BASE_PATH, "cave_enemy.png");
            public static string RUINS_ENEMY => Path.Combine(BASE_PATH, "ruins_enemy.png");
            public static string CASTLE_ENEMY => Path.Combine(BASE_PATH, "castle_enemy.png");
            
            // Герои по локациям
            public static string VILLAGE_HERO => Path.Combine(BASE_PATH, "village_hero.png");
            public static string FOREST_HERO => Path.Combine(BASE_PATH, "forest_hero.png");
            public static string CAVE_HERO => Path.Combine(BASE_PATH, "cave_hero.png");
            public static string RUINS_HERO => Path.Combine(BASE_PATH, "ruins_hero.png");
            public static string CASTLE_HERO => Path.Combine(BASE_PATH, "castle_hero.png");
            
            /// <summary>
            /// Получить путь к спрайту врага по типу
            /// </summary>
            /// <param name="enemyType">Тип врага</param>
            /// <returns>Путь к спрайту врага</returns>
            public static string GetEnemyPath(string enemyType)
            {
                return enemyType?.ToLower() switch
                {
                    "village_enemy" => VILLAGE_ENEMY,
                    "forest_enemy" => FOREST_ENEMY,
                    "cave_enemy" => CAVE_ENEMY,
                    "ruins_enemy" => RUINS_ENEMY,
                    "castle_enemy" => CASTLE_ENEMY,
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
                var normalizedLocation = locationName?.ToLower() ?? "";
                
                if (normalizedLocation.Contains("village") || normalizedLocation.Contains("деревн"))
                    return VILLAGE_HERO;
                if (normalizedLocation.Contains("forest") || normalizedLocation.Contains("лес"))
                    return FOREST_HERO;
                if (normalizedLocation.Contains("cave") || normalizedLocation.Contains("пещер"))
                    return CAVE_HERO;
                if (normalizedLocation.Contains("ruins") || normalizedLocation.Contains("руин"))
                    return RUINS_HERO;
                if (normalizedLocation.Contains("castle") || normalizedLocation.Contains("замок"))
                    return CASTLE_HERO;
                    
                return VILLAGE_HERO;
            }
            
            /// <summary>
            /// Получить путь к врагу или герою по типу локации
            /// </summary>
            /// <param name="locationType">Тип локации</param>
            /// <param name="isHero">Является ли героем</param>
            /// <returns>Путь к спрайту</returns>
            public static string GetEnemyByLocationType(string locationType, bool isHero = false)
            {
                var location = locationType?.ToLower() ?? "village";
                
                return location switch
                {
                    "village" => isHero ? VILLAGE_HERO : VILLAGE_ENEMY,
                    "forest" => isHero ? FOREST_HERO : FOREST_ENEMY,
                    "cave" => isHero ? CAVE_HERO : CAVE_ENEMY,
                    "ruins" => isHero ? RUINS_HERO : RUINS_ENEMY,
                    "castle" => isHero ? CASTLE_HERO : CASTLE_ENEMY,
                    _ => isHero ? VILLAGE_HERO : VILLAGE_ENEMY
                };
            }
            
            /// <summary>
            /// Получить путь к врагу по имени
            /// </summary>
            /// <param name="enemyName">Имя врага</param>
            /// <returns>Путь к спрайту врага</returns>
            public static string GetEnemyPathByName(string enemyName)
            {
                var normalizedName = enemyName?.ToLower() ?? "";
                
                // Проверяем по типу локации в имени
                if (normalizedName.Contains("village") || normalizedName.Contains("деревн"))
                    return normalizedName.Contains("hero") || normalizedName.Contains("герой") ? VILLAGE_HERO : VILLAGE_ENEMY;
                if (normalizedName.Contains("forest") || normalizedName.Contains("лес"))
                    return normalizedName.Contains("hero") || normalizedName.Contains("герой") ? FOREST_HERO : FOREST_ENEMY;
                if (normalizedName.Contains("cave") || normalizedName.Contains("пещер"))
                    return normalizedName.Contains("hero") || normalizedName.Contains("герой") ? CAVE_HERO : CAVE_ENEMY;
                if (normalizedName.Contains("ruins") || normalizedName.Contains("руин"))
                    return normalizedName.Contains("hero") || normalizedName.Contains("герой") ? RUINS_HERO : RUINS_ENEMY;
                if (normalizedName.Contains("castle") || normalizedName.Contains("замок"))
                    return normalizedName.Contains("hero") || normalizedName.Contains("герой") ? CASTLE_HERO : CASTLE_ENEMY;
                    
                return VILLAGE_ENEMY;
            }
        }
        
        #endregion

        #region UI
        
        /// <summary>
        /// Пути к спрайтам UI элементов
        /// </summary>
        public static class UI
        {
            private static string BASE_PATH => Path.Combine(ResourcePathManager.ImagesPath, "UI") + Path.DirectorySeparatorChar;
            
            public static string BUTTON_NORMAL => Path.Combine(BASE_PATH, "button_normal.png");
            public static string BUTTON_HOVER => Path.Combine(BASE_PATH, "button_hover.png");
            public static string BUTTON_PRESSED => Path.Combine(BASE_PATH, "button_pressed.png");
            public static string BACKGROUND => Path.Combine(BASE_PATH, "background.png");
            public static string PANEL => Path.Combine(BASE_PATH, "panel.png");
            public static string INVENTORY_SLOT => Path.Combine(BASE_PATH, "inventory_slot.png");
            
            /// <summary>
            /// Получить путь к UI элементу по названию
            /// </summary>
            /// <param name="elementName">Название элемента</param>
            /// <returns>Путь к спрайту UI элемента</returns>
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
                    _ => Path.Combine(BASE_PATH, elementName + ".png")
                };
            }
        }
        
        #endregion

        #region Utility Methods
        
        /// <summary>
        /// Получить список критически важных ресурсов для предзагрузки
        /// </summary>
        /// <returns>Коллекция путей к критически важным ресурсам</returns>
        public static IEnumerable<string> GetCriticalAssetPaths()
        {
            yield return DEFAULT_IMAGE;
            yield return Characters.PLAYER;
            yield return Weapons.WOODEN_SWORD;
            yield return Armor.WOODEN_CHESTPLATE;
            yield return Consumables.HEALING_POTION;
            yield return Materials.WOOD;
            yield return Locations.VILLAGE;
            yield return Enemies.VILLAGE_ENEMY;
            yield return UI.BUTTON_NORMAL;
            yield return UI.INVENTORY_SLOT;
        }
        
        /// <summary>
        /// Проверить, является ли ресурс критически важным
        /// </summary>
        /// <param name="assetPath">Путь к ресурсу</param>
        /// <returns>True, если ресурс критически важен</returns>
        public static bool IsCriticalAsset(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return false;
                
            var criticalAssets = GetCriticalAssetPaths();
            return criticalAssets.Any(critical => string.Equals(critical, assetPath, StringComparison.OrdinalIgnoreCase));
        }
        
        /// <summary>
        /// Проверить существование ресурса
        /// </summary>
        /// <param name="assetPath">Путь к ресурсу</param>
        /// <returns>True, если ресурс существует</returns>
        public static bool AssetExists(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return false;
                
            try
            {
                return File.Exists(assetPath);
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Получить резервный путь для отсутствующего ресурса
        /// </summary>
        /// <param name="originalPath">Оригинальный путь к ресурсу</param>
        /// <returns>Путь к резервному ресурсу</returns>
        public static string GetFallbackPath(string originalPath)
        {
            if (string.IsNullOrEmpty(originalPath))
                return DEFAULT_IMAGE;
                
            // Если оригинальный ресурс существует, возвращаем его
            if (AssetExists(originalPath))
                return originalPath;
                
            // Возвращаем изображение по умолчанию
            return DEFAULT_IMAGE;
        }
        
        #endregion
    }
} 