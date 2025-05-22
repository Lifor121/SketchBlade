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

### Crafting System

The game includes a material-based crafting system:
- 3x3 crafting grid integrated directly into the inventory screen
- Material requirements for each recipe
- Recipe book button for discovering new crafting options
- Simple material-based crafting without specific pattern requirements
- Crafting of all equipment and consumable items

Players can drag materials into the crafting grid to create items. The crafting system only requires the correct quantity of materials to be present in the grid, without needing to arrange them in specific patterns. The recipe book provides guidance on available recipes and required materials.

### Settings Screen

The settings menu allows players to customize their game experience:
- Language selection (Russian, English)
- Music and sound effects volume controls
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
- Crafting recipes discovered

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

### Sound System

The game includes a rich audio experience:
- Location-specific music themes for each area (Village, Forest, Cave, Ruins, Castle)
- Varied sound effects for different actions (15+ sound types)
- Sound preloading for performance optimization
- Volume controls for music and sound effects separately
- Muting options for both music and sound effects

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
- Drag and drop materials to craft items

## Installation

1. Download the latest release
2. Extract the files to a location of your choice
3. Run SketchBlade.exe to start the game

## Development

This project is built using:
- C# programming language
- WPF (Windows Presentation Foundation)
- .NET Framework/Core
- MVVM architecture pattern

### Implemented Features
- Complete game state management with save/load functionality
- Inventory management with drag and drop
- Battle system with animations and effects
- Material-based crafting system with 3x3 grid
- Material-based loot system from defeated enemies
- Recipe book for discovering crafting options
- World map with location progression
- Screen transitions and animations
- Sound system with location-specific music
- Localization system with multiple languages

## Development Notes

### Weapon System
The game focuses on swords as the primary weapon type for simplicity and balance. The final implementation uses only swords (Wooden, Iron, Gold, and Luminite) with consistent design and scaling damage values.

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