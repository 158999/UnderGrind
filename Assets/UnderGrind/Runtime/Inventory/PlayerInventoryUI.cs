using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnderGrind.Locomotion;

namespace UnderGrind.Inventory
{
    [DisallowMultipleComponent]
    public sealed class PlayerInventoryUI : MonoBehaviour
    {
        [SerializeField] private InventoryUISettings settings;
        [SerializeField] private PlayerInputSettings inputSettings;
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private HumanLocomotionController locomotion;

        private readonly List<Image> _slotBackgrounds = new List<Image>();
        private readonly List<Image> _slotIcons = new List<Image>();
        private readonly List<Text> _slotLabels = new List<Text>();
        private InputAction _toggleAction;
        private InputAction _nextSlotAction;
        private InputAction _previousSlotAction;
        private Canvas _canvas;
        private GameObject _panel;
        private Text _title;
        private Text _weight;
        private GridLayoutGroup _grid;
        private int _builtSlotCount = -1;
        private bool _cursorWasCaptured;

        public bool IsOpen { get; private set; }
        public InventoryUISettings Settings => settings;

        public void Configure(InventoryUISettings uiSettings, PlayerInputSettings playerInput,
            PlayerInventory playerInventory, HumanLocomotionController playerLocomotion)
        {
            settings = uiSettings;
            inputSettings = playerInput;
            inventory = playerInventory;
            locomotion = playerLocomotion;
            ResolveActions();
            if (isActiveAndEnabled) InitializeUI();
        }

        private void Awake()
        {
            if (inventory == null) inventory = GetComponent<PlayerInventory>();
            if (locomotion == null) locomotion = GetComponent<HumanLocomotionController>();
            ResolveActions();
            InitializeUI();
        }

        private void OnEnable()
        {
            if (inventory != null) inventory.Changed += Refresh;
        }

        private void OnDisable()
        {
            if (inventory != null) inventory.Changed -= Refresh;
            if (IsOpen) SetOpen(false);
        }

        private void Update()
        {
            if (_toggleAction != null && _toggleAction.WasPressedThisFrame())
                SetOpen(!IsOpen);

            if (!IsOpen || inventory == null || inventory.SlotCapacity == 0) return;
            if (_nextSlotAction != null && _nextSlotAction.WasPressedThisFrame())
                SelectRelativeSlot(1);
            if (_previousSlotAction != null && _previousSlotAction.WasPressedThisFrame())
                SelectRelativeSlot(-1);
        }

