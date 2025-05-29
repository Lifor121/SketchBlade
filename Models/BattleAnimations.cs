using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using SketchBlade.Models;
using SketchBlade.Services;

namespace SketchBlade.Models
{
    public enum ItemAnimationType
    {
        Drinking,  // Для зелий
        Throwing   // Для подушек, сюрикенов, бомб
    }

    public enum PotionEffectType
    {
        None,
        Healing,    // Розовый эффект для зелья лечения (одноразовый)
        Rage,       // Красный эффект для зелья ярости (по ходам)
        Defense,    // Синий эффект для зелья неуязвимости (по ходам)
        Poison      // Зеленый эффект для отравления (одноразовый при уроне)
    }

    public class BattleAnimations : INotifyPropertyChanged, IDisposable
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private bool _isAnimating = false;
        private bool _isPlayerAttacking = false;
        private bool _isEnemyAttacking = false;
        private bool _isUsingItem = false;
        private Character? _attackingCharacter;
        private Character? _targetCharacter;
        private int _animationDamage = 0;
        private bool _isCriticalHit = false;
        private Timer? _animationTimer; // Таймер для анимации
        private ItemAnimationType _currentItemAnimationType = ItemAnimationType.Drinking;
        private string _currentItemName = "";
        private int _animationDuration = 0;

        // Новые свойства для подсветки конкретного персонажа при использовании предметов
        private Character? _targetedCharacter = null; // Персонаж, на которого направлен предмет
        private bool _isTargetingItem = false; // Флаг для предметов, направленных на конкретную цель

        public event Action? AnimationCompleted;

        // Конструктор для явной инициализации
        public BattleAnimations()
        {
            // Явно инициализируем все свойства значениями по умолчанию
            _isAnimating = false;
            _isPlayerAttacking = false;
            _isEnemyAttacking = false;
            _isUsingItem = false;
            _attackingCharacter = null;
            _targetCharacter = null;
            _animationDamage = 0;
            _isCriticalHit = false;
            _currentItemAnimationType = ItemAnimationType.Drinking;
            _currentItemName = "";
            _animationDuration = 0;
            _targetedCharacter = null;
            _isTargetingItem = false;
        }

        public bool IsAnimating
        {
            get => _isAnimating;
            private set 
            {
                LoggingService.LogDebug($"BattleAnimations.IsAnimating: {_isAnimating} -> {value}");
                SetProperty(ref _isAnimating, value);
            }
        }

        public bool IsPlayerAttacking
        {
            get => _isPlayerAttacking;
            private set 
            {
                LoggingService.LogDebug($"BattleAnimations.IsPlayerAttacking: {_isPlayerAttacking} -> {value}");
                SetProperty(ref _isPlayerAttacking, value);
            }
        }

        public bool IsEnemyAttacking
        {
            get => _isEnemyAttacking;
            private set 
            {
                LoggingService.LogDebug($"BattleAnimations.IsEnemyAttacking: {_isEnemyAttacking} -> {value}");
                SetProperty(ref _isEnemyAttacking, value);
            }
        }

        public bool IsUsingItem
        {
            get => _isUsingItem;
            private set 
            {
                LoggingService.LogDebug($"BattleAnimations.IsUsingItem: {_isUsingItem} -> {value}");
                SetProperty(ref _isUsingItem, value);
            }
        }

        public ItemAnimationType CurrentItemAnimationType
        {
            get => _currentItemAnimationType;
            private set => SetProperty(ref _currentItemAnimationType, value);
        }

        public string CurrentItemName
        {
            get => _currentItemName;
            private set => SetProperty(ref _currentItemName, value);
        }

        public Character? AttackingCharacter
        {
            get => _attackingCharacter;
            private set 
            {
                LoggingService.LogDebug($"BattleAnimations.AttackingCharacter: {_attackingCharacter?.Name ?? "null"} -> {value?.Name ?? "null"}");
                SetProperty(ref _attackingCharacter, value);
            }
        }

        public Character? TargetCharacter
        {
            get => _targetCharacter;
            private set 
            {
                LoggingService.LogDebug($"BattleAnimations.TargetCharacter: {_targetCharacter?.Name ?? "null"} -> {value?.Name ?? "null"}");
                SetProperty(ref _targetCharacter, value);
            }
        }

        public int AnimationDamage
        {
            get => _animationDamage;
            private set => SetProperty(ref _animationDamage, value);
        }

        public bool IsCriticalHit
        {
            get => _isCriticalHit;
            private set => SetProperty(ref _isCriticalHit, value);
        }

        // Новые свойства для подсветки конкретного персонажа при использовании предметов
        public Character? TargetedCharacter
        {
            get => _targetedCharacter;
            private set => SetProperty(ref _targetedCharacter, value);
        }

        public bool IsTargetingItem
        {
            get => _isTargetingItem;
            private set => SetProperty(ref _isTargetingItem, value);
        }

