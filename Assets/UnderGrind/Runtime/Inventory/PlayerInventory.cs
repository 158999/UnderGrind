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
        [SerializeField, Min(0)] private int selectedSlotIndex;

        public event Action<PlayerInventory> Changed;

        public InventorySettings Settings => settings;
        public IReadOnlyList<InventorySlot> Slots => slots;
        public int SlotCapacity => slots != null ? slots.Count : 0;
        public int ConfiguredSlotCapacity => settings != null ? settings.MaxSlots : SlotCapacity;
        public int SelectedSlotIndex => SlotCapacity > 0 ? Mathf.Clamp(selectedSlotIndex, 0, SlotCapacity - 1) : 0;
        public InventorySlot SelectedSlot => SlotCapacity > 0 ? slots[SelectedSlotIndex] : null;
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
            if (index < 0 || index >= SlotCapacity)
                throw new ArgumentOutOfRangeException(nameof(index));
            return slots[index];
        }

        public InventoryOperationResult AddItem(ItemDefinition item, int quantity)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (quantity < 0) throw new ArgumentOutOfRangeException(nameof(quantity));
            if (quantity == 0) return new InventoryOperationResult(0, 0);
            EnsureSlotCount();

            var remaining = quantity;
            var usableSlotCount = slots.Count;
            for (var i = 0; i < usableSlotCount && remaining > 0; i++)
            {
                var slot = slots[i];
                if (slot.IsEmpty || slot.Item != item || slot.Quantity >= item.MaxStackSize) continue;
                var amount = Mathf.Min(remaining, item.MaxStackSize - slot.Quantity);
                slot.Set(item, slot.Quantity + amount);
                remaining -= amount;
            }

            var emptySlotLimit = settings != null ? Mathf.Min(settings.MaxSlots, slots.Count) : slots.Count;
            for (var i = 0; i < emptySlotLimit && remaining > 0; i++)
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

            for (var i = 0; i < SlotCapacity && remaining > 0; i++)
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

        public bool SelectSlot(int index)
        {
            ValidateIndex(index);
            if (selectedSlotIndex == index) return false;
            selectedSlotIndex = index;
            Changed?.Invoke(this);
            return true;
        }

        public InventoryOperationResult RemoveFromSlot(int index, int quantity)
        {
            ValidateIndex(index);
            if (quantity < 0) throw new ArgumentOutOfRangeException(nameof(quantity));
            if (quantity == 0) return new InventoryOperationResult(0, 0);

            var slot = slots[index];
            if (slot.IsEmpty) return new InventoryOperationResult(quantity, 0);
            var amount = Mathf.Min(quantity, slot.Quantity);
            slot.Set(slot.Item, slot.Quantity - amount);
            Changed?.Invoke(this);
            return new InventoryOperationResult(quantity, amount);
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
            if (slots.Count == 0) selectedSlotIndex = 0;
            else selectedSlotIndex = Mathf.Clamp(selectedSlotIndex, 0, slots.Count - 1);
        }

        private void ValidateIndex(int index)
        {
            if (index < 0 || index >= SlotCapacity)
                throw new ArgumentOutOfRangeException(nameof(index));
        }
    }
}