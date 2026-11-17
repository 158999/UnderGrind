using UnityEngine;
using UnityEngine.InputSystem;

namespace UnderGrind.Locomotion
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class HumanLocomotionController : MonoBehaviour
    {
        public const float DefaultWalkSpeed = 1.4f;
        public const float DefaultRunMultiplier = 4f / 3f;
        public const float DefaultJumpHeight = 0.4f;
        public const float DefaultCharacterHeight = 1.78f;

        [Header("Human movement")]
        [SerializeField, Min(1f), Tooltip("Altezza totale del personaggio in metri.")]
        private float characterHeight = DefaultCharacterHeight;
        [SerializeField, Min(0.1f), Tooltip("Velocità di camminata in m/s. Valore iniziale: 1,40.")]
        private float walkSpeed = DefaultWalkSpeed;
        [SerializeField, Min(1f), Tooltip("Moltiplicatore della camminata. 1,333 = corsa più veloce di un terzo.")]
        private float runMultiplier = DefaultRunMultiplier;
        [SerializeField, Min(0.05f), Tooltip("Altezza teorica raggiunta dal salto verticale in metri.")]
        private float jumpHeight = DefaultJumpHeight;
        [SerializeField, Min(0.1f)] private float gravity = 9.81f;
        [SerializeField, Range(0f, 1f)] private float airControl = 0.65f;

        [Header("View")]
        [SerializeField] private Transform viewPivot;
        [SerializeField] private Camera playerCamera;
        [SerializeField, Min(0.01f)] private float mouseSensitivity = 0.12f;
        [SerializeField, Min(0.5f)] private float thirdPersonDistance = 3.25f;

        private CharacterController _characterController;
        private Animator _animator;
        private float _verticalVelocity;
        private float _pitch;
        private bool _thirdPerson;
        private bool _cursorLocked;
        private Vector3 _horizontalVelocity;
        private float _measuredSpeed;

        public float CharacterHeight => characterHeight;
        public float WalkSpeed => walkSpeed;
        public float RunSpeed => walkSpeed * runMultiplier;
        public float RunMultiplier => runMultiplier;
        public float JumpHeight => jumpHeight;
        public float Gravity => gravity;
        public float JumpVelocity => Mathf.Sqrt(2f * gravity * jumpHeight);
        public float MeasuredSpeed => _measuredSpeed;
        public bool IsGrounded => _characterController != null && _characterController.isGrounded;
        public bool IsRunning { get; private set; }
        public bool IsThirdPerson => _thirdPerson;
        public float VerticalVelocity => _verticalVelocity;

        public void ConfigureViewRig(Transform pivot, Camera camera)
        {
            viewPivot = pivot;
            playerCamera = camera;
        }

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            _animator = GetComponentInChildren<Animator>();
            if (playerCamera == null)
                playerCamera = GetComponentInChildren<Camera>();
            if (viewPivot == null && playerCamera != null)
                viewPivot = playerCamera.transform.parent;

            ApplyCharacterDimensions();
            SetCursorLocked(playerCamera != null);
            ApplyCameraMode();
        }

        private void OnValidate()
        {
            characterHeight = Mathf.Max(1f, characterHeight);
            walkSpeed = Mathf.Max(0.1f, walkSpeed);
            runMultiplier = Mathf.Max(1f, runMultiplier);
            jumpHeight = Mathf.Max(0.05f, jumpHeight);
            gravity = Mathf.Max(0.1f, gravity);

            if (!Application.isPlaying)
            {
                var controller = GetComponent<CharacterController>();
                if (controller != null)
                    ApplyCharacterDimensions(controller);
            }
        }

        private void ApplyCharacterDimensions()
        {
            if (_characterController != null)
                ApplyCharacterDimensions(_characterController);
        }

        private void ApplyCharacterDimensions(CharacterController controller)
        {
            controller.height = characterHeight;
            controller.radius = Mathf.Min(0.28f, characterHeight * 0.16f);
            controller.center = new Vector3(0f, characterHeight * 0.5f, 0f);
            controller.stepOffset = Mathf.Min(0.32f, characterHeight * 0.18f);
            controller.slopeLimit = 48f;
            controller.minMoveDistance = 0f;
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard == null)
                return;

            if (keyboard.escapeKey.wasPressedThisFrame)
                SetCursorLocked(false);
            else if (mouse != null && mouse.leftButton.wasPressedThisFrame && !_cursorLocked)
                SetCursorLocked(true);

            if (keyboard.f5Key.wasPressedThisFrame)
            {
                _thirdPerson = !_thirdPerson;
                ApplyCameraMode();
            }

            ReadLook(mouse);
            Move(keyboard);
            UpdateAnimator();
        }

        private void ReadLook(Mouse mouse)
        {
            if (!_cursorLocked || mouse == null || viewPivot == null)
                return;

            var delta = mouse.delta.ReadValue();
            transform.Rotate(Vector3.up, delta.x * mouseSensitivity, Space.World);
            _pitch = Mathf.Clamp(_pitch - delta.y * mouseSensitivity, -82f, 82f);
            viewPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        private void Move(Keyboard keyboard)
        {
            var deltaTime = Time.deltaTime;
            if (deltaTime <= 0f)
                return;

            var input = Vector2.zero;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) input.x -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) input.x += 1f;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) input.y += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) input.y -= 1f;
            input = Vector2.ClampMagnitude(input, 1f);

            var groundedBeforeMove = _characterController.isGrounded;
            if (groundedBeforeMove && _verticalVelocity < 0f)
                _verticalVelocity = -2f;
            if (groundedBeforeMove && keyboard.spaceKey.wasPressedThisFrame)
                _verticalVelocity = JumpVelocity;

            _verticalVelocity -= gravity * deltaTime;
            IsRunning = (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed) && input.sqrMagnitude > 0.001f;
            var targetSpeed = IsRunning ? RunSpeed : walkSpeed;
            var desiredDirection = transform.right * input.x + transform.forward * input.y;
            if (desiredDirection.sqrMagnitude > 1f)
                desiredDirection.Normalize();

            var control = groundedBeforeMove ? 1f : airControl;
            var desiredVelocity = desiredDirection * targetSpeed;
            _horizontalVelocity = Vector3.Lerp(_horizontalVelocity, desiredVelocity, control);
            if (input.sqrMagnitude < 0.001f && groundedBeforeMove)
                _horizontalVelocity = Vector3.zero;

            _characterController.Move((_horizontalVelocity + Vector3.up * _verticalVelocity) * deltaTime);
            _measuredSpeed = new Vector3(_characterController.velocity.x, 0f, _characterController.velocity.z).magnitude;
        }

        private void UpdateAnimator()
        {
            if (_animator == null)
                return;
            _animator.SetFloat("Speed", _measuredSpeed);
            _animator.SetBool("Grounded", _characterController.isGrounded);
            _animator.SetFloat("VerticalSpeed", _verticalVelocity);
        }

        private void ApplyCameraMode()
        {
            if (playerCamera == null || viewPivot == null)
                return;

            var targetLocalPosition = _thirdPerson
                ? new Vector3(0f, 0.12f, -thirdPersonDistance)
                : Vector3.zero;

            if (_thirdPerson)
            {
                var origin = viewPivot.position;
                var desired = viewPivot.TransformPoint(targetLocalPosition);
                var direction = desired - origin;
                if (Physics.SphereCast(origin, 0.16f, direction.normalized, out var hit, direction.magnitude,
                        Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                    targetLocalPosition = viewPivot.InverseTransformPoint(hit.point + hit.normal * 0.18f);
            }

            playerCamera.transform.localPosition = targetLocalPosition;
            playerCamera.transform.localRotation = Quaternion.identity;
            playerCamera.nearClipPlane = 0.04f;

            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
            {
                var name = renderer.name.ToLowerInvariant();
                var isHeadPart = name.Contains("head") || name.Contains("face") || name.Contains("hair") ||
                                 name.Contains("eye") || name.Contains("skull") || name.Contains("jaw");
                if (isHeadPart)
                    renderer.enabled = _thirdPerson;
            }
        }

        private void SetCursorLocked(bool locked)
        {
            _cursorLocked = locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        private void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}