        public void StartAttackAnimation(Character attacker, Character target, int damage, bool isCritical)
        {
            LoggingService.LogDebug($"=== StartAttackAnimation START ===");
            LoggingService.LogDebug($"Attacker: {attacker.Name} (IsPlayer: {attacker.IsPlayer})");
            LoggingService.LogDebug($"Target: {target.Name} (IsPlayer: {target.IsPlayer})");
            LoggingService.LogDebug($"Damage: {damage}, Critical: {isCritical}");
            LoggingService.LogDebug($"Current state - IsAnimating: {IsAnimating}, IsPlayerAttacking: {IsPlayerAttacking}, IsEnemyAttacking: {IsEnemyAttacking}");

            if (IsAnimating) 
            {
                LoggingService.LogWarning("StartAttackAnimation: Анимация уже выполняется, принудительно завершаем предыдущую");
                CompleteAnimation();
            }

            // Останавливаем предыдущий таймер, если он есть
            _animationTimer?.Dispose();

            AttackingCharacter = attacker;
            TargetCharacter = target;
            AnimationDamage = damage;
            IsCriticalHit = isCritical;

            LoggingService.LogDebug($"Setting animation flags...");
            IsPlayerAttacking = attacker.IsPlayer;
            IsEnemyAttacking = !attacker.IsPlayer;
            IsUsingItem = false;
            LoggingService.LogDebug($"After setting flags - IsPlayerAttacking: {IsPlayerAttacking}, IsEnemyAttacking: {IsEnemyAttacking}");

            IsAnimating = true;

            // Запускаем таймер для завершения анимации
            // Анимация игрока: 0.6 секунды, анимация врага: 0.55 секунды
            int animationDuration = attacker.IsPlayer ? 700 : 650; // увеличиваем время для надежности
            
            LoggingService.LogDebug($"Запускаем таймер на {animationDuration}мс");
            
            _animationTimer = new Timer(_ => 
            {
                LoggingService.LogDebug("Таймер сработал, завершаем анимацию");
                CompleteAnimation();
            }, null, animationDuration, Timeout.Infinite);

            LoggingService.LogDebug($"=== StartAttackAnimation END ===");
        }

        public void StartItemUseAnimation(string itemName)
        {
            StartItemUseAnimation(itemName, null);
        }

        public void StartItemUseAnimation(string itemName, Character? target)
        {
            if (IsAnimating) return;

            // Останавливаем предыдущий таймер, если он есть
            _animationTimer?.Dispose();

            IsPlayerAttacking = false;
            IsEnemyAttacking = false;
            IsUsingItem = true;
            IsAnimating = true;
            CurrentItemName = itemName;
            TargetedCharacter = target;

            // Определяем тип анимации на основе названия предмета
            string normalizedName = itemName.ToLower();
            
            // Проверяем на зелья (питье)
            if (normalizedName.Contains("зелье") || 
                normalizedName.Contains("potion") || 
                normalizedName.Contains("healing") ||
                normalizedName.Contains("лечения") ||
                normalizedName.Contains("ярости") ||
                normalizedName.Contains("rage") ||
                normalizedName.Contains("неуязвимости") ||
                normalizedName.Contains("invulnerability"))
            {
                CurrentItemAnimationType = ItemAnimationType.Drinking;
                _animationDuration = 200; // Сокращаем время анимации для зелий
                IsTargetingItem = false; // Зелья не направлены на конкретную цель
            }
            // Проверяем на метательные предметы
            else if (normalizedName.Contains("подушка") ||
                     normalizedName.Contains("pillow") ||
                     normalizedName.Contains("сюрикен") ||
                     normalizedName.Contains("shuriken"))
            {
                CurrentItemAnimationType = ItemAnimationType.Throwing;
                _animationDuration = 600; // Быстрее для броска
                IsTargetingItem = target != null; // Есть ли конкретная цель
            }
            // Проверяем на бомбу
            else if (normalizedName.Contains("бомба") || 
                     normalizedName.Contains("bomb"))
            {
                CurrentItemAnimationType = ItemAnimationType.Throwing;
                _animationDuration = 600; // Быстрее для броска
                IsTargetingItem = false; // Бомба не направлена на конкретную цель
            }
            else
            {
                // По умолчанию - питье
                CurrentItemAnimationType = ItemAnimationType.Drinking;
                _animationDuration = 200;
                IsTargetingItem = false;
            }

            LoggingService.LogDebug($"StartItemUseAnimation: {itemName} -> {CurrentItemAnimationType}, duration: {_animationDuration}ms, target: {target?.Name ?? "none"}");

            // Запускаем таймер завершения анимации
            _animationTimer = new System.Threading.Timer(OnAnimationCompleted, null, _animationDuration, Timeout.Infinite);
        }

        private void OnAnimationCompleted(object? state)
        {
            LoggingService.LogDebug("OnAnimationCompleted: Таймер анимации сработал");
            CompleteAnimation();
        }

        private void CompleteAnimation()
        {
            LoggingService.LogDebug("CompleteAnimation: Завершаем анимацию");
            
            // Останавливаем таймер
            _animationTimer?.Dispose();
            _animationTimer = null;

            // Сбрасываем все флаги анимации
            IsPlayerAttacking = false;
            IsEnemyAttacking = false;
            IsUsingItem = false;
            IsAnimating = false;
            AttackingCharacter = null;
            TargetCharacter = null;
            AnimationDamage = 0;
            IsCriticalHit = false;
            CurrentItemName = "";
            TargetedCharacter = null;
            IsTargetingItem = false;

            // Уведомляем о завершении анимации
            LoggingService.LogDebug("CompleteAnimation: Вызываем AnimationCompleted");
            AnimationCompleted?.Invoke();
        }

        public void StopAllAnimations()
        {
            LoggingService.LogDebug("StopAllAnimations: Принудительная остановка всех анимаций");
            
            _animationTimer?.Dispose();
            _animationTimer = null;

            CompleteAnimation();
        }

        protected virtual void SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (!Equals(field, value))
            {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public void Dispose()
        {
            LoggingService.LogDebug("BattleAnimations.Dispose: Освобождаем ресурсы");
            
            _animationTimer?.Dispose();
            _animationTimer = null;
        }
    }
} 