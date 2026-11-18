using UnityEngine;

namespace UnderGrind.Inventory
{
    [CreateAssetMenu(menuName = "UnderGrind/Inventory/Interaction Settings", fileName = "PlayerInteractionSettings")]
    public sealed class PlayerInteractionSettings : ScriptableObject
    {
        [Header("Raccolta")]
        [SerializeField, Min(0.1f)] private float interactionDistance;
        [SerializeField, Min(0.001f)] private float pickupRayRadius;
        [SerializeField] private LayerMask interactionLayers;

        [Header("Rilascio")]
        [SerializeField, Min(0.1f)] private float dropForwardDistance;
        [SerializeField] private float dropHeightOffset;
        [SerializeField, Min(0f)] private float dropImpulse;
        [SerializeField, Min(1)] private int dropQuantityPerAction;

        public float InteractionDistance => interactionDistance;
        public float PickupRayRadius => pickupRayRadius;
        public LayerMask InteractionLayers => interactionLayers;
        public float DropForwardDistance => dropForwardDistance;
        public float DropHeightOffset => dropHeightOffset;
        public float DropImpulse => dropImpulse;
        public int DropQuantityPerAction => dropQuantityPerAction;

        private void OnValidate()
        {
            interactionDistance = Mathf.Max(0.1f, interactionDistance);
            pickupRayRadius = Mathf.Max(0.001f, pickupRayRadius);
            dropForwardDistance = Mathf.Max(0.1f, dropForwardDistance);
            dropImpulse = Mathf.Max(0f, dropImpulse);
            dropQuantityPerAction = Mathf.Max(1, dropQuantityPerAction);
        }
    }
}