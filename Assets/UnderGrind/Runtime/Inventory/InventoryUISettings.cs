using UnityEngine;
using UnityEngine.UI;

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
        [SerializeField] private RenderMode canvasRenderMode;
        [SerializeField] private CanvasScaler.ScaleMode canvasScaleMode;
        [SerializeField] private Vector2 referenceResolution;
        [SerializeField] private Vector2 panelAnchorMin;
        [SerializeField] private Vector2 panelAnchorMax;
        [SerializeField] private Vector2 panelPivot;
        [SerializeField] private Vector2 titleAnchorMin;
        [SerializeField] private Vector2 titleAnchorMax;
        [SerializeField] private Vector2 titlePivot;
        [SerializeField] private Vector2 weightAnchorMin;
        [SerializeField] private Vector2 weightAnchorMax;
        [SerializeField] private Vector2 weightPivot;
        [SerializeField] private Vector2 gridAnchorMin;
        [SerializeField] private Vector2 gridAnchorMax;
        [SerializeField] private GridLayoutGroup.Corner gridStartCorner;
        [SerializeField] private GridLayoutGroup.Axis gridStartAxis;
        [SerializeField] private GridLayoutGroup.Constraint gridConstraint;
        [SerializeField] private TextAnchor titleAlignment;
        [SerializeField] private TextAnchor weightAlignment;
        [SerializeField] private TextAnchor slotAlignment;
        [SerializeField] private TextAnchor gridChildAlignment;
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
        public RenderMode CanvasRenderMode => canvasRenderMode;
        public CanvasScaler.ScaleMode CanvasScaleMode => canvasScaleMode;
        public Vector2 ReferenceResolution => referenceResolution;
        public Vector2 PanelAnchorMin => panelAnchorMin;
        public Vector2 PanelAnchorMax => panelAnchorMax;
        public Vector2 PanelPivot => panelPivot;
        public Vector2 TitleAnchorMin => titleAnchorMin;
        public Vector2 TitleAnchorMax => titleAnchorMax;
        public Vector2 TitlePivot => titlePivot;
        public Vector2 WeightAnchorMin => weightAnchorMin;
        public Vector2 WeightAnchorMax => weightAnchorMax;
        public Vector2 WeightPivot => weightPivot;
        public Vector2 GridAnchorMin => gridAnchorMin;
        public Vector2 GridAnchorMax => gridAnchorMax;
        public GridLayoutGroup.Corner GridStartCorner => gridStartCorner;
        public GridLayoutGroup.Axis GridStartAxis => gridStartAxis;
        public GridLayoutGroup.Constraint GridConstraint => gridConstraint;
        public TextAnchor TitleAlignment => titleAlignment;
        public TextAnchor WeightAlignment => weightAlignment;
        public TextAnchor SlotAlignment => slotAlignment;
        public TextAnchor GridChildAlignment => gridChildAlignment;
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

        private static Vector2 ClampAnchor(Vector2 value)
        {
            return new Vector2(Mathf.Clamp01(value.x), Mathf.Clamp01(value.y));
        }

        private void OnValidate()
        {
            titleFontSize = Mathf.Max(1, titleFontSize);
            bodyFontSize = Mathf.Max(1, bodyFontSize);
            slotFontSize = Mathf.Max(1, slotFontSize);
            panelAnchorMin = ClampAnchor(panelAnchorMin);
            panelAnchorMax = ClampAnchor(panelAnchorMax);
            panelPivot = ClampAnchor(panelPivot);
            titleAnchorMin = ClampAnchor(titleAnchorMin);
            titleAnchorMax = ClampAnchor(titleAnchorMax);
            titlePivot = ClampAnchor(titlePivot);
            weightAnchorMin = ClampAnchor(weightAnchorMin);
            weightAnchorMax = ClampAnchor(weightAnchorMax);
            weightPivot = ClampAnchor(weightPivot);
            gridAnchorMin = ClampAnchor(gridAnchorMin);
            gridAnchorMax = ClampAnchor(gridAnchorMax);
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