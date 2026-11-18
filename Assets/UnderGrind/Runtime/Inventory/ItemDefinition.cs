using UnityEngine;

namespace UnderGrind.Inventory
{
    [CreateAssetMenu(menuName = "UnderGrind/Inventory/Item Definition", fileName = "ItemDefinition")]
    public sealed class ItemDefinition : ScriptableObject
    {
        [SerializeField] private string itemId;
        [SerializeField] private string displayName;
        [SerializeField] private Sprite icon;
        [SerializeField, Min(1)] private int maxStackSize = 1;
        [SerializeField, Min(0f)] private float unitWeightKg;

        public string ItemId => itemId;
        public string DisplayName => displayName;
        public Sprite Icon => icon;
        public int MaxStackSize => maxStackSize;
        public float UnitWeightKg => unitWeightKg;

        private void OnValidate()
        {
            maxStackSize = Mathf.Max(1, maxStackSize);
            if (float.IsNaN(unitWeightKg) || float.IsInfinity(unitWeightKg) || unitWeightKg < 0f)
                unitWeightKg = 0f;
        }
    }
}