using UnityEngine;
using UnityEngine.InputSystem;
using UnderGrind.Inventory;

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
        [SerializeField, Min(1f)] private float characterHeight = DefaultCharacterHeight;
        [SerializeField, Min(0.1f)] private float walkSpeed = DefaultWalkSpeed;
        [SerializeField, Min(1f)] private float runMultiplier = DefaultRunMultiplier;
        [SerializeField, Min(0.05f)] private float jumpHeight = DefaultJumpHeight;
        [SerializeField, Min(0.1f)] private float gravity = 9.81f;
        [SerializeField, Min(0.1f)] private float acceleration = 12f;
        [SerializeField, Min(0.1f)] private float deceleration = 16f;
        [SerializeField, Range(0f, 1f)] private float airControl = 0.35f;
        [SerializeField, Min(0f)] private float coyoteTime = 0.1f;
        [SerializeField, Min(0f)] private float jumpBufferTime = 0.12f;

        [Header("Inventory overload")]
        [SerializeField] private PlayerInventory inventory;

        [Header("Stance")]
        [SerializeField, Min(0.1f)] private float crouchHeight = 1.12f;
        [SerializeField, Min(0.1f)] private float crouchSpeedMultiplier = 0.5f;
        [SerializeField, Min(0.1f)] private float stanceChangeSpeed = 8f;
        [SerializeField, Min(0.1f)] private float capsuleRadius = 0.28f;

        [Header("View")]
        [SerializeField] private Transform viewPivot;
        [SerializeField] private Camera playerCamera;
        [SerializeField, Min(0.01f)] private float mouseSensitivity = 0.12f;
        [SerializeField, Min(1f)] private float gamepadLookSensitivity = 150f;
        [SerializeField, Range(45f, 89f)] private float maxLookAngle = 82f;
        [SerializeField, Min(0.5f)] private float thirdPersonDistance = 3.25f;
        [SerializeField, Min(0.1f)] private float cameraCollisionRadius = 0.16f;
        [SerializeField] private LayerMask cameraCollisionLayers = ~0;

        private CharacterController _characterController;
        private PlayerAnimationDriver _animationDriver;
        private PlayerVisualRig _visualRig;
        private float _verticalVelocity;
        private float _pitch;
        private float _currentHeight;
        private float _lastGroundedTime = float.NegativeInfinity;
        private float _jumpBufferedUntil = float.NegativeInfinity;
        private bool _thirdPerson;
        private bool _cursorLocked;
        private Vector3 _horizontalVelocity;
        private float _measuredSpeed;
        private Vector2 _moveInput;

        public float CharacterHeight => characterHeight;
        public float WalkSpeed => walkSpeed;
        public float EffectiveWalkSpeed => walkSpeed * (IsOverloaded ? inventory.OverloadedWalkSpeedMultiplier : 1f);
        public float RunSpeed => walkSpeed * runMultiplier;
        public bool IsOverloaded => inventory != null && inventory.IsOverloaded;
        public bool CanRun => !IsOverloaded;
        public bool CanJump => !IsOverloaded;
        public float RunMultiplier => runMultiplier;
        public float JumpHeight => jumpHeight;
        public float Gravity => gravity;
        public float JumpVelocity => Mathf.Sqrt(2f * gravity * jumpHeight);
        public float MeasuredSpeed => _measuredSpeed;
        public bool IsGrounded => _characterController != null && _characterController.isGrounded;
        public bool IsRunning { get; private set; }
        public bool IsCrouching { get; private set; }
        public bool IsThirdPerson => _thirdPerson;
        public float VerticalVelocity => _verticalVelocity;
        public Vector2 MoveInput => _moveInput;

        public void ConfigureViewRig(Transform pivot, Camera camera)
        {
            viewPivot = pivot;
            playerCamera = camera;
            if (viewPivot != null && viewPivot != transform && !viewPivot.IsChildOf(transform))
                viewPivot.SetParent(transform, true);
            ApplyCameraMode();
        }

        public void SetCameraMode(bool thirdPerson)
        {
            if (_thirdPerson == thirdPerson) return;
            _thirdPerson = thirdPerson;
            if (_visualRig == null) _visualRig = GetComponentInChildren<PlayerVisualRig>(true);
            if (_visualRig != null) _visualRig.SetThirdPerson(_thirdPerson);
            ApplyCameraMode();
        }

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            if (inventory == null) inventory = GetComponent<PlayerInventory>();
            _animationDriver = GetComponent<PlayerAnimationDriver>();
            if (_animationDriver == null) _animationDriver = gameObject.AddComponent<PlayerAnimationDriver>();
            _visualRig = GetComponentInChildren<PlayerVisualRig>(true);
            if (_visualRig == null) _visualRig = gameObject.AddComponent<PlayerVisualRig>();
            if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>(true);
            if (viewPivot == null && playerCamera != null) viewPivot = playerCamera.transform.parent;
            _currentHeight = characterHeight;
            ApplyCharacterDimensions();
            SetCursorLocked(playerCamera != null);
            _visualRig.SetThirdPerson(_thirdPerson);
            ApplyCameraMode();
        }

        private void OnValidate()
        {
            characterHeight = Mathf.Max(capsuleRadius * 2f, characterHeight);
            walkSpeed = Mathf.Max(0.1f, walkSpeed);
            runMultiplier = Mathf.Max(1f, runMultiplier);
            jumpHeight = Mathf.Max(0.05f, jumpHeight);
            gravity = Mathf.Max(0.1f, gravity);
            crouchHeight = Mathf.Clamp(crouchHeight, capsuleRadius * 2f, characterHeight);
            capsuleRadius = Mathf.Max(0.1f, capsuleRadius);
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            var gamepad = Gamepad.current;

            if (keyboard != null)
            {
                if (keyboard.escapeKey.wasPressedThisFrame) SetCursorLocked(false);
                else if (mouse != null && mouse.leftButton.wasPressedThisFrame && !_cursorLocked) SetCursorLocked(true);
                if (keyboard.f5Key.wasPressedThisFrame) SetCameraMode(!_thirdPerson);
            }

            if (gamepad != null && gamepad.buttonNorth.wasPressedThisFrame)
                SetCameraMode(!_thirdPerson);

            if (_cursorLocked) ReadLook(mouse, gamepad);
            ReadMovementInput(keyboard, gamepad);
            UpdateStance(keyboard, gamepad);
            MoveCharacter(keyboard, gamepad);
            UpdateCamera();
            UpdateAnimator();
        }

        private void ReadLook(Mouse mouse, Gamepad gamepad)
        {
            var look = Vector2.zero;
            if (mouse != null) look += mouse.delta.ReadValue() * mouseSensitivity;
            if (gamepad != null) look += gamepad.rightStick.ReadValue() * gamepadLookSensitivity * Time.deltaTime;
            transform.Rotate(Vector3.up, look.x, Space.World);
            _pitch = Mathf.Clamp(_pitch - look.y, -maxLookAngle, maxLookAngle);
            if (viewPivot != null) viewPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        private void ReadMovementInput(Keyboard keyboard, Gamepad gamepad)
        {
            var input = Vector2.zero;
            if (keyboard != null)
            {
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) input.x -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) input.x += 1f;
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) input.y += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) input.y -= 1f;
            }
            if (gamepad != null && gamepad.leftStick.ReadValue().sqrMagnitude > input.sqrMagnitude)
                input = gamepad.leftStick.ReadValue();
            _moveInput = Vector2.ClampMagnitude(input, 1f);
        }

        private void UpdateStance(Keyboard keyboard, Gamepad gamepad)
        {
            var crouch = keyboard != null && keyboard.cKey.isPressed ||
                         gamepad != null && gamepad.buttonEast.isPressed;
            var target = crouch ? crouchHeight : characterHeight;
            if (!crouch && !CanStand()) target = crouchHeight;
            _currentHeight = Mathf.MoveTowards(_currentHeight, target, stanceChangeSpeed * Time.deltaTime);
            IsCrouching = _currentHeight < characterHeight - 0.04f;
            ApplyCharacterDimensions();
        }

        private bool CanStand()
        {
            if (_currentHeight >= characterHeight - 0.01f) return true;
            var bottom = transform.position + Vector3.up * capsuleRadius;
            var top = transform.position + Vector3.up * (characterHeight - capsuleRadius);
            foreach (var hit in Physics.OverlapCapsule(bottom, top, capsuleRadius * 0.95f,
                         ~0, QueryTriggerInteraction.Ignore))
                if (hit != null && hit != _characterController && !hit.transform.IsChildOf(transform))
                    return false;
            return true;
        }

        private void ApplyCharacterDimensions()
        {
            if (_characterController == null) return;
            _characterController.height = Mathf.Max(capsuleRadius * 2f, _currentHeight);
            _characterController.radius = capsuleRadius;
            _characterController.center = Vector3.up * (_characterController.height * 0.5f);
            _characterController.stepOffset = Mathf.Min(0.32f, _characterController.height * 0.18f);
            _characterController.slopeLimit = 48f;
            _characterController.minMoveDistance = 0f;
        }

        private void MoveCharacter(Keyboard keyboard, Gamepad gamepad)
        {
            var dt = Time.deltaTime;
            if (dt <= 0f) return;
            var grounded = _characterController.isGrounded;
            if (grounded) _lastGroundedTime = Time.time;
            var jumpPressed = keyboard != null && keyboard.spaceKey.wasPressedThisFrame ||
                              gamepad != null && gamepad.buttonSouth.wasPressedThisFrame;
            if (IsOverloaded)
                _jumpBufferedUntil = float.NegativeInfinity;
            else if (jumpPressed)
                _jumpBufferedUntil = Time.time + jumpBufferTime;

            if (!IsOverloaded && Time.time <= _jumpBufferedUntil &&
                Time.time - _lastGroundedTime <= coyoteTime)
            {
                _verticalVelocity = JumpVelocity;
                _jumpBufferedUntil = float.NegativeInfinity;
                _lastGroundedTime = float.NegativeInfinity;
            }
            else if (grounded && _verticalVelocity < 0f) _verticalVelocity = -2f;

            _verticalVelocity -= gravity * dt;
            var moving = _moveInput.sqrMagnitude > 0.001f;
            var runHeld = keyboard != null && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed);
            IsRunning = moving && runHeld && !IsCrouching && CanRun;
            var speed = IsCrouching
                ? EffectiveWalkSpeed * crouchSpeedMultiplier
                : IsRunning ? RunSpeed : EffectiveWalkSpeed;
            var forward = _thirdPerson && playerCamera != null
                ? Vector3.ProjectOnPlane(playerCamera.transform.forward, Vector3.up).normalized : transform.forward;
            var right = _thirdPerson && playerCamera != null
                ? Vector3.ProjectOnPlane(playerCamera.transform.right, Vector3.up).normalized : transform.right;
            var direction = Vector3.ClampMagnitude(right * _moveInput.x + forward * _moveInput.y, 1f);
            var targetVelocity = direction * speed;
            var rate = moving ? acceleration : deceleration;
            var control = grounded ? 1f : airControl;
            _horizontalVelocity = Vector3.MoveTowards(_horizontalVelocity, targetVelocity, rate * control * dt);

            if (_thirdPerson && moving && direction.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction),
                    1f - Mathf.Exp(-12f * dt));

            _characterController.Move((_horizontalVelocity + Vector3.up * _verticalVelocity) * dt);
            _measuredSpeed = new Vector3(_characterController.velocity.x, 0f, _characterController.velocity.z).magnitude;
            if (_characterController.isGrounded && _verticalVelocity < 0f) _verticalVelocity = -2f;
        }

        private void UpdateCamera()
        {
            if (viewPivot == null || playerCamera == null) return;
            viewPivot.localPosition = Vector3.up * (IsCrouching ? 0.98f : 1.62f);
            var target = _thirdPerson ? new Vector3(0f, 0.12f, -thirdPersonDistance) : Vector3.zero;
            if (_thirdPerson)
            {
                var origin = viewPivot.position;
                var desired = viewPivot.TransformPoint(target);
                var ray = desired - origin;
                if (ray.sqrMagnitude > 0.001f && Physics.SphereCast(origin, cameraCollisionRadius, ray.normalized,
                        out var hit, ray.magnitude, cameraCollisionLayers, QueryTriggerInteraction.Ignore) &&
                    hit.collider != _characterController && !hit.transform.IsChildOf(transform))
                    target = viewPivot.InverseTransformPoint(hit.point + hit.normal * 0.18f);
            }
            playerCamera.transform.localPosition = Vector3.Lerp(playerCamera.transform.localPosition, target,
                1f - Mathf.Exp(-14f * Time.deltaTime));
            playerCamera.transform.localRotation = Quaternion.identity;
            playerCamera.nearClipPlane = 0.04f;

        }

        private void UpdateAnimator()
        {
            if (_animationDriver == null) _animationDriver = GetComponent<PlayerAnimationDriver>();
            if (_animationDriver == null) return;
            var grounded = _characterController.isGrounded;
            _animationDriver.Apply(_measuredSpeed, _moveInput, grounded, _verticalVelocity,
                IsRunning, IsCrouching, !grounded && _verticalVelocity > 0.05f,
                !grounded && _verticalVelocity <= 0.05f);
        }

        private void ApplyCameraMode()
        {
            if (playerCamera == null || viewPivot == null) return;
            playerCamera.nearClipPlane = 0.04f;
            playerCamera.transform.localPosition = _thirdPerson
                ? new Vector3(0f, 0.12f, -thirdPersonDistance) : Vector3.zero;
            playerCamera.transform.localRotation = Quaternion.identity;
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