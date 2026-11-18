using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnderGrind.Locomotion
{
    /// <summary>
    /// Controls explicit head and first-person renderer groups. Production naming
    /// rules are supplied by PlayerRigSettings; no geometry-based guessing is used.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerVisualRig : MonoBehaviour
    {
        [SerializeField] private PlayerRigSettings rigSettings;
        [SerializeField] private Renderer[] headRenderers = Array.Empty<Renderer>();
        [SerializeField] private Renderer[] firstPersonOnlyRenderers = Array.Empty<Renderer>();

        private readonly Dictionary<Renderer, bool> _originalEnabled = new Dictionary<Renderer, bool>();
        private bool _thirdPerson;
        private bool _initialized;

        public bool IsThirdPerson => _thirdPerson;
        public PlayerRigSettings RigSettings => rigSettings;

        private void Awake()
        {
            if (rigSettings != null && ConfigureFromHierarchy(transform, rigSettings)) return;
            CacheRenderers();
            ApplyVisibility();
        }

        public bool ConfigureFromHierarchy(Transform rigRoot)
        {
            return ConfigureFromHierarchy(rigRoot, rigSettings);
        }

        public bool ConfigureFromHierarchy(Transform rigRoot, PlayerRigSettings configuration)
        {
            if (rigRoot == null || configuration == null) return false;

            var renderers = rigRoot.GetComponentsInChildren<Renderer>(true);
            var heads = new List<Renderer>();
            var firstPersonOnly = new List<Renderer>();
            foreach (var renderer in renderers)
            {
                if (renderer == null) continue;
                if (!string.IsNullOrWhiteSpace(configuration.HeadRendererSuffix) &&
                    HasPartSuffix(renderer.name, configuration.HeadRendererSuffix))
                    heads.Add(renderer);

                if ((!string.IsNullOrWhiteSpace(configuration.FirstPersonArmsToken) &&
                     ContainsToken(renderer.name, configuration.FirstPersonArmsToken)) ||
                    (!string.IsNullOrWhiteSpace(configuration.FirstPersonHandsToken) &&
                     ContainsToken(renderer.name, configuration.FirstPersonHandsToken)))
                    firstPersonOnly.Add(renderer);
            }

            if (configuration.RequireSeparateHead && heads.Count == 0) return false;
            if (configuration.RequireFirstPersonArms && firstPersonOnly.Count == 0) return false;

            rigSettings = configuration;
            Configure(heads.ToArray(), firstPersonOnly.ToArray());
            return true;
        }

        public void Configure(Renderer[] head, Renderer[] firstPersonOnly)
        {
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

#if UNITY_EDITOR
        private void OnValidate()
        {
            headRenderers ??= Array.Empty<Renderer>();
            firstPersonOnlyRenderers ??= Array.Empty<Renderer>();
        }
#endif
    }
}