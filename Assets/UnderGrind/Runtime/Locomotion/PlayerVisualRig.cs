using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnderGrind.Locomotion
{
    /// <summary>
    /// Controls which visual renderers are visible in first- and third-person.
    /// Assign explicit renderer lists for production rigs; name discovery is a safe fallback.
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