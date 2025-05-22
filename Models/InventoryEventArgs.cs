using System;
using SketchBlade.Models;

namespace SketchBlade.Models
{
    // Class for stack splitting event arguments
    public class SplitStackEventArgs : EventArgs
    {
        public Item SourceItem { get; private set; }
        public int Amount { get; private set; }
        public string TargetSlotType { get; private set; }
        public int TargetSlotIndex { get; private set; }
        
        public SplitStackEventArgs(Item sourceItem, int amount)
        {
            SourceItem = sourceItem;
            Amount = amount;
            TargetSlotType = string.Empty;
            TargetSlotIndex = -1;
        }
        
        public SplitStackEventArgs(Item sourceItem, int amount, string targetSlotType, int targetSlotIndex)
        {
            SourceItem = sourceItem;
            Amount = amount;
            TargetSlotType = targetSlotType;
            TargetSlotIndex = targetSlotIndex;
        }
    }
} 