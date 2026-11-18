using UnityEngine;

namespace UnderGrind.Locomotion
{
    [CreateAssetMenu(menuName = "UnderGrind/Player/Rig Settings", fileName = "PlayerRigSettings")]
    public sealed class PlayerRigSettings : ScriptableObject
    {
        [Header("Radici degli scheletri")]
        [SerializeField] private string maleArmatureRootName;
        [SerializeField] private string femaleArmatureRootName;

        [Header("Prefab")]
        [SerializeField] private string malePrefabName;
        [SerializeField] private string femalePrefabName;

        [Header("Nomenclatura delle parti")]
        [SerializeField] private string headRendererSuffix;
        [SerializeField] private string firstPersonArmsToken;
        [SerializeField] private string firstPersonHandsToken;
        [SerializeField] private string thirdPersonArmsToken;
        [SerializeField] private string thirdPersonHandsToken;
        [SerializeField] private bool requireSeparateHead;
        [SerializeField] private bool requireFirstPersonArms;

        [Header("Importazione FBX")]
        [SerializeField] private bool importAnimation;
        [SerializeField] private bool optimizeGameObjects;
        [SerializeField] private bool importVisibility;

        public string MaleArmatureRootName => maleArmatureRootName;
        public string FemaleArmatureRootName => femaleArmatureRootName;
        public string MalePrefabName => malePrefabName;
        public string FemalePrefabName => femalePrefabName;
        public string HeadRendererSuffix => headRendererSuffix;
        public string FirstPersonArmsToken => firstPersonArmsToken;
        public string FirstPersonHandsToken => firstPersonHandsToken;
        public string ThirdPersonArmsToken => thirdPersonArmsToken;
        public string ThirdPersonHandsToken => thirdPersonHandsToken;
        public bool RequireSeparateHead => requireSeparateHead;
        public bool RequireFirstPersonArms => requireFirstPersonArms;
        public bool ImportAnimation => importAnimation;
        public bool OptimizeGameObjects => optimizeGameObjects;
        public bool ImportVisibility => importVisibility;

        private void OnValidate()
        {
            maleArmatureRootName = Normalize(maleArmatureRootName);
            femaleArmatureRootName = Normalize(femaleArmatureRootName);
            malePrefabName = Normalize(malePrefabName);
            femalePrefabName = Normalize(femalePrefabName);
            headRendererSuffix = Normalize(headRendererSuffix);
            firstPersonArmsToken = Normalize(firstPersonArmsToken);
            firstPersonHandsToken = Normalize(firstPersonHandsToken);
            thirdPersonArmsToken = Normalize(thirdPersonArmsToken);
            thirdPersonHandsToken = Normalize(thirdPersonHandsToken);
        }

        private static string Normalize(string value) => value != null ? value.Trim() : string.Empty;
    }
}