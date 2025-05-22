using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using System.Windows;
using System.Threading;
using SketchBlade.Models;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace SketchBlade.ViewModels
{
    public class BattleViewModel : INotifyPropertyChanged
    {
        private readonly GameState _gameState;
        private readonly Action<string> _navigateAction;
        private Character _playerCharacter;
        private Character _enemyCharacter;
        private string _battleStatus;
        private string _turnMessage;
        private string _battleResultMessage;
        private string _rewardMessage;
        private string _damageMessage;
        private bool _isPlayerTurn;
        private bool _isBattleOver;
        private bool _battleWon;
        private bool _canAdvanceTurn;
        private Character _selectedCharacter;
        private ObservableCollection<Character> _enemies;
        private ObservableCollection<Item> _usableItems;
        private Item _selectedItem;
        private Character _selectedEnemy;
        private int _turnCounter;
        private bool _isBossHeroBattle;
        private Random _random = new Random();
        private ObservableCollection<string> _battleLog;
        private int _activeEnemyIndex;
        private bool _isEnemyUsingAbility;
        private string _enemyAbilityName;
        private int _enemyAbilityDamage;
        private bool _rewardsProcessed; // Flag to prevent multiple reward processing
        
        // Animation properties
        private bool _isAnimating;
        private bool _isPlayerAttacking;
        private bool _isEnemyAttacking;
        private double _attackAnimationProgress;
        private Character _attackingCharacter;
        private Character _targetCharacter;
        private int _animationDamage;
        private bool _isCriticalHit;
        private Timer _animationTimer;
        private const int ANIMATION_UPDATE_INTERVAL = 16; // ~60fps
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        // Properties
        public Character PlayerCharacter
        {
            get => _playerCharacter;
            set
            {
                _playerCharacter = value;
                OnPropertyChanged(nameof(PlayerCharacter));
            }
        }
        
        public Character EnemyCharacter
        {
            get => _enemyCharacter;
            set
            {
                _enemyCharacter = value;
                OnPropertyChanged(nameof(EnemyCharacter));
            }
        }
        
        public ObservableCollection<Character> Enemies
        {
            get => _enemies;
            set
            {
                _enemies = value;
                OnPropertyChanged(nameof(Enemies));
            }
        }
        
        public Character SelectedEnemy
        {
            get => _selectedEnemy;
            set
            {
                _selectedEnemy = value;
                OnPropertyChanged(nameof(SelectedEnemy));
            }
        }
        
        public ObservableCollection<Item> UsableItems
        {
            get => _usableItems;
            set
            {
                _usableItems = value;
                OnPropertyChanged(nameof(UsableItems));
            }
        }
        
        public Item SelectedItem
        {
            get => _selectedItem;
            set
            {
                _selectedItem = value;
                OnPropertyChanged(nameof(SelectedItem));
            }
        }
        
        public string BattleStatus
        {
            get => _battleStatus;
            set
            {
                _battleStatus = value;
                OnPropertyChanged(nameof(BattleStatus));
            }
        }
        
        public string TurnMessage
        {
            get => _turnMessage;
            set
            {
                _turnMessage = value;
                OnPropertyChanged(nameof(TurnMessage));
            }
        }
        
        public string BattleResultMessage
        {
            get => _battleResultMessage;
            set
            {
                _battleResultMessage = value;
                OnPropertyChanged(nameof(BattleResultMessage));
            }
        }
        
        public string RewardMessage
        {
            get => _rewardMessage;
            set
            {
                _rewardMessage = value;
                OnPropertyChanged(nameof(RewardMessage));
            }
        }
        
        public string DamageMessage
        {
            get => _damageMessage;
            set
            {
                _damageMessage = value;
                OnPropertyChanged(nameof(DamageMessage));
            }
        }
        
        public bool IsPlayerTurn
        {
            get => _isPlayerTurn;
            set
            {
                _isPlayerTurn = value;
                OnPropertyChanged(nameof(IsPlayerTurn));
            }
        }
        
        public bool IsBattleOver
        {
            get => _isBattleOver;
            set
            {
                _isBattleOver = value;
                OnPropertyChanged(nameof(IsBattleOver));
            }
        }
        
        public bool BattleWon
        {
            get => _battleWon;
            set
            {
                _battleWon = value;
                OnPropertyChanged(nameof(BattleWon));
            }
        }
        
        public bool CanAdvanceTurn
        {
            get => _canAdvanceTurn;
            set
            {
                _canAdvanceTurn = value;
                OnPropertyChanged(nameof(CanAdvanceTurn));
            }
        }
        
        public Character SelectedCharacter
        {
            get => _selectedCharacter;
            set
            {
                _selectedCharacter = value;
                OnPropertyChanged(nameof(SelectedCharacter));
            }
        }
        
        // New properties
        public ObservableCollection<string> BattleLog
        {
            get => _battleLog;
            set
            {
                _battleLog = value;
                OnPropertyChanged(nameof(BattleLog));
            }
        }
        
        public int ActiveEnemyIndex
        {
            get => _activeEnemyIndex;
            set
            {
                _activeEnemyIndex = value;
                OnPropertyChanged(nameof(ActiveEnemyIndex));
            }
        }
        
        public bool IsEnemyUsingAbility
        {
            get => _isEnemyUsingAbility;
            set
            {
                _isEnemyUsingAbility = value;
                OnPropertyChanged(nameof(IsEnemyUsingAbility));
            }
        }
        
        public string EnemyAbilityName
        {
            get => _enemyAbilityName;
            set
            {
                _enemyAbilityName = value;
                OnPropertyChanged(nameof(EnemyAbilityName));
            }
        }
        
        public int EnemyAbilityDamage
        {
            get => _enemyAbilityDamage;
            set
            {
                _enemyAbilityDamage = value;
                OnPropertyChanged(nameof(EnemyAbilityDamage));
            }
        }
        
        // Animation properties
        public bool IsAnimating
        {
            get => _isAnimating;
            set
            {
                _isAnimating = value;
                OnPropertyChanged(nameof(IsAnimating));
            }
        }
        
        public bool IsPlayerAttacking
        {
            get => _isPlayerAttacking;
            set
            {
                _isPlayerAttacking = value;
                OnPropertyChanged(nameof(IsPlayerAttacking));
            }
        }
        
        public bool IsEnemyAttacking
        {
            get => _isEnemyAttacking;
            set
            {
                _isEnemyAttacking = value;
                OnPropertyChanged(nameof(IsEnemyAttacking));
            }
        }
        
        public double AttackAnimationProgress
        {
            get => _attackAnimationProgress;
            set
            {
                _attackAnimationProgress = value;
                OnPropertyChanged(nameof(AttackAnimationProgress));
            }
        }
        
        public Character AttackingCharacter
        {
            get => _attackingCharacter;
            set
            {
                _attackingCharacter = value;
                OnPropertyChanged(nameof(AttackingCharacter));
            }
        }
        
        public Character TargetCharacter
        {
            get => _targetCharacter;
            set
            {
                _targetCharacter = value;
                OnPropertyChanged(nameof(TargetCharacter));
            }
        }
        
        public int AnimationDamage
        {
            get => _animationDamage;
            set
            {
                _animationDamage = value;
                OnPropertyChanged(nameof(AnimationDamage));
            }
        }
        
        public bool IsCriticalHit
        {
            get => _isCriticalHit;
            set
            {
                _isCriticalHit = value;
                OnPropertyChanged(nameof(IsCriticalHit));
            }
        }
        
        // Player stat properties for UI binding
        public int PlayerHealth => PlayerCharacter?.CurrentHealth ?? 0;
        public int PlayerMaxHealth => PlayerCharacter?.MaxHealth ?? 0;
        public double PlayerHealthPercent => PlayerCharacter != null ? ((double)PlayerCharacter.CurrentHealth / PlayerCharacter.MaxHealth) * 100 : 0;
        public int PlayerDamage => PlayerCharacter?.GetTotalAttack() ?? 0;
        public int PlayerDefense => PlayerCharacter?.GetTotalDefense() ?? 0;
        
        // Reference to GameState for reward binding
        public GameState GameState => _gameState;
        
        // Battle reward properties
        public bool HasRewardItems => _gameState.BattleRewardItems != null && _gameState.BattleRewardItems.Count > 0;
        
        // Commands
        public ICommand NavigateCommand { get; private set; }
        public ICommand AttackCommand { get; private set; }
        public ICommand UseItemCommand { get; private set; }
        public ICommand EndBattleCommand { get; private set; }
        public ICommand NextTurnCommand { get; private set; }
        public ICommand SelectCharacterCommand { get; private set; }
        public ICommand SelectEnemyCommand { get; private set; }
        public ICommand TargetAllEnemiesCommand { get; private set; }
        public ICommand ClickEnemyCommand { get; private set; }
        
        // Property to hide dropdown selection when only one enemy
        public bool ShowEnemySelection => Enemies.Count(e => !e.IsDefeated) > 1;
        
        // Property to expose if the current battle is a boss/hero battle
        public bool IsBossHeroBattle => _isBossHeroBattle;
        
        // Constructor
        public BattleViewModel(GameState gameState, Action<string> navigateAction)
        {
            _gameState = gameState;
            _navigateAction = navigateAction;
            
            // Initialize commands - исправленная версия с лучшей обработкой ошибок
            NavigateCommand = new RelayCommand<string>(screen => {
                try {
                    // Подробное логгирование для отладки
                    Console.WriteLine($"BattleViewModel: NavigateCommand called with parameter '{screen}'");
                    
                    // Проверяем параметр на null
                    if (string.IsNullOrEmpty(screen))
                    {
                        Console.WriteLine("BattleViewModel: ERROR - Navigate command called with null or empty parameter");
                        return;
                    }
                    
                    // Нормализация имени экрана для совместимости
                    string normalizedScreen = screen;
                    
                    // Особая обработка для разных форматов имен экранов
                    if (screen.EndsWith("View", StringComparison.OrdinalIgnoreCase))
                    {
                        // Если передано с суффиксом "View", оставляем как есть
                        normalizedScreen = screen;
                    }
                    else if (screen.EndsWith("Screen", StringComparison.OrdinalIgnoreCase))
                    {
                        // Если передано с суффиксом "Screen", заменяем на "View"
                        normalizedScreen = screen.Substring(0, screen.Length - 6) + "View";
                    }
                    
                    // Подробное логгирование параметров
                    Console.WriteLine($"BattleViewModel: Normalized screen name: '{normalizedScreen}'");
                    
                    // Обновляем GameState.CurrentScreen
                    if (normalizedScreen == "WorldMapView")
                    {
                        _gameState.CurrentScreen = "WorldMapView";
                        Console.WriteLine("BattleViewModel: Set GameState.CurrentScreen = WorldMapView");
                    }
                    
                    // Вызываем действие навигации с нормализованным параметром
                    Console.WriteLine($"BattleViewModel: Calling navigateAction with '{normalizedScreen}'");
                    navigateAction(normalizedScreen);
                    
                    // Сохраняем игру при навигации
                    _gameState.SaveGame();
                    Console.WriteLine("BattleViewModel: Game saved after navigation");
                }
                catch (Exception ex) {
                    Console.WriteLine($"BattleViewModel: ERROR in NavigateCommand - {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                    
                    // Аварийная навигация напрямую через MainWindow
                    try {
                        if (Application.Current?.MainWindow is MainWindow mainWindow)
                        {
                            Console.WriteLine("BattleViewModel: Attempting emergency navigation through MainWindow");
                            mainWindow.NavigateToScreen(screen);
                        }
                    }
                    catch (Exception fallbackEx) {
                        Console.WriteLine($"BattleViewModel: CRITICAL ERROR - fallback navigation failed - {fallbackEx.Message}");
                    }
                }
            }, null, "Navigate");
            
            AttackCommand = new RelayCommand<Character>(enemy => ExecuteAttack(enemy ?? SelectedEnemy), null, "Attack");
            UseItemCommand = new RelayCommand<Item>(item => ExecuteUseItem(item ?? SelectedItem), null, "UseItem");
            
            // Explicitly create the EndBattleCommand with a specific name for debug logging
            EndBattleCommand = new RelayCommand(_ => ExecuteEndBattle(), null, "EndBattle");
            
            NextTurnCommand = new RelayCommand(_ => ExecuteNextTurn(), null, "NextTurn");
            SelectCharacterCommand = new RelayCommand<Character>(character => ExecuteSelectCharacter(character), null, "SelectCharacter");
            SelectEnemyCommand = new RelayCommand<Character>(enemy => SelectedEnemy = enemy, null, "SelectEnemy");
            TargetAllEnemiesCommand = new RelayCommand(_ => ExecuteTargetAllEnemies(), null, "TargetAllEnemies");
            ClickEnemyCommand = new RelayCommand<Character>(enemy => {
                if (IsPlayerTurn && !IsBattleOver && !IsAnimating && enemy != null && !enemy.IsDefeated) {
                    // First select the enemy
                    SelectedEnemy = enemy;
                    // Immediately attack if it's player's turn and not in animation
                    ExecuteAttack(enemy);
                }
            });
            
            // Initialize collections
            Enemies = new ObservableCollection<Character>();
            UsableItems = new ObservableCollection<Item>();
            BattleLog = new ObservableCollection<string>();
            
            // Initialize animation timer
            _animationTimer = new Timer(OnAnimationTimerTick, null, Timeout.Infinite, ANIMATION_UPDATE_INTERVAL);
            
            // Initialize battle
            InitializeBattle();
        }
        
        // Initialize battle setup
        private void InitializeBattle()
        {
            Console.WriteLine("Initializing battle...");
            
            // Проверяем, есть ли персонаж и враги
            if (_gameState.Player == null)
            {
                Console.WriteLine("ERROR: Player is null, can't initialize battle");
                _navigateAction("WorldMapView");
                return;
            }
            
            if (_gameState.CurrentEnemies == null || _gameState.CurrentEnemies.Count == 0)
            {
                Console.WriteLine("ERROR: No enemies available, can't initialize battle");
                Console.WriteLine("Creating default enemy for battle");
                
                // Создаем врага-крысу по умолчанию
                _gameState.CurrentEnemies.Add(new Character
                {
                    Name = "Village Rat",
                    MaxHealth = 20,
                    CurrentHealth = 20,
                    Attack = 5,
                    Defense = 2,
                    ImagePath = "Assets/Images/enemy.png"
                });
            }
            
            Console.WriteLine($"Found {_gameState.CurrentEnemies.Count} enemies in GameState");
            
            // Устанавливаем персонажа игрока
            PlayerCharacter = _gameState.Player;
            Console.WriteLine($"Player set: {PlayerCharacter.Name}, HP: {PlayerCharacter.CurrentHealth}/{PlayerCharacter.MaxHealth}");
            
            // ВРЕМЕННО ОТКЛЮЧЕНО: Очищаем и заполняем коллекцию врагов без обработки здоровья
            Enemies.Clear();
            foreach (var enemy in _gameState.CurrentEnemies)
            {
                // Полностью пропускаем любую обработку состояния врага
                // Просто добавляем врага как есть, без изменений
                Enemies.Add(enemy);
                Console.WriteLine($"Added enemy: {enemy.Name}, HP: {enemy.CurrentHealth}/{enemy.MaxHealth}, IsHero: {enemy.IsHero}, IsDefeated: {enemy.IsDefeated}");
            }
            
            // Устанавливаем первого врага как активного
            if (Enemies.Count > 0)
            {
                EnemyCharacter = Enemies[0];
                SelectedEnemy = EnemyCharacter;
                ActiveEnemyIndex = 0;
                Console.WriteLine($"Selected enemy: {EnemyCharacter.Name}");
            }
            
            // Определяем, это битва с героем локации или обычная битва
            _isBossHeroBattle = _gameState.CurrentEnemies.Any(e => e.IsHero);
            Console.WriteLine($"Battle type: {(_isBossHeroBattle ? "Boss/Hero battle" : "Regular battle")}");
            
            // Подготавливаем предметы для использования в бою
            UsableItems.Clear();
            foreach (var item in _gameState.Inventory.Items)
            {
                if (item != null && item.IsUsable)
                {
                    UsableItems.Add(item);
                    Console.WriteLine($"Added usable item: {item.Name}");
                }
            }
            
            // Сбрасываем счетчик ходов
            _turnCounter = 1;
            
            // Устанавливаем начальное состояние боя
            IsPlayerTurn = true;
            IsBattleOver = false;
            BattleWon = false;
            CanAdvanceTurn = false;
            IsEnemyUsingAbility = false;
            IsAnimating = false;
            IsPlayerAttacking = false;
            IsEnemyAttacking = false;
            
            // Очищаем лог битвы
            BattleLog.Clear();
            
            // Устанавливаем начальные сообщения
            BattleStatus = _isBossHeroBattle ? "Битва с героем!" : "Битва начинается!";
            TurnMessage = $"Ход {_turnCounter}: Ваш ход! Выберите действие.";
            BattleResultMessage = string.Empty;
            RewardMessage = string.Empty;
            DamageMessage = string.Empty;
            
            // Добавляем начальное сообщение в лог битвы
            AddToBattleLog(BattleStatus);
            Console.WriteLine("Battle initialization complete");
        }
        
        // Animation timer callback
        private void OnAnimationTimerTick(object state)
        {
            // Need to use Dispatcher for UI updates from timer thread
            System.Windows.Application.Current.Dispatcher.Invoke(() => {
                // Update animation progress
                AttackAnimationProgress += 0.05;
                
                // Check if animation is complete
                if (AttackAnimationProgress >= 1.0)
                {
                    // Stop animation
                    IsAnimating = false;
                    _animationTimer.Change(Timeout.Infinite, ANIMATION_UPDATE_INTERVAL);
                    
                    // Reset animation progress
                    AttackAnimationProgress = 0;
                    
                    // Apply damage to target after animation
                    if (TargetCharacter != null)
                    {
                        Console.WriteLine($"Applying damage to {TargetCharacter.Name}: {AnimationDamage}");
                        TargetCharacter.CurrentHealth = Math.Max(0, TargetCharacter.CurrentHealth - AnimationDamage);
                        
                        // Check if player is defeated
                        if (TargetCharacter == PlayerCharacter && TargetCharacter.CurrentHealth <= 0)
                        {
                            EndBattle(false);
                            return;
                        }
                        
                        // Handle enemy defeat with the helper method
                        if (TargetCharacter != PlayerCharacter)
                        {
                            HandleCharacterDefeat(TargetCharacter);
                            
                            // If battle ended after handling defeat, return
                            if (IsBattleOver)
                            {
                                return;
                            }
                        }
                    }
                    
                    // Set next turn status
                    if (!IsBattleOver)
                    {
                        if (IsPlayerAttacking)
                        {
                            // End player turn and automatically start enemy turn
                            IsPlayerAttacking = false;
                            IsPlayerTurn = false;
                            
                            // Start enemy turn with a small delay
                            System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                                // Only execute enemy turn if battle is not over
                                if (!IsBattleOver)
                                {
                                    ExecuteEnemyTurn();
                                }
                            }), System.Windows.Threading.DispatcherPriority.Background);
                        }
                        else if (IsEnemyAttacking)
                        {
                            // End enemy turn
                            IsEnemyAttacking = false;
                            IsPlayerTurn = true;
                            _turnCounter++;
                            TurnMessage = $"Ход {_turnCounter}: Ваш ход! Выберите действие.";
                        }
                    }
                    
                    // Update properties
                    OnPropertyChanged(nameof(IsAnimating));
                    OnPropertyChanged(nameof(IsPlayerAttacking));
                    OnPropertyChanged(nameof(IsEnemyAttacking));
                    OnPropertyChanged(nameof(PlayerCharacter));
                    OnPropertyChanged(nameof(EnemyCharacter));
                    OnPropertyChanged(nameof(TargetCharacter));
                    OnPropertyChanged(nameof(Enemies));
                }
                else
                {
                    // Continue animation
                    OnPropertyChanged(nameof(AttackAnimationProgress));
                }
            });
        }
        
        // Временно упрощенная версия
        private void RemoveDefeatedEnemies()
        {
            // ВРЕМЕННО ОТКЛЮЧЕНО: не выполняем никаких проверок или удалений
            Console.WriteLine("RemoveDefeatedEnemies called but temporarily disabled");
        }
        
        // Start attack animation
        private void StartAttackAnimation(Character attacker, Character target, int damage, bool isCritical)
        {
            // Set animation properties
            IsAnimating = true;
            AttackingCharacter = attacker;
            TargetCharacter = target;
            AnimationDamage = damage;
            IsCriticalHit = isCritical;
            AttackAnimationProgress = 0;
            
            // Set attack direction flag
            if (attacker == PlayerCharacter)
            {
                IsPlayerAttacking = true;
                IsEnemyAttacking = false;
            }
            else
            {
                IsPlayerAttacking = false;
                IsEnemyAttacking = true;
            }
            
            // Start animation timer
            _animationTimer.Change(0, ANIMATION_UPDATE_INTERVAL);
            
            // Update UI
            OnPropertyChanged(nameof(IsAnimating));
            OnPropertyChanged(nameof(IsPlayerAttacking));
            OnPropertyChanged(nameof(IsEnemyAttacking));
            OnPropertyChanged(nameof(AttackingCharacter));
            OnPropertyChanged(nameof(TargetCharacter));
            OnPropertyChanged(nameof(AnimationDamage));
            OnPropertyChanged(nameof(IsCriticalHit));
            
            Console.WriteLine($"Attack animation started: {attacker.Name} -> {target.Name} for {damage} damage (Critical: {isCritical})");
        }
        
        // Выполнение атаки
        private void ExecuteAttack(Character targetEnemy)
        {
            if (!IsPlayerTurn || IsBattleOver || IsAnimating) return;
            
            // Check if the target enemy is defeated
            if (targetEnemy != null && targetEnemy.IsDefeated)
            {
                AddToBattleLog("Это враг уже повержен. Выберите другую цель.");
                return;
            }
            
            // If no enemy is selected or it's invalid, try to find a valid one
            if (targetEnemy == null || !Enemies.Contains(targetEnemy) || targetEnemy.IsDefeated)
            {
                // When there's only one active enemy, select it automatically
                var activeEnemies = Enemies.Where(e => !e.IsDefeated).ToList();
                if (activeEnemies.Count == 1)
                {
                    targetEnemy = activeEnemies[0];
                    SelectedEnemy = targetEnemy;
                }
                else if (activeEnemies.Count > 1)
                {
                    // If SelectedEnemy is valid, use it
                    if (SelectedEnemy != null && !SelectedEnemy.IsDefeated)
                    {
                        targetEnemy = SelectedEnemy;
                    }
                    else
                    {
                        // Otherwise, pick the first valid enemy
                        targetEnemy = activeEnemies.FirstOrDefault();
                        if (targetEnemy != null)
                        {
                            SelectedEnemy = targetEnemy;
                        }
                    }
                }
                else
                {
                    // No enemies left, battle should end
                    EndBattle(true);
                    return;
                }
            }
            
            // Calculate damage
            int damage = CalculateDamage(PlayerCharacter, targetEnemy);
            bool isCritical = _random.Next(100) < 15; // 15% chance for critical hit
            
            if (isCritical)
            {
                damage = (int)(damage * 1.5);
                DamageMessage = $"Критический удар! {damage} урона!";
            }
            else
            {
                DamageMessage = $"Вы атакуете {targetEnemy.Name} и наносите {damage} урона.";
            }
            
            // Add to battle log
            AddToBattleLog(DamageMessage);
            
            // Start attack animation
            StartAttackAnimation(PlayerCharacter, targetEnemy, damage, isCritical);
        }
        
        // Helper method to calculate damage
        private int CalculateDamage(Character attacker, Character defender)
        {
            // Base damage formula: attack - (defense * 0.5), minimum 1
            int baseDamage = Math.Max(1, attacker.Attack - (int)(defender.Defense * 0.5));
            
            // Add some randomness (±20%)
            double randomFactor = 0.8 + (_random.NextDouble() * 0.4); // Between 0.8 and 1.2
            int finalDamage = Math.Max(1, (int)(baseDamage * randomFactor));
            
            return finalDamage;
        }
        
        // Атака по всем врагам
        private void ExecuteTargetAllEnemies()
        {
            if (!IsPlayerTurn || IsBattleOver || IsAnimating) return;
            
            // Расчет базового урона (меньше чем обычная атака)
            int baseDamage = (int)(PlayerCharacter.GetTotalAttack() * 0.7);
            
            // Применяем урон ко всем врагам
            AddToBattleLog("Вы атакуете всех врагов!");
            
            // Store the enemies to avoid collection modification during iteration
            var allEnemies = new List<Character>(Enemies.Where(e => !e.IsDefeated));
            
            // Check if there are non-defeated enemies
            if (allEnemies.Count == 0)
            {
                AddToBattleLog("Нет активных врагов для атаки!");
                return;
            }
            
            // Start animation for the first enemy, as a visual indicator of the attack
            if (allEnemies.Count > 0)
            {
                // Небольшая вариация урона для первого врага
                double randomFactor = 0.9 + (_random.NextDouble() * 0.2); // 0.9 - 1.1
                int damage = Math.Max(1, (int)(baseDamage * randomFactor));
                
                // Turn on player attacking flag to trigger animations in the UI
                IsPlayerAttacking = true;
                
                // Start actual animation for the first enemy
                StartAttackAnimation(PlayerCharacter, allEnemies[0], damage, false);
                
                // Process other enemies directly only if there are additional enemies
                // Store them to process after the animation completes to avoid threading issues
                if (allEnemies.Count > 1)
                {
                    // Collect all other enemies to process after the animation
                    var otherEnemies = allEnemies.Skip(1).ToList();
                    
                    // After the animation completes for the first enemy, process the rest on the UI thread
                    System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                        // Only process if battle is still ongoing 
                        if (!IsBattleOver)
                        {
                            foreach (var enemy in otherEnemies)
                            {
                                // Skip if battle ended or enemy already defeated
                                if (IsBattleOver || enemy.IsDefeated) continue;
                                
                                // Calculate damage for this enemy
                                double enemyRandomFactor = 0.9 + (_random.NextDouble() * 0.2);
                                int enemyDamage = Math.Max(1, (int)(baseDamage * enemyRandomFactor));
                                
                                // Apply damage
                                enemy.CurrentHealth = Math.Max(0, enemy.CurrentHealth - enemyDamage);
                                AddToBattleLog($"{enemy.Name} получает {enemyDamage} урона.");
                                
                                // Handle enemy defeat safely without recursion
                                if (enemy.CurrentHealth <= 0)
                                {
                                    enemy.CurrentHealth = 0; // Ensure zero health
                                    AddToBattleLog($"{enemy.Name} повержен!");
                                    
                                    // Update enemy selection if needed
                                    if (enemy == SelectedEnemy)
                                    {
                                        var firstActiveEnemy = Enemies.FirstOrDefault(e => !e.IsDefeated);
                                        if (firstActiveEnemy != null)
                                        {
                                            SelectedEnemy = firstActiveEnemy;
                                        }
                                    }
                                }
                                
                                // Manually force UI update for this enemy
                                OnPropertyChanged(nameof(Enemies));
                            }
                            
                            // Check if all enemies are defeated after processing
                            if (Enemies.All(e => e.IsDefeated))
                            {
                                // End battle with victory
                                EndBattle(true);
                            }
                        }
                    }), System.Windows.Threading.DispatcherPriority.Background);
                }
            }
            else
            {
                // No enemies left, end battle with victory
                EndBattle(true);
            }
        }
        
        // Использование предмета
        private void ExecuteUseItem(Item item)
        {
            if (!IsPlayerTurn || IsBattleOver || item == null) return;
            
            if (item.Use(PlayerCharacter))
            {
                // Уменьшаем стак предметов
                if (item.StackSize > 1)
                {
                    item.StackSize--;
                }
                else
                {
                    // Удаляем предмет из инвентаря
                    _gameState.Inventory.RemoveItem(item);
                    // Удаляем предмет из списка доступных для использования
                    UsableItems.Remove(item);
                }
                
                // Сообщение об использовании предмета
                DamageMessage = $"Вы использовали {item.Name}.";
                AddToBattleLog($"Вы использовали {item.Name}.");
                
                // Обновляем отображение характеристик игрока
                OnPropertyChanged(nameof(PlayerCharacter));
                
                // Переход к следующему шагу
                CanAdvanceTurn = true;
                IsPlayerTurn = false;
                TurnMessage = "Нажмите 'Следующий ход', чтобы продолжить.";
            }
        }
        
        // Ход противника
        private void ExecuteEnemyTurn()
        {
            if (IsBattleOver || IsPlayerTurn || IsAnimating) return;
            
            // Filter out defeated enemies
            var activeEnemies = Enemies.Where(e => !e.IsDefeated).ToList();
            
            // Check if there are any active enemies
            if (activeEnemies.Count == 0)
            {
                // No active enemies left, end battle with victory
                EndBattle(true);
                return;
            }
            
            // Select the best enemy for attack using BattleManager
            Character attacker = _gameState.BattleManager.SelectEnemyForAttack(activeEnemies);
            ActiveEnemyIndex = Enemies.IndexOf(attacker);
            
            // Determine if enemy will use special ability using BattleManager
            IsEnemyUsingAbility = _gameState.BattleManager.ShouldUseSpecialAbility(attacker);
            
            if (IsEnemyUsingAbility)
            {
                // Execute special ability using BattleManager
                var specialAbility = _gameState.BattleManager.GetEnemySpecialAbility(attacker, PlayerCharacter);
                EnemyAbilityName = specialAbility.abilityName;
                EnemyAbilityDamage = specialAbility.damage;
                
                if (specialAbility.isAreaEffect)
                {
                    // Area effect abilities target player
                    AddToBattleLog($"{attacker.Name} использует {EnemyAbilityName} и наносит {EnemyAbilityDamage} урона!");
                    DamageMessage = $"{attacker.Name} использует {EnemyAbilityName} и наносит {EnemyAbilityDamage} урона!";
                    StartAttackAnimation(attacker, PlayerCharacter, EnemyAbilityDamage, false);
                }
                else
                {
                    // Single target abilities
                    AddToBattleLog($"{attacker.Name} использует {EnemyAbilityName} и наносит {EnemyAbilityDamage} урона!");
                    DamageMessage = $"{attacker.Name} использует {EnemyAbilityName} и наносит {EnemyAbilityDamage} урона!";
                    StartAttackAnimation(attacker, PlayerCharacter, EnemyAbilityDamage, false);
                }
            }
            else
            {
                // Calculate damage with Character method
                int damage = attacker.CalculateDamage(PlayerCharacter);
                
                // Update messages
                DamageMessage = $"{attacker.Name} наносит {damage} урона!";
                AddToBattleLog($"{attacker.Name} атакует и наносит {damage} урона.");
                
                // Start attack animation
                StartAttackAnimation(attacker, PlayerCharacter, damage, false);
            }
        }
        
        // Переход к следующему ходу
        private void ExecuteNextTurn()
        {
            if (IsBattleOver) return;
            
            if (!IsPlayerTurn)
            {
                // Если сейчас ход противника, выполняем его
                ExecuteEnemyTurn();
            }
            else
            {
                // Если сейчас ход игрока, начинаем новый раунд
                _turnCounter++;
                IsPlayerTurn = true;
                TurnMessage = $"Ход {_turnCounter}: Ваш ход! Выберите действие.";
                DamageMessage = string.Empty;
                AddToBattleLog($"--- Ход {_turnCounter} ---");
            }
            
            // Сбрасываем статус CanAdvanceTurn после перехода
            CanAdvanceTurn = false;
            
            // Сбрасываем статус использования способности врагом
            if (!IsPlayerTurn)
            {
                IsEnemyUsingAbility = false;
            }
        }
        
        // Выбор персонажа
        private void ExecuteSelectCharacter(Character character)
        {
            if (character != null)
            {
                PlayerCharacter = character;
            }
        }
        
        // Завершение битвы
        private void ExecuteEndBattle()
        {
            Console.WriteLine("============== ExecuteEndBattle STARTED ==============");
            
            try
            {
                // Make sure battle is marked as over
                IsBattleOver = true;
                
                // Add a final message to the battle log
                AddToBattleLog("Битва завершена. Возвращение на карту мира...");
                
                // ВАЖНО: сначала обрабатываем награды, до любой навигации
                if (BattleWon)
                {
                    Console.WriteLine("ExecuteEndBattle: Processing battle rewards");
                    
                    // Если награды не были сгенерированы, генерируем их
                    if (!_rewardsProcessed)
                    {
                        Console.WriteLine("ExecuteEndBattle: Rewards not generated yet, generating now");
                        GenerateRewards();
                    }
                    
                    // Добавляем награды в инвентарь игрока
                    ProcessBattleRewards();
                    
                    // Даем небольшую задержку чтобы инвентарь успел обновиться
                    System.Threading.Thread.Sleep(100);
                    
                    // Принудительное обновление UI перед навигацией
                    ForceUIUpdate();
                    
                    // Еще одна задержка для надежности
                    System.Threading.Thread.Sleep(100);
                }
                else
                {
                    Console.WriteLine("ExecuteEndBattle: Battle not won, skipping rewards");
                }
                
                // Используем диспетчер для безопасного выполнения операций с UI
                Application.Current.Dispatcher.BeginInvoke(() => {
                    try {
                        // Сохраняем игру перед навигацией
                        _gameState.SaveGame();
                        Console.WriteLine("ExecuteEndBattle: Game saved");
                        
                        // Финальная задержка
                        System.Threading.Thread.Sleep(50);
                        
                        // Переходим на карту мира
                        CompleteEndBattle();
                    }
                    catch (Exception ex) {
                        Console.WriteLine($"Error during EndBattle UI operations: {ex.Message}");
                        CompleteEndBattle(); // Try to navigate anyway
                    }
                }, System.Windows.Threading.DispatcherPriority.Background);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in ExecuteEndBattle: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                
                // В случае ошибки, аварийно сохраняем игру и завершаем бой
                _gameState.SaveGame();
                CompleteEndBattle();
            }
        }
        
        // Окончательное завершение боя и переход на экран карты
        private void CompleteEndBattle()
        {
            try
            {
                // Сохраняем игру перед навигацией
                _gameState.SaveGame();
                
                // Выполняем навигацию на карту мира
                Console.WriteLine("CompleteEndBattle: Navigating to WorldMap");
                
                // 1. Установка текущего экрана в GameState
                _gameState.CurrentScreen = "WorldMapView";
                Console.WriteLine("BattleViewModel: Set GameState.CurrentScreen = WorldMapView");
                
                // 2. Прямой вызов метода MainWindow.NavigateToScreen на UI-потоке
                if (System.Windows.Application.Current != null)
                {
                    var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
                    if (mainWindow != null)
                    {
                        // Выполняем навигацию на WorldMapView
                        mainWindow.NavigateToScreen("WorldMapView");
                    }
                    else
                    {
                        Console.WriteLine("WARNING: ExecuteEndBattle could not find MainWindow");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in CompleteEndBattle: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                
                // Аварийная навигация
                if (System.Windows.Application.Current != null)
                {
                    var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
                    if (mainWindow != null)
                    {
                        mainWindow.NavigateToScreen("WorldMapView");
                    }
                }
            }
            
            Console.WriteLine("============== ExecuteEndBattle COMPLETED ==============");
        }
        
        // Обновление отображаемых характеристик
        private void UpdateDisplayedStats()
        {
            OnPropertyChanged(nameof(PlayerHealth));
            OnPropertyChanged(nameof(PlayerMaxHealth));
            OnPropertyChanged(nameof(PlayerHealthPercent));
            OnPropertyChanged(nameof(PlayerDamage));
            OnPropertyChanged(nameof(PlayerDefense));
        }
        
        // Обработка завершения битвы
        private void EndBattle(bool victory)
        {
            // Ensure we're on the UI thread for property and collection updates
            System.Windows.Application.Current.Dispatcher.Invoke(() => {
                // Если бой уже завершен, не выполняем повторно
                if (IsBattleOver) 
                {
                    Console.WriteLine("EndBattle: Battle already over, skipping");
                    return;
                }
                
                Console.WriteLine($"EndBattle: Setting battle outcome to {(victory ? "victory" : "defeat")}");
                
                // Set battle status
                IsBattleOver = true;
                BattleWon = victory;
                CanAdvanceTurn = false;
                
                // Prepare battle result message
                string resultMessage = victory ? "Победа!" : "Поражение!";
                BattleResultMessage = resultMessage;
                
                // Add to battle log
                AddToBattleLog(resultMessage);
                
                // Обработка наград в случае победы
                if (victory)
                {
                    Console.WriteLine("EndBattle: Victory confirmed, generating rewards");
                    
                    // Генерируем награды, если они еще не были сгенерированы
                    if (!_rewardsProcessed)
                    {
                        GenerateRewards();
                        Console.WriteLine($"EndBattle: Generated rewards: {_gameState.BattleRewardItems?.Count ?? 0} items");
                    }
                    else
                    {
                        Console.WriteLine("EndBattle: Rewards already processed, skipping generation");
                    }
                    
                    // Добавляем сообщение о наградах в боевой журнал
                    if (_gameState.BattleRewardItems != null && _gameState.BattleRewardItems.Count > 0)
                    {
                        string itemsText = string.Join(", ", _gameState.BattleRewardItems.Select(i => i.Name));
                        AddToBattleLog($"Получены предметы: {itemsText}");
                    }
                    
                    // Добавляем сообщение о завершении боя
                    AddToBattleLog("Битва окончена. Нажмите кнопку 'Завершить бой', чтобы вернуться на карту мира.");
                    
                    // Обновляем статус героя локации
                    if (_gameState.CurrentLocation != null && _gameState.CurrentEnemies.Any(e => e.IsHero))
                    {
                        _gameState.CurrentLocation.HeroDefeated = true;
                        Console.WriteLine($"EndBattle: Marked hero of {_gameState.CurrentLocation.Name} as defeated");
                    }
                }
                else
                {
                    // Defeat message
                    AddToBattleLog("Вы проиграли. Восстановите здоровье перед следующей битвой.");
                }
                
                // Обновляем UI, чтобы отобразить награды и статус боя
                OnPropertyChanged(nameof(BattleWon));
                OnPropertyChanged(nameof(BattleResultMessage));
                OnPropertyChanged(nameof(GameState));
                OnPropertyChanged(nameof(HasRewardItems));
                
                Console.WriteLine("EndBattle: Battle status updated successfully");
            });
        }
        
        /// <summary>
        /// Публичный метод для завершения боя и перехода на экран карты
        /// </summary>
        /// <param name="victory">Победил ли игрок</param>
        public void FinishBattle(bool victory)
        {
            Console.WriteLine($"\n========== FinishBattle called with victory={victory} ==========");
            
            try
            {
                // Убедимся, что бой помечен как завершенный
                if (!IsBattleOver)
                {
                    Console.WriteLine("FinishBattle: Battle not marked as over, calling EndBattle");
                    EndBattle(victory);
                }
                
                // Обработка наград в случае победы
                if (victory)
                {
                    Console.WriteLine("FinishBattle: Processing rewards for victory");
                    
                    // Если награды не были сгенерированы, генерируем их
                    if (!_rewardsProcessed)
                    {
                        Console.WriteLine("FinishBattle: Rewards not processed, generating now");
                        GenerateRewards();
                    }
                    
                    // Сохраняем состояние наград перед обработкой (для отладки)
                    int rewardCount = _gameState.BattleRewardItems?.Count ?? 0;
                    Console.WriteLine($"FinishBattle: Found {rewardCount} rewards to process");
                    
                    // Проверяем, что награды сгенерированы перед их обработкой
                    if (_gameState.BattleRewardItems != null && _gameState.BattleRewardItems.Count > 0)
                    {
                        // Логируем содержимое наград для отладки
                        Console.WriteLine("FinishBattle: Battle reward items:");
                        foreach (var item in _gameState.BattleRewardItems)
                        {
                            Console.WriteLine($"  - {item.Name} ({item.Type}, {item.Rarity}, x{item.StackSize})");
                        }
                        
                        // Добавляем награды в инвентарь
                        ProcessBattleRewards();
                        
                        // Повторно проверяем наличие неообработанных наград
                        int remainingRewards = _gameState.BattleRewardItems?.Count ?? 0;
                        if (remainingRewards > 0)
                        {
                            Console.WriteLine($"WARNING: FinishBattle: {remainingRewards} reward items were not added to inventory");
                            
                            // Одна дополнительная попытка обработать награды
                            ProcessBattleRewards();
                            
                            // Если после повторной попытки еще остались награды, принудительно очищаем список
                            if (_gameState.BattleRewardItems != null && _gameState.BattleRewardItems.Count > 0)
                            {
                                Console.WriteLine($"CRITICAL: Still have {_gameState.BattleRewardItems.Count} unprocessed rewards, forcing clear");
                                _gameState.BattleRewardItems.Clear();
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("FinishBattle: No battle reward items to process");
                    }
                }
                
                // Завершаем бой в GameState
                try
                {
                    Console.WriteLine("FinishBattle: Completing battle in GameState");
                    
                    if (victory)
                    {
                        if (!_isBossHeroBattle)
                        {
                            _gameState.CompleteBattle(true);
                            Console.WriteLine("FinishBattle: Called GameState.CompleteBattle(true) for normal battle");
                        }
                        else
                        {
                            _gameState.CompleteBossHeroBattle(true);
                            Console.WriteLine("FinishBattle: Called GameState.CompleteBossHeroBattle(true) for boss battle");
                        }
                    }
                    else
                    {
                        _gameState.CompleteBattle(false);
                        Console.WriteLine("FinishBattle: Called GameState.CompleteBattle(false) for defeat");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERROR in FinishBattle, GameState completion: {ex.Message}");
                }
                
                // Переключаемся на экран карты
                _gameState.CurrentScreen = "WorldMapView";
                Console.WriteLine("FinishBattle: Set GameState.CurrentScreen = WorldMap");
                
                // Сохраняем игру для надежности
                _gameState.SaveGame();
                Console.WriteLine("FinishBattle: Game saved");
                
                // Находим главное окно и выполняем прямой переход на экран карты
                try
                {
                    var mainWindow = Application.Current.MainWindow as MainWindow;
                    if (mainWindow != null)
                    {
                        Console.WriteLine("FinishBattle: Found MainWindow, performing direct navigation");
                        mainWindow.NavigateToScreen("WorldMapView");
                    }
                    else
                    {
                        Console.WriteLine("WARNING: FinishBattle: MainWindow not found, cannot navigate");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERROR in FinishBattle, navigation: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CRITICAL ERROR in FinishBattle: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                
                // В случае ошибки, пытаемся аварийно завершить бой
                try
                {
                    // Устанавливаем экран карты мира напрямую
                    _gameState.CurrentScreen = "WorldMapView";
                    
                    // Попытка аварийного сохранения
                    _gameState.SaveGame();
                    Console.WriteLine("FinishBattle: Emergency save completed");
                    
                    // Попытка аварийной навигации
                    var mainWindow = Application.Current.MainWindow as MainWindow;
                    if (mainWindow != null)
                    {
                        mainWindow.NavigateToScreen("WorldMapView");
                        Console.WriteLine("FinishBattle: Emergency navigation completed");
                    }
                }
                catch (Exception emergencyEx)
                {
                    Console.WriteLine($"CRITICAL: Emergency handling failed: {emergencyEx.Message}");
                }
            }
            
            Console.WriteLine($"========== FinishBattle completed ==========\n");
        }
        
        /// <summary>
        /// Обрабатывает добавление наград за бой в инвентарь
        /// </summary>
        private void ProcessBattleRewards()
        {
            if (!BattleWon)
            {
                Console.WriteLine("ProcessBattleRewards: Battle not won, skipping rewards");
                return;
            }
            
            Console.WriteLine("\n===== PROCESSING BATTLE REWARDS =====");
            
            try
            {
                // Проверяем, есть ли награды и если нет, генерируем их
                if (_gameState.BattleRewardItems == null || _gameState.BattleRewardItems.Count == 0)
                {
                    Console.WriteLine("No rewards found. Generating rewards now...");
                    GenerateRewards();
                    
                    // Повторная проверка после генерации
                    if (_gameState.BattleRewardItems == null || _gameState.BattleRewardItems.Count == 0)
                    {
                        Console.WriteLine("WARNING: Failed to generate any rewards, nothing to process");
                        return;
                    }
                }
                
                // Копируем список наград, чтобы избежать проблем при изменении коллекции
                List<Item> rewardsCopy = new List<Item>(_gameState.BattleRewardItems);
                Console.WriteLine($"Processing {rewardsCopy.Count} reward items");
                
                // Получаем информацию о наградах для отображения в сообщении
                string rewardsMessage = "";
                foreach (var item in rewardsCopy)
                {
                    rewardsMessage += $"{item.Name} (x{item.StackSize}), ";
                }
                if (rewardsMessage.Length > 2)
                {
                    rewardsMessage = rewardsMessage.Substring(0, rewardsMessage.Length - 2); // Удаляем последнюю запятую
                }
                
                // Добавляем награды в инвентарь с обработкой ошибок
                foreach (var rewardItem in rewardsCopy)
                {
                    try
                    {
                        if (rewardItem == null) 
                        {
                            Console.WriteLine("WARNING: Null reward item found, skipping");
                            continue;
                        }
                        
                        // Показываем информацию о текущем предмете
                        Console.WriteLine($"Processing reward: {rewardItem.Name} (x{rewardItem.StackSize})");
                        
                        // Создаем клон предмета для безопасности
                        Item itemClone = rewardItem.Clone();
                        
                        // Пытаемся добавить предмет
                        bool added = _gameState.Inventory.AddItem(itemClone);
                        
                        if (added)
                        {
                            Console.WriteLine($"Successfully added {itemClone.Name} to inventory");
                            
                            // Удаляем предмет из списка наград после успешного добавления
                            _gameState.BattleRewardItems.Remove(rewardItem);
                        }
                        else
                        {
                            Console.WriteLine($"WARNING: Failed to add {rewardItem.Name} to inventory (possibly inventory full)");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"ERROR adding reward item {rewardItem?.Name}: {ex.Message}");
                    }
                }
                
                // Показываем сообщение о наградах
                if (!string.IsNullOrEmpty(rewardsMessage))
                {
                    RewardMessage = $"Получены предметы: {rewardsMessage}";
                    AddToBattleLog($"Получены предметы: {rewardsMessage}");
                }
                
                // Принудительно обновляем UI после добавления всех предметов
                ForceUIUpdate();
                
                // Даем небольшую задержку для обновления UI
                System.Threading.Thread.Sleep(50);
                
                // Повторно обновляем UI для надежности
                Application.Current.Dispatcher.Invoke(() => {
                    // Обновляем коллекцию предметов еще раз - без прямого вызова защищенного метода
                    _gameState.OnPropertyChanged(nameof(_gameState.Inventory));
                    _gameState.OnPropertyChanged("Inventory.Items");
                    
                    // Затем принудительно сохраняем игру
                    _gameState.SaveGame();
                    Console.WriteLine("Game state saved to ensure rewards are persisted");
                    
                    // Очищаем список наград, только если все предметы были добавлены
                    if (_gameState.BattleRewardItems.Count == 0)
                    {
                        Console.WriteLine("All rewards were added successfully");
                    }
                    else
                    {
                        Console.WriteLine($"WARNING: {_gameState.BattleRewardItems.Count} rewards were not added to inventory");
                        // Принудительно очищаем список в любом случае
                        _gameState.BattleRewardItems.Clear();
                        Console.WriteLine("Reward items list cleared to avoid duplication");
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CRITICAL ERROR in ProcessBattleRewards: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                
                // Аварийное сохранение игры в случае ошибки
                try
                {
                    _gameState.SaveGame();
                }
                catch
                {
                    // Игнорируем ошибки аварийного сохранения
                }
            }
            
            Console.WriteLine("===== BATTLE REWARDS PROCESSING COMPLETED =====\n");
        }
        
        /// <summary>
        /// Метод для принудительного обновления UI - аналогичен методу в InventoryViewModel
        /// </summary>
        public void ForceUIUpdate()
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() => {
                    Console.WriteLine("ForceUIUpdate: Refreshing inventory UI from BattleViewModel");
                    
                    // 1. Обновляем GameState и его объекты
                    _gameState.OnPropertyChanged(nameof(_gameState.Inventory));
                    _gameState.OnPropertyChanged(nameof(_gameState.Player));
                    _gameState.OnPropertyChanged(nameof(_gameState.Gold));
                    
                    // 2. Обновляем сам инвентарь - без прямого вызова защищенного метода
                    // Вместо _gameState.Inventory.OnPropertyChanged вызываем обновление через GameState
                    _gameState.OnPropertyChanged("Inventory.Items");
                    
                    // 3. Делаем глубокую копию предметов и перезагружаем коллекцию
                    var items = _gameState.Inventory.Items;
                    
                    // Создаем глубокую копию
                    var itemsCopy = new List<Item>(items.Count);
                    for(int i = 0; i < items.Count; i++)
                    {
                        if (items[i] != null)
                        {
                            itemsCopy.Add(items[i]);
                        }
                        else
                        {
                            itemsCopy.Add(null);
                        }
                    }
                    
                    // Очищаем коллекцию
                    items.Clear();
                    
                    // Заново заполняем ее элементами из копии
                    foreach(var item in itemsCopy)
                    {
                        items.Add(item);
                    }
                    
                    // 4. Для каждого предмета вызываем NotifyPropertyChanged
                    foreach(var item in _gameState.Inventory.Items)
                    {
                        if (item != null)
                        {
                            item.NotifyPropertyChanged("StackSize");
                            item.NotifyPropertyChanged("Icon");
                            item.NotifyPropertyChanged("Name");
                            item.NotifyPropertyChanged("Description");
                        }
                    }
                    
                    // 5. Обновляем свойства вьюмодели, относящиеся к наградам
                    OnPropertyChanged(nameof(RewardMessage));
                    OnPropertyChanged(nameof(HasRewardItems));
                    OnPropertyChanged(nameof(GameState));
                    
                    // 6. Дополнительное обновление UI
                    // Убираем вызов несуществующего метода
                    var mainWindow = Application.Current.MainWindow as MainWindow;
                    if (mainWindow != null)
                    {
                        // Принудительно обновляем текущий экран
                        mainWindow.RefreshCurrentScreen();
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in ForceUIUpdate: {ex.Message}");
            }
        }
        
        // Обновление статов игрока
        private void UpdatePlayerStats(bool isVictory)
        {
            if (isVictory)
            {
                // Награды уже выданы в GameState.CompleteBattle
                AddToBattleLog("Вы получили награду за победу!");
                
                // Обновляем отображаемые статы
                UpdateDisplayedStats();
            }
        }
        
        // Добавление сообщения в лог битвы
        private void AddToBattleLog(string message)
        {
            // Use dispatcher to ensure we're on the UI thread
            System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                BattleLog.Add(message);
                
                // Ограничиваем размер лога (хранить не более 50 сообщений)
                if (BattleLog.Count > 50)
                {
                    BattleLog.RemoveAt(0);
                }
            });
        }
        
        // Method to reinitialize battle when navigating to battle screen
        public void ReinitializeBattle()
        {
            Console.WriteLine("ReinitializeBattle called");
            // Clear any existing state
            BattleStatus = string.Empty;
            TurnMessage = string.Empty;
            BattleResultMessage = string.Empty;
            DamageMessage = string.Empty;
            RewardMessage = string.Empty;
            
            IsPlayerTurn = true;
            IsBattleOver = false;
            CanAdvanceTurn = false;
            BattleWon = false;
            _rewardsProcessed = false; // Reset rewards processed flag
            
            // Reset animation state
            IsAnimating = false;
            IsPlayerAttacking = false;
            IsEnemyAttacking = false;
            
            // Ensure player character is set from game state
            PlayerCharacter = _gameState.Player;
            
            // Initialize the battle with enemies from game state
            InitializeBattle();
            
            Console.WriteLine($"Battle reinitialized with {Enemies.Count} enemies");
        }
        
        // Вспомогательный метод для уведомления об изменении свойства
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Создание тестового персонажа
        private Character CreateTestCharacter()
        {
            Character character = new Character
            {
                Name = "Test Character",
                MaxHealth = 100,
                CurrentHealth = 100,
                Attack = 10,
                Defense = 5,
                Type = "Player"
            };
            return character;
        }

        // Helper method to handle character defeat
        private void HandleCharacterDefeat(Character character)
        {
            // Always use the UI thread for any operations that modify collections
            System.Windows.Application.Current.Dispatcher.Invoke(() => {
                // Force health to zero to ensure IsDefeated is set
                if (character.CurrentHealth <= 0)
                {
                    character.CurrentHealth = 0;
                    AddToBattleLog($"{character.Name} повержен!");
                    
                    // If this was the selected enemy, choose a new one if possible
                    if (character == SelectedEnemy)
                    {
                        // Find the first non-defeated enemy
                        var firstActiveEnemy = Enemies.FirstOrDefault(e => !e.IsDefeated);
                        if (firstActiveEnemy != null)
                        {
                            SelectedEnemy = firstActiveEnemy;
                        }
                    }
                    
                    // Update ShowEnemySelection property when enemy count changes
                    OnPropertyChanged(nameof(ShowEnemySelection));
                }
                
                // Check if all enemies are defeated
                if (character != PlayerCharacter && Enemies.All(e => e.IsDefeated || e.CurrentHealth <= 0))
                {
                    // Ensure all near-zero health enemies are truly at zero
                    foreach (var enemy in Enemies.Where(e => e.CurrentHealth <= 0))
                    {
                        enemy.CurrentHealth = 0;
                    }
                    
                    // End battle with victory
                    EndBattle(true);
                }
            });
        }

        /// <summary>
        /// Генерирует награды за победу в бою
        /// </summary>
        private void GenerateRewards()
        {
            Console.WriteLine("\n===== GENERATING BATTLE REWARDS =====");
            
            // Проверяем, не были ли уже обработаны награды
            if (_rewardsProcessed)
            {
                Console.WriteLine("Rewards already processed, skipping reward generation");
                return;
            }
            
            try
            {
                // Отмечаем, что награды обработаны, чтобы избежать повторной генерации
                _rewardsProcessed = true;
                Console.WriteLine("Rewards marked as processed to prevent duplicate generation");
                
                // Запрашиваем награды от BattleManager
                Console.WriteLine("Requesting rewards from battle manager...");
                (int gold, List<Item> items) = _gameState.BattleManager.CalculateRewards(true, _gameState.CurrentEnemies);
                
                // Добавляем золото за победу
                if (gold > 0)
                {
                    _gameState.Gold += gold;
                    Console.WriteLine($"Added {gold} gold to player");
                }
                
                // Проверяем полученные награды
                if (items == null)
                {
                    Console.WriteLine("WARNING: Battle manager returned null items list, creating empty list");
                    items = new List<Item>();
                }
                
                // Логируем информацию о полученных наградах
                Console.WriteLine($"Battle manager returned: {gold} gold and {items.Count} items");
                foreach (var item in items)
                {
                    Console.WriteLine($"  - {item.Name} (Type: {item.Type}, Rarity: {item.Rarity}, Stack: {item.StackSize})");
                }
                
                // Инициализируем или очищаем коллекцию наград
                if (_gameState.BattleRewardItems == null)
                {
                    Console.WriteLine("Creating new BattleRewardItems collection");
                    _gameState.BattleRewardItems = new List<Item>();
                }
                else if (_gameState.BattleRewardItems.Count > 0)
                {
                    Console.WriteLine($"Clearing existing {_gameState.BattleRewardItems.Count} reward items");
                    _gameState.BattleRewardItems.Clear();
                }
                
                // Добавляем каждый предмет в коллекцию наград
                foreach (var item in items)
                {
                    // Создаем глубокую копию предмета для безопасности
                    Item rewardItem = new Item
                    {
                        Name = item.Name,
                        Description = item.Description,
                        Type = item.Type,
                        Rarity = item.Rarity,
                        Material = item.Material,
                        StackSize = item.StackSize,
                        MaxStackSize = item.MaxStackSize,
                        Value = item.Value,
                        Weight = item.Weight,
                        Damage = item.Damage,
                        Defense = item.Defense,
                        EffectPower = item.EffectPower,
                        SpritePath = item.SpritePath
                    };
                    
                    // Копируем статистические бонусы
                    foreach (var bonus in item.StatBonuses)
                    {
                        rewardItem.StatBonuses.Add(bonus.Key, bonus.Value);
                    }
                    
                    _gameState.BattleRewardItems.Add(rewardItem);
                    Console.WriteLine($"Added {rewardItem.Name} to BattleRewardItems");
                }
                
                // Сохраняем золото в GameState
                _gameState.BattleRewardGold = gold;
                Console.WriteLine($"Set BattleRewardGold to {gold}");
                
                // Формируем сообщение о наградах для отображения
                string itemsText = items.Count > 0 
                    ? string.Join(", ", items.Select(i => i.Name)) 
                    : "нет предметов";
                
                // Обновляем UI в главном потоке
                Application.Current.Dispatcher.Invoke(() => {
                    try {
                        // Задаем сообщение о наградах для UI
                        RewardMessage = items.Count > 0
                            ? $"Получены предметы: {itemsText}"
                            : "Нет полученных предметов";
                        
                        // Добавляем информацию о наградах в лог боя
                        if (items.Count > 0)
                        {
                            AddToBattleLog($"Получены предметы: {itemsText}");
                        }
                        
                        if (gold > 0)
                        {
                            AddToBattleLog($"Получено золото: {gold}");
                        }
                        
                        // Обновляем свойство с золотом
                        _gameState.OnPropertyChanged(nameof(_gameState.Gold));
                        
                        // Обновляем UI-свойства
                        OnPropertyChanged(nameof(GameState));
                        OnPropertyChanged(nameof(HasRewardItems));
                        OnPropertyChanged(nameof(RewardMessage));
                    }
                    catch (Exception ex) {
                        Console.WriteLine($"Error updating UI in GenerateRewards: {ex.Message}");
                    }
                });
                
                // Сохраняем состояние, чтобы не потерять сгенерированные награды
                _gameState.SaveGame();
                Console.WriteLine("Game state saved with generated rewards");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in GenerateRewards: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                
                // Создаем аварийные награды в случае ошибки
                try
                {
                    if (_gameState.BattleRewardItems == null)
                    {
                        _gameState.BattleRewardItems = new List<Item>();
                    }
                    
                    // Если наград нет, добавляем хотя бы одну базовую
                    if (_gameState.BattleRewardItems.Count == 0)
                    {
                        var emergencyItem = new Item
                        {
                            Name = "Wood",
                            Description = "Basic crafting material",
                            Type = ItemType.Material,
                            Rarity = ItemRarity.Common,
                            StackSize = 5,
                            MaxStackSize = 20
                        };
                        
                        _gameState.BattleRewardItems.Add(emergencyItem);
                        Console.WriteLine("Added emergency reward item (Wood x5)");
                    }
                }
                catch
                {
                    // Игнорируем ошибки аварийного восстановления
                }
            }
            
            Console.WriteLine($"Generation complete: {_gameState.BattleRewardItems?.Count ?? 0} items ready");
            Console.WriteLine("===== BATTLE REWARDS GENERATION COMPLETED =====\n");
        }
    }
} 