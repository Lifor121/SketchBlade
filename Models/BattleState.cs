using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SketchBlade.Models;

namespace SketchBlade.Models
{
    public class BattleState : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private Character _playerCharacter;
        private Character _selectedEnemy;
        private ObservableCollection<Character> _enemies = new();
        
        private ObservableCollection<Item> _usableItems = new();
        private Item _selectedItem;
        
        private bool _isPlayerTurn = true;
        private bool _isBattleOver = false;
        private bool _battleWon = false;
        private int _turnCounter = 0;
        private bool _isBossHeroBattle = false;
        
        private string _battleStatus = "";
        private string _turnMessage = "";
        private string _battleResultMessage = "";
        private string _rewardMessage = "";
        private string _damageMessage = "";
        
        private ObservableCollection<string> _battleLog = new();
        private bool _isEnemyUsingAbility = false;
        private string _enemyAbilityName = "";
        private int _enemyAbilityDamage = 0;
        
        private bool _rewardsProcessed = false;
        private int _currentEnemyIndex = 0;

        // Новые свойства для режима выбора цели предметов
        private bool _isTargetSelectionMode = false;
        private Item _pendingTargetItem = null;
        private string _targetSelectionMessage = "";

        public Character PlayerCharacter
        {
            get => _playerCharacter;
            set => SetProperty(ref _playerCharacter, value);
        }

        public Character SelectedEnemy
        {
            get => _selectedEnemy;
            set => SetProperty(ref _selectedEnemy, value);
        }

        public ObservableCollection<Character> Enemies
        {
            get => _enemies;
            set => SetProperty(ref _enemies, value);
        }

        public ObservableCollection<Item> UsableItems
        {
            get => _usableItems;
            set => SetProperty(ref _usableItems, value);
        }

        public Item SelectedItem
        {
            get => _selectedItem;
            set => SetProperty(ref _selectedItem, value);
        }

        public bool IsPlayerTurn
        {
            get => _isPlayerTurn;
            set => SetProperty(ref _isPlayerTurn, value);
        }

        public bool IsBattleOver
        {
            get => _isBattleOver;
            set => SetProperty(ref _isBattleOver, value);
        }

        public bool BattleWon
        {
            get => _battleWon;
            set => SetProperty(ref _battleWon, value);
        }

        public int TurnCounter
        {
            get => _turnCounter;
            set => SetProperty(ref _turnCounter, value);
        }

        public bool IsBossHeroBattle
        {
            get => _isBossHeroBattle;
            set => SetProperty(ref _isBossHeroBattle, value);
        }

        public string BattleStatus
        {
            get => _battleStatus;
            set => SetProperty(ref _battleStatus, value);
        }

        public string TurnMessage
        {
            get => _turnMessage;
            set => SetProperty(ref _turnMessage, value);
        }

        public string BattleResultMessage
        {
            get => _battleResultMessage;
            set => SetProperty(ref _battleResultMessage, value);
        }

        public string RewardMessage
        {
            get => _rewardMessage;
            set => SetProperty(ref _rewardMessage, value);
        }

        public string DamageMessage
        {
            get => _damageMessage;
            set => SetProperty(ref _damageMessage, value);
        }

        public ObservableCollection<string> BattleLog
        {
            get => _battleLog;
            set => SetProperty(ref _battleLog, value);
        }

        public bool IsEnemyUsingAbility
        {
            get => _isEnemyUsingAbility;
            set => SetProperty(ref _isEnemyUsingAbility, value);
        }

        public string EnemyAbilityName
        {
            get => _enemyAbilityName;
            set => SetProperty(ref _enemyAbilityName, value);
        }

        public int EnemyAbilityDamage
        {
            get => _enemyAbilityDamage;
            set => SetProperty(ref _enemyAbilityDamage, value);
        }

        public bool RewardsProcessed
        {
            get => _rewardsProcessed;
            set => SetProperty(ref _rewardsProcessed, value);
        }

        public int CurrentEnemyIndex
        {
            get => _currentEnemyIndex;
            set => SetProperty(ref _currentEnemyIndex, value);
        }

        // Новые свойства для режима выбора цели предметов
        public bool IsTargetSelectionMode
        {
            get => _isTargetSelectionMode;
            set => SetProperty(ref _isTargetSelectionMode, value);
        }

        public Item PendingTargetItem
        {
            get => _pendingTargetItem;
            set => SetProperty(ref _pendingTargetItem, value);
        }

        public string TargetSelectionMessage
        {
            get => _targetSelectionMessage;
            set => SetProperty(ref _targetSelectionMessage, value);
        }

        public int PlayerHealth => PlayerCharacter?.CurrentHealth ?? 0;
        public int PlayerMaxHealth => PlayerCharacter?.MaxHealth ?? 0;
        public double PlayerHealthPercent => PlayerCharacter != null ? 
            ((double)PlayerCharacter.CurrentHealth / PlayerCharacter.MaxHealth) * 100 : 0;
        public int PlayerDamage => PlayerCharacter?.GetTotalAttack() ?? 0;
        public int PlayerDefense => PlayerCharacter?.GetTotalDefense() ?? 0;
        public bool ShowEnemySelection => Enemies.Count(e => !e.IsDefeated) > 1;

        public void AddToBattleLog(string message)
        {
            // Логи боя отключены по просьбе пользователя
            // BattleLog.Add($"{DateTime.Now:HH:mm:ss}: {message}");
        }

        public void Reset()
        {
            IsPlayerTurn = true;
            IsBattleOver = false;
            BattleWon = false;
            TurnCounter = 0;
            RewardsProcessed = false;
            CurrentEnemyIndex = 0;
            
            // Сброс режима выбора цели
            IsTargetSelectionMode = false;
            PendingTargetItem = null;
            TargetSelectionMessage = "";
            
            BattleLog.Clear();
            Enemies.Clear();
            UsableItems.Clear();
            
            SelectedEnemy = null;
            SelectedItem = null;
            
            BattleStatus = "";
            TurnMessage = "";
            BattleResultMessage = "";
            RewardMessage = "";
            DamageMessage = "";
            
            IsEnemyUsingAbility = false;
            EnemyAbilityName = "";
            EnemyAbilityDamage = 0;
        }

        protected virtual void SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (!Equals(field, value))
            {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
} 