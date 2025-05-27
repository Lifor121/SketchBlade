# SketchBlade

A fantasy RPG game built with C# and WPF that features inventory management, world exploration, and turn-based combat.

## Game Overview

SketchBlade is a single-window RPG game where players navigate through various locations, fight enemies, collect loot, and upgrade their character. The game features a responsive UI design that scales well on different screen sizes.

## Game Screens

### Main Menu

The starting screen features:
- **New Game** button - Starts a new game
- **Continue** button - Loads saved game (inactive if no save exists)
- **Options** button - Opens game settings
- **Exit** button - Exits the game

### Inventory Screen

The main character management screen includes:
- Character display with equipped items (player.png)
- Equipment slots:
  - Helmet slot
  - Chestplate slot
  - Leggings slot
  - Weapon slot (for swords)
  - Shield slot
- Quick access slots (2) for consumable items like healing potions, rage potions, invulnerability potions, bombs, pillows, and poisoned shurikens
- Inventory grid (15 slots) for storing items
- Trash slot (items placed here disappear when leaving the screen or when replaced)
- Character stats display showing:
  - Health
  - Defense
  - Damage
- Simplified crafting panel showing available craftable items

All slots share the same functionality. Players can move items between inventory and equipment slots (with type checking for equipment slots). Consumable items can stack, while armor and weapons cannot.

### World Map Screen

The world exploration screen shows:
- Visual representation of the current location (village, forest, cave, ancient ruins, castle)
- Location name and description
- Navigation arrows for moving between locations
- Detailed information about each location including whether the location is locked or unlocked
- Action buttons:
  - "Travel" - Enter the location
  - "Fight Hero" - Battle the location's boss
  - "Fight Mobs" - Battle regular enemies in the location
- Location status indicators:
  - Completed (green)
  - Available (blue)
  - Locked (gray)
  - Selected (yellow outline)

### Battle Screen

The combat screen includes:
- Player character on the left
- Enemies on the right (1-3 enemies - stronger if solo, weaker if multiple)
- Action buttons:
  - Attack - When clicked, allows targeting an enemy if multiple are present
  - Item - Use consumable items during battle
  - Flee - Attempt to escape from battle

The battle system features:
- Advanced combat mechanics with various types of attacks
- Special abilities for boss enemies with unique effects
- Critical hits with visual effects and increased damage
- Area-of-effect attacks for certain enemy types
- Detailed damage calculation based on equipment, character stats, and random factors

All attacks feature various animations including:
- Character movement animations with acceleration
- Damage display animations
- Special effect animations for critical hits
- Victory and defeat animations

#### Enemy Abilities
Enemies may use special abilities during combat:
- Regular enemies have a 20% chance to use special abilities
- Hero (boss) enemies have a 30% chance to use special abilities 
- Special ability chance increases by 20% when enemy health is below 50%
- Some abilities can target multiple players with area-of-effect attacks
- Special abilities deal 20-60% more damage than normal attacks

#### Damage Calculation
Damage is calculated using the following formula:
- Base damage = attacker's attack - (defender's defense / 2)
- Random factor of ±20% is applied to add variation
- Critical hits deal 50% more damage and occur with 10% probability
- Minimum damage is always 1, regardless of defense

### Simplified Crafting System

The game includes a simplified material-based crafting system:
- Crafting panel integrated directly into the inventory screen
- List of available craftable items displayed as clickable icons
- Simple material requirements - only requires correct quantity of materials in inventory
- No crafting grid or pattern requirements
- Automatic crafting when materials are available

Players can click on available craftable items to create them instantly if they have the required materials. The system automatically deducts materials from inventory and adds the crafted item.

### Settings Screen

The settings menu allows players to customize their game experience:
- Language selection (Russian, English)
- Difficulty settings (Easy, Normal, Hard)
- UI scale adjustment
- Display preferences for item descriptions and combat damage numbers

## Game Progression

Each location has a corresponding hero (boss). Defeating this hero marks the location as completed and allows progression to the next location. The difficulty increases with each location, providing a progressively challenging experience. The loot from battles varies by location:

- **Village**: Basic materials like wood, herbs, cloth, and water flasks
- **Forest**: Common materials from Village plus iron ore, crystal dust, and feathers
- **Cave**: Common iron materials, gunpowder, and occasional gold ore
- **Ruins**: Iron and gold materials, poison extract, and rare luminite fragments
- **Castle**: Gold materials, luminite fragments, and luminite

The quality of materials increases with each location, and players must craft their own equipment to advance through the game.

## Technical Details

### Sprite Loading

