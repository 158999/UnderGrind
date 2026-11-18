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
        [SerializeField] private Camera uiCamera;

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
        private bool _inventoryEventsBound;

        public bool IsOpen { get; private set; }
        public InventoryUISettings Settings => settings;

        public void Configure(InventoryUISettings uiSettings, PlayerInputSettings playerInput,
            PlayerInventory playerInventory, HumanLocomotionController playerLocomotion)
        {
            UnbindInventoryEvents();
            settings = uiSettings;
            inputSettings = playerInput;
            inventory = playerInventory;
            locomotion = playerLocomotion;
            ResolveActions();
            if (isActiveAndEnabled) BindInventoryEvents();
            if (isActiveAndEnabled) InitializeUI();
        }

        private void Awake()
        {
            if (inventory == null) inventory = GetComponent<PlayerInventory>();
            if (locomotion == null) locomotion = GetComponent<HumanLocomotionController>();
            if (uiCamera == null) uiCamera = GetComponentInChildren<Camera>(true);
            ResolveActions();
            InitializeUI();
        }

        private void OnEnable()
        {
            BindInventoryEvents();
        }

        private void OnDisable()
        {
            UnbindInventoryEvents();
            if (!IsOpen) return;

            IsOpen = false;
            if (_panel != null) _panel.SetActive(false);
            if (locomotion != null)
            {
                locomotion.SetInputSuppressed(false);
                if (locomotion.isActiveAndEnabled)
                    locomotion.SetCursorCaptured(_cursorWasCaptured);
                else
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        private void BindInventoryEvents()
        {
            if (_inventoryEventsBound || inventory == null) return;
            inventory.Changed += Refresh;
            _inventoryEventsBound = true;
        }

        private void UnbindInventoryEvents()
        {
            if (!_inventoryEventsBound) return;
            if (inventory != null) inventory.Changed -= Refresh;
            _inventoryEventsBound = false;
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
                if (locomotion != null)
                {
                    locomotion.SetInputSuppressed(true);
                    locomotion.SetCursorCaptured(false);
                }
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
            else if (locomotion != null)
            {
                locomotion.SetInputSuppressed(false);
                if (_cursorWasCaptured) locomotion.SetCursorCaptured(true);
            }

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
            _canvas.renderMode = settings.CanvasRenderMode;
            _canvas.worldCamera = uiCamera;
            _canvas.sortingOrder = settings.SortingOrder;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = settings.CanvasScaleMode;
            scaler.referenceResolution = settings.ReferenceResolution;

            _panel = CreateUiObject("Inventory Panel", canvasObject.transform);
            var panelRect = _panel.GetComponent<RectTransform>();
            panelRect.anchorMin = settings.PanelAnchorMin;
            panelRect.anchorMax = settings.PanelAnchorMax;
            panelRect.pivot = settings.PanelPivot;
            panelRect.anchoredPosition = settings.PanelOffset;
            panelRect.sizeDelta = settings.PanelSize;
            _panel.AddComponent<Image>().color = settings.PanelColor;

            _title = CreateText("Title", panelRect, settings.PanelTitle, settings.TitleFontSize,
                settings.TitleAlignment);
            var titleRect = _title.rectTransform;
            titleRect.anchorMin = settings.TitleAnchorMin;
            titleRect.anchorMax = settings.TitleAnchorMax;
            titleRect.pivot = settings.TitlePivot;
            titleRect.anchoredPosition = new Vector2(0f, -settings.PanelPadding);
            titleRect.sizeDelta = new Vector2(-settings.PanelPadding * 2f, settings.HeaderHeight);

            _weight = CreateText("Weight", panelRect, string.Empty, settings.BodyFontSize,
                settings.WeightAlignment);
            var weightRect = _weight.rectTransform;
            weightRect.anchorMin = settings.WeightAnchorMin;
            weightRect.anchorMax = settings.WeightAnchorMax;
            weightRect.pivot = settings.WeightPivot;
            weightRect.anchoredPosition = new Vector2(0f, -settings.PanelPadding);
            weightRect.sizeDelta = new Vector2(-settings.PanelPadding * 2f, settings.HeaderHeight);

            var gridObject = CreateUiObject("Slots", panelRect);
            var gridRect = gridObject.GetComponent<RectTransform>();
            gridRect.anchorMin = settings.GridAnchorMin;
            gridRect.anchorMax = settings.GridAnchorMax;
            gridRect.offsetMin = new Vector2(settings.PanelPadding, settings.PanelPadding);
            gridRect.offsetMax = new Vector2(-settings.PanelPadding,
                -settings.HeaderHeight - settings.PanelPadding);
            _grid = gridObject.AddComponent<GridLayoutGroup>();
            _grid.cellSize = settings.SlotSize;
            _grid.spacing = settings.SlotSpacing;
            _grid.constraint = settings.GridConstraint;
            _grid.constraintCount = settings.Columns;
            _grid.startCorner = settings.GridStartCorner;
            _grid.startAxis = settings.GridStartAxis;
            _grid.childAlignment = settings.GridChildAlignment;
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
                iconRect.anchorMin = settings.IconAnchorMin;
                iconRect.anchorMax = settings.IconAnchorMax;
                iconRect.offsetMin = Vector2.zero;
                iconRect.offsetMax = Vector2.zero;
                var icon = iconObject.AddComponent<Image>();
                icon.preserveAspect = settings.PreserveIconAspect;

                var label = CreateText("Label", slot.transform, string.Empty, settings.SlotFontSize,
                    settings.SlotAlignment, font);
                var labelRect = label.rectTransform;
                labelRect.anchorMin = settings.LabelAnchorMin;
                labelRect.anchorMax = settings.LabelAnchorMax;
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
            var count = VisibleSlotCount();
            if (count <= 0) return;
            var next = (inventory.SelectedSlotIndex + direction + count) % count;
            inventory.SelectSlot(next);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (inventory == null) inventory = GetComponent<PlayerInventory>();
            if (locomotion == null) locomotion = GetComponent<HumanLocomotionController>();
            if (uiCamera == null) uiCamera = GetComponentInChildren<Camera>(true);
        }
#endif
    }
}