using UnityEngine;

namespace UnderGrind.Locomotion
{
    [RequireComponent(typeof(HumanLocomotionController))]
    public sealed class LocomotionDebugOverlay : MonoBehaviour
    {
        private HumanLocomotionController _movement;
        private GUIStyle _style;
        private GUIStyle _heading;
        private GUIStyle _hint;
        private Texture2D _panelTexture;

        private void Awake() => _movement = GetComponent<HumanLocomotionController>();

        private void OnGUI()
        {
            if (_movement == null)
                return;

            EnsureStyles();

            var safe = Screen.safeArea;
            var scale = Mathf.Clamp(Screen.width / 1280f, 0.78f, 1.15f);
            var width = Mathf.Min(370f, safe.width - 24f);
            var lineHeight = 23f * scale;
            var panelHeight = 222f * scale;
            var panel = new Rect(safe.xMin + 12f, safe.yMax - panelHeight - 12f, width, panelHeight);

            GUI.Box(panel, GUIContent.none, GUI.skin.box);
            GUILayout.BeginArea(new Rect(panel.x + 12f, panel.y + 9f, panel.width - 24f, panel.height - 18f));
            GUILayout.Label("UNDERGRIND  |  LOCOMOTION TEST", _heading, GUILayout.Height(lineHeight + 2f));
            GUILayout.Label($"Velocità attuale: {_movement.MeasuredSpeed:0.00} m/s", _style, GUILayout.Height(lineHeight));
            GUILayout.Label($"Camminata: {_movement.WalkSpeed:0.00} m/s", _style, GUILayout.Height(lineHeight));
            GUILayout.Label($"Corsa: {_movement.RunSpeed:0.00} m/s  (× {_movement.RunMultiplier:0.00})", _style, GUILayout.Height(lineHeight));
            GUILayout.Label($"Salto: {_movement.JumpHeight:0.00} m  |  v₀ {_movement.JumpVelocity:0.00} m/s", _style, GUILayout.Height(lineHeight));
            GUILayout.Label($"Altezza: {_movement.CharacterHeight:0.00} m  |  Gravità: {_movement.Gravity:0.00} m/s²", _style, GUILayout.Height(lineHeight));
            GUILayout.Label($"Stato: {(_movement.IsGrounded ? "a terra" : "in aria")}  |  {(_movement.IsRunning ? "corsa" : "cammino")}", _style, GUILayout.Height(lineHeight));
            GUILayout.Label($"Visuale: {(_movement.IsThirdPerson ? "terza persona" : "prima persona")}", _style, GUILayout.Height(lineHeight));
            GUILayout.EndArea();

            var hint = new Rect(safe.xMin + 12f, safe.yMin + 10f, safe.width - 24f, 24f * scale);
            GUI.Label(hint, "WASD / frecce: muovi   SHIFT: corri   SPAZIO: salta   F5: visuale   ESC: mouse", _hint);
        }

        private void EnsureStyles()
        {
            if (_style != null)
                return;

            _style = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(14f * Mathf.Clamp(Screen.width / 1280f, 0.78f, 1.15f)),
                wordWrap = false,
                clipping = TextClipping.Clip,
                normal = { textColor = Color.white }
            };
            _heading = new GUIStyle(_style)
            {
                fontStyle = FontStyle.Bold,
                fontSize = Mathf.RoundToInt(15f * Mathf.Clamp(Screen.width / 1280f, 0.78f, 1.15f))
            };
            _hint = new GUIStyle(_style)
            {
                fontSize = Mathf.RoundToInt(12f * Mathf.Clamp(Screen.width / 1280f, 0.78f, 1.15f)),
                wordWrap = true
            };
        }
    }
}