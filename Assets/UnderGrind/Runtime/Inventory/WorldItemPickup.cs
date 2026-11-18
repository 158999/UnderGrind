using UnityEngine;

namespace UnderGrind.Inventory
{
    [DisallowMultipleComponent]
    public sealed class WorldItemPickup : MonoBehaviour
    {
        [SerializeField] private ItemDefinition item;
        [SerializeField, Min(1)] private int quantity = 1;

        public ItemDefinition Item => item;
        public int Quantity => quantity;

        public bool Configure(ItemDefinition definition, int amount)
        {
            if (definition == null || amount < 1) return false;
            item = definition;
            quantity = amount;
            return true;
        }

        public bool TryPickup(PlayerInventory inventory)
        {
            if (inventory == null || item == null || quantity < 1) return false;
            var result = inventory.AddItem(item, quantity);
            if (result.Processed <= 0) return false;

            quantity = result.Remaining;
            if (quantity == 0) Destroy(gameObject);
            return true;
        }

        private void OnValidate()
        {
            quantity = Mathf.Max(1, quantity);
        }
    }
}