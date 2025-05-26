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
                File.AppendAllText("error_log.txt", 
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [ERROR] [DragDrop] InventorySlot_DragEnter exception: {ex.Message}\r\n");
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
                File.AppendAllText("error_log.txt", 
                    $"[{DateTime.Now}] [ERROR] InventorySlot_DragOver: {ex.Message}\r\n");
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
                File.AppendAllText("error_log.txt", 
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [ERROR] [DragDrop] InventorySlot_DragLeave exception: {ex.Message}\r\n");
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

                _viewModel.MoveItemBetweenSlots(dragData.SlotType, dragData.SlotIndex, "Equipment", GetEquipmentSlotIndex(equipmentType));
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
                
                // Принудительно обновляем UI после перемещения
                ForceUIUpdate();
                
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
                
                // Принудительно обновляем UI после перемещения
                ForceUIUpdate();
                
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
                // The primary mechanism for UI update should be through the InventoryData's notification.
                // This will trigger PropertyChanged for collections in InventoryData
                // and then dispatch a comprehensive UI update via InventoryData.NotifyInventoryChanged's
                // own Dispatcher.BeginInvoke block.
                _viewModel.GameData.Inventory.OnInventoryChanged();

                // Optional: If crafting UI needs an immediate hint that recipes might have changed,
                // this can be dispatched as well, but ensure it doesn't conflict with
                // updates triggered by OnInventoryChanged.
                // For simplicity and to avoid potential conflicts, we'll rely on OnInventoryChanged
                // to refresh everything necessary, including what's done by mainVM.RefreshUICommand
                // or mainWindow.RefreshCurrentScreen() which are called from NotifyInventoryChanged.

                // Example of a more targeted update if still needed *after* OnInventoryChanged
                // and if its effects are not sufficient for crafting:
                /*
                if (_viewModel.SimplifiedCraftingViewModel != null)
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        _viewModel.SimplifiedCraftingViewModel.RefreshAvailableRecipes();
                    }), DispatcherPriority.ContextIdle);
                }
                */
            }
            catch (Exception ex)
            {
                // Log the error, but don't let UI update issues crash the drag-drop.
                File.AppendAllText("error_log.txt", 
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [ERROR] Simplified ForceUIUpdate error: {ex.Message}\r\n");

                // Fallback: If the primary way fails, attempt a direct refresh on the UI thread.
                // This is a safety net, ideally OnInventoryChanged() should handle everything.
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    try 
                    {
                        _viewModel.RefreshAllSlots();
                        _viewModel.ForceUIUpdate();
                        if (Application.Current.MainWindow is MainWindow mainWindow)
                        {
                            mainWindow.RefreshCurrentScreen();
                        }
                    } 
                    catch (Exception innerEx)
                    {
                         File.AppendAllText("error_log.txt", 
                            $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [ERROR] Fallback ForceUIUpdate error: {innerEx.Message}\r\n");
                    }
                }), System.Windows.Threading.DispatcherPriority.Normal);
            }
        }
        
        private void RefreshUIAfterMove()
        {
            try
            {
                // Обновляем основной ViewModel инвентаря
                _viewModel.RefreshAllSlots();
                
                // Принудительно обновляем крафт
                if (_viewModel.SimplifiedCraftingViewModel != null)
                {
                    _viewModel.SimplifiedCraftingViewModel.RefreshAvailableRecipes();
                }
                
                // Принудительно обновляем инвентарь через GameState
                _viewModel.GameData.Inventory.OnInventoryChanged();
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"RefreshUIAfterMove: {ex.Message}", ex);
            }
        }
        
        #endregion
    }
} 