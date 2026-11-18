using System;
using UnityEngine;

namespace UnderGrind.Testing
{
    public enum GrayboxMaterialKind
    {
        Concrete,
        Accent,
        Marker
    }

    [Serializable]
    public sealed class GrayboxBlockDefinition
    {
        [SerializeField] private string objectName;
        [SerializeField] private Vector3 position;
        [SerializeField] private Vector3 size;
        [SerializeField] private GrayboxMaterialKind materialKind;
        [SerializeField] private bool colliderEnabled;

        public string ObjectName => objectName;
        public Vector3 Position => position;
        public Vector3 Size => size;
        public GrayboxMaterialKind MaterialKind => materialKind;
        public bool ColliderEnabled => colliderEnabled;
    }

    [Serializable]
    public sealed class GrayboxRampDefinition
    {
        [SerializeField] private string objectName;
        [SerializeField] private Vector3 position;
        [SerializeField] private Vector3 size;
        [SerializeField] private Vector3 rotationEuler;
        [SerializeField] private GrayboxMaterialKind materialKind;
        [SerializeField] private bool colliderEnabled;

        public string ObjectName => objectName;
        public Vector3 Position => position;
        public Vector3 Size => size;
        public Vector3 RotationEuler => rotationEuler;
        public GrayboxMaterialKind MaterialKind => materialKind;
        public bool ColliderEnabled => colliderEnabled;
    }

    [CreateAssetMenu(menuName = "UnderGrind/Testing/Player Graybox Settings", fileName = "PlayerGrayboxSettings")]
    public sealed class PlayerGrayboxSettings : ScriptableObject
    {
        [Header("Player")]
        [SerializeField] private Vector3 playerSpawnPosition;

        [Header("Pavimento")]
        [SerializeField] private Vector3 floorPosition;
        [SerializeField] private Vector2 floorSize;

        [Header("Marcatori di distanza")]
        [SerializeField] private float markerInterval;
        [SerializeField] private float markerMaxDistance;
        [SerializeField] private float markerLineX;
        [SerializeField] private float markerLineWidth;
        [SerializeField] private float markerLineHeight;
        [SerializeField] private float markerLineDepth;
        [SerializeField] private float markerSurfaceOffset;
        [SerializeField] private Vector3 markerLabelOffset;
        [SerializeField] private float markerTextCharacterSize;
        [SerializeField] private int markerTextFontSize;
        [SerializeField] private TextAnchor markerTextAlignment;
        [SerializeField] private Color markerTextColor;

        [Header("Materiali")]
        [SerializeField] private Color groundColor;
        [SerializeField] private Color concreteColor;
        [SerializeField] private Color markerColor;
        [SerializeField] private Color accentColor;

        [Header("Ostacoli, scale e piattaforme")]
        [SerializeField] private GrayboxBlockDefinition[] blocks;
        [SerializeField] private GrayboxRampDefinition[] ramps;

        [Header("Illuminazione")]
        [SerializeField] private Vector3 directionalLightEuler;
        [SerializeField] private float directionalLightIntensity;
        [SerializeField] private Color ambientColor;

        public Vector3 PlayerSpawnPosition => playerSpawnPosition;
        public Vector3 FloorPosition => floorPosition;
        public Vector2 FloorSize => floorSize;
        public float MarkerInterval => markerInterval;
        public float MarkerMaxDistance => markerMaxDistance;
        public float MarkerLineX => markerLineX;
        public float MarkerLineWidth => markerLineWidth;
        public float MarkerLineHeight => markerLineHeight;
        public float MarkerLineDepth => markerLineDepth;
        public float MarkerSurfaceOffset => markerSurfaceOffset;
        public Vector3 MarkerLabelOffset => markerLabelOffset;
        public float MarkerTextCharacterSize => markerTextCharacterSize;
        public int MarkerTextFontSize => markerTextFontSize;
        public TextAnchor MarkerTextAlignment => markerTextAlignment;
        public Color MarkerTextColor => markerTextColor;
        public Color GroundColor => groundColor;
        public Color ConcreteColor => concreteColor;
        public Color MarkerColor => markerColor;
        public Color AccentColor => accentColor;
        public GrayboxBlockDefinition[] Blocks => blocks ?? Array.Empty<GrayboxBlockDefinition>();
        public GrayboxRampDefinition[] Ramps => ramps ?? Array.Empty<GrayboxRampDefinition>();
        public Vector3 DirectionalLightEuler => directionalLightEuler;
        public float DirectionalLightIntensity => directionalLightIntensity;
        public Color AmbientColor => ambientColor;

#if UNITY_EDITOR
        private void OnValidate()
        {
            floorSize.x = Mathf.Max(1f, floorSize.x);
            floorSize.y = Mathf.Max(1f, floorSize.y);
            markerInterval = Mathf.Max(0.1f, markerInterval);
            markerMaxDistance = Mathf.Max(0f, markerMaxDistance);
            markerLineWidth = Mathf.Max(0.01f, markerLineWidth);
            markerLineHeight = Mathf.Max(0.001f, markerLineHeight);
            markerLineDepth = Mathf.Max(0.001f, markerLineDepth);
            markerTextCharacterSize = Mathf.Max(0.001f, markerTextCharacterSize);
            markerTextFontSize = Mathf.Max(1, markerTextFontSize);
            directionalLightIntensity = Mathf.Max(0f, directionalLightIntensity);
        }
#endif
    }
}