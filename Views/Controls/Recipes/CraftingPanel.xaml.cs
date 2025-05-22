using System;
using System.Windows.Controls;
using System.Windows.Input;
using SketchBlade.ViewModels;

namespace SketchBlade.Views.Controls.Recipes
{
    /// <summary>
    /// Interaction logic for CraftingPanel.xaml
    /// </summary>
    public partial class CraftingPanel : UserControl
    {
        public CraftingPanel()
        {
            InitializeComponent();
        }
        
        private void CraftableItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                // Handle left-click on a craftable item
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    // Get the clicked item's data context
                    if (sender is CoreInventorySlot slot && slot.DataContext is CraftingRecipeViewModel recipe)
                    {
                        Console.WriteLine($"Clicked on craftable item: {recipe.Name}");
                        
                        // Get the CraftingViewModel
                        if (DataContext is CraftingViewModel viewModel)
                        {
                            // Craft the item
                            viewModel.CraftItem(recipe);
                            e.Handled = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CraftableItem_MouseDown: {ex.Message}");
            }
        }
    }
} 