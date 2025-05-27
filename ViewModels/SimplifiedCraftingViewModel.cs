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

            // LoggingService.LogDebug("������������� SimplifiedCraftingViewModel");

            CraftItemCommand = new RelayCommand<object>(_ => CraftSelectedItem());
            RefreshRecipesCommand = new RelayCommand<object>(_ => RefreshAvailableRecipes());

            LoadRecipes();

            // ������������� �� ��������� ���������
            if (_gameState.Inventory != null)
            {
                _gameState.Inventory.InventoryChanged += OnInventoryChanged;
                // LoggingService.LogDebug("�������� �� ��������� ��������� ���������");
            }
            else
            {
                LoggingService.LogError("��������� � GameData ����� null");
            }
        }

        private void LoadRecipes()
        {
            try
            {
                AvailableRecipes.Clear();
                // LoggingService.LogDebug("AvailableRecipes �������");
                
                // ��������� ��� ��������� �������, � �� ������ ��, ������� ����� �������
                var allRecipes = _craftingSystem.GetAvailableRecipes();
                // LoggingService.LogDebug($"����� �������� � �������: {allRecipes.Count}");
                
                // ���������: ��������� � ���������� ���� � ������������ ��� ���� ��������
                VerifyAndFixImagePaths(allRecipes);
                
                // ��������� ������ ������ � ��������� ��� �������
                foreach (var recipe in allRecipes)
                {
                    bool canCraft = _craftingSystem.CanCraft(recipe, _gameState.Inventory);
                    // LoggingService.LogDebug($"������ {recipe.Name}: ����� ������� = {canCraft}");
                    
                    // ��������� ��� �������, ���������� �� �����������
                    var viewModel = new SimplifiedCraftingRecipeViewModel(recipe, _craftingSystem, _gameState.Inventory);
                    AvailableRecipes.Add(viewModel);
                    // LoggingService.LogDebug($"�������� ������ � AvailableRecipes: {recipe.Name} (��������: {viewModel.CanCraft})");
                }

                // LoggingService.LogDebug($"��������� {AvailableRecipes.Count} ��������");
                // LoggingService.LogInfo($"����� �������� � AvailableRecipes: {AvailableRecipes.Count}");
                
                // ���������� �����: ������������� ���������� UI �� ����������
                // LoggingService.LogDebug("���������� PropertyChanged ����������� ��� AvailableRecipes");
                OnPropertyChanged(nameof(AvailableRecipes));
                
                // �������������� �����������
                // LoggingService.LogDebug($"AvailableRecipes.Count ����� ��������: {AvailableRecipes.Count}");
                // for (int i = 0; i < AvailableRecipes.Count; i++)
                // {
                //     var recipe = AvailableRecipes[i];
                //     LoggingService.LogDebug($"  AvailableRecipes[{i}]: {recipe.Name}, Result: {recipe.Result?.Name ?? "null"}, CanCraft: {recipe.CanCraft}");
                // }
            }
            catch (Exception ex)
            {
                LoggingService.LogError("������ ��� �������� ��������", ex);
            }
        }

        /// <summary>
        /// ��������� � ���������� ���� � ������������ ��� ���� ��������
        /// </summary>
        private void VerifyAndFixImagePaths(List<SimplifiedCraftingRecipe> recipes)
        {
            try
            {
                // LoggingService.LogInfo("�������� � ����������� ����� � ������������ ��� ��������");
                
                foreach (var recipe in recipes)
                {
                    if (recipe.Result == null)
                    {
                        LoggingService.LogWarning($"��������� ������� {recipe.Name} ����� null");
                        continue;
                    }
                    
                    // ��������� ���� � �����������
                    if (string.IsNullOrEmpty(recipe.Result.SpritePath))
                    {
                        LoggingService.LogWarning($"������ SpritePath ��� ���������� ������� {recipe.Name}, ������������� ���������");
                        recipe.Result.SpritePath = Utilities.AssetPaths.DEFAULT_IMAGE;
                    }
                    
                    // ��������� ������������� �����
                    string fullPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, recipe.Result.SpritePath);
                    if (!System.IO.File.Exists(fullPath))
                    {
                        LoggingService.LogWarning($"���� ����������� �� ������: {fullPath}, ���������� ���������");
                        recipe.Result.SpritePath = Utilities.AssetPaths.DEFAULT_IMAGE;
                    }
                    
                    // �������� �������� ����
                                            // LoggingService.LogDebug($"�������� SpritePath ��� {recipe.Name}: {recipe.Result.SpritePath}");
                }
                
                // LoggingService.LogInfo($"�������� � ����������� ����� ��������� ��� {recipes.Count} ��������");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"������ ��� �������� ����� � ������������: {ex.Message}", ex);
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
                        // ��������� ������ ����������� ������ ��� ������� �������
                        foreach (var recipeViewModel in AvailableRecipes)
                        {
                            recipeViewModel.UpdateCraftability();
                        }

                        // ��������� ��������� ��������� ��� ���������� �������
                        UpdateRequiredMaterials();

                        OnPropertyChanged(nameof(CanCraft));
                        // LoggingService.LogDebug("������� ���������");
                    }
                    catch (Exception ex)
                    {
                        LoggingService.LogError($"������ ��� ���������� �������� � ����������: {ex.Message}", ex);
                    }
                    finally
                    {
                        _isRefreshing = false;
                    }
                }));
            }
            catch (Exception ex)
            {
                LoggingService.LogError("������ ��� ���������� ��������", ex);
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
                LoggingService.LogError("������ ��� ���������� ��������� ����������", ex);
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
            // LoggingService.LogInfo($"[CRAFT-SYSTEM] ========== ������ CraftSelectedItem ==========");
            // LoggingService.LogInfo($"[CRAFT-SYSTEM] CraftSelectedItem: ������ ����� ������. SelectedRecipe: {SelectedRecipe?.Name ?? "null"}, CanCraft: {CanCraft}");
            
            if (SelectedRecipe == null)
            {
                LoggingService.LogWarning("[CRAFT-SYSTEM] CraftSelectedItem: ������ �� ������");
                return;
            }
            
            if (!CanCraft)
            {
                LoggingService.LogWarning($"[CRAFT-SYSTEM] CraftSelectedItem: ������������ ���������� ��� ������ {SelectedRecipe.Name}");
                
                // �������� ����������� ���������
                var missingMaterials = _craftingSystem.GetMissingMaterials(SelectedRecipe, _gameState.Inventory);
                foreach (var missing in missingMaterials)
                {
                    LoggingService.LogWarning($"[CRAFT-SYSTEM] CraftSelectedItem: ��������� {missing.Value} {missing.Key}");
                }
                return;
            }

            try
            {
                // LoggingService.LogInfo($"[CRAFT-SYSTEM] === ������ ������: {SelectedRecipe.Name} ===");
                
                // �������� ��������� ��������� ����� �������
                // LoggingService.LogInfo($"[CRAFT-SYSTEM] ��������� ��������� ����� �������:");
                // for (int i = 0; i < _gameState.Inventory.Items.Count; i++)
                // {
                //     var item = _gameState.Inventory.Items[i];
                //     LoggingService.LogInfo($"[CRAFT-SYSTEM]   ���������[{i}]: {item?.Name ?? "null"}");
                // }
                
                // LoggingService.LogInfo($"[CRAFT-SYSTEM] �������� _craftingSystem.Craft()...");
                bool success = _craftingSystem.Craft(SelectedRecipe, _gameState.Inventory);
                // LoggingService.LogInfo($"[CRAFT-SYSTEM] _craftingSystem.Craft() ������: {success}");
                
                if (success)
                {
                    // LoggingService.LogInfo($"[CRAFT-SYSTEM] ����� �������� �������: {SelectedRecipe.Name}");
                    
                    // �������� ��������� ��������� ����� ������
                    // LoggingService.LogInfo($"[CRAFT-SYSTEM] ��������� ��������� ����� ������:");
                    // for (int i = 0; i < _gameState.Inventory.Items.Count; i++)
                    // {
                    //     var item = _gameState.Inventory.Items[i];
                    //     LoggingService.LogInfo($"[CRAFT-SYSTEM]   ���������[{i}]: {item?.Name ?? "null"}");
                    // }
                    
                    // ��������� ������� � ���������� �� ��������� ���������
                    // LoggingService.LogInfo($"[CRAFT-SYSTEM] ��������� �������...");
                    RefreshAvailableRecipes();
                    // LoggingService.LogInfo($"[CRAFT-SYSTEM] ���������� �� ��������� ���������...");
                    _gameState.Inventory.OnInventoryChanged();
                    
                    // ������� ���������� UI ��� ���������� �������
                    // LoggingService.LogInfo($"[CRAFT-SYSTEM] ���� InventoryViewModel...");
                    var inventoryVM = FindInventoryViewModel();
                    if (inventoryVM != null)
                    {
                        // LoggingService.LogInfo($"[CRAFT-SYSTEM] InventoryViewModel ������, �������� RefreshAllSlots()...");
                        inventoryVM.RefreshAllSlots();
                        // LoggingService.LogInfo($"[CRAFT-SYSTEM] RefreshAllSlots() ��������");
                    }
                    else
                    {
                        LoggingService.LogError($"[CRAFT-SYSTEM] InventoryViewModel �� ������!");
                    }
                    
                    // LoggingService.LogInfo($"[CRAFT-SYSTEM] === ����� ��������: {SelectedRecipe.Name} ===");
                }
                else
                {
                    LoggingService.LogError($"[CRAFT-SYSTEM] �� ������� ������� �������: {SelectedRecipe.Name}");
                }
                
                // LoggingService.LogInfo($"[CRAFT-SYSTEM] ========== ����� CraftSelectedItem ==========");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"[CRAFT-SYSTEM] ������ ��� ������ �������� {SelectedRecipe?.Name}: {ex.Message}", ex);
                LoggingService.LogError($"[CRAFT-SYSTEM] StackTrace: {ex.StackTrace}");
            }
        }
        
        private InventoryViewModel? FindInventoryViewModel()
        {
            try
            {
                // ������ 1: ����� Application.Current.Resources
                if (System.Windows.Application.Current.Resources.Contains("InventoryViewModel"))
                {
                    var vm = System.Windows.Application.Current.Resources["InventoryViewModel"] as InventoryViewModel;
                    if (vm != null)
                    {
                        // LoggingService.LogInfo("InventoryViewModel ������ ����� Application.Current.Resources");
                        return vm;
                    }
                }
                
                // ������ 2: ����� MainWindow
                var mainWindow = System.Windows.Application.Current.MainWindow;
                if (mainWindow?.DataContext is MainViewModel mainVM)
                {
                    // ���������� �������� ������ � InventoryViewModel ����� MainViewModel
                    var inventoryVM = GetInventoryViewModelFromMainViewModel(mainVM);
                    if (inventoryVM != null)
                    {
                        // LoggingService.LogInfo("InventoryViewModel ������ ����� MainWindow.DataContext");
                        return inventoryVM;
                    }
                }
                
                // ������ 3: ����� �� ���� ����� ����������
                foreach (System.Windows.Window window in System.Windows.Application.Current.Windows)
                {
                    if (window.DataContext is InventoryViewModel inventoryVM)
                    {
                        // LoggingService.LogInfo("InventoryViewModel ������ � ���� ����������");
                        return inventoryVM;
                    }
                }
                
                LoggingService.LogError("InventoryViewModel �� ������ �� ����� �� ��������");
                return null;
            }
            catch (Exception ex)
            {
                LoggingService.LogError("������ ��� ������ InventoryViewModel", ex);
                return null;
            }
        }
        
        private InventoryViewModel? GetInventoryViewModelFromMainViewModel(object mainVM)
        {
            try
            {
                // ���������� ��������� ��� ��������� InventoryViewModel
                var inventoryVMProperty = mainVM.GetType().GetProperty("InventoryViewModel");
                if (inventoryVMProperty != null)
                {
                    return inventoryVMProperty.GetValue(mainVM) as InventoryViewModel;
                }
                
                // �������������, ���������� ����� ����
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
                LoggingService.LogError("������ ��� ��������� InventoryViewModel �� MainViewModel", ex);
                return null;
            }
        }

        private void OnInventoryChanged(object? sender, EventArgs e)
        {
            try
            {
                // LoggingService.LogDebug("OnInventoryChanged: ��������� ���������, ��������� �������");
                
                // �������� ������� ��������� ��������� ������ ��� �������������
                // ���� ������� ��������� ���, ���������� ��������� ����������
                bool detailedLogging = false;
                #if DEBUG
                detailedLogging = true;
                #endif
                
                if (detailedLogging)
                {
                    var nonNullCount = _gameState.Inventory.Items.Count(x => x != null);
                    // LoggingService.LogDebug($"OnInventoryChanged: � ��������� {nonNullCount} ��-null ��������� �� {_gameState.Inventory.Items.Count}");
                    
                    // foreach (var item in _gameState.Inventory.Items.Where(x => x != null))
                    // {
                    //     LoggingService.LogDebug($"OnInventoryChanged: ������� � ���������: {item.Name} x{item.StackSize}");
                    // }
                }
                
                // ���������� DispatcherQueue ��� ����������� ���������� UI � ������������
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
                
                // ������������� ������ ��� ������ ��������� ���������,
                // ����� �������� UI ������ ����� ����, ��� ��� ��������� ����� ���������
                _deferredRefreshTimer.Stop();
                _deferredRefreshTimer.Start();
                
                // LoggingService.LogDebug("OnInventoryChanged: ������������� ���������� ��������");
            }
            catch (Exception ex)
            {
                LoggingService.LogError("������ � OnInventoryChanged", ex);
            }
        }

        public string GetMissingMaterials()
        {
            if (SelectedRecipe == null)
                return "������ �� ������";

            try
            {
                var missingMaterials = _craftingSystem.GetMissingMaterials(SelectedRecipe, _gameState.Inventory);
                
                if (missingMaterials.Count == 0)
                    return "��� ��������� ��������";

                var missingList = missingMaterials.Select(m => $"{m.Key}: {m.Value}").ToArray();
                return string.Join("\n", missingList);
            }
            catch (Exception ex)
            {
                LoggingService.LogError("������ ��� ��������� ����������� ����������", ex);
                return "������ ��� �������� ����������";
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

            // ���������� ���������� ��� ����������� �������� ������
            // LoggingService.LogDebug($"[CraftingRecipeVM] ������ ViewModel ��� �������: {_recipe.Name}");
            // LoggingService.LogDebug($"[CraftingRecipeVM] Result: {_recipe.Result?.Name ?? "null"}");
            // LoggingService.LogDebug($"[CraftingRecipeVM] Result SpritePath: {_recipe.Result?.SpritePath ?? "null"}");
            
            // ���������� �����: ��������� � ���������� SpritePath ���� �� ������
            if (_recipe.Result != null && string.IsNullOrEmpty(_recipe.Result.SpritePath))
            {
                LoggingService.LogWarning($"[CraftingRecipeVM] SpritePath ������ ��� {_recipe.Result.Name}, ������������� ���������");
                _recipe.Result.SpritePath = AssetPaths.DEFAULT_IMAGE;
            }
            
            // ��������� ������������� ����� �����������
            if (_recipe.Result != null && !string.IsNullOrEmpty(_recipe.Result.SpritePath))
            {
                var fullPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _recipe.Result.SpritePath);
                if (!System.IO.File.Exists(fullPath))
                {
                    LoggingService.LogWarning($"[CraftingRecipeVM] ���� ����������� �� ������: {fullPath}, ���������� ���������");
                    _recipe.Result.SpritePath = AssetPaths.DEFAULT_IMAGE;
                }
                else
                {
                    // LoggingService.LogDebug($"[CraftingRecipeVM] ���� ����������� ������: {fullPath}");
                }
            }

            UpdateRequiredMaterials();
            UpdateCraftability();
        }

        public void UpdateCraftability()
        {
            CanCraft = _craftingSystem.CanCraft(_recipe, _inventory);
            
            // Также обновляем материалы, чтобы показать актуальные данные
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

