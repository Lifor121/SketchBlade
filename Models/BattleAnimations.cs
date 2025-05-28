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

        // Новые свойства для цветовых эффектов зелий
        private bool _isPlayerPotionEffect = false;
        private bool _isEnemyPotionEffect = false;
        private PotionEffectType _currentPotionEffect = PotionEffectType.None;
        private Timer? _potionEffectTimer;
        private int _potionEffectTurnsRemaining = 0; // Для эффектов по ходам
        private bool _isPlayerTurnBasedEffect = false; // Флаг для эффектов по ходам
        private bool _isEnemyTurnBasedEffect = false;

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
            _isPlayerPotionEffect = false;
            _isEnemyPotionEffect = false;
            _currentPotionEffect = PotionEffectType.None;
            _potionEffectTurnsRemaining = 0;
            _isPlayerTurnBasedEffect = false;
            _isEnemyTurnBasedEffect = false;
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

        // Новые свойства для цветовых эффектов
        public bool IsPlayerPotionEffect
        {
            get => _isPlayerPotionEffect;
            private set => SetProperty(ref _isPlayerPotionEffect, value);
        }

        public bool IsEnemyPotionEffect
        {
            get => _isEnemyPotionEffect;
            private set => SetProperty(ref _isEnemyPotionEffect, value);
        }

        public PotionEffectType CurrentPotionEffect
        {
            get => _currentPotionEffect;
            private set => SetProperty(ref _currentPotionEffect, value);
        }

        public int PotionEffectTurnsRemaining
        {
            get => _potionEffectTurnsRemaining;
            private set => SetProperty(ref _potionEffectTurnsRemaining, value);
        }

        public bool IsPlayerTurnBasedEffect
        {
            get => _isPlayerTurnBasedEffect;
            private set => SetProperty(ref _isPlayerTurnBasedEffect, value);
        }

        public bool IsEnemyTurnBasedEffect
        {
            get => _isEnemyTurnBasedEffect;
            private set => SetProperty(ref _isEnemyTurnBasedEffect, value);
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

        // Новые публичные свойства для подсветки конкретного персонажа
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
            
            // Проверяем на зелья (питье) - подсвечиваем игрока
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
                
                // Определяем тип эффекта зелья
                if (normalizedName.Contains("healing") || normalizedName.Contains("лечения"))
                {
                    CurrentPotionEffect = PotionEffectType.Healing;
                }
                else if (normalizedName.Contains("rage") || normalizedName.Contains("ярости"))
                {
                    CurrentPotionEffect = PotionEffectType.Rage;
                }
                else if (normalizedName.Contains("invulnerability") || normalizedName.Contains("неуязвимости"))
                {
                    CurrentPotionEffect = PotionEffectType.Defense;
                }
                else
                {
                    CurrentPotionEffect = PotionEffectType.Healing; // По умолчанию
                }
                
                // Запускаем цветовой эффект для игрока (только для зелий)
                StartPotionColorEffect(true);
            }
            // Проверяем на метательные предметы - подсвечиваем противника (кроме бомбы)
            else if (normalizedName.Contains("подушка") ||
                     normalizedName.Contains("pillow") ||
                     normalizedName.Contains("сюрикен") ||
                     normalizedName.Contains("shuriken"))
            {
                CurrentItemAnimationType = ItemAnimationType.Throwing;
                _animationDuration = 600; // Быстрее для броска
                CurrentPotionEffect = PotionEffectType.None;
                IsTargetingItem = target != null; // Есть ли конкретная цель
                
                // Для метательных предметов (кроме бомбы) можно добавить эффект для противника
                // Пока оставляем без цветового эффекта, так как это не зелья
            }
            // Проверяем на бомбу - никого не подсвечиваем
            else if (normalizedName.Contains("бомба") || 
                     normalizedName.Contains("bomb"))
            {
                CurrentItemAnimationType = ItemAnimationType.Throwing;
                _animationDuration = 600; // Быстрее для броска
                CurrentPotionEffect = PotionEffectType.None;
                IsTargetingItem = false; // Бомба не направлена на конкретную цель
                
                // Для бомбы не запускаем цветовой эффект
            }
            else
            {
                // По умолчанию - питье (для неизвестных предметов считаем зельями)
                CurrentItemAnimationType = ItemAnimationType.Drinking;
                _animationDuration = 200;
                CurrentPotionEffect = PotionEffectType.Healing;
                IsTargetingItem = false;
                StartPotionColorEffect(true);
            }

            LoggingService.LogDebug($"StartItemUseAnimation: {itemName} -> {CurrentItemAnimationType}, duration: {_animationDuration}ms, effect: {CurrentPotionEffect}, target: {target?.Name ?? "none"}");

            // Запускаем таймер завершения анимации
            _animationTimer = new System.Threading.Timer(OnAnimationCompleted, null, _animationDuration, Timeout.Infinite);
        }

        // Новый метод для запуска цветового эффекта зелья
        public void StartPotionColorEffect(bool isPlayer, int turns = 0)
        {
            // Останавливаем предыдущий эффект, если он есть
            _potionEffectTimer?.Dispose();
            
            if (isPlayer)
            {
                IsPlayerPotionEffect = true;
                IsEnemyPotionEffect = false;
            }
            else
            {
                IsPlayerPotionEffect = false;
                IsEnemyPotionEffect = true;
            }
            
            // Определяем тип эффекта и длительность
            if (CurrentPotionEffect == PotionEffectType.Healing || CurrentPotionEffect == PotionEffectType.Poison)
            {
                // Одноразовые эффекты - показываем на 2 секунды
                int effectDuration = 2000; // 2 секунды
                PotionEffectTurnsRemaining = 0;
                
                if (isPlayer)
                {
                    IsPlayerTurnBasedEffect = false;
                    IsEnemyTurnBasedEffect = false;
                }
                else
                {
                    IsPlayerTurnBasedEffect = false;
                    IsEnemyTurnBasedEffect = false;
                }
                
                LoggingService.LogDebug($"StartPotionColorEffect: одноразовый эффект {CurrentPotionEffect}, isPlayer={isPlayer}, duration={effectDuration}ms");
                
                // Запускаем таймер для завершения цветового эффекта
                _potionEffectTimer = new Timer(_ => 
                {
                    LoggingService.LogDebug("Одноразовый цветовой эффект завершен");
                    StopPotionColorEffect();
                }, null, effectDuration, Timeout.Infinite);
            }
            else if (CurrentPotionEffect == PotionEffectType.Rage || CurrentPotionEffect == PotionEffectType.Defense)
            {
                // Эффекты по ходам - показываем пока не закончатся ходы
                PotionEffectTurnsRemaining = turns;
                
                if (isPlayer)
                {
                    IsPlayerTurnBasedEffect = true;
                    IsEnemyTurnBasedEffect = false;
                }
                else
                {
                    IsPlayerTurnBasedEffect = false;
                    IsEnemyTurnBasedEffect = true;
                }
                
                LoggingService.LogDebug($"StartPotionColorEffect: эффект по ходам {CurrentPotionEffect}, isPlayer={isPlayer}, turns={turns}");
            }
        }

        // Метод для остановки цветового эффекта
        public void StopPotionColorEffect()
        {
            _potionEffectTimer?.Dispose();
            _potionEffectTimer = null;
            
            IsPlayerPotionEffect = false;
            IsEnemyPotionEffect = false;
            CurrentPotionEffect = PotionEffectType.None;
            PotionEffectTurnsRemaining = 0;
            IsPlayerTurnBasedEffect = false;
            IsEnemyTurnBasedEffect = false;
            
            LoggingService.LogDebug("StopPotionColorEffect: Цветовой эффект остановлен");
        }

        // Новый метод для запуска эффекта зелья у врага
        public void StartEnemyPotionEffect(PotionEffectType effectType, int turns = 0)
        {
            CurrentPotionEffect = effectType;
            StartPotionColorEffect(false, turns);
        }

        // Новый метод для запуска эффекта зелья у игрока
        public void StartPlayerPotionEffect(PotionEffectType effectType, int turns = 0)
        {
            CurrentPotionEffect = effectType;
            StartPotionColorEffect(true, turns);
        }

        // Метод для уменьшения количества ходов эффекта
        public void DecrementTurnBasedEffect()
        {
            if (PotionEffectTurnsRemaining > 0)
            {
                PotionEffectTurnsRemaining--;
                LoggingService.LogDebug($"DecrementTurnBasedEffect: осталось ходов {PotionEffectTurnsRemaining}");
                
                if (PotionEffectTurnsRemaining <= 0)
                {
                    LoggingService.LogDebug("DecrementTurnBasedEffect: эффект по ходам завершен");
                    StopPotionColorEffect();
                }
            }
        }

        // Метод для запуска эффекта отравления
        public void StartPoisonEffect(bool isPlayer)
        {
            CurrentPotionEffect = PotionEffectType.Poison;
            StartPotionColorEffect(isPlayer);
        }

        private void OnAnimationCompleted(object? state)
        {
            LoggingService.LogDebug("OnAnimationCompleted: Завершаем анимацию");
            CompleteAnimation();
        }

        private void CompleteAnimation()
        {
            LoggingService.LogDebug("=== CompleteAnimation START ===");
            LoggingService.LogDebug($"Current state - IsAnimating: {IsAnimating}, IsPlayerAttacking: {IsPlayerAttacking}, IsEnemyAttacking: {IsEnemyAttacking}, IsUsingItem: {IsUsingItem}");
            
            // Останавливаем таймер
            _animationTimer?.Dispose();
            _animationTimer = null;
            
            IsAnimating = false;
            IsPlayerAttacking = false;
            IsEnemyAttacking = false;
            IsUsingItem = false;
            AttackingCharacter = null;
            TargetCharacter = null;
            AnimationDamage = 0;
            IsCriticalHit = false;
            CurrentItemName = "";
            CurrentItemAnimationType = ItemAnimationType.Drinking; // Сбрасываем тип анимации
            
            // Сбрасываем новые свойства для подсветки персонажей
            TargetedCharacter = null;
            IsTargetingItem = false;

            LoggingService.LogDebug("Вызываем событие AnimationCompleted");
            AnimationCompleted?.Invoke();
            LoggingService.LogDebug("=== CompleteAnimation END ===");
        }

        public void StopAllAnimations()
        {
            LoggingService.LogDebug("=== StopAllAnimations ===");
            LoggingService.LogDebug($"Current state - IsAnimating: {IsAnimating}");
            if (IsAnimating)
            {
                CompleteAnimation();
            }
            
            // Также останавливаем цветовые эффекты
            StopPotionColorEffect();
        }

        protected virtual void SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            try
            {
                if (!Equals(field, value))
                {
                    field = value;
                    LoggingService.LogDebug($"BattleAnimations.{propertyName}: Property changed to {value}");
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"BattleAnimations.SetProperty error for {propertyName}: {ex.Message}");
                // В случае ошибки все равно устанавливаем значение
                field = value;
            }
        }

        public void Dispose()
        {
            LoggingService.LogDebug("BattleAnimations.Dispose: Освобождаем ресурсы");
            _animationTimer?.Dispose();
            _animationTimer = null;
            _potionEffectTimer?.Dispose();
            _potionEffectTimer = null;
        }
    }
} 