using UnityEngine;

namespace UnderGrind.Locomotion
{
    [RequireComponent(typeof(HumanLocomotionController))]
    public sealed class LocomotionDebugOverlay : MonoBehaviour
    {
        private HumanLocomotionController _movement;
        private GUIStyle _style;
        private GUIStyle _heading;

        private void Awake() => _movement = GetComponent<HumanLocomotionController>();

        private void OnGUI()
        {
            if (_movement == null) return;
            _style ??= new GUIStyle(GUI.skin.label) { fontSize = 14, normal = { textColor = Color.white } };
            _heading ??= new GUIStyle(_style) { fontStyle = FontStyle.Bold, fontSize = 16 };

            GUI.Box(new Rect(14, 14, 310, 174), GUIContent.none);
            GUILayout.BeginArea(new Rect(26, 22, 286, 160));
            GUILayout.Label("UNDERGRIND  |  LOCOMOTION TEST", _heading);
            GUILayout.Label($"Velocità: {_movement.MeasuredSpeed:0.00} m/s", _style);
            GUILayout.Label($"Camminata: {_movement.WalkSpeed:0.00} m/s", _style);
            GUILayout.Label($"Corsa: {_movement.RunSpeed:0.00} m/s", _style);
            GUILayout.Label($"Salto: {_movement.JumpHeight:0.00} m", _style);
            GUILayout.Label($"Stato: {(_movement.IsGrounded ? "a terra" : "in aria")}  |  {(_movement.IsRunning ? "corsa" : "cammino")}", _style);
            GUILayout.Label($"Visuale: {(_movement.IsThirdPerson ? "terza persona" : "prima persona")}", _style);
            GUILayout.EndArea();
            GUI.Label(new Rect(14, Screen.height - 34, 700, 24),
                "WASD / frecce: muovi   SHIFT: corri   SPAZIO: salta   F5: cambia visuale   ESC: libera mouse", _style);
        }
    }
}