If a required sprite isn't found, the game loads def.png as a fallback, which is guaranteed to exist.

### Item Types

- **Materials**: Various crafting materials (Wood, Iron Ore, Iron Ingot, Gold Ore, Gold Ingot, Luminite Fragment, Luminite, etc.)
- **Armor**: Helmets, chestplates, leggings
- **Weapons**: Swords (Wooden, Iron, Gold, and Luminite)
- **Shields**: Protective shields made of various materials (Iron, Gold, Luminite)
- **Consumables**: 
  - Healing potions (restore health)
  - Rage potions (temporarily increase attack)
  - Invulnerability potions (temporarily increase defense)
  - Bombs (deal area damage to enemies)
  - Pillows (temporarily stun enemies)
  - Poisoned shurikens (deal damage over time)

### Equipment Slot Types

- **Head**: For helmet armor
- **Chest**: For chestplate armor
- **Legs**: For leggings armor
- **Weapon**: For main-hand weapons
- **Shield**: For protective shields

### Loot System

The game features a sophisticated material drop system:
- Material rarity tiers (Common, Uncommon, Rare, Epic, Legendary)
- Location-based material drops with appropriate progression
- Special rare materials from boss enemies
- Stack sizes based on material rarity (rarer materials come in smaller quantities)
- Value calculation based on material quality and usefulness

### Game Balance

A dedicated balancing system ensures appropriate difficulty progression:
- Difficulty scaling based on location progression (each location is more challenging than the previous)
- Dynamic enemy strength adjustment
- Reward scaling based on challenge level
- Special modifiers for boss battles

### Game State Management

The game maintains a persistent state including:
- Player inventory and materials
- Character statistics
- Unlocked locations
- Equipment
- Game settings

### Game Saving

Players can save their progress and load it later through the main menu. The game also features an automatic saving system that:
- Saves after significant actions (completing battles, changing locations)
- Provides seamless experience across game sessions
- Maintains all important game state information

### Localization System

The game features a comprehensive localization system:
- Full support for Russian and English languages
- JSON-based localization files for easy editing
- Dynamic language switching without restarting the game
- Localized UI elements, item descriptions, and dialogue

### Screen Transitions

Smooth transitions between game screens:
- Fade transitions between major game screens
- Slide animations for location changes
- Custom transition effects for battles and special events

## Controls

The game is controlled entirely with mouse interaction:
- Click buttons to perform actions
- Drag and drop items between slots
- Click on enemies to target them during battle
- Click on craftable items to create them

## Installation

1. Download the latest release
2. Extract the files to a location of your choice
3. Run SketchBlade.exe to start the game

## Development

This project is built using:
- C# programming language
- WPF (Windows Presentation Foundation)
- .NET 9.0
- MVVM architecture pattern

### Implemented Features
- Complete game state management with save/load functionality
- Inventory management with drag and drop
- Battle system with animations and effects
- Simplified material-based crafting system
- Material-based loot system from defeated enemies
- World map with location progression
- Screen transitions and animations
- Localization system with multiple languages

## Требования к оформлению кода

### Общие принципы
- **Архитектура**: Строго следовать паттерну MVVM (Model-View-ViewModel)
- **Namespace**: Использовать структурированные пространства имен: `SketchBlade.Models`, `SketchBlade.ViewModels`, `SketchBlade.Services`, `SketchBlade.Views`, `SketchBlade.Utilities`
- **Именование**: Использовать PascalCase для классов, методов и свойств; camelCase для приватных полей с префиксом `_`

### Обработка ошибок и отладка
- **НЕ использовать** `Console.WriteLine()` для вывода отладочной информации
- **НЕ использовать** `File.AppendAllText()` для логирования в моделях
- **Использовать** `LoggingService.LogError()` для критических ошибок
- **Использовать** `LoggingService.LogDebug()` для отладочной информации
- **Использовать** `MessageBox.Show()` умеренно и только в случаях, когда пользователю действительно нужно знать об ошибке или важном событии
- **Обрабатывать исключения** в try-catch блоках с соответствующим уведомлением пользователя через MessageBox только при критических ошибках

### Комментарии
- **Использовать комментарии редко** - код должен быть самодокументируемым
- **Добавлять комментарии** только для сложной бизнес-логики или неочевидных решений
- **Язык комментариев**: Все комментарии должны быть на русском языке в UTF-8 кодировке
- **Использовать XML-документацию** для публичных методов и классов только при необходимости
- **Избегать** очевидных комментариев типа `// Устанавливаем значение`

