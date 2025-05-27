using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using SketchBlade.Models;
using SketchBlade.Services;

namespace SketchBlade.Models
{
    public class BattleAnimations : INotifyPropertyChanged, IDisposable
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private bool _isAnimating = false;
        private bool _isPlayerAttacking = false;
        private bool _isEnemyAttacking = false;
        private Character? _attackingCharacter;
        private Character? _targetCharacter;
        private int _animationDamage = 0;
        private bool _isCriticalHit = false;
        private Timer? _animationTimer; // Таймер для анимации

        public event Action? AnimationCompleted;

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

        private void CompleteAnimation()
        {
            LoggingService.LogDebug("=== CompleteAnimation START ===");
            LoggingService.LogDebug($"Current state - IsAnimating: {IsAnimating}, IsPlayerAttacking: {IsPlayerAttacking}, IsEnemyAttacking: {IsEnemyAttacking}");
            
            // Останавливаем таймер
            _animationTimer?.Dispose();
            _animationTimer = null;
            
            IsAnimating = false;
            IsPlayerAttacking = false;
            IsEnemyAttacking = false;
            AttackingCharacter = null;
            TargetCharacter = null;
            AnimationDamage = 0;
            IsCriticalHit = false;

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
        }

        protected virtual void SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (!Equals(field, value))
            {
                field = value;
                LoggingService.LogDebug($"BattleAnimations.{propertyName}: Property changed to {value}");
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