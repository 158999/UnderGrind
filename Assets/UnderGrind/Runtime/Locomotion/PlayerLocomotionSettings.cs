using UnityEngine;

namespace UnderGrind.Locomotion
{
    [CreateAssetMenu(menuName = "UnderGrind/Player/Locomotion Settings", fileName = "PlayerLocomotionSettings")]
    public sealed class PlayerLocomotionSettings : ScriptableObject
    {
        [Header("Antropometria e movimento")]
        [SerializeField, Min(0.1f)] private float characterHeight;
        [SerializeField, Min(0.1f)] private float walkSpeed;
        [SerializeField, Min(1f)] private float runMultiplier;
        [SerializeField, Min(0.01f)] private float jumpHeight;
        [SerializeField, Min(0.01f)] private float gravity;

        [Header("Accelerazione e controllo")]
        [SerializeField, Min(0f)] private float acceleration;
        [SerializeField, Min(0f)] private float deceleration;
        [SerializeField, Range(0f, 1f)] private float airControl;
        [SerializeField, Min(0f)] private float coyoteTime;
        [SerializeField, Min(0f)] private float jumpBufferTime;
        [SerializeField, Min(0f)] private float groundedStickVelocity;
        [SerializeField, Min(0f)] private float movementInputThreshold;
        [SerializeField, Min(0f)] private float directionThreshold;
        [SerializeField, Min(0f)] private float jumpApexStateThreshold;

        [Header("Accovacciamento e capsula")]
        [SerializeField, Min(0.1f)] private float crouchHeight;
        [SerializeField, Range(0.01f, 1f)] private float crouchSpeedMultiplier;
        [SerializeField, Min(0.01f)] private float stanceChangeSpeed;
        [SerializeField, Min(0.05f)] private float capsuleRadius;
        [SerializeField, Min(0f)] private float standingClearance;
        [SerializeField, Range(0.01f, 1f)] private float stanceOverlapRadiusMultiplier;
        [SerializeField] private LayerMask characterCollisionLayers;
        [SerializeField, Min(0f)] private float characterStepOffset;
        [SerializeField, Range(0f, 90f)] private float characterSlopeLimit;
        [SerializeField, Min(0f)] private float characterMinMoveDistance;

        [Header("Camera e visuale")]
        [SerializeField, Min(0.001f)] private float mouseSensitivity;
        [SerializeField, Min(0.001f)] private float gamepadLookSensitivity;
        [SerializeField, Range(1f, 89f)] private float maxLookAngle;
        [SerializeField, Min(0f)] private float standingViewHeight;
        [SerializeField, Min(0f)] private float crouchingViewHeight;
        [SerializeField] private Vector3 thirdPersonCameraOffset;
        [SerializeField, Min(0.1f)] private float thirdPersonDistance;
        [SerializeField, Min(0.001f)] private float cameraCollisionRadius;
        [SerializeField] private LayerMask cameraCollisionLayers;
        [SerializeField, Min(0.001f)] private float cameraFollowSharpness;
        [SerializeField, Min(0.001f)] private float thirdPersonRotationSharpness;
        [SerializeField, Min(0.001f)] private float cameraNearClipPlane;
        [SerializeField, Range(1f, 179f)] private float cameraFieldOfView;

