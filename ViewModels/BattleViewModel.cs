using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using SketchBlade.Models;
using SketchBlade.Services;

namespace SketchBlade.ViewModels
{
    /// <summary>
    /// Упрощенная версия BattleViewModel (было 2103 строки, стало ~300)
    /// </summary>
    public class BattleViewModel : ViewModelBase, IDisposable
    {
        private readonly GameData _gameState;
        private readonly Action<string> _navigateAction;
        private readonly BattleState _battleState;
        private readonly BattleLogic _battleLogic;
        private readonly BattleAnimations _animations;
        private bool _waitingForEnemyTurn = false;
        private System.Threading.Timer? _safetyTimer; // Таймер безопасности для предотвращения зависания

        public ICommand NavigateCommand { get; private set; }
        public ICommand AttackCommand { get; private set; }
        public ICommand UseItemCommand { get; private set; }
        public ICommand EndBattleCommand { get; private set; }
        public ICommand SelectEnemyCommand { get; private set; }
        public ICommand ClickEnemyCommand { get; private set; }
        public ICommand ForceEndTurnCommand { get; private set; }

        public BattleViewModel(GameData GameData, Action<string> navigateAction)
        {
            LoggingService.LogDebug("=== BattleViewModel CONSTRUCTOR CALLED ===");
            
            _gameState = GameData;
            _navigateAction = navigateAction;
            
            _battleState = new BattleState();
            _battleLogic = new BattleLogic(GameData);
            _animations = new BattleAnimations();

            _battleState.PropertyChanged += OnBattleStateChanged;
            _animations.PropertyChanged += OnAnimationsChanged;
            _animations.AnimationCompleted += OnAnimationCompleted;
            _gameState.PropertyChanged += OnGameStateChanged;

            InitializeCommands();
            InitializeBattle();
            
            LoggingService.LogDebug("=== BattleViewModel CONSTRUCTOR COMPLETED ===");
        }

        private void InitializeCommands()
        {
            NavigateCommand = new RelayCommand<string>(ExecuteNavigate, null, "Navigate");
            AttackCommand = new RelayCommand<object>(_ => ExecuteAttack(), _ => CanAttack(), "Attack");
            UseItemCommand = new RelayCommand<Item>(ExecuteUseItem, CanUseItem, "UseItem");
            EndBattleCommand = new RelayCommand<object>(_ => ExecuteEndBattle(), null, "EndBattle");
            SelectEnemyCommand = new RelayCommand<Character>(ExecuteSelectEnemy, null, "SelectEnemy");
            ClickEnemyCommand = new RelayCommand<Character>(ExecuteClickEnemy, CanClickEnemy, "ClickEnemy");
            ForceEndTurnCommand = new RelayCommand<object>(_ => ExecuteForceEndTurn(), null, "ForceEndTurn");
        }

