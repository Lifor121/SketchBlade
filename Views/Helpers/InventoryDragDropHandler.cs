using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SketchBlade.Models;
using SketchBlade.ViewModels;
using SketchBlade.Views.Controls;
using SketchBlade.Services;
using System.IO;
using System.Windows.Threading;
using System.Collections.Generic;

namespace SketchBlade.Views.Helpers
{
    /// <summary>
    /// Handles all drag and drop operations for the inventory system
    /// </summary>
    public class InventoryDragDropHandler
    {
        private readonly InventoryViewModel _viewModel;
        
        public InventoryDragDropHandler(InventoryViewModel viewModel)
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        }
        
        #region Drag Enter Events
        
        public void HandleCraftSlotDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("ItemSlotInfo") || e.Data.GetDataPresent(typeof(Item)))
            {
                e.Effects = DragDropEffects.Move;
                e.Handled = true;
                
                if (sender is FrameworkElement element)
                {
                    element.Opacity = 0.7;
                }
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }
        
        public void HandleInventorySlotDragEnter(object sender, DragEventArgs e)
        {
            try
            {
                if (sender is not CoreInventorySlot targetSlot)
                {
                    e.Effects = DragDropEffects.None;
                    return;
                }

                // Проверяем наличие данных
                if (!e.Data.GetDataPresent("ItemSlotInfo"))
                {
                    e.Effects = DragDropEffects.None;
                    return;
                }

                var dragData = e.Data.GetData("ItemSlotInfo") as SketchBlade.Views.Controls.ItemSlotInfo;
                if (dragData == null)
                {
                    e.Effects = DragDropEffects.None;
                    return;
                }

                // Показываем визуальную подсказку
                targetSlot.Background = new SolidColorBrush(Color.FromArgb(128, 0, 255, 0));
                e.Effects = DragDropEffects.Move;
            }
            catch (Exception ex)
            {
                e.Effects = DragDropEffects.None;
            }
        }
        
        #endregion
        
        #region Drag Over Events
        
        public void HandleCraftSlotDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("ItemSlotInfo") || e.Data.GetDataPresent(typeof(Item)))
            {
                e.Effects = DragDropEffects.Move;
                e.Handled = true;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }
        
        public void HandleInventorySlotDragOver(object sender, DragEventArgs e)
        {
            try
            {
                if (e.Data.GetDataPresent("ItemSlotInfo") || e.Data.GetDataPresent(typeof(Item)))
                {
                    e.Effects = DragDropEffects.Move;
                    e.Handled = true;
                }
                else
                {
                    e.Effects = DragDropEffects.None;
                }
            }
            catch (Exception ex)
            {
                // Error in drag over - continue silently
            }
        }
        
        #endregion
        
        #region Drag Leave Events
        
        public void HandleCraftSlotDragLeave(object sender, DragEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                element.Opacity = 1.0;
            }
        }
        
        public void HandleInventorySlotDragLeave(object sender, DragEventArgs e)
        {
            try
            {
                if (sender is CoreInventorySlot targetSlot)
                {
                    targetSlot.Background = null;
                }
            }
            catch (Exception ex)
            {
                // Error in drag leave - continue silently
            }
        }
        
        #endregion
        
        #region Drop Events
        
        public void HandleEquipmentSlotDrop(object sender, DragEventArgs e)
        {
            try
            {
                if (sender is not CoreInventorySlot targetSlot)
                    return;

                var dragData = e.Data.GetData("ItemSlotInfo") as SketchBlade.Views.Controls.ItemSlotInfo;
                if (dragData?.Item == null)
                    return;

                string equipmentType = targetSlot.SlotType;
                
                // Проверяем совместимость предмета с типом слота
                if (!IsItemCompatibleWithEquipmentSlot(dragData.Item, equipmentType))
                {
                    MessageBox.Show($"Этот предмет нельзя экипировать в слот {equipmentType}");
                    return;
                }

                _viewModel.MoveItemBetweenSlots(dragData.SlotType, dragData.SlotIndex, equipmentType, 0);
                
                // Принудительно обновляем UI сразу после drag-and-drop для немедленного отображения изменений
                RefreshUIAfterMove();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in EquipmentSlot_Drop: {ex.Message}");
            }
        }
        
        public void HandleQuickSlotDrop(object sender, DragEventArgs e)
        {
            try
            {
                if (sender is not CoreInventorySlot targetSlot)
                    return;

                var dragData = e.Data.GetData("ItemSlotInfo") as SketchBlade.Views.Controls.ItemSlotInfo;
                if (dragData?.Item == null)
                    return;

                // Проверяем, что предмет можно поместить в быстрый слот
                if (dragData.Item.Type != ItemType.Consumable)
                {
                    MessageBox.Show("В быстрые слоты можно помещать только расходные предметы");
                    return;
                }

                int quickSlotIndex = targetSlot.SlotIndex;
                _viewModel.MoveItemBetweenSlots(dragData.SlotType, dragData.SlotIndex, "Quick", quickSlotIndex);
                
                // Принудительно обновляем UI сразу после drag-and-drop для немедленного отображения изменений
                RefreshUIAfterMove();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in QuickSlot_Drop: {ex.Message}");
            }
        }
        
        public void HandleTrashSlotDrop(object sender, DragEventArgs e)
        {
            try
            {
                var dragData = e.Data.GetData("ItemSlotInfo") as SketchBlade.Views.Controls.ItemSlotInfo;
                if (dragData?.Item == null)
                    return;

                var result = MessageBox.Show(
                    $"Вы уверены, что хотите удалить {dragData.Item.Name}?",
                    "Подтверждение удаления",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _viewModel.MoveItemBetweenSlots(dragData.SlotType, dragData.SlotIndex, "Trash", 0);
                    
                    // Принудительно обновляем UI сразу после drag-and-drop для немедленного отображения изменений
                    RefreshUIAfterMove();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in TrashSlot_Drop: {ex.Message}");
            }
        }
        
        public void HandleInventorySlotDrop(object sender, DragEventArgs e)
        {
            try
            {
                if (sender is not CoreInventorySlot targetSlot)
                    return;

                var dragData = e.Data.GetData("ItemSlotInfo") as SketchBlade.Views.Controls.ItemSlotInfo;
                if (dragData == null)
                    return;

                int targetIndex = targetSlot.SlotIndex;
                _viewModel.MoveItemBetweenSlots(dragData.SlotType, dragData.SlotIndex, "Inventory", targetIndex);
                
                // Принудительно обновляем UI сразу после drag-and-drop для немедленного отображения изменений
                RefreshUIAfterMove();
                
                // Сбрасываем фон слота
                targetSlot.Background = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in InventorySlot_Drop: {ex.Message}");
            }
        }
        
        public void HandleCraftSlotDrop(object sender, DragEventArgs e)
        {
            try
            {
                if (sender is not CoreInventorySlot targetSlot)
                    return;

                var dragData = e.Data.GetData("ItemSlotInfo") as SketchBlade.Views.Controls.ItemSlotInfo;
                if (dragData == null)
                    return;

                int targetIndex = targetSlot.SlotIndex;
                _viewModel.MoveItemBetweenSlots(dragData.SlotType, dragData.SlotIndex, "Craft", targetIndex);
                
                // Принудительно обновляем UI сразу после drag-and-drop для немедленного отображения изменений
                RefreshUIAfterMove();
                
                // Восстанавливаем прозрачность слота
                if (sender is FrameworkElement element)
                {
                    element.Opacity = 1.0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in CraftSlot_Drop: {ex.Message}");
            }
        }
        
        #endregion
        
        #region Helper Methods
        
        private bool IsItemCompatibleWithEquipmentSlot(Item item, string equipmentType)
        {
            return equipmentType switch
            {
                "Helmet" => item.Type == ItemType.Helmet,
                "Armor" => item.Type == ItemType.Chestplate,
                "Leggings" => item.Type == ItemType.Leggings,
                "Weapon" => item.Type == ItemType.Weapon,
                "Shield" => item.Type == ItemType.Shield,
                _ => false
            };
        }
        
        private int GetEquipmentSlotIndex(string equipmentType)
        {
            return equipmentType switch
            {
                "Helmet" => 0,
                "Armor" => 1,
                "Leggings" => 2,
                "Weapon" => 3,
                "Shield" => 4,
                _ => 0
            };
        }
        
        private void ForceUIUpdate()
        {
            try
            {
                // Используем тот же механизм обновления, что и при переключении экранов
                // для обеспечения консистентности поведения UI
                
                // 1. Сначала обновляем данные инвентаря
                _viewModel.GameData.Inventory.OnInventoryChanged();
                
                // 2. Принудительно обновляем ViewModel инвентаря (как при переключении экранов)
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    try 
                    {
                        // Используем тот же метод, что вызывается при переключении экранов
                        _viewModel.RefreshAllSlots();
                        
                        _viewModel.ForceUIUpdate();
                        
                        // Обновляем крафтинг если нужно
                        if (_viewModel.SimplifiedCraftingViewModel != null)
                        {
                            _viewModel.SimplifiedCraftingViewModel.RefreshAvailableRecipes();
                        }
                        
                        // Принудительно обновляем главное окно (как при переключении экранов)
                        if (Application.Current.MainWindow is MainWindow mainWindow)
                        {
                            mainWindow.RefreshCurrentScreen();
                        }
                    } 
                    catch (Exception innerEx)
                    {
                        // Error in UI update - continue silently
                    }
                }), System.Windows.Threading.DispatcherPriority.Render); // Используем высокий приоритет для немедленного обновления
            }
            catch (Exception ex)
            {
                // Log the error, but don't let UI update issues crash the drag-drop.
            }
        }
        
        private void RefreshUIAfterMove()
        {
            try
            {
                // Обновляем основной ViewModel инвентаря
                _viewModel.RefreshAllSlots();
                
                // НОВОЕ: Принудительно обновляем UI контролы (это главное!)
                ForceUpdateUIControls();
                
                // Обновляем крафт ОДИН РАЗ в конце (без вызова OnInventoryChanged)
                if (_viewModel.SimplifiedCraftingViewModel != null)
                {
                    _viewModel.SimplifiedCraftingViewModel.RefreshAvailableRecipes();
                }
            }
            catch (Exception ex)
            {
                // Error in refresh UI after move - continue silently
            }
        }
        
        /// <summary>
        /// Принудительно обновляет все UI контролы CoreInventorySlot
        /// </summary>
        private void ForceUpdateUIControls()
        {
            try
            {
                // Используем Dispatcher для обновления на UI потоке
                if (System.Windows.Application.Current?.Dispatcher != null)
                {
                    System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        try
                        {
                            // Находим главное окно
                            var mainWindow = System.Windows.Application.Current.MainWindow;
                            if (mainWindow != null)
                            {
                                // Находим все CoreInventorySlot контролы
                                var inventorySlots = FindVisualChildren<CoreInventorySlot>(mainWindow);
                                
                                foreach (var slot in inventorySlots)
                                {
                                    try
                                    {
                                        // ИСКЛЮЧАЕМ CraftResult слоты - они управляются системой крафта
                                        if (slot.SlotType == "CraftResult")
                                        {
                                            continue;
                                        }
                                        
                                        // Получаем актуальные данные из ViewModel
                                        var actualItem = _viewModel.GetItemFromSlot(slot.SlotType, slot.SlotIndex);
                                        
                                        // Принудительно устанавливаем Item если он отличается
                                        if (slot.Item != actualItem)
                                        {
                                            slot.Item = actualItem;
                                            
                                            // Принудительно обновляем визуальное отображение только если изменился Item
                                            slot.InvalidateVisual();
                                            slot.UpdateLayout();
                                        }
                                    }
                                    catch (Exception slotEx)
                                    {
                                        // Error updating slot - continue silently
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            // Error in dispatcher - continue silently
                        }
                    }), System.Windows.Threading.DispatcherPriority.Render);
                }
            }
            catch (Exception ex)
            {
                // Error in force update UI controls - continue silently
            }
        }
        
        /// <summary>
        /// Находит все дочерние элементы указанного типа в визуальном дереве
        /// </summary>
        private static IEnumerable<T> FindVisualChildren<T>(System.Windows.DependencyObject depObj) where T : System.Windows.DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    var child = System.Windows.Media.VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }
        
        #endregion
    }
} 