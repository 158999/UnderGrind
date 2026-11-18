using UnityEngine;
using UnityEngine.InputSystem;
using UnderGrind.Inventory;

namespace UnderGrind.Locomotion
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class HumanLocomotionController : MonoBehaviour
    {
        [Header("Configurazione")]
        [SerializeField] private PlayerLocomotionSettings settings;
        [SerializeField] private PlayerInputSettings inputSettings;
        [SerializeField] private PlayerInventory inventory;

        [Header("Riferimenti visuale")]
        [SerializeField] private Transform viewPivot;
        [SerializeField] private Camera playerCamera;

        private CharacterController _characterController;
        private PlayerAnimationDriver _animationDriver;
        private PlayerVisualRig _visualRig;
        private InputActionMap _inputMap;
        private InputAction _moveAction;
        private InputAction _lookMouseAction;
        private InputAction _lookGamepadAction;
        private InputAction _jumpAction;
        private InputAction _runAction;
        private InputAction _crouchAction;
        private InputAction _toggleCameraAction;
        private InputAction _cancelAction;
        private InputAction _captureCursorAction;
        private float _verticalVelocity;
        private float _pitch;
        private float _currentHeight;
        private float _lastGroundedTime = float.NegativeInfinity;
        private float _jumpBufferedUntil = float.NegativeInfinity;
        private bool _thirdPerson;
        private bool _cursorLocked;
        private bool _missingSettingsReported;
        private bool _missingInputReported;
        private Vector3 _horizontalVelocity;
        private float _measuredSpeed;
        private Vector2 _moveInput;

        public PlayerLocomotionSettings Settings => settings;
        public PlayerInputSettings InputSettings => inputSettings;
        public PlayerInventory Inventory => inventory;
        public float CharacterHeight => settings != null ? settings.CharacterHeight : 0f;
        public float WalkSpeed => settings != null ? settings.WalkSpeed : 0f;
        public float EffectiveWalkSpeed => settings != null
            ? settings.WalkSpeed * (IsOverloaded ? inventory.OverloadedWalkSpeedMultiplier : 1f) : 0f;
        public float RunSpeed => settings != null ? settings.RunSpeed : 0f;
        public bool IsOverloaded => inventory != null && inventory.IsOverloaded;
        public float InventoryWeightKg => inventory != null ? inventory.CurrentWeightKg : 0f;
        public float InventoryWeightCapacityKg => inventory != null ? inventory.MaxWeightKg : 0f;
        public bool CanRun => !IsOverloaded;
        public bool CanJump => !IsOverloaded;
        public float RunMultiplier => settings != null ? settings.RunMultiplier : 0f;
        public float JumpHeight => settings != null ? settings.JumpHeight : 0f;
        public float Gravity => settings != null ? settings.Gravity : 0f;
        public float JumpVelocity => settings != null ? settings.JumpVelocity : 0f;
        public float MeasuredSpeed => _measuredSpeed;
        public bool IsGrounded => _characterController != null && _characterController.isGrounded;
        public bool IsRunning { get; private set; }
        public bool IsCrouching { get; private set; }
        public bool IsThirdPerson => _thirdPerson;
        public bool IsCursorCaptured => _cursorLocked;
        public float VerticalVelocity => _verticalVelocity;
        public Vector2 MoveInput => _moveInput;

        public void ConfigureSettings(PlayerLocomotionSettings value)
        {
            settings = value;
            if (settings == null) return;
            _currentHeight = settings.CharacterHeight;
            ApplyCharacterDimensions();
            ApplyCameraMode();
        }

        public void ConfigureInputSettings(PlayerInputSettings value)
        {
            if (_inputMap != null) _inputMap.Disable();
            inputSettings = value;
            BindInputActions();
            if (inputSettings != null && inputSettings.LockCursorOnStart && playerCamera != null)
                SetCursorLocked(true);
        }

        public void ConfigureViewRig(Transform pivot, Camera camera)
        {
            viewPivot = pivot;
            playerCamera = camera;
            if (viewPivot != null && viewPivot != transform && !viewPivot.IsChildOf(transform))
                viewPivot.SetParent(transform, true);
            ApplyCameraMode();
        }

        public void SetCursorCaptured(bool captured)
        {
            SetCursorLocked(captured);
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
            if (settings != null) _currentHeight = settings.CharacterHeight;
            ApplyCharacterDimensions();
            BindInputActions();
            SetCursorLocked(inputSettings != null && inputSettings.LockCursorOnStart && playerCamera != null);
            _visualRig.SetThirdPerson(_thirdPerson);
            ApplyCameraMode();
        }

        private void OnEnable()
        {
            if (_inputMap != null) _inputMap.Enable();
        }

        private void Update()
        {
            if (settings == null)
            {
                if (!_missingSettingsReported)
                {
                    Debug.LogError("UnderGrind: assegna un asset PlayerLocomotionSettings al componente HumanLocomotionController.", this);
                    _missingSettingsReported = true;
                }
                return;
            }

            if (_inputMap == null)
            {
                if (!_missingInputReported && inputSettings != null)
                {
                    Debug.LogError("UnderGrind: la configurazione Input Actions non contiene l'action map o le azioni richieste.", this);
                    _missingInputReported = true;
                }
                _moveInput = Vector2.zero;
                IsRunning = false;
            }
            else
            {
                if (_cancelAction != null && _cancelAction.WasPressedThisFrame())
                    SetCursorLocked(false);
                else if (!_cursorLocked && inputSettings != null && inputSettings.RecaptureCursorOnClick &&
                         _captureCursorAction != null && _captureCursorAction.WasPressedThisFrame())
                    SetCursorLocked(true);

                if (_toggleCameraAction != null && _toggleCameraAction.WasPressedThisFrame())
                    SetCameraMode(!_thirdPerson);

                if (_cursorLocked) ReadLook();
                ReadMovementInput();
                UpdateStance();
            }

            MoveCharacter();
            UpdateCamera();
            UpdateAnimator();
        }

        private void BindInputActions()
        {
            _inputMap = inputSettings != null ? inputSettings.FindActionMap() : null;
            _moveAction = inputSettings != null ? inputSettings.GetAction(PlayerInputActionId.Move) : null;
            _lookMouseAction = inputSettings != null ? inputSettings.GetAction(PlayerInputActionId.LookMouse) : null;
            _lookGamepadAction = inputSettings != null ? inputSettings.GetAction(PlayerInputActionId.LookGamepad) : null;
            _jumpAction = inputSettings != null ? inputSettings.GetAction(PlayerInputActionId.Jump) : null;
            _runAction = inputSettings != null ? inputSettings.GetAction(PlayerInputActionId.Run) : null;
            _crouchAction = inputSettings != null ? inputSettings.GetAction(PlayerInputActionId.Crouch) : null;
            _toggleCameraAction = inputSettings != null ? inputSettings.GetAction(PlayerInputActionId.ToggleCamera) : null;
            _cancelAction = inputSettings != null ? inputSettings.GetAction(PlayerInputActionId.Cancel) : null;
            _captureCursorAction = inputSettings != null ? inputSettings.GetAction(PlayerInputActionId.CaptureCursor) : null;
            if (_inputMap != null && isActiveAndEnabled) _inputMap.Enable();
        }

        private void ReadLook()
        {
            var look = Vector2.zero;
            if (_lookMouseAction != null)
                look += _lookMouseAction.ReadValue<Vector2>() * settings.MouseSensitivity;
            if (_lookGamepadAction != null)
                look += _lookGamepadAction.ReadValue<Vector2>() * settings.GamepadLookSensitivity * Time.deltaTime;
            transform.Rotate(Vector3.up, look.x, Space.World);
            _pitch = Mathf.Clamp(_pitch - look.y, -settings.MaxLookAngle, settings.MaxLookAngle);
            if (viewPivot != null) viewPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        private void ReadMovementInput()
        {
            _moveInput = _moveAction != null
                ? Vector2.ClampMagnitude(_moveAction.ReadValue<Vector2>(), 1f)
                : Vector2.zero;
        }

        private void UpdateStance()
        {
            var crouch = _crouchAction != null && _crouchAction.IsPressed();
            var target = crouch ? settings.CrouchHeight : settings.CharacterHeight;
            if (!crouch && !CanStand()) target = settings.CrouchHeight;
            _currentHeight = Mathf.MoveTowards(_currentHeight, target, settings.StanceChangeSpeed * Time.deltaTime);
            IsCrouching = _currentHeight < settings.CharacterHeight - settings.StandingClearance;
            ApplyCharacterDimensions();
        }

        private bool CanStand()
        {
            if (_currentHeight >= settings.CharacterHeight - settings.StandingClearance) return true;
            var bottom = transform.position + Vector3.up * settings.CapsuleRadius;
            var top = transform.position + Vector3.up * (settings.CharacterHeight - settings.CapsuleRadius);
            foreach (var hit in Physics.OverlapCapsule(bottom, top,
                         settings.CapsuleRadius * settings.StanceOverlapRadiusMultiplier,
                         settings.CharacterCollisionLayers, QueryTriggerInteraction.Ignore))
                if (hit != null && hit != _characterController && !hit.transform.IsChildOf(transform))
                    return false;
            return true;
        }

        private void ApplyCharacterDimensions()
        {
            if (_characterController == null || settings == null) return;
            _characterController.height = Mathf.Max(settings.CapsuleRadius * 2f, _currentHeight);
            _characterController.radius = settings.CapsuleRadius;
            _characterController.center = Vector3.up * (_characterController.height * 0.5f);
            _characterController.stepOffset = Mathf.Min(settings.CharacterStepOffset,
                _characterController.height * settings.CharacterStepOffsetHeightRatio);
            _characterController.slopeLimit = settings.CharacterSlopeLimit;
            _characterController.minMoveDistance = settings.CharacterMinMoveDistance;
            _characterController.skinWidth = settings.CharacterSkinWidth;
            _characterController.detectCollisions = settings.CharacterDetectCollisions;
            _characterController.enableOverlapRecovery = settings.CharacterEnableOverlapRecovery;
        }

        private void MoveCharacter()
        {
            var dt = Time.deltaTime;
            if (dt <= 0f || _characterController == null) return;
            var grounded = _characterController.isGrounded;
            if (grounded) _lastGroundedTime = Time.time;
            var jumpPressed = _jumpAction != null && _jumpAction.WasPressedThisFrame();
            if (!CanJump)
                _jumpBufferedUntil = float.NegativeInfinity;
            else if (jumpPressed)
                _jumpBufferedUntil = Time.time + settings.JumpBufferTime;

            if (CanJump && Time.time <= _jumpBufferedUntil &&
                Time.time - _lastGroundedTime <= settings.CoyoteTime)
            {
                _verticalVelocity = settings.JumpVelocity;
                _jumpBufferedUntil = float.NegativeInfinity;
                _lastGroundedTime = float.NegativeInfinity;
            }
            else if (grounded && _verticalVelocity < 0f)
                _verticalVelocity = -settings.GroundedStickVelocity;

            _verticalVelocity -= settings.Gravity * dt;
            var moving = _moveInput.sqrMagnitude > settings.MovementInputThreshold;
            var runHeld = _runAction != null && _runAction.IsPressed();
            IsRunning = moving && runHeld && !IsCrouching && CanRun;
            var speed = IsCrouching
                ? EffectiveWalkSpeed * settings.CrouchSpeedMultiplier
                : IsRunning ? RunSpeed : EffectiveWalkSpeed;
            var forward = _thirdPerson && playerCamera != null
                ? Vector3.ProjectOnPlane(playerCamera.transform.forward, Vector3.up).normalized : transform.forward;
            var right = _thirdPerson && playerCamera != null
                ? Vector3.ProjectOnPlane(playerCamera.transform.right, Vector3.up).normalized : transform.right;
            var direction = Vector3.ClampMagnitude(right * _moveInput.x + forward * _moveInput.y, 1f);
            var targetVelocity = direction * speed;
            var rate = moving ? settings.Acceleration : settings.Deceleration;
            var control = grounded ? 1f : settings.AirControl;
            _horizontalVelocity = Vector3.MoveTowards(_horizontalVelocity, targetVelocity, rate * control * dt);
            if (IsOverloaded && _horizontalVelocity.sqrMagnitude > EffectiveWalkSpeed * EffectiveWalkSpeed)
                _horizontalVelocity = _horizontalVelocity.normalized * EffectiveWalkSpeed;

            if (_thirdPerson && moving && direction.sqrMagnitude > settings.DirectionThreshold)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction),
                    1f - Mathf.Exp(-settings.ThirdPersonRotationSharpness * dt));

            _characterController.Move((_horizontalVelocity + Vector3.up * _verticalVelocity) * dt);
            _measuredSpeed = new Vector3(_characterController.velocity.x, 0f, _characterController.velocity.z).magnitude;
            if (_characterController.isGrounded && _verticalVelocity < 0f)
                _verticalVelocity = -settings.GroundedStickVelocity;
        }

        private void UpdateCamera()
        {
            if (viewPivot == null || playerCamera == null || settings == null) return;
            viewPivot.localPosition = Vector3.up * (IsCrouching
                ? settings.CrouchingViewHeight : settings.StandingViewHeight);
            var target = _thirdPerson
                ? new Vector3(settings.ThirdPersonCameraOffset.x,
                    settings.ThirdPersonCameraOffset.y, -settings.ThirdPersonDistance + settings.ThirdPersonCameraOffset.z)
                : Vector3.zero;
            if (_thirdPerson)
            {
                var origin = viewPivot.position;
                var desired = viewPivot.TransformPoint(target);
                var ray = desired - origin;
                if (ray.sqrMagnitude > settings.DirectionThreshold &&
                    Physics.SphereCast(origin, settings.CameraCollisionRadius, ray.normalized,
                        out var hit, ray.magnitude, settings.CameraCollisionLayers, QueryTriggerInteraction.Ignore) &&
                    hit.collider != _characterController && !hit.transform.IsChildOf(transform))
                    target = viewPivot.InverseTransformPoint(hit.point +
                        hit.normal * settings.CameraCollisionSurfaceOffset);
            }
            playerCamera.transform.localPosition = Vector3.Lerp(playerCamera.transform.localPosition, target,
                1f - Mathf.Exp(-settings.CameraFollowSharpness * Time.deltaTime));
            playerCamera.transform.localRotation = Quaternion.identity;
        }

        private void UpdateAnimator()
        {
            if (_animationDriver == null) _animationDriver = GetComponent<PlayerAnimationDriver>();
            if (_animationDriver == null || settings == null || _characterController == null) return;
            var grounded = _characterController.isGrounded;
            _animationDriver.Apply(_measuredSpeed, _moveInput, grounded, _verticalVelocity,
                IsRunning, IsCrouching,
                !grounded && _verticalVelocity > settings.JumpApexStateThreshold,
                !grounded && _verticalVelocity <= settings.JumpApexStateThreshold);
        }

        private void ApplyCameraMode()
        {
            if (playerCamera == null || viewPivot == null || settings == null) return;
            playerCamera.nearClipPlane = settings.CameraNearClipPlane;
            playerCamera.fieldOfView = settings.CameraFieldOfView;
            playerCamera.transform.localPosition = _thirdPerson
                ? new Vector3(settings.ThirdPersonCameraOffset.x, settings.ThirdPersonCameraOffset.y,
                    -settings.ThirdPersonDistance + settings.ThirdPersonCameraOffset.z)
                : Vector3.zero;
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
            if (_inputMap != null) _inputMap.Disable();
            SetCursorLocked(false);
        }
    }
}