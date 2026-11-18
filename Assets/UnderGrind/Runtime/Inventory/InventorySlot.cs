using System;
using UnityEngine;

namespace UnderGrind.Inventory
{
    [Serializable]
    public sealed class InventorySlot
    {
        [SerializeField] private ItemDefinition item;
        [SerializeField, Min(0)] private int quantity;

        public ItemDefinition Item => item;
        public int Quantity => quantity;
        public bool IsEmpty => item == null || quantity <= 0;
        public float TotalWeightKg => IsEmpty ? 0f : item.UnitWeightKg * quantity;

        internal void Set(ItemDefinition definition, int amount)
        {
            item = amount > 0 ? definition : null;
            quantity = amount > 0 ? amount : 0;
        }

        internal void Clear()
        {
            item = null;
            quantity = 0;
        }
    }
}