using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using SketchBlade.Models;
using SketchBlade.Services;
using SketchBlade.Utilities;
using System.Windows.Threading;

namespace SketchBlade.ViewModels
{
    // Define the log levels for simplicity
    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Warning = 2,
        Error = 3
    }

    public class SimplifiedCraftingViewModel : INotifyPropertyChanged
    {
        private readonly GameData _gameState;
        private SimplifiedCraftingSystem _craftingSystem;
        private SimplifiedCraftingRecipe? _selectedRecipe;
        private bool _isRefreshing = false;
        private DispatcherTimer? _deferredRefreshTimer;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<SimplifiedCraftingRecipeViewModel> AvailableRecipes { get; } = new ObservableCollection<SimplifiedCraftingRecipeViewModel>();
        public ObservableCollection<MaterialRequirementViewModel> RequiredMaterials { get; } = new ObservableCollection<MaterialRequirementViewModel>();

        public SimplifiedCraftingRecipe? SelectedRecipe
        {
            get => _selectedRecipe;
            set
            {
                if (_selectedRecipe != value)
                {
                    _selectedRecipe = value;
                    OnPropertyChanged();
                    UpdateRequiredMaterials();
                    OnPropertyChanged(nameof(CanCraft));
                }
            }
        }

        public bool CanCraft => SelectedRecipe != null && _craftingSystem.CanCraft(SelectedRecipe, _gameState.Inventory);

        public ICommand CraftItemCommand { get; }
        public ICommand RefreshRecipesCommand { get; }

        public SimplifiedCraftingViewModel(GameData GameData)
        {
            _gameState = GameData ?? throw new ArgumentNullException(nameof(GameData));
            _craftingSystem = new SimplifiedCraftingSystem();

            // LoggingService.LogDebug("Инициализация SimplifiedCraftingViewModel");

            CraftItemCommand = new RelayCommand<object>(_ => CraftSelectedItem());
            RefreshRecipesCommand = new RelayCommand<object>(_ => RefreshAvailableRecipes());

            LoadRecipes();

            // Подписываемся на изменение инвентаря
            if (_gameState.Inventory != null)
            {
                _gameState.Inventory.InventoryChanged += OnInventoryChanged;
                // LoggingService.LogDebug("Подписка на изменение инвентаря выполнена");
            }
            else
            {
                LoggingService.LogError("Инвентарь в GameData равен null");
            }
        }

        private void LoadRecipes()
        {
            try
            {
                AvailableRecipes.Clear();
                // LoggingService.LogDebug("AvailableRecipes очищена");
                
                // Загружаем все доступные рецепты, а не только те, которые можно создать
                var allRecipes = _craftingSystem.GetAvailableRecipes();
                // LoggingService.LogDebug($"Всего рецептов в системе: {allRecipes.Count}");
                
                // ДОБАВЛЕНО: Проверяем и исправляем пути к изображениям для всех рецептов
                VerifyAndFixImagePaths(allRecipes);
                
                // Проверяем каждый рецепт и добавляем все рецепты
                foreach (var recipe in allRecipes)
                {
                    bool canCraft = _craftingSystem.CanCraft(recipe, _gameState.Inventory);
                    // LoggingService.LogDebug($"Рецепт {recipe.Name}: можно создать = {canCraft}");
                    
                    // Добавляем все рецепты, независимо от доступности
                    var viewModel = new SimplifiedCraftingRecipeViewModel(recipe, _craftingSystem, _gameState.Inventory);
                    AvailableRecipes.Add(viewModel);
                    // LoggingService.LogDebug($"Добавлен рецепт в AvailableRecipes: {recipe.Name} (Доступен: {viewModel.CanCraft})");
                }

                // LoggingService.LogDebug($"Загружено {AvailableRecipes.Count} рецептов");
                // LoggingService.LogInfo($"Итого рецептов в AvailableRecipes: {AvailableRecipes.Count}");
                
                // КРИТИЧЕСКИ ВАЖНО: Принудительно уведомляем UI об изменениях
                // LoggingService.LogDebug("Отправляем PropertyChanged уведомление для AvailableRecipes");
                OnPropertyChanged(nameof(AvailableRecipes));
                
                // Дополнительная диагностика
                // LoggingService.LogDebug($"AvailableRecipes.Count после загрузки: {AvailableRecipes.Count}");
                // for (int i = 0; i < AvailableRecipes.Count; i++)
                // {
                //     var recipe = AvailableRecipes[i];
                //     LoggingService.LogDebug($"  AvailableRecipes[{i}]: {recipe.Name}, Result: {recipe.Result?.Name ?? "null"}, CanCraft: {recipe.CanCraft}");
                // }
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Ошибка при загрузке рецептов", ex);
            }
        }

        /// <summary>
        /// Проверяет и исправляет пути к изображениям для всех рецептов
        /// </summary>
        private void VerifyAndFixImagePaths(List<SimplifiedCraftingRecipe> recipes)
        {
            try
            {
                // LoggingService.LogInfo("Проверка и исправление путей к изображениям для рецептов");
                
                foreach (var recipe in recipes)
                {
                    if (recipe.Result == null)
                    {
                        LoggingService.LogWarning($"Результат рецепта {recipe.Name} равен null");
                        continue;
                    }
                    
                    // Проверяем путь к изображению
                    if (string.IsNullOrEmpty(recipe.Result.SpritePath))
                    {
                        LoggingService.LogWarning($"Пустой SpritePath для результата рецепта {recipe.Name}, устанавливаем дефолтный");
                        recipe.Result.SpritePath = Utilities.AssetPaths.DEFAULT_IMAGE;
                    }
                    
                    // Проверяем существование файла
                    string fullPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, recipe.Result.SpritePath);
                    if (!System.IO.File.Exists(fullPath))
                    {
                        LoggingService.LogWarning($"Файл изображения не найден: {fullPath}, используем дефолтный");
                        recipe.Result.SpritePath = Utilities.AssetPaths.DEFAULT_IMAGE;
                    }
                    
                    // Логируем итоговый путь
                                            // LoggingService.LogDebug($"Итоговый SpritePath для {recipe.Name}: {recipe.Result.SpritePath}");
                }
                
                // LoggingService.LogInfo($"Проверка и исправление путей завершены для {recipes.Count} рецептов");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Ошибка при проверке путей к изображениям: {ex.Message}", ex);
            }
        }

        public void RefreshAvailableRecipes()
        {
            try
            {
                // Prevent multiple refreshes happening in rapid succession
                if (_isRefreshing)
                {
                    // If a refresh is already in progress, defer this one
                    if (_deferredRefreshTimer == null)
                    {
                        _deferredRefreshTimer = new DispatcherTimer
                        {
                            // Increase interval for more aggressive debouncing
                            Interval = TimeSpan.FromMilliseconds(300)
                        };
                        _deferredRefreshTimer.Tick += (s, e) =>
                        {
                            _deferredRefreshTimer?.Stop();
                            if (!_isRefreshing)
                            {
                                RefreshAvailableRecipes();
                            }
                        };
                    }
                    
                    if (!_deferredRefreshTimer.IsEnabled)
                    {
                        _deferredRefreshTimer.Start();
                    }
                    return;
                }

                _isRefreshing = true;
                
                // Process updates in batch using Dispatcher to avoid UI thread blocking
                System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        // Обновляем статус доступности крафта для каждого рецепта
                        foreach (var recipeViewModel in AvailableRecipes)
                        {
                            recipeViewModel.UpdateCraftability();
                        }

                        // Обновляем требуемые материалы для выбранного рецепта
                        UpdateRequiredMaterials();

                        OnPropertyChanged(nameof(CanCraft));
                        // LoggingService.LogDebug("Рецепты обновлены");
                    }
                    catch (Exception ex)
                    {
                        LoggingService.LogError($"Ошибка при обновлении рецептов в диспетчере: {ex.Message}", ex);
                    }
                    finally
                    {
                        _isRefreshing = false;
                    }
                }));
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Ошибка при обновлении рецептов", ex);
                _isRefreshing = false;
            }
        }

        private void UpdateRequiredMaterials()
        {
            RequiredMaterials.Clear();

            if (SelectedRecipe == null) return;

            try
            {
                foreach (var material in SelectedRecipe.RequiredMaterials)
                {
                    int available = CountMaterialInInventory(material.Key);
                    var materialViewModel = new MaterialRequirementViewModel
                    {
                        Name = material.Key,
                        Required = material.Value,
                        Available = available,
                        IsAvailable = available >= material.Value
                    };

                    RequiredMaterials.Add(materialViewModel);
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Ошибка при обновлении требуемых материалов", ex);
            }
        }

        private int CountMaterialInInventory(string materialName)
        {
            return _gameState.Inventory.Items
                .Where(item => item?.Name == materialName)
                .Sum(item => item?.StackSize ?? 0);
        }

        public void CraftSelectedItem()
        {
            // LoggingService.LogInfo($"[CRAFT-SYSTEM] ========== НАЧАЛО CraftSelectedItem ==========");
            // LoggingService.LogInfo($"[CRAFT-SYSTEM] CraftSelectedItem: Вызван метод крафта. SelectedRecipe: {SelectedRecipe?.Name ?? "null"}, CanCraft: {CanCraft}");
            
            if (SelectedRecipe == null)
            {
                LoggingService.LogWarning("[CRAFT-SYSTEM] CraftSelectedItem: Рецепт не выбран");
                return;
            }
            
            if (!CanCraft)
            {
                LoggingService.LogWarning($"[CRAFT-SYSTEM] CraftSelectedItem: Недостаточно материалов для крафта {SelectedRecipe.Name}");
                
                // Логируем недостающие материалы
                var missingMaterials = _craftingSystem.GetMissingMaterials(SelectedRecipe, _gameState.Inventory);
                foreach (var missing in missingMaterials)
                {
                    LoggingService.LogWarning($"[CRAFT-SYSTEM] CraftSelectedItem: Недостает {missing.Value} {missing.Key}");
                }
                return;
            }

            try
            {
                // LoggingService.LogInfo($"[CRAFT-SYSTEM] === НАЧАЛО КРАФТА: {SelectedRecipe.Name} ===");
                
                // Логируем состояние инвентаря ПЕРЕД крафтом
                // LoggingService.LogInfo($"[CRAFT-SYSTEM] Состояние инвентаря ПЕРЕД крафтом:");
                // for (int i = 0; i < _gameState.Inventory.Items.Count; i++)
                // {
                //     var item = _gameState.Inventory.Items[i];
                //     LoggingService.LogInfo($"[CRAFT-SYSTEM]   Инвентарь[{i}]: {item?.Name ?? "null"}");
                // }
                
                // LoggingService.LogInfo($"[CRAFT-SYSTEM] Вызываем _craftingSystem.Craft()...");
                bool success = _craftingSystem.Craft(SelectedRecipe, _gameState.Inventory);
                // LoggingService.LogInfo($"[CRAFT-SYSTEM] _craftingSystem.Craft() вернул: {success}");
                
                if (success)
                {
                    // LoggingService.LogInfo($"[CRAFT-SYSTEM] Крафт выполнен успешно: {SelectedRecipe.Name}");
                    
                    // Логируем состояние инвентаря ПОСЛЕ крафта
                    // LoggingService.LogInfo($"[CRAFT-SYSTEM] Состояние инвентаря ПОСЛЕ крафта:");
                    // for (int i = 0; i < _gameState.Inventory.Items.Count; i++)
                    // {
                    //     var item = _gameState.Inventory.Items[i];
                    //     LoggingService.LogInfo($"[CRAFT-SYSTEM]   Инвентарь[{i}]: {item?.Name ?? "null"}");
                    // }
                    
                    // Обновляем рецепты и уведомляем об изменении инвентаря
                    // LoggingService.LogInfo($"[CRAFT-SYSTEM] Обновляем рецепты...");
                    RefreshAvailableRecipes();
                    // LoggingService.LogInfo($"[CRAFT-SYSTEM] Уведомляем об изменении инвентаря...");
                    _gameState.Inventory.OnInventoryChanged();
                    
                    // Простое обновление UI без избыточных вызовов
                    // LoggingService.LogInfo($"[CRAFT-SYSTEM] Ищем InventoryViewModel...");
                    var inventoryVM = FindInventoryViewModel();
                    if (inventoryVM != null)
                    {
                        // LoggingService.LogInfo($"[CRAFT-SYSTEM] InventoryViewModel найден, вызываем RefreshAllSlots()...");
                        inventoryVM.RefreshAllSlots();
                        // LoggingService.LogInfo($"[CRAFT-SYSTEM] RefreshAllSlots() завершен");
                    }
                    else
                    {
                        LoggingService.LogError($"[CRAFT-SYSTEM] InventoryViewModel НЕ НАЙДЕН!");
                    }
                    
                    // LoggingService.LogInfo($"[CRAFT-SYSTEM] === КРАФТ ЗАВЕРШЕН: {SelectedRecipe.Name} ===");
                }
                else
                {
                    LoggingService.LogError($"[CRAFT-SYSTEM] Не удалось создать предмет: {SelectedRecipe.Name}");
                }
                
                // LoggingService.LogInfo($"[CRAFT-SYSTEM] ========== КОНЕЦ CraftSelectedItem ==========");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"[CRAFT-SYSTEM] Ошибка при крафте предмета {SelectedRecipe?.Name}: {ex.Message}", ex);
                LoggingService.LogError($"[CRAFT-SYSTEM] StackTrace: {ex.StackTrace}");
            }
        }
        
        private InventoryViewModel? FindInventoryViewModel()
        {
            try
            {
                // Способ 1: Через Application.Current.Resources
                if (System.Windows.Application.Current.Resources.Contains("InventoryViewModel"))
                {
                    var vm = System.Windows.Application.Current.Resources["InventoryViewModel"] as InventoryViewModel;
                    if (vm != null)
                    {
                        // LoggingService.LogInfo("InventoryViewModel найден через Application.Current.Resources");
                        return vm;
                    }
                }
                
                // Способ 2: Через MainWindow
                var mainWindow = System.Windows.Application.Current.MainWindow;
                if (mainWindow?.DataContext is MainViewModel mainVM)
                {
                    // Попытаемся получить доступ к InventoryViewModel через MainViewModel
                    var inventoryVM = GetInventoryViewModelFromMainViewModel(mainVM);
                    if (inventoryVM != null)
                    {
                        // LoggingService.LogInfo("InventoryViewModel найден через MainWindow.DataContext");
                        return inventoryVM;
                    }
                }
                
                // Способ 3: Поиск во всех окнах приложения
                foreach (System.Windows.Window window in System.Windows.Application.Current.Windows)
                {
                    if (window.DataContext is InventoryViewModel inventoryVM)
                    {
                        // LoggingService.LogInfo("InventoryViewModel найден в окне приложения");
                        return inventoryVM;
                    }
                }
                
                LoggingService.LogError("InventoryViewModel не найден ни одним из способов");
                return null;
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Ошибка при поиске InventoryViewModel", ex);
                return null;
            }
        }
        
        private InventoryViewModel? GetInventoryViewModelFromMainViewModel(object mainVM)
        {
            try
            {
                // Используем рефлексию для получения InventoryViewModel
                var inventoryVMProperty = mainVM.GetType().GetProperty("InventoryViewModel");
                if (inventoryVMProperty != null)
                {
                    return inventoryVMProperty.GetValue(mainVM) as InventoryViewModel;
                }
                
                // Альтернативно, попытаемся найти поле
                var inventoryVMField = mainVM.GetType().GetField("_inventoryViewModel", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (inventoryVMField != null)
                {
                    return inventoryVMField.GetValue(mainVM) as InventoryViewModel;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Ошибка при получении InventoryViewModel из MainViewModel", ex);
                return null;
            }
        }

        private void OnInventoryChanged(object? sender, EventArgs e)
        {
            try
            {
                // LoggingService.LogDebug("OnInventoryChanged: Инвентарь изменился, обновляем рецепты");
                
                // Логируем текущее состояние инвентаря только при необходимости
                // Если включен детальный лог, показываем подробную информацию
                bool detailedLogging = false;
                #if DEBUG
                detailedLogging = true;
                #endif
                
                if (detailedLogging)
                {
                    var nonNullCount = _gameState.Inventory.Items.Count(x => x != null);
                    // LoggingService.LogDebug($"OnInventoryChanged: В инвентаре {nonNullCount} не-null предметов из {_gameState.Inventory.Items.Count}");
                    
                    // foreach (var item in _gameState.Inventory.Items.Where(x => x != null))
                    // {
                    //     LoggingService.LogDebug($"OnInventoryChanged: Предмет в инвентаре: {item.Name} x{item.StackSize}");
                    // }
                }
                
                // Используем DispatcherQueue для отложенного обновления UI с дебаунсингом
                if (_deferredRefreshTimer == null)
                {
                    _deferredRefreshTimer = new DispatcherTimer
                    {
                        Interval = TimeSpan.FromMilliseconds(150)
                    };
                    _deferredRefreshTimer.Tick += (s, e) =>
                    {
                        _deferredRefreshTimer?.Stop();
                        if (!_isRefreshing)
                        {
                            RefreshAvailableRecipes();
                        }
                    };
                }
                
                // Перезапускаем таймер при каждом изменении инвентаря,
                // чтобы обновить UI только после того, как все изменения будут завершены
                _deferredRefreshTimer.Stop();
                _deferredRefreshTimer.Start();
                
                // LoggingService.LogDebug("OnInventoryChanged: Запланировано обновление рецептов");
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Ошибка в OnInventoryChanged", ex);
            }
        }

        public string GetMissingMaterials()
        {
            if (SelectedRecipe == null)
                return "Рецепт не выбран";

            try
            {
                var missingMaterials = _craftingSystem.GetMissingMaterials(SelectedRecipe, _gameState.Inventory);
                
                if (missingMaterials.Count == 0)
                    return "Все материалы доступны";

                var missingList = missingMaterials.Select(m => $"{m.Key}: {m.Value}").ToArray();
                return string.Join("\n", missingList);
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Ошибка при получении недостающих материалов", ex);
                return "Ошибка при проверке материалов";
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class SimplifiedCraftingRecipeViewModel : INotifyPropertyChanged
    {
        private readonly SimplifiedCraftingRecipe _recipe;
        private readonly SimplifiedCraftingSystem _craftingSystem;
        private readonly Inventory _inventory;
        private bool _canCraft;
        private readonly ObservableCollection<MaterialRequirementViewModel> _requiredMaterials = new ObservableCollection<MaterialRequirementViewModel>();

        public event PropertyChangedEventHandler? PropertyChanged;

        public string Name => _recipe.Name;
        public string Description => _recipe.Description;
        public Item Result => _recipe.Result;
        public int ResultQuantity => _recipe.ResultQuantity;
        
        public SimplifiedCraftingRecipe Recipe => _recipe;

        public ObservableCollection<MaterialRequirementViewModel> RequiredMaterials => _requiredMaterials;

        public bool CanCraft
        {
            get => _canCraft;
            private set
            {
                if (_canCraft != value)
                {
                    _canCraft = value;
                    OnPropertyChanged();
                }
            }
        }

        public SimplifiedCraftingRecipeViewModel(SimplifiedCraftingRecipe recipe, SimplifiedCraftingSystem craftingSystem, Inventory inventory)
        {
            _recipe = recipe ?? throw new ArgumentNullException(nameof(recipe));
            _craftingSystem = craftingSystem ?? throw new ArgumentNullException(nameof(craftingSystem));
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));

            // Отладочная информация для диагностики привязки данных
            // LoggingService.LogDebug($"[CraftingRecipeVM] Создан ViewModel для рецепта: {_recipe.Name}");
            // LoggingService.LogDebug($"[CraftingRecipeVM] Result: {_recipe.Result?.Name ?? "null"}");
            // LoggingService.LogDebug($"[CraftingRecipeVM] Result SpritePath: {_recipe.Result?.SpritePath ?? "null"}");
            
            // КРИТИЧЕСКИ ВАЖНО: Проверяем и исправляем SpritePath если он пустой
            if (_recipe.Result != null && string.IsNullOrEmpty(_recipe.Result.SpritePath))
            {
                LoggingService.LogWarning($"[CraftingRecipeVM] SpritePath пустой для {_recipe.Result.Name}, устанавливаем дефолтный");
                _recipe.Result.SpritePath = AssetPaths.DEFAULT_IMAGE;
            }
            
            // Проверяем существование файла изображения
            if (_recipe.Result != null && !string.IsNullOrEmpty(_recipe.Result.SpritePath))
            {
                var fullPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _recipe.Result.SpritePath);
                if (!System.IO.File.Exists(fullPath))
                {
                    LoggingService.LogWarning($"[CraftingRecipeVM] Файл изображения не найден: {fullPath}, используем дефолтный");
                    _recipe.Result.SpritePath = AssetPaths.DEFAULT_IMAGE;
                }
                else
                {
                    // LoggingService.LogDebug($"[CraftingRecipeVM] Файл изображения найден: {fullPath}");
                }
            }

            UpdateRequiredMaterials();
            UpdateCraftability();
        }

        public void UpdateCraftability()
        {
            CanCraft = _craftingSystem.CanCraft(_recipe, _inventory);
            
            // РўР°РєР¶Рµ РѕР±РЅРѕРІР»СЏРµРј РјР°С‚РµСЂРёР°Р»С‹, С‡С‚РѕР±С‹ РїРѕРєР°Р·Р°С‚СЊ Р°РєС‚СѓР°Р»СЊРЅС‹Рµ РґР°РЅРЅС‹Рµ
            UpdateRequiredMaterials();
        }
        
        private void UpdateRequiredMaterials()
        {
            RequiredMaterials.Clear();
            
            foreach (var material in _recipe.RequiredMaterials)
            {
                int available = CountMaterialInInventory(material.Key);
                var materialViewModel = new MaterialRequirementViewModel
                {
                    Name = material.Key,
                    Required = material.Value,
                    Available = available,
                    IsAvailable = available >= material.Value
                };

                RequiredMaterials.Add(materialViewModel);
            }
        }
        
        private int CountMaterialInInventory(string materialName)
        {
            return _inventory.Items
                .Where(item => item?.Name == materialName)
                .Sum(item => item?.StackSize ?? 0);
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class MaterialRequirementViewModel : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private int _required;
        private int _available;
        private bool _isAvailable;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }
        }

        public int Required
        {
            get => _required;
            set
            {
                if (_required != value)
                {
                    _required = value;
                    OnPropertyChanged();
                }
            }
        }

        public int Available
        {
            get => _available;
            set
            {
                if (_available != value)
                {
                    _available = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsAvailable
        {
            get => _isAvailable;
            set
            {
                if (_isAvailable != value)
                {
                    _isAvailable = value;
                    OnPropertyChanged();
                }
            }
        }

        public string DisplayText => $"{Name}: {Available}/{Required}";

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 

