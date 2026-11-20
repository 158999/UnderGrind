using System.Collections.Generic;
using UnityEngine;

namespace UnderGrind.Locomotion
{
    /// <summary>
    /// Sends measured locomotion state to the Animator. CharacterController remains
    /// the sole authority for world movement; root motion is always disabled.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerAnimationDriver : MonoBehaviour
    {
        [SerializeField] private PlayerAnimationSettings settings;
        [SerializeField] private Animator animator;

        private readonly Dictionary<int, AnimatorControllerParameterType> _availableParameters =
            new Dictionary<int, AnimatorControllerParameterType>();
        private RuntimeAnimatorController _cachedController;
        private int _parameterCount = -1;
        private bool _wasGrounded;
        private bool _hasGroundedSample;

        public Animator Animator => animator;
        public PlayerAnimationSettings Settings => settings;

        private void Awake()
        {
            ResolveAnimator();
            ApplyConfiguredController();
            RefreshParameters();
        }

        public void Configure(Animator target)
        {
            animator = target;
            ApplyConfiguredController();
            RefreshParameters();
        }

        public void Configure(PlayerAnimationSettings animationSettings, Animator target = null)
        {
            settings = animationSettings;
            if (target != null) animator = target;
            ResolveAnimator();
            ApplyConfiguredController();
            RefreshParameters();
        }

        public void Apply(float speed, Vector2 moveInput, bool grounded, float verticalSpeed,
            bool running, bool crouching, bool jumping, bool falling)
        {
            ResolveAnimator();
            if (animator == null) return;
            if (animator.applyRootMotion) animator.applyRootMotion = false;
            if (animator.runtimeAnimatorController == null)
            {
                ClearParameters();
                return;
            }
            if (animator.runtimeAnimatorController != _cachedController ||
                animator.parameters.Length != _parameterCount) RefreshParameters();
            if (settings == null) return;

            var landed = _hasGroundedSample && grounded && !_wasGrounded;
            _wasGrounded = grounded;
            _hasGroundedSample = true;

            SetFloat(settings.SpeedParameterName, speed);
            SetFloat(settings.MoveXParameterName, moveInput.x);
            SetFloat(settings.MoveYParameterName, moveInput.y);
            SetFloat(settings.VerticalSpeedParameterName, verticalSpeed);
            SetBool(settings.GroundedParameterName, grounded);
            SetBool(settings.RunningParameterName, running);
            SetBool(settings.CrouchingParameterName, crouching);
            SetBool(settings.JumpingParameterName, jumping);
            SetBool(settings.FallingParameterName, falling);
            SetBool(settings.LandedParameterName, landed);
        }

        private void ResolveAnimator()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>(true);
            if (animator != null && settings != null &&
                settings.AnimatorController != null &&
                animator.runtimeAnimatorController != settings.AnimatorController)
                ApplyConfiguredController();
            if (animator == null) return;
            if (animator.runtimeAnimatorController == null)
            {
                ClearParameters();
                return;
            }
            if (animator.runtimeAnimatorController != _cachedController ||
                animator.parameters.Length != _parameterCount)
                RefreshParameters();
        }

        private void ApplyConfiguredController()
        {
            if (animator == null || settings == null) return;
            if (settings.AnimatorController != null &&
                animator.runtimeAnimatorController != settings.AnimatorController)
                animator.runtimeAnimatorController = settings.AnimatorController;
            animator.applyRootMotion = false;
        }

        private void RefreshParameters()
        {
            _availableParameters.Clear();
            if (animator == null)
            {
                _cachedController = null;
                _parameterCount = 0;
                return;
            }

            if (animator.runtimeAnimatorController == null)
            {
                ClearParameters();
                return;
            }

            var parameters = animator.parameters;
            _cachedController = animator.runtimeAnimatorController;
            _parameterCount = parameters.Length;
            foreach (var parameter in parameters)
                _availableParameters[parameter.nameHash] = parameter.type;
        }

        private void ClearParameters()
        {
            _availableParameters.Clear();
            _cachedController = null;
            _parameterCount = 0;
        }

        private void SetFloat(string parameterName, float value)
        {
            if (string.IsNullOrWhiteSpace(parameterName)) return;
            var hash = Animator.StringToHash(parameterName);
            if (_availableParameters.TryGetValue(hash, out var type) &&
                type == AnimatorControllerParameterType.Float)
            {
                if (settings.SpeedDampTime > 0f)
                    animator.SetFloat(hash, value, settings.SpeedDampTime, Time.deltaTime);
                else animator.SetFloat(hash, value);
            }
        }

        private void SetBool(string parameterName, bool value)
        {
            if (string.IsNullOrWhiteSpace(parameterName)) return;
            var hash = Animator.StringToHash(parameterName);
            if (_availableParameters.TryGetValue(hash, out var type) &&
                type == AnimatorControllerParameterType.Bool)
                animator.SetBool(hash, value);
        }
    }
}