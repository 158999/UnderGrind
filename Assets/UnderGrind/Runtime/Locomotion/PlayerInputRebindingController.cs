using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UnderGrind.Locomotion
{
    [DisallowMultipleComponent]
    public sealed class PlayerInputRebindingController : MonoBehaviour
    {
        [SerializeField] private PlayerInputSettings settings;

        private InputActionRebindingExtensions.RebindingOperation _operation;
        private PlayerInputActionId _activeAction;
        private int _activeBindingIndex = -1;

        public event Action<PlayerInputActionId, int, string> RebindCompleted;
        public event Action<PlayerInputActionId, int> RebindCancelled;
        public bool IsRebinding => _operation != null;

        public void Configure(PlayerInputSettings value)
        {
            CancelRebind();
            settings = value;
            LoadOverrides();
        }

        public bool BeginRebind(PlayerInputActionId actionId, int bindingIndex)
        {
            CancelRebind();
            var action = settings != null ? settings.GetAction(actionId) : null;
            if (action == null || bindingIndex < 0 || bindingIndex >= action.bindings.Count ||
                action.bindings[bindingIndex].isComposite)
                return false;

            _activeAction = actionId;
            _activeBindingIndex = bindingIndex;
            _operation = action.PerformInteractiveRebinding(bindingIndex)
                .WithTimeout(settings.RebindTimeoutSeconds)
                .OnComplete(operation =>
                {
                    var display = action.GetBindingDisplayString(bindingIndex);
                    operation.Dispose();
                    _operation = null;
                    SaveOverrides();
                    RebindCompleted?.Invoke(_activeAction, _activeBindingIndex, display);
                    _activeBindingIndex = -1;
                })
                .OnCancel(operation =>
                {
                    operation.Dispose();
                    _operation = null;
                    RebindCancelled?.Invoke(_activeAction, _activeBindingIndex);
                    _activeBindingIndex = -1;
                });
            _operation.Start();
            return true;
        }

        public void CancelRebind()
        {
            if (_operation == null) return;
            _operation.Cancel();
        }

        public string GetBindingDisplayString(PlayerInputActionId actionId, int bindingIndex)
        {
            var action = settings != null ? settings.GetAction(actionId) : null;
            return action != null && bindingIndex >= 0 && bindingIndex < action.bindings.Count
                ? action.GetBindingDisplayString(bindingIndex) : string.Empty;
        }

        public bool SaveOverrides()
        {
            if (settings == null || settings.ActionAsset == null ||
                string.IsNullOrWhiteSpace(settings.OverrideStorageKey))
                return false;

            PlayerPrefs.SetString(settings.OverrideStorageKey, settings.ActionAsset.SaveBindingOverridesAsJson());
            PlayerPrefs.Save();
            return true;
        }

        public bool LoadOverrides()
        {
            if (settings == null || settings.ActionAsset == null ||
                string.IsNullOrWhiteSpace(settings.OverrideStorageKey) ||
                !PlayerPrefs.HasKey(settings.OverrideStorageKey))
                return false;

            settings.ActionAsset.LoadBindingOverridesFromJson(
                PlayerPrefs.GetString(settings.OverrideStorageKey));
            return true;
        }

        public void ResetOverrides()
        {
            if (settings == null || settings.ActionAsset == null) return;
            settings.ActionAsset.RemoveAllBindingOverrides();
            if (!string.IsNullOrWhiteSpace(settings.OverrideStorageKey))
            {
                PlayerPrefs.DeleteKey(settings.OverrideStorageKey);
                PlayerPrefs.Save();
            }
        }

        private void OnEnable()
        {
            LoadOverrides();
        }

        private void OnDisable()
        {
            CancelRebind();
        }
    }
}