        private void InitializeBattle()
        {
            // LoggingService.LogDebug("=== InitializeBattle STARTED ===");
            // LoggingService.LogDebug($"Player null: {_gameState.Player == null}");
            // LoggingService.LogDebug($"CurrentEnemies null: {_gameState.CurrentEnemies == null}");
            // LoggingService.LogDebug($"CurrentEnemies count: {_gameState.CurrentEnemies?.Count ?? 0}");
            
            if (_gameState.Player == null || _gameState.CurrentEnemies?.Count == 0)
            {
                LoggingService.LogError("InitializeBattle: No player or enemies, navigating to WorldMapView");
                _navigateAction("WorldMapView");
                return;
            }

            // Reset battle state to ensure clean start
            // LoggingService.LogDebug("Calling _battleState.Reset()");
            _battleState.Reset();
            // LoggingService.LogDebug($"After Reset: IsBattleOver = {_battleState.IsBattleOver}");

            _battleState.PlayerCharacter = _gameState.Player;
            LoggingService.LogDebug($"InitializeBattle: Player.IsPlayer = {_gameState.Player?.IsPlayer}, Player.Name = {_gameState.Player?.Name}");
            _battleState.IsBossHeroBattle = _gameState.CurrentEnemies.Any(e => e.IsHero);
            
            // LoggingService.LogDebug($"IsBossHeroBattle: {_battleState.IsBossHeroBattle}");

            _battleState.Enemies.Clear();
            foreach (var enemy in _gameState.CurrentEnemies)
            {
                // LoggingService.LogDebug($"Processing enemy: {enemy.Name}, IsDefeated before reset: {enemy.IsDefeated}, Health: {enemy.CurrentHealth}/{enemy.MaxHealth}");
                
                // Reset enemy defeated status for new battle
                enemy.SetDefeated(false);
                // Restore hero health if it's a hero battle
                if (enemy.IsHero && enemy.CurrentHealth <= 0)
                {
                    enemy.CurrentHealth = enemy.MaxHealth;
                    // LoggingService.LogDebug($"Restored hero {enemy.Name} health to {enemy.MaxHealth}");
                }
                
                // LoggingService.LogDebug($"Enemy after reset: {enemy.Name}, IsDefeated: {enemy.IsDefeated}, Health: {enemy.CurrentHealth}/{enemy.MaxHealth}");
                _battleState.Enemies.Add(enemy);
            }
            
            // LoggingService.LogDebug($"After adding enemies: IsBattleOver = {_battleState.IsBattleOver}");

            _battleState.SelectedEnemy = _battleState.Enemies.FirstOrDefault(e => !e.IsDefeated);
            // LoggingService.LogDebug($"Selected enemy: {_battleState.SelectedEnemy?.Name ?? "null"}");
            
            LoadUsableItems();
            
            _battleState.IsPlayerTurn = true;
            _battleState.BattleStatus = "Начало боя!";
            _battleState.AddToBattleLog("Начало боя!");
            
            //  :   
            bool allEnemiesDefeated = _battleLogic.IsAllEnemiesDefeated(_battleState);
            // LoggingService.LogDebug($"All enemies defeated check: {allEnemiesDefeated}");
            
            if (allEnemiesDefeated)
            {
                LoggingService.LogError(" :      !");
                //     -  !
                LoggingService.LogError("    -  !");
            }
            
            // LoggingService.LogDebug($"Battle initialized - IsBattleOver: {_battleState.IsBattleOver}, IsPlayerTurn: {_battleState.IsPlayerTurn}");
            // LoggingService.LogDebug("=== InitializeBattle COMPLETED ===");
        }

        private void LoadUsableItems()
        {
            _battleState.UsableItems.Clear();
            
            //    null  
            var consumableItems = _gameState.Inventory.Items
                .Where(item => item != null && item.Type == ItemType.Consumable)
                .ToList();

            foreach (var item in consumableItems)
            {
                _battleState.UsableItems.Add(item);
            }
        }

        private void ExecuteNavigate(string screen)
        {
            try
            {
                _navigateAction(screen ?? "WorldMapView");
                var saveManager = new GameSaveManager();
                saveManager.SaveGame(_gameState);
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Navigation error in battle", ex);
            }
        }

