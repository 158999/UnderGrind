using UnityEditor;
using UnityEngine;
using UnderGrind.Locomotion;

namespace UnderGrind.Editor
{
    [CustomEditor(typeof(HumanLocomotionController))]
    [CanEditMultipleObjects]
    public sealed class HumanLocomotionControllerEditor : UnityEditor.Editor
    {
        private SerializedProperty _characterHeight;
        private SerializedProperty _walkSpeed;
        private SerializedProperty _runMultiplier;
        private SerializedProperty _jumpHeight;
        private SerializedProperty _gravity;
        private SerializedProperty _airControl;
        private SerializedProperty _viewPivot;
        private SerializedProperty _playerCamera;
        private SerializedProperty _mouseSensitivity;
        private SerializedProperty _thirdPersonDistance;

        private void OnEnable()
        {
            _characterHeight = serializedObject.FindProperty("characterHeight");
            _walkSpeed = serializedObject.FindProperty("walkSpeed");
            _runMultiplier = serializedObject.FindProperty("runMultiplier");
            _jumpHeight = serializedObject.FindProperty("jumpHeight");
            _gravity = serializedObject.FindProperty("gravity");
            _airControl = serializedObject.FindProperty("airControl");
            _viewPivot = serializedObject.FindProperty("viewPivot");
            _playerCamera = serializedObject.FindProperty("playerCamera");
            _mouseSensitivity = serializedObject.FindProperty("mouseSensitivity");
            _thirdPersonDistance = serializedObject.FindProperty("thirdPersonDistance");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Parametri antropometrici e locomozione", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_characterHeight);
            EditorGUILayout.PropertyField(_walkSpeed);
            EditorGUILayout.PropertyField(_runMultiplier);
            EditorGUILayout.PropertyField(_jumpHeight);
            EditorGUILayout.PropertyField(_gravity);
            EditorGUILayout.PropertyField(_airControl);

            var walk = Mathf.Max(0.1f, _walkSpeed.floatValue);
            var run = walk * Mathf.Max(1f, _runMultiplier.floatValue);
            var gravity = Mathf.Max(0.1f, _gravity.floatValue);
            var jump = Mathf.Max(0.05f, _jumpHeight.floatValue);
            EditorGUILayout.HelpBox(
                $"Camminata: {walk:0.00} m/s    Corsa: {run:0.00} m/s    Salto: {jump:0.00} m\n" +
                $"Velocità iniziale del salto: {Mathf.Sqrt(2f * gravity * jump):0.00} m/s",
                MessageType.Info);

            if (GUILayout.Button("Ripristina parametri umani iniziali"))
            {
                _characterHeight.floatValue = HumanLocomotionController.DefaultCharacterHeight;
                _walkSpeed.floatValue = HumanLocomotionController.DefaultWalkSpeed;
                _runMultiplier.floatValue = HumanLocomotionController.DefaultRunMultiplier;
                _jumpHeight.floatValue = HumanLocomotionController.DefaultJumpHeight;
                _gravity.floatValue = 9.81f;
                _airControl.floatValue = 0.65f;
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Visuale", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_viewPivot);
            EditorGUILayout.PropertyField(_playerCamera);
            EditorGUILayout.PropertyField(_mouseSensitivity);
            EditorGUILayout.PropertyField(_thirdPersonDistance);

            serializedObject.ApplyModifiedProperties();
        }
    }
}