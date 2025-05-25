using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using SketchBlade.Models;

namespace SketchBlade.Models
{
    public class BattleAnimations : INotifyPropertyChanged, IDisposable
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private Timer? _animationTimer;
        private const int ANIMATION_UPDATE_INTERVAL = 35;
        private const double ANIMATION_SPEED = 0.08;

        private bool _isAnimating = false;
        private bool _isPlayerAttacking = false;
        private bool _isEnemyAttacking = false;
        private double _attackAnimationProgress = 0.0;
        private Character? _attackingCharacter;
        private Character? _targetCharacter;
        private int _animationDamage = 0;
        private bool _isCriticalHit = false;

        public event Action? AnimationCompleted;

        public bool IsAnimating
        {
            get => _isAnimating;
            private set => SetProperty(ref _isAnimating, value);
        }

        public bool IsPlayerAttacking
        {
            get => _isPlayerAttacking;
            private set => SetProperty(ref _isPlayerAttacking, value);
        }

        public bool IsEnemyAttacking
        {
            get => _isEnemyAttacking;
            private set => SetProperty(ref _isEnemyAttacking, value);
        }

        public double AttackAnimationProgress
        {
            get => _attackAnimationProgress;
            private set => SetProperty(ref _attackAnimationProgress, value);
        }

        public Character? AttackingCharacter
        {
            get => _attackingCharacter;
            private set => SetProperty(ref _attackingCharacter, value);
        }

        public Character? TargetCharacter
        {
            get => _targetCharacter;
            private set => SetProperty(ref _targetCharacter, value);
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

        public BattleAnimations()
        {
            _animationTimer = new Timer(OnAnimationTimerTick, null, Timeout.Infinite, ANIMATION_UPDATE_INTERVAL);
        }

        public void StartAttackAnimation(Character attacker, Character target, int damage, bool isCritical)
        {
            if (IsAnimating) return;

            AttackingCharacter = attacker;
            TargetCharacter = target;
            AnimationDamage = damage;
            IsCriticalHit = isCritical;
            AttackAnimationProgress = 0.0;

            IsPlayerAttacking = attacker == target;
            IsEnemyAttacking = !IsPlayerAttacking;

            IsAnimating = true;

            _animationTimer?.Change(0, ANIMATION_UPDATE_INTERVAL);
        }

        private void OnAnimationTimerTick(object? state)
        {
            if (!IsAnimating) return;

            AttackAnimationProgress += ANIMATION_SPEED;

            if (AttackAnimationProgress >= 1.0)
            {
                CompleteAnimation();
            }
        }

        private void CompleteAnimation()
        {
            _animationTimer?.Change(Timeout.Infinite, ANIMATION_UPDATE_INTERVAL);

            IsAnimating = false;
            IsPlayerAttacking = false;
            IsEnemyAttacking = false;
            AttackAnimationProgress = 0.0;
            AttackingCharacter = null;
            TargetCharacter = null;
            AnimationDamage = 0;
            IsCriticalHit = false;

            AnimationCompleted?.Invoke();
        }

        public void StopAllAnimations()
        {
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
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public void Dispose()
        {
            _animationTimer?.Dispose();
            _animationTimer = null;
        }
    }
} 