        public float CharacterHeight => characterHeight;
        public float WalkSpeed => walkSpeed;
        public float RunMultiplier => runMultiplier;
        public float JumpHeight => jumpHeight;
        public float Gravity => gravity;
        public float Acceleration => acceleration;
        public float Deceleration => deceleration;
        public float AirControl => airControl;
        public float CoyoteTime => coyoteTime;
        public float JumpBufferTime => jumpBufferTime;
        public float GroundedStickVelocity => groundedStickVelocity;
        public float MovementInputThreshold => movementInputThreshold;
        public float DirectionThreshold => directionThreshold;
        public float JumpApexStateThreshold => jumpApexStateThreshold;
        public float CrouchHeight => crouchHeight;
        public float CrouchSpeedMultiplier => crouchSpeedMultiplier;
        public float StanceChangeSpeed => stanceChangeSpeed;
        public float CapsuleRadius => capsuleRadius;
        public float StandingClearance => standingClearance;
        public float StanceOverlapRadiusMultiplier => stanceOverlapRadiusMultiplier;
        public LayerMask CharacterCollisionLayers => characterCollisionLayers;
        public float CharacterStepOffset => characterStepOffset;
        public float CharacterSlopeLimit => characterSlopeLimit;
        public float CharacterMinMoveDistance => characterMinMoveDistance;
        public float MouseSensitivity => mouseSensitivity;
        public float GamepadLookSensitivity => gamepadLookSensitivity;
        public float MaxLookAngle => maxLookAngle;
        public float StandingViewHeight => standingViewHeight;
        public float CrouchingViewHeight => crouchingViewHeight;
        public Vector3 ThirdPersonCameraOffset => thirdPersonCameraOffset;
        public float ThirdPersonDistance => thirdPersonDistance;
        public float CameraCollisionRadius => cameraCollisionRadius;
        public LayerMask CameraCollisionLayers => cameraCollisionLayers;
        public float CameraFollowSharpness => cameraFollowSharpness;
        public float ThirdPersonRotationSharpness => thirdPersonRotationSharpness;
        public float CameraNearClipPlane => cameraNearClipPlane;
        public float CameraFieldOfView => cameraFieldOfView;
        public float RunSpeed => walkSpeed * runMultiplier;
        public float JumpVelocity => Mathf.Sqrt(2f * gravity * jumpHeight);

        private void OnValidate()
        {
            characterHeight = Mathf.Max(capsuleRadius * 2f, characterHeight);
            walkSpeed = Mathf.Max(0.1f, walkSpeed);
            runMultiplier = Mathf.Max(1f, runMultiplier);
            jumpHeight = Mathf.Max(0.01f, jumpHeight);
            gravity = Mathf.Max(0.01f, gravity);
            acceleration = Mathf.Max(0f, acceleration);
            deceleration = Mathf.Max(0f, deceleration);
            airControl = Mathf.Clamp01(airControl);
            coyoteTime = Mathf.Max(0f, coyoteTime);
            jumpBufferTime = Mathf.Max(0f, jumpBufferTime);
            groundedStickVelocity = Mathf.Max(0f, groundedStickVelocity);
            movementInputThreshold = Mathf.Max(0f, movementInputThreshold);
            directionThreshold = Mathf.Max(0f, directionThreshold);
            jumpApexStateThreshold = Mathf.Max(0f, jumpApexStateThreshold);
            capsuleRadius = Mathf.Max(0.05f, capsuleRadius);
            crouchHeight = Mathf.Clamp(crouchHeight, capsuleRadius * 2f, characterHeight);
            crouchSpeedMultiplier = Mathf.Clamp01(crouchSpeedMultiplier);
            stanceChangeSpeed = Mathf.Max(0.01f, stanceChangeSpeed);
            standingClearance = Mathf.Max(0f, standingClearance);
            stanceOverlapRadiusMultiplier = Mathf.Clamp01(stanceOverlapRadiusMultiplier);
            characterStepOffset = Mathf.Max(0f, characterStepOffset);
            characterSlopeLimit = Mathf.Clamp(characterSlopeLimit, 0f, 90f);
            characterMinMoveDistance = Mathf.Max(0f, characterMinMoveDistance);
            mouseSensitivity = Mathf.Max(0.001f, mouseSensitivity);
            gamepadLookSensitivity = Mathf.Max(0.001f, gamepadLookSensitivity);
            maxLookAngle = Mathf.Clamp(maxLookAngle, 1f, 89f);
            standingViewHeight = Mathf.Max(0f, standingViewHeight);
            crouchingViewHeight = Mathf.Max(0f, crouchingViewHeight);
            thirdPersonDistance = Mathf.Max(0.1f, thirdPersonDistance);
            cameraCollisionRadius = Mathf.Max(0.001f, cameraCollisionRadius);
            cameraFollowSharpness = Mathf.Max(0.001f, cameraFollowSharpness);
            thirdPersonRotationSharpness = Mathf.Max(0.001f, thirdPersonRotationSharpness);
            cameraNearClipPlane = Mathf.Max(0.001f, cameraNearClipPlane);
            cameraFieldOfView = Mathf.Clamp(cameraFieldOfView, 1f, 179f);
        }
    }
}