### Структура классов
- **ViewModels**: Наследовать от `ViewModelBase`, использовать `INotifyPropertyChanged`
- **Models**: Реализовывать `INotifyPropertyChanged` для привязки данных
- **Services**: Использовать интерфейсы для абстракции (например, `IFileSaveService`, `IImageService`)
- **Commands**: Использовать `RelayCommand` с обработкой ошибок

### Управление ресурсами
- **Изображения**: Использовать `ResourceService.Instance.GetImage()` для загрузки изображений
- **Пути к ресурсам**: Использовать константы из `AssetPaths` вместо магических строк
- **Fallback**: Всегда предусматривать fallback на `AssetPaths.DEFAULT_IMAGE` для отсутствующих спрайтов
- **Сериализация**: Помечать несериализуемые поля атрибутом `[NonSerialized]`

### Обработка событий
- **UI события**: Обрабатывать в code-behind только для простых операций
- **Бизнес-логика**: Выносить в ViewModels через команды
- **Исключения UI**: Обрабатывать в `DispatcherUnhandledException` с логированием

### Локализация
- **Строки**: Все пользовательские строки должны проходить через `LanguageService`
- **Ключи**: Использовать осмысленные ключи локализации
- **Fallback**: Предусматривать отображение ключа, если перевод не найден

### Производительность
- **Lazy Loading**: Использовать для тяжелых ресурсов (изображения)
- **Кэширование**: Кэшировать часто используемые данные через `ResourceService`
- **Dispose**: Правильно освобождать ресурсы в классах, реализующих `IDisposable`

### Примеры правильного оформления

#### Обработка ошибок:
```csharp
try
{
    // Выполнение операции
    var result = SomeOperation();
}
catch (Exception ex)
{
    // Логирование ошибки
    LoggingService.LogError($"Ошибка в методе MethodName: {ex.Message}", ex);
    
    // Уведомление пользователя только при критических ошибках
    MessageBox.Show("Произошла ошибка при выполнении операции.", 
        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
}
```

#### Загрузка изображений:
```csharp
try
{
    // Использование AssetPaths и ResourceService
    var sprite = ResourceService.Instance.GetImage(AssetPaths.Characters.PLAYER);
}
catch (Exception ex)
{
    LoggingService.LogError($"Ошибка загрузки спрайта: {ex.Message}", ex);
    sprite = ResourceService.Instance.GetImage(AssetPaths.DEFAULT_IMAGE);
}
```

#### Свойства ViewModel:
```csharp
private string _playerName = string.Empty;
public string PlayerName
{
    get => _playerName;
    set => SetProperty(ref _playerName, value);
}
```

#### Команды:
```csharp
public ICommand SaveGameCommand => new RelayCommand(SaveGame, "SaveGame");

private void SaveGame()
{
    try
    {
        _saveService.SaveGame(_gameState);
    }
    catch (Exception ex)
    {
        LoggingService.LogError($"Ошибка сохранения игры: {ex.Message}", ex);
        MessageBox.Show("Не удалось сохранить игру.", 
            "Ошибка сохранения", MessageBoxButton.OK, MessageBoxImage.Warning);
    }
}
```

#### Отладочное логирование:
```csharp
// Вместо Console.WriteLine или File.AppendAllText
// LoggingService.LogDebug($"Загружен предмет: {item.Name} с путем {item.SpritePath}");

// Для важных событий
LoggingService.LogInfo($"Игрок перешел в локацию: {location.Name}");

// Для ошибок
LoggingService.LogError($"Не удалось загрузить локацию: {ex.Message}", ex);
```

## Development Notes

### Weapon System
The game focuses on swords as the primary weapon type for simplicity and balance. The implementation uses only swords (Wooden, Iron, Gold, and Luminite) with consistent design and scaling damage values.

### Shield System
Shields are available only in metal variants (Iron, Gold, and Luminite). Wooden shields were removed to better balance early game progression, ensuring players must advance to at least the Cave location before obtaining protective gear for their off-hand.

### Material Design
Materials follow a clear progression path with consistent naming conventions:
- Basic materials from Village (Wood, Herbs, Cloth, Water Flasks)
- Forest adds Iron Ore, Crystal Dust, and Feathers
- Cave introduces Gold Ore and Gunpowder
- Ruins feature Gold materials, Poison Extract, and rare Luminite Fragments
- Castle offers Luminite as the ultimate material

The simplified material system creates a more streamlined crafting experience.

### Simplified Crafting System
The crafting system has been simplified from the original design:
- **No 3x3 crafting grid** - items are crafted directly from available recipes
- **No recipe book** - available recipes are displayed directly in the crafting panel
- **Material-based only** - only requires having the correct materials in inventory
- **One-click crafting** - clicking on a craftable item creates it instantly

