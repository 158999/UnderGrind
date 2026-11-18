using UnityEngine;

namespace UnderGrind.Inventory
{
    [CreateAssetMenu(menuName = "UnderGrind/Inventory/Inventory Settings", fileName = "PlayerInventorySettings")]
    public sealed class InventorySettings : ScriptableObject
    {
        [Header("Capacità")]
        [SerializeField, Min(1)] private int maxSlots;
        [SerializeField, Min(0f)] private float maxCarryWeightKg;

        [Header("Penalità da sovraccarico")]
        [SerializeField, Range(0f, 100f)] private float overloadedWalkSpeedReductionPercent;

        public int MaxSlots => maxSlots;
        public float MaxCarryWeightKg => maxCarryWeightKg;
        public float OverloadedWalkSpeedReductionPercent => overloadedWalkSpeedReductionPercent;
        public float OverloadedWalkSpeedMultiplier => 1f - overloadedWalkSpeedReductionPercent / 100f;

        private void OnValidate()
        {
            maxSlots = Mathf.Max(1, maxSlots);
            if (float.IsNaN(maxCarryWeightKg) || float.IsInfinity(maxCarryWeightKg) || maxCarryWeightKg < 0f)
                maxCarryWeightKg = 0f;
            overloadedWalkSpeedReductionPercent = Mathf.Clamp(overloadedWalkSpeedReductionPercent, 0f, 100f);
        }
    }
}