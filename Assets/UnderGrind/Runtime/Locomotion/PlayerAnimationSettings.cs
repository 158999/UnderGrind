using UnityEngine;

namespace UnderGrind.Locomotion
{
    public enum PlayerAnimationRigType
    {
        Humanoid,
        Generic
    }

    [CreateAssetMenu(menuName = "UnderGrind/Player/Animation Settings", fileName = "PlayerAnimationSettings")]
    public sealed class PlayerAnimationSettings : ScriptableObject
    {
        [SerializeField, HideInInspector] private int settingsVersion;

        [Header("Controller e clip")]
        [SerializeField] private RuntimeAnimatorController animatorController;
        [SerializeField] private AnimationClip idleClip;
        [SerializeField] private AnimationClip walkClip;
        [SerializeField] private AnimationClip runClip;
        [SerializeField] private AnimationClip jumpClip;
        [SerializeField] private AnimationClip fallClip;
        [SerializeField] private AnimationClip landClip;
        [SerializeField] private AnimationClip crouchIdleClip;
        [SerializeField] private AnimationClip crouchWalkClip;

        [Header("Ciclo delle clip")]
        [SerializeField] private bool idleLoops;
        [SerializeField] private bool walkLoops;
        [SerializeField] private bool runLoops;
        [SerializeField] private bool jumpLoops;
        [SerializeField] private bool fallLoops;
        [SerializeField] private bool landLoops;
        [SerializeField] private bool crouchIdleLoops;
        [SerializeField] private bool crouchWalkLoops;

        [Header("Importazione FBX")]
        [SerializeField] private bool configureFbxImports = true;
        [SerializeField] private PlayerAnimationRigType fbxRigType = PlayerAnimationRigType.Humanoid;
        [SerializeField] private bool createAvatarFromFbx = true;
        [SerializeField] private bool importAnimationClips = true;
        [SerializeField] private bool importMaterials;
        [SerializeField] private bool importCameras;
        [SerializeField] private bool importLights;
        [SerializeField] private bool optimizeFbxGameObjects;

        [Header("Parametri Animator")]
        [SerializeField] private string speedParameterName;
        [SerializeField] private string moveXParameterName;
        [SerializeField] private string moveYParameterName;
        [SerializeField] private string groundedParameterName;
        [SerializeField] private string verticalSpeedParameterName;
        [SerializeField] private string runningParameterName;
        [SerializeField] private string crouchingParameterName;
        [SerializeField] private string jumpingParameterName;
        [SerializeField] private string fallingParameterName;
        [SerializeField] private string landedParameterName;

        [Header("Soglie e transizioni")]
        [SerializeField, Min(0f)] private float idleThreshold;
        [SerializeField, Min(0f)] private float walkThreshold;
        [SerializeField, Min(0f)] private float runThreshold;
        [SerializeField, Min(0f)] private float crouchWalkThreshold;
        [SerializeField, Min(0f)] private float speedDampTime;
        [SerializeField, Min(0f)] private float transitionDuration;
        [SerializeField, Range(0f, 1f)] private float landingExitTime;

        public bool IdleLoops => idleLoops;
        public bool WalkLoops => walkLoops;
        public bool RunLoops => runLoops;
        public bool JumpLoops => jumpLoops;
        public bool FallLoops => fallLoops;
        public bool LandLoops => landLoops;
        public bool CrouchIdleLoops => crouchIdleLoops;
        public bool CrouchWalkLoops => crouchWalkLoops;
        public int SettingsVersion => settingsVersion;
        public RuntimeAnimatorController AnimatorController => animatorController;
        public AnimationClip IdleClip => idleClip;
        public AnimationClip WalkClip => walkClip;
        public AnimationClip RunClip => runClip;
        public AnimationClip JumpClip => jumpClip;
        public AnimationClip FallClip => fallClip;
        public AnimationClip LandClip => landClip;
        public AnimationClip CrouchIdleClip => crouchIdleClip;
        public AnimationClip CrouchWalkClip => crouchWalkClip;
        public bool ConfigureFbxImports => configureFbxImports;
        public PlayerAnimationRigType FbxRigType => fbxRigType;
        public bool CreateAvatarFromFbx => createAvatarFromFbx;
        public bool ImportAnimationClips => importAnimationClips;
        public bool ImportMaterials => importMaterials;
        public bool ImportCameras => importCameras;
        public bool ImportLights => importLights;
        public bool OptimizeFbxGameObjects => optimizeFbxGameObjects;
        public string SpeedParameterName => speedParameterName;
        public string MoveXParameterName => moveXParameterName;
        public string MoveYParameterName => moveYParameterName;
        public string GroundedParameterName => groundedParameterName;
        public string VerticalSpeedParameterName => verticalSpeedParameterName;
        public string RunningParameterName => runningParameterName;
        public string CrouchingParameterName => crouchingParameterName;
        public string JumpingParameterName => jumpingParameterName;
        public string FallingParameterName => fallingParameterName;
        public string LandedParameterName => landedParameterName;
        public float IdleThreshold => idleThreshold;
        public float WalkThreshold => walkThreshold;
        public float RunThreshold => runThreshold;
        public float CrouchWalkThreshold => crouchWalkThreshold;
        public float SpeedDampTime => speedDampTime;
        public float TransitionDuration => transitionDuration;
        public float LandingExitTime => landingExitTime;

        private void OnValidate()
        {
            idleThreshold = Mathf.Max(0f, idleThreshold);
            walkThreshold = Mathf.Max(idleThreshold, walkThreshold);
            runThreshold = Mathf.Max(walkThreshold, runThreshold);
            crouchWalkThreshold = Mathf.Max(0f, crouchWalkThreshold);
            speedDampTime = Mathf.Max(0f, speedDampTime);
            transitionDuration = Mathf.Max(0f, transitionDuration);
            landingExitTime = Mathf.Clamp01(landingExitTime);
        }
    }
}