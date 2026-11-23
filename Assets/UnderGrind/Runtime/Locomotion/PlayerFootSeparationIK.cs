using UnityEngine;

namespace UnderGrind.Locomotion
{
    /// <summary>
    /// Applies a bounded lateral correction to Humanoid foot IK targets when an animation
    /// places the feet on the wrong sides of the character's current hip axis.
    /// Requires the Animator layer's IK Pass to be enabled.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public sealed class PlayerFootSeparationIK : MonoBehaviour
    {
        [Header("Correzione laterale")]
        [SerializeField, Min(0f)] private float minimumFootSeparation = 0.12f;
        [SerializeField, Range(0f, 1f)] private float correctionWeight = 1f;
        [SerializeField, Min(0f)] private float maximumCorrectionPerFoot = 0.14f;

        [Header("Condizioni")]
        [SerializeField] private bool onlyWhenGrounded = true;
        [SerializeField] private string groundedParameterName = "Grounded";

        private Animator _animator;
        private int _groundedParameterHash;

        public float MinimumFootSeparation => minimumFootSeparation;
        public float CorrectionWeight => correctionWeight;
        public float MaximumCorrectionPerFoot => maximumCorrectionPerFoot;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _groundedParameterHash = string.IsNullOrWhiteSpace(groundedParameterName)
                ? 0
                : Animator.StringToHash(groundedParameterName);
        }

        private void OnValidate()
        {
            minimumFootSeparation = Mathf.Max(0f, minimumFootSeparation);
            correctionWeight = Mathf.Clamp01(correctionWeight);
            maximumCorrectionPerFoot = Mathf.Max(0f, maximumCorrectionPerFoot);
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (_animator == null || !_animator.isHuman || correctionWeight <= 0f ||
                minimumFootSeparation <= 0f || maximumCorrectionPerFoot <= 0f)
                return;

            if (onlyWhenGrounded && _groundedParameterHash != 0 &&
                _animator.HasBoolParameter(_groundedParameterHash) &&
                !_animator.GetBool(_groundedParameterHash))
                return;

            var leftHip = _animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
            var rightHip = _animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
            var leftFoot = _animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            var rightFoot = _animator.GetBoneTransform(HumanBodyBones.RightFoot);
            if (leftHip == null || rightHip == null || leftFoot == null || rightFoot == null)
                return;

            var lateral = leftHip.position - rightHip.position;
            if (lateral.sqrMagnitude < 0.0001f) return;
            lateral.Normalize();

            var separation = Vector3.Dot(leftFoot.position - rightFoot.position, lateral);
            var perFootCorrection = CalculatePerFootCorrection(
                separation, minimumFootSeparation, maximumCorrectionPerFoot);
            if (perFootCorrection <= 0f) return;

            _animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, correctionWeight);
            _animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, correctionWeight);
            _animator.SetIKPosition(AvatarIKGoal.LeftFoot, leftFoot.position + lateral * perFootCorrection);
            _animator.SetIKPosition(AvatarIKGoal.RightFoot, rightFoot.position - lateral * perFootCorrection);
        }

        public static float CalculatePerFootCorrection(
            float currentSeparation, float minimumSeparation, float maximumCorrectionPerFoot)
        {
            if (minimumSeparation <= 0f || maximumCorrectionPerFoot <= 0f ||
                currentSeparation >= minimumSeparation)
                return 0f;

            return Mathf.Min((minimumSeparation - currentSeparation) * 0.5f,
                maximumCorrectionPerFoot);
        }
    }

    internal static class AnimatorParameterExtensions
    {
        public static bool HasBoolParameter(this Animator animator, int parameterHash)
        {
            if (animator == null || parameterHash == 0) return false;
            foreach (var parameter in animator.parameters)
                if (parameter.nameHash == parameterHash && parameter.type == AnimatorControllerParameterType.Bool)
                    return true;
            return false;
        }
    }
}
