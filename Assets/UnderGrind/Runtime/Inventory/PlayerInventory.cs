using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnderGrind.Inventory
{
    [DisallowMultipleComponent]
    public sealed class PlayerInventory : MonoBehaviour
    {
        [SerializeField] private InventorySettings settings;
        [SerializeField] private List<InventorySlot> slots = new List<InventorySlot>();

        public event Action<PlayerInventory> Changed;

        public InventorySettings Settings => settings;
        public IReadOnlyList<InventorySlot> Slots => slots;
        public int SlotCapacity => settings != null ? settings.MaxSlots : slots.Count;
        public float MaxWeightKg => settings != null ? settings.MaxCarryWeightKg : 0f;
        public float CurrentWeightKg
        {
            get
            {
                var total = 0f;
                foreach (var slot in slots)
                    if (slot != null) total += slot.TotalWeightKg;
                return total;
            }
        }
        public float RemainingWeightKg => Mathf.Max(0f, MaxWeightKg - CurrentWeightKg);
        public bool IsOverloaded => settings != null && CurrentWeightKg > settings.MaxCarryWeightKg;
        public float OverloadedWalkSpeedMultiplier =>
            settings != null ? settings.OverloadedWalkSpeedMultiplier : 1f;

        private void Awake() => EnsureSlotCount();

        private void OnValidate()
        {
            if (slots == null) slots = new List<InventorySlot>();
            EnsureSlotCount();
        }

        public void Configure(InventorySettings inventorySettings)
        {
            settings = inventorySettings;
            EnsureSlotCount();
            Changed?.Invoke(this);
        }

        public InventorySlot GetSlot(int index)
        {
            if (index < 0 || index >= slots.Count) throw new ArgumentOutOfRangeException(nameof(index));
            return slots[index];
        }

        public InventoryOperationResult AddItem(ItemDefinition item, int quantity)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (quantity < 0) throw new ArgumentOutOfRangeException(nameof(quantity));
            if (quantity == 0) return new InventoryOperationResult(0, 0);
            EnsureSlotCount();

            var remaining = quantity;
            var usableSlotCount = Mathf.Min(slots.Count, SlotCapacity);
            for (var i = 0; i < usableSlotCount && remaining > 0; i++)
            {
                var slot = slots[i];
                if (slot.IsEmpty || slot.Item != item || slot.Quantity >= item.MaxStackSize) continue;
                var amount = Mathf.Min(remaining, item.MaxStackSize - slot.Quantity);
                slot.Set(item, slot.Quantity + amount);
                remaining -= amount;
            }

            for (var i = 0; i < usableSlotCount && remaining > 0; i++)
            {
                var slot = slots[i];
                if (!slot.IsEmpty) continue;
                var amount = Mathf.Min(remaining, item.MaxStackSize);
                slot.Set(item, amount);
                remaining -= amount;
            }

            var result = new InventoryOperationResult(quantity, quantity - remaining);
            if (result.Processed > 0) Changed?.Invoke(this);
            return result;
        }

        public InventoryOperationResult RemoveItem(ItemDefinition item, int quantity)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (quantity < 0) throw new ArgumentOutOfRangeException(nameof(quantity));
            var remaining = quantity;

            for (var i = 0; i < slots.Count && remaining > 0; i++)
            {
                var slot = slots[i];
                if (slot.IsEmpty || slot.Item != item) continue;
                var amount = Mathf.Min(remaining, slot.Quantity);
                slot.Set(item, slot.Quantity - amount);
                remaining -= amount;
            }

            var result = new InventoryOperationResult(quantity, quantity - remaining);
            if (result.Processed > 0) Changed?.Invoke(this);
            return result;
        }

        public bool MoveStack(int sourceIndex, int destinationIndex, int quantity)
        {
            ValidateIndex(sourceIndex);
            ValidateIndex(destinationIndex);
            if (sourceIndex == destinationIndex || quantity <= 0) return false;

            var source = slots[sourceIndex];
            var destination = slots[destinationIndex];
            if (source.IsEmpty || quantity > source.Quantity) return false;

            if (destination.IsEmpty)
            {
                destination.Set(source.Item, quantity);
                source.Set(source.Item, source.Quantity - quantity);
                Changed?.Invoke(this);
                return true;
            }

            if (destination.Item != source.Item) return false;
            var available = source.Item.MaxStackSize - destination.Quantity;
            if (available < quantity) return false;
            destination.Set(destination.Item, destination.Quantity + quantity);
            source.Set(source.Item, source.Quantity - quantity);
            Changed?.Invoke(this);
            return true;
        }

        public bool SplitStack(int sourceIndex, int destinationIndex, int quantity)
        {
            ValidateIndex(sourceIndex);
            ValidateIndex(destinationIndex);
            if (sourceIndex == destinationIndex || quantity <= 0) return false;
            var source = slots[sourceIndex];
            var destination = slots[destinationIndex];
            if (source.IsEmpty || !destination.IsEmpty || quantity >= source.Quantity) return false;
            destination.Set(source.Item, quantity);
            source.Set(source.Item, source.Quantity - quantity);
            Changed?.Invoke(this);
            return true;
        }

        private void EnsureSlotCount()
        {
            if (slots == null) slots = new List<InventorySlot>();
            for (var i = 0; i < slots.Count; i++)
                if (slots[i] == null) slots[i] = new InventorySlot();
            if (settings == null) return;
            while (slots.Count < settings.MaxSlots) slots.Add(new InventorySlot());
            while (slots.Count > settings.MaxSlots)
            {
                var last = slots[slots.Count - 1];
                if (last != null && !last.IsEmpty) break;
                slots.RemoveAt(slots.Count - 1);
            }
        }

        private void ValidateIndex(int index)
        {
            if (index < 0 || index >= slots.Count) throw new ArgumentOutOfRangeException(nameof(index));
        }
    }
}