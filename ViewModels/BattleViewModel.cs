using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using SketchBlade.Models;
using SketchBlade.Services;
using System.Threading.Tasks;

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
        private DateTime _lastItemUseTime = DateTime.MinValue; // Добавляем кулдаун для использования предметов
        private const int ITEM_USE_COOLDOWN_MS = 500; // Кулдаун 0.5 секунды между использованием предметов

        public ICommand NavigateCommand { get; private set; }
        public ICommand AttackCommand { get; private set; }
        public ICommand UseItemCommand { get; private set; }
        public ICommand EndBattleCommand { get; private set; }
        public ICommand SelectEnemyCommand { get; private set; }
        public ICommand ClickEnemyCommand { get; private set; }
        public ICommand ForceEndTurnCommand { get; private set; }
        public ICommand CancelTargetSelectionCommand { get; private set; }
        public ICommand NextTurnCommand { get; private set; }

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
            CancelTargetSelectionCommand = new RelayCommand<object>(_ => ExecuteCancelTargetSelection(), null, "CancelTargetSelection");
            NextTurnCommand = new RelayCommand<object>(_ => ExecuteNextTurn(), null, "NextTurn");
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
            // Убираем надпись "Начало боя!"
            // _battleState.BattleStatus = "Начало боя!";
            // _battleState.AddToBattleLog("Начало боя!");
            
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
            
            // Загружаем только предметы из панели быстрого доступа
            var quickItems = _gameState.Inventory.QuickItems
                .Where(item => item != null && item.Type == ItemType.Consumable)
                .ToList();

            foreach (var item in quickItems)
            {
                _battleState.UsableItems.Add(item);
            }
        }

        private void ExecuteNavigate(string screen)
        {
            try
            {
                // Если идет переход на карту мира (бегство из боя), поднимаем здоровье до 20
                if (screen == "WorldMapView" && _battleState.PlayerCharacter != null)
                {
                    if (_battleState.PlayerCharacter.CurrentHealth < 20)
                    {
                        _battleState.PlayerCharacter.CurrentHealth = 20;
                        LoggingService.LogInfo($"Player health restored to 20 after fleeing from battle");
                    }
                }
                
                _navigateAction(screen ?? "WorldMapView");
                // Сохраняем игру после бегства
                OptimizedSaveSystem.SaveGame(_gameState);
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Navigation error in battle", ex);
            }
        }

        private void ExecuteAttack()
        {
            LoggingService.LogDebug("=== ExecuteAttack START ===");
            
            if (!_battleState.IsPlayerTurn || _battleState.IsBattleOver || _animations.IsAnimating)
            {
                LoggingService.LogWarning("ExecuteAttack: Атака не разрешена - не ход игрока, бой завершен или идет анимация");
                return;
            }
            
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
            OnPropertyChanged(nameof(IsPlayerLowHealth));
            
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

            // Вместо прямого изменения флага, используем EndTurn метод
            _ = EndTurn();
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

            // Проверяем кулдаун
            var timeSinceLastUse = DateTime.Now - _lastItemUseTime;
            if (timeSinceLastUse.TotalMilliseconds < ITEM_USE_COOLDOWN_MS)
            {
                _battleState.AddToBattleLog("Подождите перед использованием следующего предмета");
                return;
            }

            var player = _battleState.PlayerCharacter;

            // Устанавливаем время последнего использования предмета
            _lastItemUseTime = DateTime.Now;

            // Нормализуем название предмета для сравнения
            string itemName = item.Name.ToLower();
            bool itemUsedSuccessfully = false;

            if (itemName.Contains("healing") || itemName.Contains("зелье лечения"))
            {
                // Запускаем анимацию использования предмета ТОЛЬКО для зелий
                _animations.StartItemUseAnimation(item.Name);
                
                int healAmount = 30;
                int actualHeal = player.CurrentHealth;
                _battleLogic.UseHealingPotion(player, healAmount);
                actualHeal = player.CurrentHealth - actualHeal;
                // Убираем сообщение об использовании зелья
                // _battleState.AddToBattleLog($"Использовано зелье лечения (+{actualHeal} HP)");
                // Запускаем эффект лечения для игрока (одноразовый)
                player.StartColorEffect(PotionEffectType.Healing, 1500);
                itemUsedSuccessfully = true;
            }
            else if (itemName.Contains("rage") || itemName.Contains("зелье ярости"))
            {
                // Запускаем анимацию использования предмета ТОЛЬКО для зелий
                _animations.StartItemUseAnimation(item.Name);
                
                _battleLogic.ApplyRagePotion(player, 10, 3); // 10 атаки на 3 хода
                // Убираем сообщение об использовании зелья
                // _battleState.AddToBattleLog("Использовано зелье ярости (+10 атака на 3 хода)");
                // Запускаем эффект ярости для игрока (многоразовый)
                player.StartColorEffect(PotionEffectType.Rage, 2000, true);
                itemUsedSuccessfully = true;
            }
            else if (itemName.Contains("invulnerability") || itemName.Contains("зелье неуязвимости"))
            {
                // Запускаем анимацию использования предмета ТОЛЬКО для зелий
                _animations.StartItemUseAnimation(item.Name);
                
                _battleLogic.ApplyDefensePotion(player, 15, 3); // 15 защиты на 3 хода
                // Убираем сообщение об использовании зелья
                // _battleState.AddToBattleLog("Использовано зелье неуязвимости (+15 защита на 3 хода)");
                // Запускаем эффект неуязвимости для игрока (многоразовый)
                player.StartColorEffect(PotionEffectType.Defense, 2000, true);
                itemUsedSuccessfully = true;
            }
            else if (itemName.Contains("bomb") || itemName.Contains("бомба"))
            {
                // Запускаем анимацию использования предмета для бомбы
                _animations.StartItemUseAnimation(item.Name);
                
                int bombDamage = 20;
                int enemiesHit = 0;
                
                // Получаем всех живых врагов
                var aliveEnemies = _battleState.Enemies.Where(e => !e.IsDefeated).ToList();
                
                // Применяем урон ко всем живым врагам
                foreach (var enemy in aliveEnemies)
                {
                    enemy.TakeDamage(bombDamage);
                    enemiesHit++;
                    
                    if (_battleLogic.IsCharacterDefeated(enemy))
                    {
                        var defeatedEnemy = enemy;
                        defeatedEnemy.SetDefeated(true);
                        // Убираем сообщение о поражении врага
                        // _battleState.AddToBattleLog($"{defeatedEnemy.Name} побеждён взрывом!");
                    }
                }
                
                // Убираем сообщение об использовании бомбы
                // _battleState.AddToBattleLog($"Бомба нанесла {bombDamage} урона {enemiesHit} врагам");
                
                // Обновляем UI после использования бомбы
                OnPropertyChanged(nameof(Enemies));
                
                itemUsedSuccessfully = true;
            }
            else if (itemName.Contains("pillow") || itemName.Contains("подушка"))
            {
                // Переходим в режим выбора цели
                _battleState.IsTargetSelectionMode = true;
                _battleState.PendingTargetItem = item;
                // Убираем сообщение о выборе цели
                // _battleState.AddToBattleLog("Выберите цель для подушки...");
                
                // Логика для подушки - включаем режим выбора цели
                var aliveEnemies = _battleState.Enemies.Where(e => !e.IsDefeated).ToList();
                if (aliveEnemies.Count > 0)
                {
                    if (aliveEnemies.Count == 1)
                    {
                        // Если враг один, применяем эффект сразу
                        var target = aliveEnemies[0];
                        
                        // Запускаем анимацию броска ЗДЕСЬ, когда есть цель
                        _animations.StartItemUseAnimation(item.Name, target);
                        
                        target.ApplyBuff(BuffType.Stun, 100, 3); // 3 хода оглушения
                        // Убираем сообщение о попадании подушки
                        // _battleState.AddToBattleLog($"Подушка попала в {target.Name} и оглушила его!");
                        itemUsedSuccessfully = true;
                        
                        // Запускаем анимацию дрожания для цели после небольшой задержки
                        System.Threading.Tasks.Task.Delay(400).ContinueWith(_ => 
                        {
                            System.Windows.Application.Current.Dispatcher.Invoke(() => 
                            {
                                // Имитируем получение урона для анимации дрожания
                                _animations.StartAttackAnimation(player, target, 0, false);
                            });
                        });
                    }
                    else
                    {
                        // Если врагов несколько, включаем режим выбора цели
                        _battleState.IsTargetSelectionMode = true;
                        _battleState.PendingTargetItem = item;
                        _battleState.TargetSelectionMessage = "Выберите цель для подушки (левой кнопкой мыши)";
                        // Убираем сообщение о выборе цели
                        // _battleState.AddToBattleLog("Выберите цель для подушки...");
                        return; // Выходим без удаления предмета
                    }
                }
                else
                {
                    // Убираем сообщение о промахе подушки
                    // _battleState.AddToBattleLog("Подушка пролетела мимо - нет живых врагов!");
                }
            }
            else if (itemName.Contains("shuriken") || itemName.Contains("сюрикен"))
            {
                // Переходим в режим выбора цели
                _battleState.IsTargetSelectionMode = true;
                _battleState.PendingTargetItem = item;
                // Убираем сообщение о выборе цели
                // _battleState.AddToBattleLog("Выберите цель для сюрикена...");
            }
            else
            {
                // Обработка неизвестного предмета
                _animations.StartItemUseAnimation(item.Name);
                itemUsedSuccessfully = true;
            }

            // Удаляем предмет из быстрых слотов только если он был успешно использован
            if (itemUsedSuccessfully)
            {
                for (int i = 0; i < _gameState.Inventory.QuickItems.Count; i++)
                {
                    if (_gameState.Inventory.QuickItems[i] == item)
                    {
                        if (item.StackSize > 1)
                        {
                            item.StackSize--;
                        }
                        else
                        {
                            _gameState.Inventory.QuickItems[i] = null;
                        }
                        break;
                    }
                }
                
                LoadUsableItems();
            }

            // Обновляем UI характеристик после использования предмета
            OnPropertyChanged(nameof(PlayerCharacter));
            OnPropertyChanged(nameof(PlayerHealth));
            OnPropertyChanged(nameof(PlayerDamage));
            OnPropertyChanged(nameof(PlayerDefense));
            OnPropertyChanged(nameof(IsPlayerLowHealth));
            RefreshEnemiesUI();

            // ВАЖНО: Проверяем, не завершилась ли битва после использования предмета
            if (itemUsedSuccessfully)
            {
                // Проверяем, не убил ли предмет всех врагов
                if (_battleLogic.IsAllEnemiesDefeated(_battleState))
                {
                    LoggingService.LogDebug("ExecuteUseItem: Все враги побеждены предметом, завершаем бой");
                    EndBattle(true);
                    return;
                }
                
                // Проверяем, не убил ли предмет игрока (например, взрыв бомбы)
                if (_battleLogic.IsCharacterDefeated(_battleState.PlayerCharacter))
                {
                    LoggingService.LogDebug("ExecuteUseItem: Игрок побежден собственным предметом, завершаем бой");
                    EndBattle(false);
                    return;
                }
            }

            // ВАЖНО: Ход НЕ заканчивается при использовании предмета!
            // Игрок может продолжать использовать предметы или атаковать
            // _battleState.IsPlayerTurn остается true
            
            LoggingService.LogDebug("ExecuteUseItem: Предмет использован, ход игрока продолжается");
            
            // Анимация завершится автоматически через таймер в BattleAnimations
        }

        private bool CanUseItem(Item item)
        {
            // Проверяем кулдаун использования предметов
            var timeSinceLastUse = DateTime.Now - _lastItemUseTime;
            bool cooldownReady = timeSinceLastUse.TotalMilliseconds >= ITEM_USE_COOLDOWN_MS;
            
            return _battleState.IsPlayerTurn && 
                   !_battleState.IsBattleOver && 
                   !_animations.IsAnimating && 
                   item != null &&
                   cooldownReady;
        }

        private void ExecuteEnemyTurn()
        {
            LoggingService.LogDebug("=== ExecuteEnemyTurn START ===");
            LoggingService.LogDebug($"Current animation state - IsPlayerAttacking: {_animations.IsPlayerAttacking}, IsEnemyAttacking: {_animations.IsEnemyAttacking}");
            LoggingService.LogDebug($"IsAnimating: {_animations.IsAnimating}");
            
            // Обрабатываем отравление в начале хода врага
            if (_battleState.PlayerCharacter.IsPoisoned)
            {
                ProcessPoisonDamage();
            }
            
            // Обработка отравления для всех отравленных врагов проводится в методе ProcessPoisonDamage
            bool anyEnemyPoisoned = _battleState.Enemies.Any(e => !e.IsDefeated && e.IsPoisoned);
            if (anyEnemyPoisoned)
            {
                // Не вызываем ProcessPoisonDamage отдельно для каждого врага
                // Это уже делается в общем методе ProcessPoisonDamage
            }
            
            // Обновляем временные эффекты в начале хода врага
            _battleState.PlayerCharacter.UpdateTemporaryBonuses();
            foreach (var enemy in _battleState.Enemies.Where(e => !e.IsDefeated))
            {
                enemy.UpdateTemporaryBonuses();
            }
            
            var activeEnemy = _battleLogic.GetNextActiveEnemy(_battleState);
            if (activeEnemy == null)
            {
                LoggingService.LogDebug("ExecuteEnemyTurn: Нет активных врагов, завершаем бой");
                EndBattle(true);
                return;
            }

            LoggingService.LogDebug($"ExecuteEnemyTurn: {activeEnemy.Name} атакует игрока");

            var player = _battleState.PlayerCharacter;
            
            // Проверяем, оглушен ли враг
            if (activeEnemy.IsStunned)
            {
                _battleState.AddToBattleLog($"{activeEnemy.Name} оглушен и пропускает ход!");
                LoggingService.LogDebug($"ExecuteEnemyTurn: {activeEnemy.Name} оглушен, пропускает ход");
                
                // Используем EndTurn вместо прямого переключения флага
                _ = EndTurn();
                return;
            }
            
            // Проверяем, должен ли враг использовать зелье (с вероятностью 15%)
            var random = new Random();
            bool shouldUsePotion = random.Next(100) < 15; // 15% шанс
            
            if (shouldUsePotion)
            {
                // Определяем тип зелья на основе состояния врага
                PotionEffectType potionType = PotionEffectType.Healing; // По умолчанию
                string potionMessage = "";
                
                // Если здоровье меньше 50%, используем зелье лечения
                if (activeEnemy.CurrentHealth < activeEnemy.MaxHealth * 0.5)
                {
                    potionType = PotionEffectType.Healing;
                    int healAmount = 25;
                    activeEnemy.CurrentHealth = Math.Min(activeEnemy.MaxHealth, activeEnemy.CurrentHealth + healAmount);
                    potionMessage = $"{activeEnemy.Name} использовал зелье лечения (+{healAmount} HP)";
                }
                // Иначе случайно выбираем между зельем ярости и неуязвимости
                else
                {
                    if (random.Next(2) == 0)
                    {
                        potionType = PotionEffectType.Rage;
                        activeEnemy.ApplyBuff(BuffType.Attack, 8, 3);
                        potionMessage = $"{activeEnemy.Name} использовал зелье ярости (+8 атака на 3 хода)";
                    }
                    else
                    {
                        potionType = PotionEffectType.Defense;
                        activeEnemy.ApplyBuff(BuffType.Defense, 10, 3);
                        potionMessage = $"{activeEnemy.Name} использовал зелье неуязвимости (+10 защита на 3 хода)";
                    }
                }
                
                // Запускаем цветовой эффект для врага с правильным количеством ходов
                if (potionType == PotionEffectType.Healing)
                {
                    // Одноразовый эффект лечения - показываем на 1.5 секунды
                    activeEnemy.StartColorEffect(potionType, 1500);
                }
                else
                {
                    // Эффекты по ходам - устанавливаем persistent=true
                    activeEnemy.StartColorEffect(potionType, 3000, true);
                }
                _battleState.AddToBattleLog(potionMessage);
                
                // Обновляем UI
                OnPropertyChanged(nameof(PlayerCharacter));
                OnPropertyChanged(nameof(PlayerHealth));
                OnPropertyChanged(nameof(PlayerDamage));
                OnPropertyChanged(nameof(PlayerDefense));
                OnPropertyChanged(nameof(IsPlayerLowHealth));
                RefreshEnemiesUI();
                
                // Возвращаем ход игроку после использования зелья
                _ = EndTurn();
                return;
            }
            
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
            OnPropertyChanged(nameof(IsPlayerLowHealth));
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
            if (!CanClickEnemy(enemy)) return;

            // Если активен режим выбора цели для предмета
            if (_battleState.IsTargetSelectionMode && _battleState.PendingTargetItem != null)
            {
                ApplyTargetedItemEffect(_battleState.PendingTargetItem, enemy);
                
                // Выходим из режима выбора цели
                _battleState.IsTargetSelectionMode = false;
                _battleState.PendingTargetItem = null;
                _battleState.TargetSelectionMessage = "";
                
                return;
            }

            // Обычная логика атаки
            _battleState.SelectedEnemy = enemy;
            ExecuteAttack();
        }

        private void ApplyTargetedItemEffect(Item item, Character target)
        {
            if (item == null || target == null || target.IsDefeated) return;

            var player = _battleState.PlayerCharacter;
            string itemName = item.Name.ToLower();
            bool itemUsedSuccessfully = false;

            // Запускаем анимацию использования предмета с указанием цели
            _animations.StartItemUseAnimation(item.Name, target);

            if (itemName.Contains("pillow") || itemName.Contains("подушка"))
            {
                target.ApplyBuff(BuffType.Stun, 100, 3); // 3 хода оглушения
                // Убираем сообщение о попадании подушки
                // _battleState.AddToBattleLog($"Подушка попала в {target.Name} и оглушила его!");
                itemUsedSuccessfully = true;
                
                // Запускаем анимацию дрожания для цели после небольшой задержки
                System.Threading.Tasks.Task.Delay(400).ContinueWith(_ => 
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => 
                    {
                        // Имитируем получение урона для анимации дрожания
                        _animations.StartAttackAnimation(player, target, 0, false);
                    });
                });
            }
            else if (itemName.Contains("shuriken") || itemName.Contains("сюрикен"))
            {
                var targets = new List<Character> { target };
                bool success = item.UseInCombat(player, targets);
                if (success)
                {
                    // Убираем сообщение об использовании сюрикена
                    // _battleState.AddToBattleLog($"Отравленный сюрикен нанёс {item.Damage} урона {target.Name} и отравил его на 3 хода!");
                    // Запускаем визуальный эффект отравления для цели (одноразовый)
                    target.StartColorEffect(PotionEffectType.Poison, 1000);
                    itemUsedSuccessfully = true;
                    
                    // Запускаем анимацию дрожания для цели после небольшой задержки
                    System.Threading.Tasks.Task.Delay(400).ContinueWith(_ => 
                    {
                        System.Windows.Application.Current.Dispatcher.Invoke(() => 
                        {
                            // Имитируем получение урона для анимации дрожания
                            _animations.StartAttackAnimation(player, target, item.Damage, false);
                        });
                    });
                }
            }

            // Удаляем предмет из быстрых слотов только если он был успешно использован
            if (itemUsedSuccessfully)
            {
                for (int i = 0; i < _gameState.Inventory.QuickItems.Count; i++)
                {
                    if (_gameState.Inventory.QuickItems[i] == item)
                    {
                        if (item.StackSize > 1)
                        {
                            item.StackSize--;
                        }
                        else
                        {
                            _gameState.Inventory.QuickItems[i] = null;
                        }
                        break;
                    }
                }
                
                LoadUsableItems();
            }

            // Обновляем UI характеристик после использования предмета
            OnPropertyChanged(nameof(PlayerCharacter));
            OnPropertyChanged(nameof(PlayerHealth));
            OnPropertyChanged(nameof(PlayerDamage));
            OnPropertyChanged(nameof(PlayerDefense));
            OnPropertyChanged(nameof(IsPlayerLowHealth));
            RefreshEnemiesUI();
            
            // ВАЖНО: Проверяем, не завершилась ли битва после использования метательного предмета
            if (itemUsedSuccessfully)
            {
                // Проверяем, не убил ли предмет всех врагов
                if (_battleLogic.IsAllEnemiesDefeated(_battleState))
                {
                    LoggingService.LogDebug("ApplyTargetedItemEffect: Все враги побеждены метательным предметом, завершаем бой");
                    EndBattle(true);
                    return;
                }
                
                // Проверяем, не был ли убит конкретный враг
                if (target.IsDefeated)
                {
                    // Убираем сообщение о поражении врага
                    // _battleState.AddToBattleLog($"{target.Name} побеждён предметом!");
                    
                    // Если этот враг был выбран, выбираем следующего живого
                    if (_battleState.SelectedEnemy == target)
                    {
                        var nextEnemy = _battleState.Enemies.FirstOrDefault(e => !e.IsDefeated);
                        _battleState.SelectedEnemy = nextEnemy;
                        OnPropertyChanged(nameof(SelectedEnemy));
                    }
                }
            }
        }

        private bool CanClickEnemy(Character enemy)
        {
            // В режиме выбора цели разрешаем клики по живым врагам
            if (_battleState.IsTargetSelectionMode)
            {
                return enemy != null && !enemy.IsDefeated;
            }

            // Обычная логика для атаки
            return _battleState.IsPlayerTurn && 
                   !_battleState.IsBattleOver && 
                   !_animations.IsAnimating && 
                   enemy != null && 
                   !enemy.IsDefeated;
        }

        private void ExecuteEndBattle()
        {
            LoggingService.LogInfo("=== ExecuteEndBattle: начато завершение битвы ===");
            
            if (_battleState.IsBattleOver)
            {
                LoggingService.LogInfo($"ExecuteEndBattle: битва завершена, победа: {_battleState.BattleWon}");
                
                // Применяем награды, если битва выиграна
                if (_battleState.BattleWon)
                {
                    // Подсчитываем количество побежденных врагов
                    int enemyCount = _battleState.Enemies.Count(e => e.IsDefeated);
                    bool isHeroDefeated = _battleState.Enemies.Any(e => e.IsHero && e.IsDefeated);
                    
                    LoggingService.LogInfo($"ExecuteEndBattle: применяем награды за {enemyCount} врагов (герой побежден: {isHeroDefeated})");
                    
                    // Применяем все награды через BattleLogic
                    _battleLogic.ApplyBattleRewards(_gameState, enemyCount, isHeroDefeated);
                    
                    LoggingService.LogInfo("ExecuteEndBattle: все награды применены успешно");
                    
                    // Автоматически сохраняем игру после получения наград
                    _gameState.SaveGame();
                    LoggingService.LogInfo("ExecuteEndBattle: игра сохранена после получения наград");
                    
                    LoggingService.LogInfo("ExecuteEndBattle: переходим на карту мира");
                    _navigateAction("WorldMapView");
                }
                else
                {
                    LoggingService.LogInfo("ExecuteEndBattle: битва проиграна, удаляем сохранение и переходим в главное меню");
                    
                    // Удаляем сохранение при поражении игрока
                    OptimizedSaveSystem.DeleteSaveFile();
                    
                    // Сбрасываем состояние игры
                    _gameState.Reset();
                    _gameState.HasSaveGame = false;
                    
                    // Переходим на главное меню при поражении
                    LoggingService.LogInfo("ExecuteEndBattle: переход в главное меню после поражения");
                    _navigateAction("MainMenuView");
                }
            }
            else
            {
                LoggingService.LogWarning("ExecuteEndBattle: битва еще не завершена");
            }
        }

        private void EndBattle(bool victory)
        {
            LoggingService.LogInfo($"=== EndBattle:  , : {victory} ===");
            
            _battleState.IsBattleOver = true;
            _battleState.BattleWon = victory;

            if (victory)
            {
                // _battleState.BattleResultMessage = "Победа!";
                // Убираем сообщение о победе
                // _battleState.AddToBattleLog("Победа!");
                
                LoggingService.LogInfo("EndBattle:  ...");
                var rewards = _battleLogic.GenerateBattleRewards(
                    _gameState, 
                    _battleState.IsBossHeroBattle);
                
                LoggingService.LogInfo($"EndBattle:  {rewards?.Count ?? 0} ");
                
                if (rewards != null && rewards.Count > 0)
                {
                    foreach (var reward in rewards)
                    {
                        LoggingService.LogInfo($"EndBattle: : {reward.Name}");
                    }
                }
                else
                {
                    LoggingService.LogWarning("EndBattle:      ");
                }
                
                _gameState.BattleRewardItems = rewards;
                LoggingService.LogInfo($"EndBattle: BattleRewardItems , : {_gameState.BattleRewardItems?.Count ?? 0}");
                
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
                
                // ИСПРАВЛЕНИЕ: Вызываем CompleteBattle для корректной обработки победы и разблокировки локаций
                _gameState.CompleteBattle(true);
                LoggingService.LogInfo("EndBattle: Called CompleteBattle for location unlocking");
                
                // Уведомляем об изменении локаций для обновления UI карты мира
                OnPropertyChanged(nameof(GameData.Locations));
                OnPropertyChanged(nameof(GameState.Locations));
            }
            else
            {
                // _battleState.BattleResultMessage = "Поражение!";
                // Убираем сообщение о поражении
                // _battleState.AddToBattleLog("Поражение...");
                
                LoggingService.LogInfo("EndBattle: Игрок потерпел поражение, удаляем сохранение и возвращаем в главное меню");
                
                // Удаляем сохранение при поражении игрока
                OptimizedSaveSystem.DeleteSaveFile();
                
                // Сбрасываем состояние игры
                _gameState.Reset();
                _gameState.HasSaveGame = false;
                
                // Переходим сразу на главное меню
                LoggingService.LogInfo("EndBattle: Переход в главное меню после поражения");
                _navigateAction("MainMenuView");
                return;
            }
            
            LoggingService.LogInfo("=== EndBattle:   ===");
        }
        
        /// <summary>
        ///       
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
                    OnPropertyChanged(nameof(IsPlayerLowHealth));
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
                case nameof(BattleState.IsTargetSelectionMode):
                    OnPropertyChanged(nameof(IsTargetSelectionMode));
                    break;
                case nameof(BattleState.PendingTargetItem):
                    OnPropertyChanged(nameof(PendingTargetItem));
                    break;
                case nameof(BattleState.TargetSelectionMessage):
                    OnPropertyChanged(nameof(TargetSelectionMessage));
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
                    OnPropertyChanged(nameof(IsAnimating));
                    OnPropertyChanged(nameof(CanAdvanceTurn));
                    break;
                case nameof(BattleAnimations.IsPlayerAttacking):
                    LoggingService.LogDebug($"OnAnimationsChanged: IsPlayerAttacking = {_animations.IsPlayerAttacking}");
                    OnPropertyChanged(nameof(IsPlayerAttacking));
                    break;
                case nameof(BattleAnimations.IsEnemyAttacking):
                    LoggingService.LogDebug($"OnAnimationsChanged: IsEnemyAttacking = {_animations.IsEnemyAttacking}");
                    OnPropertyChanged(nameof(IsEnemyAttacking));
                    break;
                case nameof(BattleAnimations.IsUsingItem):
                    LoggingService.LogDebug($"OnAnimationsChanged: IsUsingItem = {_animations.IsUsingItem}");
                    OnPropertyChanged(nameof(IsUsingItem));
                    break;
                case nameof(BattleAnimations.CurrentItemAnimationType):
                    LoggingService.LogDebug($"OnAnimationsChanged: CurrentItemAnimationType = {_animations.CurrentItemAnimationType}");
                    OnPropertyChanged(nameof(CurrentItemAnimationType));
                    break;
                case nameof(BattleAnimations.CurrentItemName):
                    LoggingService.LogDebug($"OnAnimationsChanged: CurrentItemName = {_animations.CurrentItemName}");
                    OnPropertyChanged(nameof(CurrentItemName));
                    OnPropertyChanged(nameof(CurrentItemUsageMessage));
                    break;
                // Добавляем обработчики для новых свойств подсветки персонажей
                case nameof(BattleAnimations.TargetedCharacter):
                    LoggingService.LogDebug($"OnAnimationsChanged: TargetedCharacter = {_animations.TargetedCharacter?.Name ?? "null"}");
                    OnPropertyChanged(nameof(TargetedCharacter));
                    break;
                case nameof(BattleAnimations.IsTargetingItem):
                    LoggingService.LogDebug($"OnAnimationsChanged: IsTargetingItem = {_animations.IsTargetingItem}");
                    OnPropertyChanged(nameof(IsTargetingItem));
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

        private async void OnAnimationCompleted()
        {
            LoggingService.LogDebug($"OnAnimationCompleted: Анимация завершена, IsPlayerTurn={_battleState.IsPlayerTurn}, IsAnimating={_animations.IsAnimating}");
            
            // Останавливаем таймер безопасности
            _safetyTimer?.Dispose();
            _safetyTimer = null;
            
            // Если бой завершен, ничего не делаем
            if (_battleState.IsBattleOver)
            {
                LoggingService.LogDebug("OnAnimationCompleted: Бой завершен, игнорируем завершение анимации");
                return;
            }
            
            // Если ожидается ход врага, запускаем его
            if (_waitingForEnemyTurn)
            {
                _waitingForEnemyTurn = false;
                LoggingService.LogDebug("OnAnimationCompleted: Запускаем ход врага");
                
                // Запускаем ход врага после небольшой задержки
                await System.Threading.Tasks.Task.Delay(300);
                
                if (!_battleState.IsBattleOver)
                {
                    LoggingService.LogDebug("OnAnimationCompleted: Выполняем ExecuteEnemyTurn");
                    ExecuteEnemyTurn();
                }
                else
                {
                    LoggingService.LogDebug("OnAnimationCompleted: Бой завершился во время задержки");
                }
            }
            // Иначе завершилась анимация врага - возвращаем ход игроку
            else if (!_battleState.IsPlayerTurn)
            {
                LoggingService.LogDebug("OnAnimationCompleted: Завершена анимация врага, возвращаем ход игроку");
                
                // Используем EndTurn вместо прямого переключения флага
                await EndTurn();
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
        public bool IsUsingItem => _animations.IsUsingItem;
        public ItemAnimationType CurrentItemAnimationType => _animations.CurrentItemAnimationType;
        public string CurrentItemName => _animations.CurrentItemName;
        public string CurrentItemUsageMessage => string.IsNullOrEmpty(_animations.CurrentItemName) ? 
            "" : 
            LocalizationService.Instance.GetTranslation("Battle.Using", _animations.CurrentItemName);
        public int AnimationDamage => _animations.AnimationDamage;
        public bool IsCriticalHit => _animations.IsCriticalHit;
        public Character AttackingCharacter => _animations.AttackingCharacter;
        public Character TargetCharacter => _animations.TargetCharacter;

        // Новые свойства для режима выбора цели
        public bool IsTargetSelectionMode => _battleState.IsTargetSelectionMode;
        public Item PendingTargetItem => _battleState.PendingTargetItem;
        public string TargetSelectionMessage => _battleState.TargetSelectionMessage;

        // Свойства для отображения характеристик игрока в UI
        public string PlayerHealth => $"{PlayerCharacter?.CurrentHealth ?? 0}/{PlayerCharacter?.MaxHealth ?? 0}";
        public string PlayerDamage => PlayerCharacter?.GetTotalAttack().ToString() ?? "0";
        public string PlayerDefense => PlayerCharacter?.GetTotalDefense().ToString() ?? "0";
        
        // Дополнительные свойства для боевого интерфейса
        public string TurnMessage => _battleState.IsPlayerTurn ? 
            LocalizationService.Instance.GetTranslation("Battle.YourTurn") : 
            LocalizationService.Instance.GetTranslation("Battle.EnemyTurn");
        public string DamageMessage => _battleState.DamageMessage;
        public bool HasRewardItems => _gameState.BattleRewardItems?.Count > 0;
        
        // Свойство для проверки низкого здоровья (меньше 20 единиц)
        public bool IsPlayerLowHealth => PlayerCharacter?.CurrentHealth < 20;

        // Дополнительные свойства для совместимости с XAML
        public bool IsAnimating => _animations.IsAnimating;
        public bool CanAdvanceTurn => !_battleState.IsPlayerTurn && !_battleState.IsBattleOver && !_animations.IsAnimating;

        // Новые свойства для подсветки конкретного персонажа при использовании предметов
        public Character? TargetedCharacter => _animations.TargetedCharacter;
        public bool IsTargetingItem => _animations.IsTargetingItem;

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

        private void ExecuteCancelTargetSelection()
        {
            _battleState.IsTargetSelectionMode = false;
            _battleState.PendingTargetItem = null;
            // _battleState.TargetSelectionMessage = "";
            
            _battleState.AddToBattleLog("Выбор цели отменен");
        }

        // Метод для обработки урона от отравления с визуальным эффектом
        private void ProcessPoisonDamage()
        {
            LoggingService.LogDebug("ProcessPoisonDamage: Начинаем обработку урона от яда");

            // Обрабатываем отравление игрока
            if (_battleState.PlayerCharacter.IsPoisoned)
            {
                LoggingService.LogInfo($"Игрок отравлен, наносим урон");
                int poisonDamage = _battleState.PlayerCharacter.PoisonDamage;
                _battleState.PlayerCharacter.TakeDamage(poisonDamage);
                
                // Показываем визуальный эффект отравления для игрока
                _battleState.PlayerCharacter.StartColorEffect(PotionEffectType.Poison, 1500);
                
                _battleState.DamageMessage = $"Игрок получил {poisonDamage} урона от яда!";
                LoggingService.LogInfo($"Игрок получил {poisonDamage} урона от яда");
            }

            // Обрабатываем отравление врагов
            foreach (var enemy in _battleState.Enemies.Where(e => !e.IsDefeated && e.IsPoisoned))
            {
                LoggingService.LogInfo($"Враг {enemy.Name} отравлен, наносим урон");
                int poisonDamage = enemy.PoisonDamage;
                enemy.TakeDamage(poisonDamage);
                
                // Показываем визуальный эффект отравления для конкретного врага
                enemy.StartColorEffect(PotionEffectType.Poison, 1500);
                
                _battleState.DamageMessage = $"{enemy.Name} получил {poisonDamage} урона от яда!";
                LoggingService.LogInfo($"Враг {enemy.Name} получил {poisonDamage} урона от яда");
            }

            // Диагностика состояния цветовых эффектов
            DiagnoseColorEffects();
            
            LoggingService.LogDebug("ProcessPoisonDamage: Обработка урона от яда завершена");
        }

        private void ExecuteNextTurn()
        {
            LoggingService.LogDebug("ExecuteNextTurn: Принудительный переход к следующему ходу");
            
            if (!_battleState.IsPlayerTurn && !_battleState.IsBattleOver)
            {
                _battleState.IsPlayerTurn = true;
                OnPropertyChanged(nameof(IsPlayerTurn));
                OnPropertyChanged(nameof(CanAttack));
                OnPropertyChanged(nameof(CanAdvanceTurn));
                _battleState.AddToBattleLog("Ход передан игроку");
            }
        }

        public void DiagnoseColorEffects()
        {
            LoggingService.LogInfo("=== ДИАГНОСТИКА ЦВЕТОВЫХ ЭФФЕКТОВ ===");
            LoggingService.LogInfo($"Игрок {PlayerCharacter.Name}: HasActiveColorEffect={PlayerCharacter.HasActiveColorEffect}, CurrentColorEffect={PlayerCharacter.CurrentColorEffect}");
            
            for (int i = 0; i < Enemies.Count; i++)
            {
                var enemy = Enemies[i];
                LoggingService.LogInfo($"Враг {i} {enemy.Name}: HasActiveColorEffect={enemy.HasActiveColorEffect}, CurrentColorEffect={enemy.CurrentColorEffect}");
            }
            LoggingService.LogInfo("============================================");
        }

        public async Task EndTurn()
        {
            // Process any end-of-turn effects for the player character
            _battleState.PlayerCharacter.ProcessEndOfTurn();

            // If player turn ends, then it's enemy's turn
            if (_battleState.IsPlayerTurn)
            {
                _battleState.IsPlayerTurn = false;
                OnPropertyChanged(nameof(IsPlayerTurn));
                OnPropertyChanged(nameof(CanAttack));
                
                // Set the waiting flag for enemy turn
                _waitingForEnemyTurn = true;
                
                // Start safety timer
                StartSafetyTimer();
                
                // Give a slight delay before enemy acts
                await System.Threading.Tasks.Task.Delay(600);
                
                // Only start enemy turn if animation has completed
                if (!_animations.IsAnimating && _waitingForEnemyTurn)
                {
                    _waitingForEnemyTurn = false;
                    ExecuteEnemyTurn();
                }
            }
            else
            {
                // Process any end-of-turn effects for all active enemies
                foreach (var enemy in _battleState.Enemies.Where(e => !e.IsDefeated))
                {
                    enemy.ProcessEndOfTurn();
                }
            
                _battleState.IsPlayerTurn = true;
                OnPropertyChanged(nameof(IsPlayerTurn));
                OnPropertyChanged(nameof(CanAttack));
                
                // Increment turn counter after a full round
                _battleState.TurnCounter++;
                
                // Update UI after processing effects
                OnPropertyChanged(nameof(PlayerCharacter));
                OnPropertyChanged(nameof(PlayerHealth));
                OnPropertyChanged(nameof(PlayerDamage));
                OnPropertyChanged(nameof(PlayerDefense));
                OnPropertyChanged(nameof(IsPlayerLowHealth));
                RefreshEnemiesUI();
            }
        }

        private void EnemyTurn()
        {
            ExecuteEnemyTurn();
        }
    }
} 
