using UnityEditor;
using UnityEngine;
using UnderGrind.Locomotion;

namespace UnderGrind.Editor
{
    [CustomEditor(typeof(HumanLocomotionController))]
    [CanEditMultipleObjects]
    public sealed class HumanLocomotionControllerEditor : UnityEditor.Editor
    {
        private SerializedProperty _settings;
        private SerializedProperty _inventory;
        private SerializedProperty _viewPivot;
        private SerializedProperty _playerCamera;

        private void OnEnable()
        {
            _settings = serializedObject.FindProperty("settings");
            _inventory = serializedObject.FindProperty("inventory");
            _viewPivot = serializedObject.FindProperty("viewPivot");
            _playerCamera = serializedObject.FindProperty("playerCamera");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Configurazione della locomozione", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_settings);
            var settings = _settings.objectReferenceValue as PlayerLocomotionSettings;
            if (settings == null)
            {
                EditorGUILayout.HelpBox(
                    "Assegna un asset PlayerLocomotionSettings. Senza una configurazione il controller non applica il movimento.",
                    MessageType.Error);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    $"Camminata: {settings.WalkSpeed:0.00} m/s    Corsa: {settings.RunSpeed:0.00} m/s    " +
                    $"Salto: {settings.JumpHeight:0.00} m\n" +
                    $"Velocità iniziale del salto: {settings.JumpVelocity:0.00} m/s    " +
                    $"Altezza personaggio: {settings.CharacterHeight:0.00} m",
                    MessageType.Info);
                if (GUILayout.Button("Seleziona asset di configurazione"))
                {
                    Selection.activeObject = settings;
                    EditorGUIUtility.PingObject(settings);
                }
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Riferimenti runtime", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_inventory);
            EditorGUILayout.PropertyField(_viewPivot);
            EditorGUILayout.PropertyField(_playerCamera);

            serializedObject.ApplyModifiedProperties();
        }
    }
}