        private void ExecuteAttack()
        {
            LoggingService.LogDebug($"=== ExecuteAttack START ===");
            LoggingService.LogDebug($"SelectedEnemy: {_battleState.SelectedEnemy?.Name ?? "null"}");
            LoggingService.LogDebug($"IsAnimating: {_animations.IsAnimating}");
            LoggingService.LogDebug($"Current animation state - IsPlayerAttacking: {_animations.IsPlayerAttacking}, IsEnemyAttacking: {_animations.IsEnemyAttacking}");
            
            if (_battleState.SelectedEnemy == null || _animations.IsAnimating) 
            {
                LoggingService.LogDebug("ExecuteAttack: Выход - нет цели или анимация уже идет");
                return;
            }

            LoggingService.LogDebug($"ExecuteAttack: Игрок атакует {_battleState.SelectedEnemy.Name}");

            var target = _battleState.SelectedEnemy;
            var player = _battleState.PlayerCharacter;
            
            LoggingService.LogDebug($"ExecuteAttack: player.IsPlayer = {player?.IsPlayer}, player.Name = {player?.Name}");
            LoggingService.LogDebug($"ExecuteAttack: target.IsPlayer = {target?.IsPlayer}, target.Name = {target?.Name}");

            int damage = _battleLogic.CalculateDamage(player, target);
            bool isCritical = _battleLogic.IsCriticalHit();

            if (isCritical) damage = (int)(damage * 1.5);

            LoggingService.LogDebug($"ExecuteAttack: Запускаем анимацию атаки игрока -> {target.Name}, урон: {damage}");
            _animations.StartAttackAnimation(player, target, damage, isCritical);
            _battleLogic.ApplyDamage(target, damage);

            string attackMessage = isCritical 
                ? $"Критический удар! Игрок нанёс {damage} урона {target.Name}"
                : $"Игрок атаковал {target.Name} и нанёс {damage} урона";
            
            _battleState.AddToBattleLog(attackMessage);

            // Обновляем UI характеристик после удара игрока
            RefreshEnemiesUI();
            OnPropertyChanged(nameof(PlayerCharacter));
            OnPropertyChanged(nameof(PlayerHealth));
            OnPropertyChanged(nameof(PlayerDamage));
            OnPropertyChanged(nameof(PlayerDefense));
            
            if (_battleLogic.IsCharacterDefeated(target))
            {
                target.SetDefeated(true);
                _battleState.AddToBattleLog($"{target.Name} побеждён!");
                
                // Принудительно обновляем UI после поражения врага
                OnPropertyChanged(nameof(Enemies));
                OnPropertyChanged(nameof(CanAttack));
                
                if (_battleLogic.IsAllEnemiesDefeated(_battleState))
                {
                    LoggingService.LogDebug("ExecuteAttack: Все враги побеждены, завершаем бой");
                    EndBattle(true);
                    return;
                }
                
                // Выбираем следующего живого врага
                var nextEnemy = _battleState.Enemies.FirstOrDefault(e => !e.IsDefeated);
                if (nextEnemy != null)
                {
                    _battleState.SelectedEnemy = nextEnemy;
                    OnPropertyChanged(nameof(SelectedEnemy));
                }
            }

            _battleState.IsPlayerTurn = false;
            OnPropertyChanged(nameof(IsPlayerTurn));
            OnPropertyChanged(nameof(CanAttack));
            
            // Устанавливаем флаг ожидания хода врага
            _waitingForEnemyTurn = true;
            LoggingService.LogDebug("ExecuteAttack: Ожидаем завершения анимации для хода врага");
            
            // Запускаем таймер безопасности
            StartSafetyTimer();
            
            // НЕ вызываем ExecuteEnemyTurn() сразу - он будет вызван после завершения анимации
        }

        private bool CanAttack()
        {
            return _battleState.IsPlayerTurn && 
                   !_battleState.IsBattleOver && 
                   !_animations.IsAnimating && 
                   _battleState.SelectedEnemy != null &&
                   !_battleState.SelectedEnemy.IsDefeated;
        }

