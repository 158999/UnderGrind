using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnderGrind.Inventory;
using UnderGrind.Locomotion;

namespace UnderGrind.Tests
{
    public sealed class PlayerSettingsDefaultsTests
    {
        private ScriptableObject _settings;
        private InputActionAsset _inputAsset;

        [TearDown]
        public void TearDown()
        {
            if (_settings != null) Object.DestroyImmediate(_settings);
            if (_inputAsset != null) Object.DestroyImmediate(_inputAsset);
        }

        [Test]
        public void InteractionDefaults_LoadValidLayerMaskAndDropConfiguration()
        {
            var settings = Load<PlayerInteractionSettings>("PlayerInteractionSettings.json");
            Assert.AreEqual(2.5f, settings.InteractionDistance);
            Assert.AreEqual(0.08f, settings.PickupRayRadius);
            Assert.AreEqual(-1, settings.InteractionLayers.value);
            Assert.AreEqual(1, settings.DropQuantityPerAction);
        }

        [Test]
        public void InventoryUIDefaults_LoadFixedColumnLayoutAndVisibleSlots()
        {
            var settings = Load<InventoryUISettings>("InventoryUISettings.json");
            Assert.AreEqual(RenderMode.ScreenSpaceOverlay, settings.CanvasRenderMode);
            Assert.AreEqual(CanvasScaler.ScaleMode.ScaleWithScreenSize, settings.CanvasScaleMode);
            Assert.AreEqual(GridLayoutGroup.Constraint.FixedColumnCount, settings.GridConstraint);
            Assert.AreEqual(6, settings.Columns);
            Assert.AreEqual("LegacyRuntime.ttf", settings.FallbackFontResourceName);
            Assert.AreEqual(0, settings.MaxVisibleSlots);
        }

        [Test]
        public void AnimationDefaults_LoadLoopFlagsAndAnimatorParameters()
        {
            var settings = Load<PlayerAnimationSettings>("PlayerAnimationSettings.json");
            Assert.IsTrue(settings.IdleLoops);
            Assert.IsTrue(settings.WalkLoops);
            Assert.IsTrue(settings.RunLoops);
            Assert.IsFalse(settings.JumpLoops);
            Assert.AreEqual("Speed", settings.SpeedParameterName);
            Assert.AreEqual(2f, settings.RunThreshold);
            Assert.IsTrue(settings.ConfigureFbxImports);
            Assert.AreEqual(PlayerAnimationRigType.Humanoid, settings.FbxRigType);
            Assert.IsTrue(settings.CreateAvatarFromFbx);
            Assert.IsTrue(settings.ImportAnimationClips);
        }

        [Test]
        public void RigDefaults_LoadConfiguredArmatureRootsAndHeadRequirement()
        {
            var settings = Load<PlayerRigSettings>("PlayerRigSettings.json");
            Assert.AreEqual("UG_Male_Armature", settings.MaleArmatureRootName);
            Assert.AreEqual("UG_Female_Armature", settings.FemaleArmatureRootName);
            Assert.AreEqual("Assets/UnderGrind/Art/Source/Rigged/Human_Male_Rigged.fbx", settings.MaleSourceAssetPath);
            Assert.AreEqual("Assets/UnderGrind/Art/Source/Rigged/Human_Female_Rigged.fbx", settings.FemaleSourceAssetPath);
            Assert.AreEqual("_TP_Arms", settings.ThirdPersonArmsToken);
            Assert.AreEqual("_TP_Hands", settings.ThirdPersonHandsToken);
            Assert.IsTrue(settings.RequireSeparateHead);
            Assert.IsFalse(settings.RequireFirstPersonArms);
        }

        [Test]
        public void InputActionsTemplate_LoadsEveryPlayerAction()
        {
            var path = Path.Combine(Application.dataPath, "UnderGrind", "Settings", "PlayerInputActions.inputactions");
            Assert.IsTrue(File.Exists(path), "Missing Player Input Actions template: " + path);
            _inputAsset = InputActionAsset.FromJson(File.ReadAllText(path));
            var map = _inputAsset.FindActionMap("Player", false);
            Assert.IsNotNull(map);
            foreach (var actionName in new[]
                     { "Move", "LookMouse", "LookGamepad", "Jump", "Run", "Crouch",
                       "ToggleCamera", "Cancel", "CaptureCursor", "Interact", "Drop",
                       "ToggleInventory", "NextSlot", "PreviousSlot" })
                Assert.IsNotNull(map.FindAction(actionName, false), "Missing input action: " + actionName);
            Assert.Greater(map.FindAction("Move", false).bindings.Count, 0);
        }
        [Test]
        public void InputDefaults_LoadDeadZonesAndRebindingSettings()
        {
            var inputPath = Path.Combine(Application.dataPath, "UnderGrind", "Settings", "PlayerInputActions.inputactions");
            Assert.IsTrue(File.Exists(inputPath), "Missing Player Input Actions asset: " + inputPath);
            _inputAsset = InputActionAsset.FromJson(File.ReadAllText(inputPath));
            var settings = ScriptableObject.CreateInstance<PlayerInputSettings>();
            SetField(settings, "actionAsset", _inputAsset);
            _settings = settings;
            ApplyDefaults(settings, "PlayerInputSettings.json");

            Assert.AreEqual("Player", settings.ActionMapName);
            Assert.AreEqual(0.15f, settings.MovementDeadzone);
            Assert.AreEqual(0.12f, settings.GamepadLookDeadzone);
            Assert.AreEqual(8f, settings.RebindTimeoutSeconds);
            Assert.AreEqual("UnderGrind.Player.InputOverrides", settings.OverrideStorageKey);
        }

        private T Load<T>(string fileName) where T : ScriptableObject
        {
            var settings = ScriptableObject.CreateInstance<T>();
            _settings = settings;
            ApplyDefaults(settings, fileName);
            return settings;
        }

        private static void ApplyDefaults(ScriptableObject target, string fileName)
        {
            var path = Path.Combine(Application.dataPath, "UnderGrind", "Settings", "Defaults", fileName);
            Assert.IsTrue(File.Exists(path), "Missing settings defaults file: " + path);
            JsonUtility.FromJsonOverwrite(File.ReadAllText(path), target);
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, "Missing serialized field: " + fieldName);
            field.SetValue(target, value);
        }
    }
}