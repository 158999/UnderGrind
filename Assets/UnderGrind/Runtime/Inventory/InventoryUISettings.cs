using UnityEngine;

namespace UnderGrind.Inventory
{
    [CreateAssetMenu(menuName = "UnderGrind/Inventory/UI Settings", fileName = "PlayerInventoryUISettings")]
    public sealed class InventoryUISettings : ScriptableObject
    {
        [Header("Testi")]
        [SerializeField] private string panelTitle;
        [SerializeField] private string weightTextFormat;
        [SerializeField] private string emptySlotText;
        [SerializeField] private Font font;
        [SerializeField, Min(1)] private int titleFontSize;
        [SerializeField, Min(1)] private int bodyFontSize;
        [SerializeField, Min(1)] private int slotFontSize;

        [Header("Layout")]
        [SerializeField] private Vector2 referenceResolution;
        [SerializeField] private Vector2 panelSize;
        [SerializeField] private Vector2 panelOffset;
        [SerializeField, Min(1)] private int columns;
        [SerializeField] private Vector2 slotSize;
        [SerializeField] private Vector2 slotSpacing;
        [SerializeField] private Vector2 iconAnchorMin;
        [SerializeField] private Vector2 iconAnchorMax;
        [SerializeField] private Vector2 labelAnchorMin;
        [SerializeField] private Vector2 labelAnchorMax;
        [SerializeField] private bool preserveIconAspect;
        [SerializeField, Min(0)] private int panelPadding;
        [SerializeField, Min(0f)] private float headerHeight;
        [SerializeField, Min(0)] private int maxVisibleSlots;
        [SerializeField] private int sortingOrder;

        [Header("Colori")]
        [SerializeField] private Color panelColor;
        [SerializeField] private Color slotColor;
        [SerializeField] private Color selectedSlotColor;
        [SerializeField] private Color textColor;
        [SerializeField] private Color overloadedWeightColor;

        public string PanelTitle => panelTitle;
        public string WeightTextFormat => weightTextFormat;
        public string EmptySlotText => emptySlotText;
        public Font Font => font;
        public int TitleFontSize => titleFontSize;
        public int BodyFontSize => bodyFontSize;
        public int SlotFontSize => slotFontSize;
        public Vector2 ReferenceResolution => referenceResolution;
        public Vector2 PanelSize => panelSize;
        public Vector2 PanelOffset => panelOffset;
        public int Columns => columns;
        public Vector2 SlotSize => slotSize;
        public Vector2 SlotSpacing => slotSpacing;
        public Vector2 IconAnchorMin => iconAnchorMin;
        public Vector2 IconAnchorMax => iconAnchorMax;
        public Vector2 LabelAnchorMin => labelAnchorMin;
        public Vector2 LabelAnchorMax => labelAnchorMax;
        public bool PreserveIconAspect => preserveIconAspect;
        public int PanelPadding => panelPadding;
        public float HeaderHeight => headerHeight;
        public int MaxVisibleSlots => maxVisibleSlots;
        public int SortingOrder => sortingOrder;
        public Color PanelColor => panelColor;
        public Color SlotColor => slotColor;
        public Color SelectedSlotColor => selectedSlotColor;
        public Color TextColor => textColor;
        public Color OverloadedWeightColor => overloadedWeightColor;

        private void OnValidate()
        {
            titleFontSize = Mathf.Max(1, titleFontSize);
            bodyFontSize = Mathf.Max(1, bodyFontSize);
            slotFontSize = Mathf.Max(1, slotFontSize);
            referenceResolution.x = Mathf.Max(1f, referenceResolution.x);
            referenceResolution.y = Mathf.Max(1f, referenceResolution.y);
            panelSize.x = Mathf.Max(1f, panelSize.x);
            panelSize.y = Mathf.Max(1f, panelSize.y);
            columns = Mathf.Max(1, columns);
            slotSize.x = Mathf.Max(1f, slotSize.x);
            slotSize.y = Mathf.Max(1f, slotSize.y);
            slotSpacing.x = Mathf.Max(0f, slotSpacing.x);
            slotSpacing.y = Mathf.Max(0f, slotSpacing.y);
            iconAnchorMin = new Vector2(Mathf.Clamp01(iconAnchorMin.x), Mathf.Clamp01(iconAnchorMin.y));
            iconAnchorMax = new Vector2(Mathf.Clamp(iconAnchorMax.x, iconAnchorMin.x, 1f),
                Mathf.Clamp(iconAnchorMax.y, iconAnchorMin.y, 1f));
            labelAnchorMin = new Vector2(Mathf.Clamp01(labelAnchorMin.x), Mathf.Clamp01(labelAnchorMin.y));
            labelAnchorMax = new Vector2(Mathf.Clamp(labelAnchorMax.x, labelAnchorMin.x, 1f),
                Mathf.Clamp(labelAnchorMax.y, labelAnchorMin.y, 1f));
            panelPadding = Mathf.Max(0, panelPadding);
            headerHeight = Mathf.Max(0f, headerHeight);
            maxVisibleSlots = Mathf.Max(0, maxVisibleSlots);
        }
    }
}