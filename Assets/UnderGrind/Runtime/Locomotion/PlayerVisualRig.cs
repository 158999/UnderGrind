using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnderGrind.Locomotion
{
    /// <summary>
    /// Controls head, body and first-/third-person limb renderer groups. The body is
    /// revealed in first person when the view pitch crosses the configured look-down threshold.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerVisualRig : MonoBehaviour
    {
        [SerializeField] private PlayerRigSettings rigSettings;
        [SerializeField] private Renderer[] headRenderers = Array.Empty<Renderer>();
        [SerializeField] private Renderer[] bodyRenderers = Array.Empty<Renderer>();
        [SerializeField] private Renderer[] thirdPersonOnlyRenderers = Array.Empty<Renderer>();
        [SerializeField] private Renderer[] firstPersonOnlyRenderers = Array.Empty<Renderer>();
        [SerializeField] private Transform viewReference;

        private readonly Dictionary<Renderer, bool> _originalEnabled = new Dictionary<Renderer, bool>();
        private bool _thirdPerson;
        private bool _bodyVisibleWhileLookingDown = true;
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
            var bodies = new List<Renderer>();
            var thirdPersonOnly = new List<Renderer>();
            var firstPersonOnly = new List<Renderer>();
            foreach (var renderer in renderers)
            {
                if (renderer == null) continue;
                if (!string.IsNullOrWhiteSpace(configuration.HeadRendererSuffix) &&
                    HasPartSuffix(renderer.name, configuration.HeadRendererSuffix))
                    heads.Add(renderer);

                if (!string.IsNullOrWhiteSpace(configuration.BodyRendererToken) &&
                    ContainsToken(renderer.name, configuration.BodyRendererToken))
                    bodies.Add(renderer);

                if ((!string.IsNullOrWhiteSpace(configuration.ThirdPersonArmsToken) &&
                     ContainsToken(renderer.name, configuration.ThirdPersonArmsToken)) ||
                    (!string.IsNullOrWhiteSpace(configuration.ThirdPersonHandsToken) &&
                     ContainsToken(renderer.name, configuration.ThirdPersonHandsToken)))
                    thirdPersonOnly.Add(renderer);

                if ((!string.IsNullOrWhiteSpace(configuration.FirstPersonArmsToken) &&
                     ContainsToken(renderer.name, configuration.FirstPersonArmsToken)) ||
                    (!string.IsNullOrWhiteSpace(configuration.FirstPersonHandsToken) &&
                     ContainsToken(renderer.name, configuration.FirstPersonHandsToken)))
                    firstPersonOnly.Add(renderer);
            }

            if (configuration.RequireSeparateHead && heads.Count == 0) return false;
            if (configuration.RequireFirstPersonArms && firstPersonOnly.Count == 0) return false;

            rigSettings = configuration;
            Configure(heads.ToArray(), bodies.ToArray(), thirdPersonOnly.ToArray(), firstPersonOnly.ToArray());
            return true;
        }

        public void Configure(Renderer[] head, Renderer[] firstPersonOnly)
        {
            Configure(head, Array.Empty<Renderer>(), firstPersonOnly);
        }

        public void Configure(Renderer[] head, Renderer[] thirdPersonOnly, Renderer[] firstPersonOnly)
        {
            Configure(head, Array.Empty<Renderer>(), thirdPersonOnly, firstPersonOnly);
        }

        public void Configure(Renderer[] head, Renderer[] body, Renderer[] thirdPersonOnly,
            Renderer[] firstPersonOnly)
        {
            headRenderers = head ?? Array.Empty<Renderer>();
            bodyRenderers = body ?? Array.Empty<Renderer>();
            thirdPersonOnlyRenderers = thirdPersonOnly ?? Array.Empty<Renderer>();
            firstPersonOnlyRenderers = firstPersonOnly ?? Array.Empty<Renderer>();
            _bodyVisibleWhileLookingDown = _thirdPerson || viewReference == null;
            CacheRenderers();
            ApplyVisibility();
        }

        public void SetViewReference(Transform reference)
        {
            viewReference = reference;
            _bodyVisibleWhileLookingDown = _thirdPerson || viewReference == null;
            UpdateBodyVisibility(true);
        }

        public void SetThirdPerson(bool thirdPerson)
        {
            if (_thirdPerson == thirdPerson && _initialized) return;
            _thirdPerson = thirdPerson;
            _bodyVisibleWhileLookingDown = _thirdPerson || viewReference == null;
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
            Cache(bodyRenderers);
            Cache(thirdPersonOnlyRenderers);
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
                var isBody = Array.IndexOf(bodyRenderers, pair.Key) >= 0;
                var isThirdPersonOnly = Array.IndexOf(thirdPersonOnlyRenderers, pair.Key) >= 0;
                var isFirstPersonOnly = Array.IndexOf(firstPersonOnlyRenderers, pair.Key) >= 0;
                var bodyVisible = _thirdPerson || _bodyVisibleWhileLookingDown || viewReference == null;
                pair.Key.enabled = pair.Value && (!isHead || _thirdPerson) &&
                                   (!isBody || bodyVisible) &&
                                   (!isThirdPersonOnly || _thirdPerson) &&
                                   (!isFirstPersonOnly || !_thirdPerson);
            }
        }

        private void LateUpdate()
        {
            UpdateBodyVisibility(false);
        }

        private void UpdateBodyVisibility(bool force)
        {
            if (_thirdPerson || rigSettings == null || viewReference == null || bodyRenderers.Length == 0)
                return;

            var localForward = transform.root.InverseTransformDirection(viewReference.forward);
            var downwardPitch = Mathf.Asin(Mathf.Clamp(-localForward.y, -1f, 1f)) * Mathf.Rad2Deg;
            var visible = _bodyVisibleWhileLookingDown
                ? downwardPitch > rigSettings.FirstPersonBodyHidePitch
                : downwardPitch >= rigSettings.FirstPersonBodyRevealPitch;
            if (force || visible != _bodyVisibleWhileLookingDown)
            {
                _bodyVisibleWhileLookingDown = visible;
                ApplyVisibility();
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
            bodyRenderers ??= Array.Empty<Renderer>();
            thirdPersonOnlyRenderers ??= Array.Empty<Renderer>();
            firstPersonOnlyRenderers ??= Array.Empty<Renderer>();
        }
#endif
    }
}