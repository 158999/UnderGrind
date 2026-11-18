using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using UnderGrind.Locomotion;

namespace UnderGrind.Tests
{
    public sealed class PlayerInputSettingsTests
    {
        private InputActionAsset _asset;
        private InputActionMap _map;
        private PlayerInputSettings _settings;

        [SetUp]
        public void SetUp()
        {
            _asset = ScriptableObject.CreateInstance<InputActionAsset>();
            _map = new InputActionMap("Player");
            _asset.AddActionMap(_map);
            _map.AddAction("Move", InputActionType.Value, expectedControlType: "Vector2");
            _map.AddAction("Jump", InputActionType.Button, expectedControlType: "Button");

            _settings = ScriptableObject.CreateInstance<PlayerInputSettings>();
            var serialized = new SerializedObject(_settings);
            serialized.FindProperty("actionAsset").objectReferenceValue = _asset;
            serialized.FindProperty("actionMapName").stringValue = "Player";
            serialized.FindProperty("moveActionName").stringValue = "Move";
            serialized.FindProperty("jumpActionName").stringValue = "Jump";
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            _map.Disable();
            Object.DestroyImmediate(_settings);
            Object.DestroyImmediate(_asset);
        }

        [Test]
        public void GetAction_ResolvesConfiguredActionByMapAndName()
        {
            Assert.AreSame(_map.FindAction("Move"), _settings.GetAction(PlayerInputActionId.Move));
            Assert.AreSame(_map.FindAction("Jump"), _settings.GetAction(PlayerInputActionId.Jump));
        }

        [Test]
        public void EnableAndDisableActionMap_ControlTheConfiguredMap()
        {
            Assert.IsTrue(_settings.EnableActionMap());
            Assert.IsTrue(_map.enabled);

            _settings.DisableActionMap();

            Assert.IsFalse(_map.enabled);
        }

        [Test]
        public void MissingActionMapReturnsNoActionWithoutThrowing()
        {
            var serialized = new SerializedObject(_settings);
            LogAssert.Expect(LogType.Error, "UnderGrind: action map non trovato: Missing");
            serialized.FindProperty("actionMapName").stringValue = "Missing";
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Assert.IsNull(_settings.GetAction(PlayerInputActionId.Move));
            Assert.IsFalse(_settings.EnableActionMap());
        }
    }
}