        public void SetOpen(bool open)
        {
            if (IsOpen == open) return;
            if (open)
            {
                _cursorWasCaptured = locomotion != null && locomotion.IsCursorCaptured;
                if (locomotion != null) locomotion.SetCursorCaptured(false);
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
            else if (_cursorWasCaptured && locomotion != null)
                locomotion.SetCursorCaptured(true);

            IsOpen = open;
            if (_panel != null) _panel.SetActive(open);
            if (open) Refresh();
        }

        public void Refresh()
        {
            if (settings == null || inventory == null) return;
            var count = VisibleSlotCount();
            if (count != _builtSlotCount) BuildSlots(count);

            if (_weight != null)
            {
                _weight.text = string.Format(settings.WeightTextFormat,
                    inventory.CurrentWeightKg, inventory.MaxWeightKg);
                _weight.color = inventory.IsOverloaded
                    ? settings.OverloadedWeightColor : settings.TextColor;
            }

            for (var i = 0; i < _slotBackgrounds.Count; i++)
            {
                var slot = i < inventory.SlotCapacity ? inventory.GetSlot(i) : null;
                var empty = slot == null || slot.IsEmpty;
                _slotBackgrounds[i].color = i == inventory.SelectedSlotIndex
                    ? settings.SelectedSlotColor : settings.SlotColor;
                _slotIcons[i].sprite = empty ? null : slot.Item.Icon;
                _slotIcons[i].enabled = !empty && slot.Item.Icon != null;
                _slotLabels[i].text = empty
                    ? settings.EmptySlotText
                    : slot.Item.DisplayName + "\n×" + slot.Quantity;
            }
        }

        private void InitializeUI()
        {
            if (settings == null || inventory == null) return;
            ResolveActions();
            BuildCanvas();
            BuildSlots(VisibleSlotCount());
            if (_panel != null) _panel.SetActive(IsOpen);
            Refresh();
        }

        private void ResolveActions()
        {
            _toggleAction = inputSettings != null
                ? inputSettings.GetAction(PlayerInputActionId.ToggleInventory) : null;
            _nextSlotAction = inputSettings != null
                ? inputSettings.GetAction(PlayerInputActionId.NextSlot) : null;
            _previousSlotAction = inputSettings != null
                ? inputSettings.GetAction(PlayerInputActionId.PreviousSlot) : null;
        }

        private int VisibleSlotCount()
        {
            if (inventory == null) return 0;
            return settings != null && settings.MaxVisibleSlots > 0
                ? Mathf.Min(inventory.SlotCapacity, settings.MaxVisibleSlots)
                : inventory.SlotCapacity;
        }

        private void BuildCanvas()
        {
            if (_canvas != null) return;
            var canvasObject = new GameObject("Player Inventory Canvas",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            _canvas = canvasObject.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = settings.SortingOrder;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = settings.ReferenceResolution;

            _panel = CreateUiObject("Inventory Panel", canvasObject.transform);
            var panelRect = _panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = settings.PanelOffset;
            panelRect.sizeDelta = settings.PanelSize;
            _panel.AddComponent<Image>().color = settings.PanelColor;

            _title = CreateText("Title", panelRect, settings.PanelTitle, settings.TitleFontSize,
                TextAnchor.MiddleLeft);
            var titleRect = _title.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -settings.PanelPadding);
            titleRect.sizeDelta = new Vector2(-settings.PanelPadding * 2f, settings.HeaderHeight);

            _weight = CreateText("Weight", panelRect, string.Empty, settings.BodyFontSize,
                TextAnchor.MiddleRight);
            var weightRect = _weight.rectTransform;
            weightRect.anchorMin = new Vector2(0f, 1f);
            weightRect.anchorMax = new Vector2(1f, 1f);
            weightRect.pivot = new Vector2(0.5f, 1f);
            weightRect.anchoredPosition = new Vector2(0f, -settings.PanelPadding);
            weightRect.sizeDelta = new Vector2(-settings.PanelPadding * 2f, settings.HeaderHeight);

            var gridObject = CreateUiObject("Slots", panelRect);
            var gridRect = gridObject.GetComponent<RectTransform>();
            gridRect.anchorMin = Vector2.zero;
            gridRect.anchorMax = Vector2.one;
            gridRect.offsetMin = new Vector2(settings.PanelPadding, settings.PanelPadding);
            gridRect.offsetMax = new Vector2(-settings.PanelPadding,
                -settings.HeaderHeight - settings.PanelPadding);
            _grid = gridObject.AddComponent<GridLayoutGroup>();
            _grid.cellSize = settings.SlotSize;
            _grid.spacing = settings.SlotSpacing;
            _grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            _grid.constraintCount = settings.Columns;
            _grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            _grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            _grid.childAlignment = TextAnchor.UpperLeft;
        }

        private void BuildSlots(int count)
        {
            if (_grid == null) return;
            foreach (Transform child in _grid.transform)
                Destroy(child.gameObject);
            _slotBackgrounds.Clear();
            _slotIcons.Clear();
            _slotLabels.Clear();

            var font = settings.Font != null
                ? settings.Font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            for (var i = 0; i < count; i++)
            {
                var slot = CreateUiObject("Slot " + (i + 1), _grid.transform);
                var background = slot.AddComponent<Image>();
                background.color = settings.SlotColor;
                var iconObject = CreateUiObject("Icon", slot.transform);
                var iconRect = iconObject.GetComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0.12f, 0.28f);
                iconRect.anchorMax = new Vector2(0.88f, 0.92f);
                iconRect.offsetMin = Vector2.zero;
                iconRect.offsetMax = Vector2.zero;
                var icon = iconObject.AddComponent<Image>();
                icon.preserveAspect = true;

                var label = CreateText("Label", slot.transform, string.Empty, settings.SlotFontSize,
                    TextAnchor.LowerCenter, font);
                var labelRect = label.rectTransform;
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = new Vector2(1f, 0.34f);
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;

                _slotBackgrounds.Add(background);
                _slotIcons.Add(icon);
                _slotLabels.Add(label);
            }
            _builtSlotCount = count;
        }

        private Text CreateText(string objectName, Transform parent, string value, int fontSize,
            TextAnchor alignment, Font assignedFont = null)
        {
            var textObject = CreateUiObject(objectName, parent);
            var text = textObject.AddComponent<Text>();
            text.font = assignedFont != null ? assignedFont :
                settings.Font != null ? settings.Font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = settings.TextColor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static GameObject CreateUiObject(string objectName, Transform parent)
        {
            var item = new GameObject(objectName, typeof(RectTransform));
            item.transform.SetParent(parent, false);
            return item;
        }

        private void SelectRelativeSlot(int direction)
        {
            var count = inventory.SlotCapacity;
            if (count <= 0) return;
            var next = (inventory.SelectedSlotIndex + direction + count) % count;
            inventory.SelectSlot(next);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (inventory == null) inventory = GetComponent<PlayerInventory>();
            if (locomotion == null) locomotion = GetComponent<HumanLocomotionController>();
        }
#endif
    }
}