This approach reduces complexity while maintaining the core crafting mechanics.

## Структура проекта

Проект организован в соответствии с паттерном MVVM и принципами чистой архитектуры:

```
SketchBlade/
├── Models/                     # Модели данных
│   ├── GameState.cs           # Состояние игры
│   ├── Character.cs           # Персонаж игрока
│   ├── Item.cs                # Предметы и снаряжение
│   ├── ItemFactory.cs         # Фабрика предметов
│   ├── Inventory.cs           # Система инвентаря
│   ├── BattleManager.cs       # Менеджер боевой системы
│   ├── SimplifiedCraftingSystem.cs # Упрощенная система крафта
│   ├── Location.cs            # Локации мира
│   └── GameSettings.cs        # Настройки игры
├── ViewModels/                # View Models (MVVM)
│   ├── ViewModelBase.cs       # Базовый класс ViewModel
│   ├── MainViewModel.cs       # Главное меню
│   ├── InventoryViewModel.cs  # Инвентарь и экипировка
│   ├── BattleViewModel.cs     # Боевая система
│   ├── MapViewModel.cs        # Карта мира
│   ├── SimplifiedCraftingViewModel.cs # Упрощенная система крафта
│   ├── SettingsViewModel.cs   # Настройки
│   └── RelayCommand.cs        # Команды UI
├── Views/                     # Пользовательские интерфейсы
│   ├── Controls/              # Пользовательские элементы UI
│   ├── MainMenuView.xaml      # Главное меню
│   ├── InventoryView.xaml     # Экран инвентаря
│   ├── BattleView.xaml        # Экран боя
│   ├── WorldMapView.xaml      # Карта мира
│   └── SettingsView.xaml      # Настройки
├── Services/                  # Сервисный слой
│   ├── GameSaveService.cs     # Сохранение/загрузка игры
│   ├── LanguageService.cs     # Локализация
│   ├── ScreenTransitionService.cs # Переходы между экранами
│   ├── ImageCacheService.cs   # Кэширование изображений
│   ├── InventoryService.cs    # Логика инвентаря
│   ├── NotificationService.cs # Уведомления
│   ├── GameBalanceService.cs  # Балансировка игры
│   ├── ConfigurationService.cs # Конфигурация
│   └── AutoSaveService.cs     # Автосохранение
├── Helpers/                   # Вспомогательные классы
│   ├── Converters/           # WPF конвертеры
│   └── ImageHelper.cs        # Работа с изображениями
├── Assets/                    # Игровые ресурсы
│   └── Images/               # Изображения
│       ├── items/           # Спрайты предметов
│       ├── Characters/      # Спрайты персонажей
│       ├── Enemies/         # Спрайты врагов
│       └── Locations/       # Изображения локаций
├── Resources/                 # Конфигурационные ресурсы
│   └── Localization/         # Файлы локализации (JSON)
├── bin/                      # Папка сборки (НЕ КОММИТИТЬ)
├── obj/                      # Временные файлы сборки (НЕ КОММИТИТЬ)
├── MainWindow.xaml          # Главное окно приложения
├── App.xaml                 # Конфигурация приложения
└── SketchBlade.csproj       # Файл проекта
```

### Принципы организации кода

- **MVVM архитектура**: Четкое разделение между моделями, представлениями и логикой
- **Сервисный слой**: Вся бизнес-логика вынесена в отдельные сервисы
- **Dependency Injection**: Сервисы используются через интерфейсы
- **Единый стиль**: Все классы следуют одинаковым соглашениям именования

## Требования к сборке

### Папки сборки

**ВАЖНО**: Сборка проекта всегда должна выполняться в отведенную для этого папку:

- **`bin/`** - Основная папка для скомпилированных файлов
  - `Debug/` - Отладочная сборка
  - `Release/` - Финальная сборка для распространения
- **`obj/`** - Временные файлы компиляции и промежуточные результаты

### Правила сборки

1. **НЕ КОММИТИТЬ** папки `bin/` и `obj/` в систему контроля версий
2. **Использовать только отведенные папки** для выходных файлов сборки
3. **Сборка Release** должна производиться в `bin/Release/` для распространения
4. **Все ресурсы** автоматически копируются в папку сборки:
   - Файлы локализации из `Resources/Localization/`
   - Изображения загружаются динамически

### Конфигурация сборки

Проект настроен для:
- **.NET 9.0** с поддержкой Windows
- **WPF** приложение с современным UI
- **Автоматическое копирование ресурсов** в папку сборки