        private void ExecuteUseItem(Item item)
        {
            if (item == null || _animations.IsAnimating) return;

            var player = _battleState.PlayerCharacter;

            switch (item.Name)
            {
                case "Healing Potion":
                    _battleLogic.UseHealingPotion(player, 30);
                    _battleState.AddToBattleLog("Использовано зелье лечения");
                    break;
                case "Rage Potion":
                    _battleLogic.ApplyRagePotion(player, 10, 3);
                    _battleState.AddToBattleLog("Использовано зелье ярости");
                    break;
                case "Bomb":
                    int bombDamage = _battleLogic.CalculateBombDamage();
                    foreach (var enemy in _battleState.Enemies.Where(e => !e.IsDefeated))
                    {
                        _battleLogic.ApplyDamage(enemy, bombDamage);
                    }
                    _battleState.AddToBattleLog($"Выпущена бомба, нанесён урон {bombDamage} всем врагам");
                    break;
            }

            _gameState.Inventory.RemoveItem(item, 1);
            LoadUsableItems();

            // Обновляем UI характеристик после использования предмета
            OnPropertyChanged(nameof(PlayerCharacter));
            OnPropertyChanged(nameof(PlayerHealth));
            OnPropertyChanged(nameof(PlayerDamage));
            OnPropertyChanged(nameof(PlayerDefense));
            RefreshEnemiesUI();

            _battleState.IsPlayerTurn = false;
            
            // Устанавливаем флаг ожидания хода врага
            _waitingForEnemyTurn = true;
            LoggingService.LogDebug("ExecuteUseItem: Ожидаем хода врага после использования предмета");
            
            // Запускаем таймер безопасности
            StartSafetyTimer();
            
            // Запускаем ход врага после небольшой задержки (без анимации для предметов)
            System.Threading.Tasks.Task.Delay(500).ContinueWith(_ => 
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => 
                {
                    if (!_battleState.IsBattleOver)
                    {
                        _waitingForEnemyTurn = false;
                        // Останавливаем таймер безопасности, так как переходим к ходу врага
                        _safetyTimer?.Dispose();
                        _safetyTimer = null;
                        ExecuteEnemyTurn();
                    }
                });
            });
        }

        private bool CanUseItem(Item item)
        {
            return _battleState.IsPlayerTurn && 
                   !_battleState.IsBattleOver && 
                   !_animations.IsAnimating && 
                   item != null;
        }

        private void ExecuteEnemyTurn()
        {
            LoggingService.LogDebug("=== ExecuteEnemyTurn START ===");
            LoggingService.LogDebug($"Current animation state - IsPlayerAttacking: {_animations.IsPlayerAttacking}, IsEnemyAttacking: {_animations.IsEnemyAttacking}");
            LoggingService.LogDebug($"IsAnimating: {_animations.IsAnimating}");
            
            var activeEnemy = _battleLogic.GetNextActiveEnemy(_battleState);
            if (activeEnemy == null)
            {
                LoggingService.LogDebug("ExecuteEnemyTurn: Нет активных врагов, завершаем бой");
                EndBattle(true);
                return;
            }

            LoggingService.LogDebug($"ExecuteEnemyTurn: {activeEnemy.Name} атакует игрока");

            var player = _battleState.PlayerCharacter;
            bool useSpecialAbility = _battleLogic.ShouldEnemyUseSpecialAbility(activeEnemy);
            int damage;
            string attackMessage;

            if (useSpecialAbility)
            {
                damage = _battleLogic.CalculateSpecialAbilityDamage(activeEnemy, player);
                string abilityName = _battleLogic.GetEnemySpecialAbilityName(activeEnemy);
                attackMessage = $"{activeEnemy.Name} использовал {abilityName} и нанёс {damage} урона";
                
                _battleState.IsEnemyUsingAbility = true;
                _battleState.EnemyAbilityName = abilityName;
                _battleState.EnemyAbilityDamage = damage;
                
                LoggingService.LogDebug($"ExecuteEnemyTurn: Враг использует способность {abilityName}");
            }
            else
            {
                damage = _battleLogic.CalculateDamage(activeEnemy, player);
                attackMessage = $"{activeEnemy.Name} атаковал и нанёс {damage} урона";
                LoggingService.LogDebug($"ExecuteEnemyTurn: Обычная атака, урон: {damage}");
            }

            // Запускаем анимацию атаки конкретного врага
            LoggingService.LogDebug($"ExecuteEnemyTurn: Запускаем анимацию атаки врага {activeEnemy.Name} -> игрок, урон: {damage}");
            _animations.StartAttackAnimation(activeEnemy, player, damage, false);
            
            _battleLogic.ApplyDamage(player, damage);
            _battleState.AddToBattleLog(attackMessage);

            // Обновляем UI характеристик после удара врага
            OnPropertyChanged(nameof(PlayerCharacter));
            OnPropertyChanged(nameof(PlayerHealth));
            OnPropertyChanged(nameof(PlayerDamage));
            OnPropertyChanged(nameof(PlayerDefense));
            RefreshEnemiesUI();

            if (_battleLogic.IsCharacterDefeated(player))
            {
                LoggingService.LogDebug("ExecuteEnemyTurn: Игрок побежден, завершаем бой");
                EndBattle(false);
                return;
            }

            _battleState.IsEnemyUsingAbility = false;
            
            // Запускаем таймер безопасности для возврата хода игроку
            StartSafetyTimer();
            
            LoggingService.LogDebug("ExecuteEnemyTurn: Ожидаем завершения анимации для возврата хода игроку");
            
            // НЕ возвращаем ход игроку сразу - это будет сделано после завершения анимации
        }

        private void ExecuteSelectEnemy(Character enemy)
        {
            if (enemy != null && !enemy.IsDefeated)
            {
                _battleState.SelectedEnemy = enemy;
            }
        }

        private void ExecuteClickEnemy(Character enemy)
        {
            if (CanClickEnemy(enemy))
            {
                _battleState.SelectedEnemy = enemy;
                ExecuteAttack();
            }
        }

        private bool CanClickEnemy(Character enemy)
        {
            return _battleState.IsPlayerTurn && 
                   !_battleState.IsBattleOver && 
                   !_animations.IsAnimating && 
                   enemy != null && 
                   !enemy.IsDefeated;
        }

        private void ExecuteEndBattle()
        {
            LoggingService.LogInfo("=== ExecuteEndBattle: ������ '���������' ������ ===");
            
            if (_battleState.IsBattleOver)
            {
                LoggingService.LogInfo($"ExecuteEndBattle: ����� ���������, ������: {_battleState.BattleWon}");
                
                // ���� ����� ��������, ������������ �������
                if (_battleState.BattleWon && _gameState.BattleRewardItems != null && _gameState.BattleRewardItems.Count > 0)
                {
                    LoggingService.LogInfo($"ExecuteEndBattle: ������������ {_gameState.BattleRewardItems.Count} ������");
                    
                    foreach (var rewardItem in _gameState.BattleRewardItems)
                    {
                        LoggingService.LogInfo($"ExecuteEndBattle: ��������� � ���������: {rewardItem.Name}");
                        _gameState.Inventory.AddItem(rewardItem, 1);
                    }
                    
                    // ������� ������� ����� ����������
                    _gameState.BattleRewardItems.Clear();
                    LoggingService.LogInfo("ExecuteEndBattle: ������� ��������� � ��������� � �������");
                }
                else
                {
                    LoggingService.LogInfo("ExecuteEndBattle: ��� ������ ��� ���������");
                }
                
                LoggingService.LogInfo("ExecuteEndBattle: ��������� �� ����� ����");
                _navigateAction("WorldMapView");
            }
            else
            {
                LoggingService.LogWarning("ExecuteEndBattle: ����� ��� �� ���������");
            }
        }

        private void EndBattle(bool victory)
        {
            LoggingService.LogInfo($"=== EndBattle: ���������� �����, ������: {victory} ===");
            
            _battleState.IsBattleOver = true;
            _battleState.BattleWon = victory;

            if (victory)
            {
                _battleState.BattleResultMessage = "Победа!";
                _battleState.AddToBattleLog("Победа!");
                
                LoggingService.LogInfo($"EndBattle: IsBossHeroBattle = {_battleState.IsBossHeroBattle}");
                
                // �����: �������� ����� ��� ������������ ��� ������
                if (_battleState.IsBossHeroBattle && _gameState.CurrentLocation != null)
                {
                    _gameState.CurrentLocation.HeroDefeated = true;
                    _gameState.CurrentLocation.IsCompleted = true;
                    LoggingService.LogInfo($"Hero {_gameState.CurrentLocation.Hero?.Name} marked as defeated in {_gameState.CurrentLocation.Name}");
                    
                    // ������������ ��������� ������� ��������
                    UnlockNextLocation();
                }
                
                LoggingService.LogInfo("EndBattle: ���������� �������...");
                var rewards = _battleLogic.GenerateBattleRewards(
                    _gameState, 
                    _battleState.IsBossHeroBattle);
                
                LoggingService.LogInfo($"EndBattle: ������������� {rewards?.Count ?? 0} ������");
                
                if (rewards != null && rewards.Count > 0)
                {
                    foreach (var reward in rewards)
                    {
                        LoggingService.LogInfo($"EndBattle: �������: {reward.Name}");
                    }
                }
                else
                {
                    LoggingService.LogWarning("EndBattle: ������� �� ������������� ��� ������ ����");
                }
                
                _gameState.BattleRewardItems = rewards;
                LoggingService.LogInfo($"EndBattle: BattleRewardItems ����������, ����������: {_gameState.BattleRewardItems?.Count ?? 0}");
                
                // Отладочная информация о путях к изображениям
                if (rewards != null && rewards.Count > 0)
                {
                    foreach (var reward in rewards)
                    {
                        LoggingService.LogInfo($"EndBattle: Предмет '{reward.Name}' - SpritePath: '{reward.SpritePath}', ImagePath: '{reward.ImagePath}'");
                    }
                }
                
                // Уведомляем UI об изменении наград
                OnPropertyChanged(nameof(HasRewardItems));
                OnPropertyChanged(nameof(GameData));
            }
            else
            {
                _battleState.BattleResultMessage = "Поражение...";
                _battleState.AddToBattleLog("Поражение...");
                LoggingService.LogInfo("EndBattle: ,   ");
            }
            
            LoggingService.LogInfo("=== EndBattle: ���������� ������ ===");
        }
        
        /// <summary>
        /// ������������� ��������� ������� ����� ������ ��� ������
        /// </summary>
        private void UnlockNextLocation()
        {
            if (_gameState.CurrentLocation == null) return;
            
            int currentIndex = _gameState.CurrentLocationIndex;
            int nextIndex = currentIndex + 1;
            
            if (nextIndex < _gameState.Locations.Count)
            {
                var nextLocation = _gameState.Locations[nextIndex];
                nextLocation.IsUnlocked = true;
                nextLocation.IsAvailable = true;
                LoggingService.LogInfo($"Unlocked next location: {nextLocation.Name}");
            }
        }

        private void OnGameStateChanged(object sender, PropertyChangedEventArgs e)
        {
            // Обрабатываем изменения в GameData
            switch (e.PropertyName)
            {
                case nameof(GameData.BattleRewardItems):
                    OnPropertyChanged(nameof(HasRewardItems));
                    OnPropertyChanged(nameof(GameData));
                    OnPropertyChanged(nameof(GameState));
                    break;
            }
        }

        private void OnBattleStateChanged(object sender, PropertyChangedEventArgs e)
        {
            // Передаём изменения свойств из BattleState в ViewModel
            switch (e.PropertyName)
            {
                case nameof(BattleState.PlayerCharacter):
                    OnPropertyChanged(nameof(PlayerCharacter));
                    OnPropertyChanged(nameof(PlayerHealth));
                    OnPropertyChanged(nameof(PlayerDamage));
                    OnPropertyChanged(nameof(PlayerDefense));
                    break;
                case nameof(BattleState.SelectedEnemy):
                    OnPropertyChanged(nameof(SelectedEnemy));
                    OnPropertyChanged(nameof(CanAttack));
                    break;
                case nameof(BattleState.Enemies):
                    OnPropertyChanged(nameof(Enemies));
                    OnPropertyChanged(nameof(CanAttack));
                    break;
                case nameof(BattleState.IsPlayerTurn):
                    OnPropertyChanged(nameof(IsPlayerTurn));
                    OnPropertyChanged(nameof(CanAttack));
                    OnPropertyChanged(nameof(TurnMessage));
                    break;
                case nameof(BattleState.IsBattleOver):
                    OnPropertyChanged(nameof(IsBattleOver));
                    OnPropertyChanged(nameof(CanAttack));
                    break;
                case nameof(BattleState.BattleWon):
                    OnPropertyChanged(nameof(BattleWon));
                    break;
                case nameof(BattleState.BattleStatus):
                    OnPropertyChanged(nameof(BattleStatus));
                    break;
                case nameof(BattleState.BattleResultMessage):
                    OnPropertyChanged(nameof(BattleResultMessage));
                    break;
                case nameof(BattleState.UsableItems):
                    OnPropertyChanged(nameof(UsableItems));
                    break;
                case nameof(BattleState.SelectedItem):
                    OnPropertyChanged(nameof(SelectedItem));
                    break;
                case nameof(BattleState.ShowEnemySelection):
                    OnPropertyChanged(nameof(ShowEnemySelection));
                    break;
                case nameof(BattleState.BattleLog):
                    OnPropertyChanged(nameof(BattleLog));
                    break;
            }
        }

        private void OnAnimationsChanged(object sender, PropertyChangedEventArgs e)
        {
            LoggingService.LogDebug($"OnAnimationsChanged: {e.PropertyName} изменилось");
            
            // Передаём изменения свойств анимаций в ViewModel
            switch (e.PropertyName)
            {
                case nameof(BattleAnimations.IsAnimating):
                    LoggingService.LogDebug($"OnAnimationsChanged: IsAnimating = {_animations.IsAnimating}");
                    OnPropertyChanged(nameof(CanAttack));
                    break;
                case nameof(BattleAnimations.IsPlayerAttacking):
                    LoggingService.LogDebug($"OnAnimationsChanged: IsPlayerAttacking = {_animations.IsPlayerAttacking}");
                    OnPropertyChanged(nameof(IsPlayerAttacking));
                    break;
                case nameof(BattleAnimations.IsEnemyAttacking):
                    LoggingService.LogDebug($"OnAnimationsChanged: IsEnemyAttacking = {_animations.IsEnemyAttacking}");
                    OnPropertyChanged(nameof(IsEnemyAttacking));
                    break;
                case nameof(BattleAnimations.AnimationDamage):
                case nameof(BattleAnimations.IsCriticalHit):
                case nameof(BattleAnimations.AttackingCharacter):
                case nameof(BattleAnimations.TargetCharacter):
                    OnPropertyChanged(e.PropertyName);
                    // Обновляем характеристики при изменении анимации
                    OnPropertyChanged(nameof(PlayerCharacter));
                    OnPropertyChanged(nameof(PlayerHealth));
                    OnPropertyChanged(nameof(PlayerDamage));
                    OnPropertyChanged(nameof(PlayerDefense));
                    RefreshEnemiesUI();
                    break;
            }
        }

        private void OnAnimationCompleted()
        {
            LoggingService.LogDebug("OnAnimationCompleted: Анимация завершена");
            
            // Останавливаем таймер безопасности
            _safetyTimer?.Dispose();
            _safetyTimer = null;
            
            OnPropertyChanged(nameof(CanAttack));
            OnPropertyChanged(nameof(IsPlayerAttacking));
            OnPropertyChanged(nameof(IsEnemyAttacking));
            
            // Обновляем UI характеристик после завершения анимации
            OnPropertyChanged(nameof(PlayerCharacter));
            OnPropertyChanged(nameof(PlayerHealth));
            OnPropertyChanged(nameof(PlayerDamage));
            OnPropertyChanged(nameof(PlayerDefense));
            RefreshEnemiesUI();
            
            // Если бой уже завершен, ничего не делаем
            if (_battleState.IsBattleOver) 
            {
                LoggingService.LogDebug("OnAnimationCompleted: Бой уже завершен, выходим");
                return;
            }
            
            LoggingService.LogDebug($"OnAnimationCompleted: _waitingForEnemyTurn={_waitingForEnemyTurn}, IsPlayerTurn={_battleState.IsPlayerTurn}");
            
            // Если ждем хода врага (завершилась анимация игрока)
            if (_waitingForEnemyTurn)
            {
                _waitingForEnemyTurn = false;
                LoggingService.LogDebug("OnAnimationCompleted: Запускаем ход врага");
                
                // Запускаем ход врага после небольшой задержки
                System.Threading.Tasks.Task.Delay(300).ContinueWith(_ => 
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => 
                    {
                        if (!_battleState.IsBattleOver)
                        {
                            LoggingService.LogDebug("OnAnimationCompleted: Выполняем ExecuteEnemyTurn");
                            ExecuteEnemyTurn();
                        }
                        else
                        {
                            LoggingService.LogDebug("OnAnimationCompleted: Бой завершился во время задержки");
                        }
                    });
                });
            }
            // Иначе завершилась анимация врага - возвращаем ход игроку
            else if (!_battleState.IsPlayerTurn)
            {
                LoggingService.LogDebug("OnAnimationCompleted: Возвращаем ход игроку");
                _battleState.IsPlayerTurn = true;
                OnPropertyChanged(nameof(IsPlayerTurn));
                OnPropertyChanged(nameof(CanAttack));
            }
            else
            {
                LoggingService.LogDebug("OnAnimationCompleted: Неопределенное состояние - принудительно возвращаем ход игроку");
                _battleState.IsPlayerTurn = true;
                OnPropertyChanged(nameof(IsPlayerTurn));
                OnPropertyChanged(nameof(CanAttack));
            }
        }

        //   UI
        public BattleState BattleState => _battleState;
        public BattleAnimations Animations => _animations;
        public GameData GameData => _gameState;
        public GameData GameState => _gameState; // Alias for XAML compatibility

        // �������� �������������
        public Character PlayerCharacter => _battleState.PlayerCharacter;
        public Character SelectedEnemy 
        { 
            get => _battleState.SelectedEnemy;
            set => _battleState.SelectedEnemy = value;
        }
        public bool IsPlayerTurn => _battleState.IsPlayerTurn;
        public bool IsBattleOver => _battleState.IsBattleOver;
        public bool BattleWon => _battleState.BattleWon;
        public string BattleStatus => _battleState.BattleStatus;
        public string BattleResultMessage => _battleState.BattleResultMessage;
        
        // ����������� �������� ��� UI ��������
        public System.Collections.ObjectModel.ObservableCollection<Character> Enemies => _battleState.Enemies;
        public System.Collections.ObjectModel.ObservableCollection<string> BattleLog => _battleState.BattleLog;
        public System.Collections.ObjectModel.ObservableCollection<Item> UsableItems => _battleState.UsableItems;
        public Item SelectedItem 
        { 
            get => _battleState.SelectedItem;
            set => _battleState.SelectedItem = value;
        }
        public bool ShowEnemySelection => _battleState.ShowEnemySelection;
        public bool IsPlayerAttacking => _animations.IsPlayerAttacking;
        public bool IsEnemyAttacking => _animations.IsEnemyAttacking;
        public int AnimationDamage => _animations.AnimationDamage;
        public bool IsCriticalHit => _animations.IsCriticalHit;
        public Character AttackingCharacter => _animations.AttackingCharacter;
        public Character TargetCharacter => _animations.TargetCharacter;

        // Свойства для отображения характеристик игрока в UI
        public string PlayerHealth => $"{PlayerCharacter?.CurrentHealth ?? 0}/{PlayerCharacter?.MaxHealth ?? 0}";
        public string PlayerDamage => PlayerCharacter?.GetTotalAttack().ToString() ?? "0";
        public string PlayerDefense => PlayerCharacter?.GetTotalDefense().ToString() ?? "0";
        
        // Дополнительные свойства для боевого интерфейса
        public string TurnMessage => _battleState.IsPlayerTurn ? "Ваш ход" : "Ход противника";
        public string DamageMessage => _battleState.DamageMessage;
        public bool HasRewardItems => _gameState.BattleRewardItems?.Count > 0;

        public void Dispose()
        {
            LoggingService.LogDebug("BattleViewModel.Dispose: Освобождаем ресурсы");
            
            // Останавливаем таймер безопасности
            _safetyTimer?.Dispose();
            _safetyTimer = null;
            
            _animations?.Dispose();
            
            if (_battleState != null)
                _battleState.PropertyChanged -= OnBattleStateChanged;
            
            if (_animations != null)
            {
                _animations.PropertyChanged -= OnAnimationsChanged;
                _animations.AnimationCompleted -= OnAnimationCompleted;
            }
            
            if (_gameState != null)
                _gameState.PropertyChanged -= OnGameStateChanged;
        }

        // ��������� ����� ��� �������� ������������� � View
        public void EndBattlePublic(bool victory)
        {
            EndBattle(victory);
        }

        private void ExecuteForceEndTurn()
        {
            LoggingService.LogWarning("ExecuteForceEndTurn: Принудительное завершение хода");
            
            // Останавливаем все анимации
            _animations.StopAllAnimations();
            
            // Останавливаем таймер безопасности
            _safetyTimer?.Dispose();
            _safetyTimer = null;
            
            // Сбрасываем флаги ожидания
            _waitingForEnemyTurn = false;
            
            // Принудительно возвращаем ход игроку
            if (!_battleState.IsBattleOver)
            {
                _battleState.IsPlayerTurn = true;
                OnPropertyChanged(nameof(IsPlayerTurn));
                OnPropertyChanged(nameof(CanAttack));
                _battleState.AddToBattleLog("Ход принудительно завершен");
            }
        }

        private void StartSafetyTimer()
        {
            // Останавливаем предыдущий таймер, если он есть
            _safetyTimer?.Dispose();
            
            // Запускаем новый таймер на 3 секунды (достаточно для любой анимации)
            _safetyTimer = new System.Threading.Timer(SafetyTimerCallback, null, 3000, Timeout.Infinite);
            LoggingService.LogDebug("StartSafetyTimer: Запущен таймер безопасности на 3 секунды");
        }

        private void SafetyTimerCallback(object? state)
        {
            LoggingService.LogWarning("SafetyTimerCallback: Сработал таймер безопасности - принудительно завершаем ожидание");
            
            System.Windows.Application.Current.Dispatcher.Invoke(() => 
            {
                if (_waitingForEnemyTurn && !_battleState.IsBattleOver)
                {
                    LoggingService.LogWarning("SafetyTimerCallback: Принудительно запускаем ход врага");
                    _waitingForEnemyTurn = false;
                    ExecuteEnemyTurn();
                }
                else if (!_battleState.IsPlayerTurn && !_battleState.IsBattleOver)
                {
                    LoggingService.LogWarning("SafetyTimerCallback: Принудительно возвращаем ход игроку");
                    _battleState.IsPlayerTurn = true;
                    OnPropertyChanged(nameof(IsPlayerTurn));
                    OnPropertyChanged(nameof(CanAttack));
                }
            });
        }

        /// <summary>
        /// Принудительно обновляет UI для всех противников
        /// </summary>
        private void RefreshEnemiesUI()
        {
            // Принудительно обновляем каждого противника
            foreach (var enemy in _battleState.Enemies)
            {
                enemy.RefreshUI();
            }
            
            // Уведомляем об изменении коллекции противников
            OnPropertyChanged(nameof(Enemies));
        }
    }
} 
