using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnderGrind.Locomotion
{
    /// <summary>
    /// Controls which visual renderers are visible in first- and third-person.
    /// Production rigs should use the stable part-name convention emitted by the
    /// UnderGrind humanoid asset pipeline; explicit renderer lists remain supported.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerVisualRig : MonoBehaviour
    {
        [Header("Explicit renderer groups")]
        [SerializeField] private Renderer[] headRenderers = Array.Empty<Renderer>();
        [SerializeField] private Renderer[] firstPersonOnlyRenderers = Array.Empty<Renderer>();
        [SerializeField] private bool discoverRenderersByName = true;

        private readonly Dictionary<Renderer, bool> _originalEnabled = new Dictionary<Renderer, bool>();
        private bool _thirdPerson;
        private bool _initialized;

        public bool IsThirdPerson => _thirdPerson;

        private void Awake()
        {
            CacheRenderers();
            ApplyVisibility();
        }

        /// <summary>
        /// Binds the separate head and optional first-person arms/hand renderers
        /// using the asset pipeline naming convention:
        /// *_Head, *_FP_Arms and *_FP_Hands.
        /// Returns false when no separate head renderer is found.
        /// </summary>
        public bool ConfigureFromHierarchy(Transform rigRoot)
        {
            if (rigRoot == null) return false;

            var renderers = rigRoot.GetComponentsInChildren<Renderer>(true);
            var heads = new List<Renderer>();
            var firstPersonOnly = new List<Renderer>();
            foreach (var renderer in renderers)
            {
                if (renderer == null) continue;
                var partName = renderer.name;
                if (HasPartSuffix(partName, "_Head")) heads.Add(renderer);
                if (ContainsToken(partName, "_FP_Arms") || ContainsToken(partName, "_FP_Hands"))
                    firstPersonOnly.Add(renderer);
            }

            if (heads.Count == 0) return false;
            Configure(heads.ToArray(), firstPersonOnly.ToArray());
            return true;
        }

        public void Configure(Renderer[] head, Renderer[] firstPersonOnly)
        {
            // Explicit rig bindings must be authoritative. Name discovery is only a fallback
            // for prototypes and must not silently add unrelated hair/face/arm renderers.
            discoverRenderersByName = false;
            headRenderers = head ?? Array.Empty<Renderer>();
            firstPersonOnlyRenderers = firstPersonOnly ?? Array.Empty<Renderer>();
            CacheRenderers();
            ApplyVisibility();
        }

        public void SetThirdPerson(bool thirdPerson)
        {
            if (_thirdPerson == thirdPerson && _initialized) return;
            _thirdPerson = thirdPerson;
            if (!_initialized) CacheRenderers();
            ApplyVisibility();
        }

        public void RefreshRenderers()
        {
            CacheRenderers();
            ApplyVisibility();
        }

        private void CacheRenderers()
        {
            if (discoverRenderersByName)
            {
                var all = GetComponentsInChildren<Renderer>(true);
                var heads = new List<Renderer>(headRenderers);
                var firstPerson = new List<Renderer>(firstPersonOnlyRenderers);
                foreach (var renderer in all)
                {
                    if (renderer == null) continue;
                    var name = renderer.name.ToLowerInvariant();
                    if (ContainsAny(name, "head", "face", "hair", "eye", "skull", "jaw"))
                    {
                        if (!heads.Contains(renderer)) heads.Add(renderer);
                    }
                    if (ContainsAny(name, "fp_", "firstperson", "first_person", "arms_fp", "hands_fp"))
                    {
                        if (!firstPerson.Contains(renderer)) firstPerson.Add(renderer);
                    }
                }
                headRenderers = heads.ToArray();
                firstPersonOnlyRenderers = firstPerson.ToArray();
            }

            Cache(headRenderers);
            Cache(firstPersonOnlyRenderers);
            _initialized = true;
        }

        private void Cache(IEnumerable<Renderer> renderers)
        {
            foreach (var renderer in renderers)
                if (renderer != null && !_originalEnabled.ContainsKey(renderer))
                    _originalEnabled.Add(renderer, renderer.enabled);
        }

        private void ApplyVisibility()
        {
            if (!_initialized) return;
            foreach (var pair in _originalEnabled)
            {
                if (pair.Key == null) continue;
                var isHead = Array.IndexOf(headRenderers, pair.Key) >= 0;
                var isFirstPersonOnly = Array.IndexOf(firstPersonOnlyRenderers, pair.Key) >= 0;
                pair.Key.enabled = pair.Value && (!isHead || _thirdPerson) &&
                                   (!isFirstPersonOnly || !_thirdPerson);
            }
        }

        private static bool HasPartSuffix(string value, string suffix) =>
            value.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);

        private static bool ContainsToken(string value, string token) =>
            value.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;

        private static bool ContainsAny(string value, params string[] tokens)
        {
            foreach (var token in tokens)
                if (value.Contains(token)) return true;
            return false;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            headRenderers ??= Array.Empty<Renderer>();
            firstPersonOnlyRenderers ??= Array.Empty<Renderer>();
        }
#endif
    }
}