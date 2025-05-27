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

        public ICommand NavigateCommand { get; private set; }
        public ICommand AttackCommand { get; private set; }
        public ICommand UseItemCommand { get; private set; }
        public ICommand EndBattleCommand { get; private set; }
        public ICommand SelectEnemyCommand { get; private set; }
        public ICommand ClickEnemyCommand { get; private set; }

        public BattleViewModel(GameData GameData, Action<string> navigateAction)
        {
            // LoggingService.LogDebug("=== BattleViewModel CONSTRUCTOR CALLED ===");
            
            _gameState = GameData;
            _navigateAction = navigateAction;
            
            _battleState = new BattleState();
            _battleLogic = new BattleLogic(GameData);
            _animations = new BattleAnimations();

            _battleState.PropertyChanged += OnBattleStateChanged;
            _animations.PropertyChanged += OnAnimationsChanged;
            _animations.AnimationCompleted += OnAnimationCompleted;

            InitializeCommands();
            InitializeBattle();
            
            // LoggingService.LogDebug("=== BattleViewModel CONSTRUCTOR COMPLETED ===");
        }

        private void InitializeCommands()
        {
            NavigateCommand = new RelayCommand<string>(ExecuteNavigate, null, "Navigate");
            AttackCommand = new RelayCommand<object>(_ => ExecuteAttack(), _ => CanAttack(), "Attack");
            UseItemCommand = new RelayCommand<Item>(ExecuteUseItem, CanUseItem, "UseItem");
            EndBattleCommand = new RelayCommand<object>(_ => ExecuteEndBattle(), null, "EndBattle");
            SelectEnemyCommand = new RelayCommand<Character>(ExecuteSelectEnemy, null, "SelectEnemy");
            ClickEnemyCommand = new RelayCommand<Character>(ExecuteClickEnemy, CanClickEnemy, "ClickEnemy");
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
            _battleState.BattleStatus = "��� �������!";
            _battleState.AddToBattleLog("��� �������!");
            
            // ���������� �����: ��������� �������������� ����������
            bool allEnemiesDefeated = _battleLogic.IsAllEnemiesDefeated(_battleState);
            // LoggingService.LogDebug($"All enemies defeated check: {allEnemiesDefeated}");
            
            if (allEnemiesDefeated)
            {
                LoggingService.LogError("�������� �������: ��� ����� ��� ��������� ��� �������������!");
                // �� ������������� ��������� ����� - ��� ������!
                LoggingService.LogError("�� ��������� ����� ������������� - ��� ���!");
            }
            
            // LoggingService.LogDebug($"Battle initialized - IsBattleOver: {_battleState.IsBattleOver}, IsPlayerTurn: {_battleState.IsPlayerTurn}");
            // LoggingService.LogDebug("=== InitializeBattle COMPLETED ===");
        }

        private void LoadUsableItems()
        {
            _battleState.UsableItems.Clear();
            
            // ��������� ������ �� null ��������� � ���������
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
            if (_battleState.SelectedEnemy == null || _animations.IsAnimating) return;

            var target = _battleState.SelectedEnemy;
            var player = _battleState.PlayerCharacter;

            int damage = _battleLogic.CalculateDamage(player, target);
            bool isCritical = _battleLogic.IsCriticalHit();

            if (isCritical) damage = (int)(damage * 1.5);

            _animations.StartAttackAnimation(player, target, damage, isCritical);
            _battleLogic.ApplyDamage(target, damage);

            string attackMessage = isCritical 
                ? $"����������� ����! ����� ����� {damage} ����� {target.Name}"
                : $"����� �������� {target.Name} � ����� {damage} �����";
            
            _battleState.AddToBattleLog(attackMessage);

            if (_battleLogic.IsCharacterDefeated(target))
            {
                target.SetDefeated(true);
                _battleState.AddToBattleLog($"{target.Name} ��������!");
                
                if (_battleLogic.IsAllEnemiesDefeated(_battleState))
                {
                    EndBattle(true);
                    return;
                }
            }

            _battleState.IsPlayerTurn = false;
            ExecuteEnemyTurn();
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
                    _battleState.AddToBattleLog("����� ���������� ����� �������");
                    break;
                case "Rage Potion":
                    _battleLogic.ApplyRagePotion(player, 10, 3);
                    _battleState.AddToBattleLog("����� ���������� ����� ������");
                    break;
                case "Bomb":
                    int bombDamage = _battleLogic.CalculateBombDamage();
                    foreach (var enemy in _battleState.Enemies.Where(e => !e.IsDefeated))
                    {
                        _battleLogic.ApplyDamage(enemy, bombDamage);
                    }
                    _battleState.AddToBattleLog($"����� ������� �����, ������� {bombDamage} ����� ���� ������");
                    break;
            }

            _gameState.Inventory.RemoveItem(item, 1);
            LoadUsableItems();

            _battleState.IsPlayerTurn = false;
            ExecuteEnemyTurn();
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
            var activeEnemy = _battleLogic.GetNextActiveEnemy(_battleState);
            if (activeEnemy == null)
            {
                EndBattle(true);
                return;
            }

            var player = _battleState.PlayerCharacter;
            bool useSpecialAbility = _battleLogic.ShouldEnemyUseSpecialAbility(activeEnemy);
            int damage;
            string attackMessage;

            if (useSpecialAbility)
            {
                damage = _battleLogic.CalculateSpecialAbilityDamage(activeEnemy, player);
                string abilityName = _battleLogic.GetEnemySpecialAbilityName(activeEnemy);
                attackMessage = $"{activeEnemy.Name} ���������� {abilityName} � ������� {damage} �����";
                
                _battleState.IsEnemyUsingAbility = true;
                _battleState.EnemyAbilityName = abilityName;
                _battleState.EnemyAbilityDamage = damage;
            }
            else
            {
                damage = _battleLogic.CalculateDamage(activeEnemy, player);
                attackMessage = $"{activeEnemy.Name} ������� � ������� {damage} �����";
            }

            _battleLogic.ApplyDamage(player, damage);
            _battleState.AddToBattleLog(attackMessage);

            if (_battleLogic.IsCharacterDefeated(player))
            {
                EndBattle(false);
                return;
            }

            _battleState.IsPlayerTurn = true;
            _battleState.IsEnemyUsingAbility = false;
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
                _battleState.BattleResultMessage = "������!";
                _battleState.AddToBattleLog("����� ������� �����!");
                
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
            }
            else
            {
                _battleState.BattleResultMessage = "���������...";
                _battleState.AddToBattleLog("����� ��������...");
                LoggingService.LogInfo("EndBattle: ���������, ������� �� ������������");
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

        private void OnBattleStateChanged(object sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(e.PropertyName);
        }

        private void OnAnimationsChanged(object sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(e.PropertyName);
        }

        private void OnAnimationCompleted()
        {
            OnPropertyChanged(nameof(CanAttack));
        }

        // �������� ��� UI
        public BattleState BattleState => _battleState;
        public BattleAnimations Animations => _animations;
        public GameData GameData => _gameState;

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

        public void Dispose()
        {
            _animations?.Dispose();
            
            if (_battleState != null)
                _battleState.PropertyChanged -= OnBattleStateChanged;
            
            if (_animations != null)
            {
                _animations.PropertyChanged -= OnAnimationsChanged;
                _animations.AnimationCompleted -= OnAnimationCompleted;
            }
        }

        // ��������� ����� ��� �������� ������������� � View
        public void EndBattlePublic(bool victory)
        {
            EndBattle(victory);
        }
    }
} 
