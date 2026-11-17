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
        private int _styleFontSize = -1;

        private void Awake() => _movement = GetComponent<HumanLocomotionController>();

        private void OnGUI()
        {
            if (_movement == null)
                return;

            var safe = Screen.safeArea;
            var scale = Mathf.Clamp(Mathf.Min(Screen.width / 1280f, Screen.height / 720f), 0.68f, 1.15f);
            EnsureStyles(scale);

            var labels = new[]
            {
                $"Velocità attuale: {_movement.MeasuredSpeed:0.00} m/s",
                $"Camminata: {_movement.WalkSpeed:0.00} m/s",
                $"Corsa: {_movement.RunSpeed:0.00} m/s  (× {_movement.RunMultiplier:0.00})",
                $"Salto: {_movement.JumpHeight:0.00} m  |  v₀ {_movement.JumpVelocity:0.00} m/s",
                $"Altezza: {_movement.CharacterHeight:0.00} m  |  Gravità: {_movement.Gravity:0.00} m/s²",
                $"Stato: {(_movement.IsGrounded ? "a terra" : "in aria")}  |  {(_movement.IsRunning ? "corsa" : "cammino")}",
                $"Visuale: {(_movement.IsThirdPerson ? "terza persona" : "prima persona")}"
            };

            var width = Mathf.Min(390f, Mathf.Max(250f, safe.width - 24f));
            var contentWidth = Mathf.Max(1f, width - 24f);
            const string title = "UNDERGRIND  |  LOCOMOTION TEST";
            var headingHeight = _heading.CalcHeight(new GUIContent(title), contentWidth);
            var contentHeight = headingHeight + 6f;
            foreach (var label in labels)
                contentHeight += _style.CalcHeight(new GUIContent(label), contentWidth) + 2f;

            var panelHeight = contentHeight + 22f;
            var panel = new Rect(safe.xMin + 12f, safe.yMax - panelHeight - 12f, width, panelHeight);
            GUI.Box(panel, GUIContent.none, GUI.skin.box);

            var y = panel.y + 9f;
            GUI.Label(new Rect(panel.x + 12f, y, contentWidth, headingHeight), title, _heading);
            y += headingHeight + 6f;

            foreach (var label in labels)
            {
                var height = _style.CalcHeight(new GUIContent(label), contentWidth);
                GUI.Label(new Rect(panel.x + 12f, y, contentWidth, height), label, _style);
                y += height + 2f;
            }

            const string controls = "WASD / frecce: muovi   SHIFT: corri   SPAZIO: salta   F5: visuale   ESC: mouse";
            var hintWidth = Mathf.Max(1f, safe.width - 24f);
            var hintHeight = _hint.CalcHeight(new GUIContent(controls), hintWidth);
            GUI.Label(new Rect(safe.xMin + 12f, safe.yMin + 10f, hintWidth, hintHeight), controls, _hint);
        }

        private void EnsureStyles(float scale)
        {
            var fontSize = Mathf.Max(10, Mathf.RoundToInt(14f * scale));
            if (_style != null && _styleFontSize == fontSize)
                return;

            _styleFontSize = fontSize;
            _style = new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                wordWrap = true,
                clipping = TextClipping.Clip,
                normal = { textColor = Color.white }
            };
            _heading = new GUIStyle(_style)
            {
                fontStyle = FontStyle.Bold,
                fontSize = fontSize + 1
            };
            _hint = new GUIStyle(_style)
            {
                fontSize = Mathf.Max(9, fontSize - 2),
                wordWrap = true
            };
        }
    }
}