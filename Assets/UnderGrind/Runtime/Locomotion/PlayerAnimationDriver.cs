using System.Collections.Generic;
using UnityEngine;

namespace UnderGrind.Locomotion
{
    /// <summary>
    /// Optional, parameter-safe bridge between player locomotion and an Animator Controller.
    /// The CharacterController remains the sole authority for world movement.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerAnimationDriver : MonoBehaviour
    {
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int MoveXHash = Animator.StringToHash("MoveX");
        private static readonly int MoveYHash = Animator.StringToHash("MoveY");
        private static readonly int GroundedHash = Animator.StringToHash("Grounded");
        private static readonly int VerticalSpeedHash = Animator.StringToHash("VerticalSpeed");
        private static readonly int RunningHash = Animator.StringToHash("Running");
        private static readonly int CrouchingHash = Animator.StringToHash("Crouching");
        private static readonly int JumpingHash = Animator.StringToHash("Jumping");
        private static readonly int FallingHash = Animator.StringToHash("Falling");

        [SerializeField] private Animator animator;
        private readonly Dictionary<int, AnimatorControllerParameterType> _availableParameters =
            new Dictionary<int, AnimatorControllerParameterType>();
        private RuntimeAnimatorController _cachedController;
        private int _parameterCount = -1;

        public Animator Animator => animator;

        private void Awake()
        {
            ResolveAnimator();
            RefreshParameters();
        }

        public void Configure(Animator target)
        {
            animator = target;
            RefreshParameters();
        }

        public void Apply(float speed, Vector2 moveInput, bool grounded, float verticalSpeed,
            bool running, bool crouching, bool jumping, bool falling)
        {
            ResolveAnimator();
            if (animator == null) return;
            if (animator.applyRootMotion) animator.applyRootMotion = false;
            if (animator.runtimeAnimatorController != _cachedController ||
                animator.parameters.Length != _parameterCount) RefreshParameters();

            SetFloat(SpeedHash, speed);
            SetFloat(MoveXHash, moveInput.x);
            SetFloat(MoveYHash, moveInput.y);
            SetFloat(VerticalSpeedHash, verticalSpeed);
            SetBool(GroundedHash, grounded);
            SetBool(RunningHash, running);
            SetBool(CrouchingHash, crouching);
            SetBool(JumpingHash, jumping);
            SetBool(FallingHash, falling);
        }

        private void ResolveAnimator()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>(true);
            if (animator != null && (animator.runtimeAnimatorController != _cachedController ||
                animator.parameters.Length != _parameterCount))
                RefreshParameters();
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

            var parameters = animator.parameters;
            _cachedController = animator.runtimeAnimatorController;
            _parameterCount = parameters.Length;
            foreach (var parameter in parameters)
                _availableParameters[parameter.nameHash] = parameter.type;
        }

        private void SetFloat(int hash, float value)
        {
            if (_availableParameters.TryGetValue(hash, out var type) &&
                type == AnimatorControllerParameterType.Float)
                animator.SetFloat(hash, value);
        }

        private void SetBool(int hash, bool value)
        {
            if (_availableParameters.TryGetValue(hash, out var type) &&
                type == AnimatorControllerParameterType.Bool)
                animator.SetBool(hash, value);
        }
    }
}