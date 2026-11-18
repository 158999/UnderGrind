using UnityEngine;

namespace UnderGrind.Locomotion
{
    public enum PlayerCharacterVariant
    {
        Male,
        Female
    }

    [CreateAssetMenu(menuName = "UnderGrind/Player/Rig Settings", fileName = "PlayerRigSettings")]
    public sealed class PlayerRigSettings : ScriptableObject
    {
        [Header("Configurazione asset")]
        [SerializeField, HideInInspector] private int settingsVersion;

        [Header("Sorgenti FBX separate")]
        [SerializeField] private string maleSourceAssetPath;
        [SerializeField] private string femaleSourceAssetPath;

        [Header("Radici degli scheletri")]
        [SerializeField] private string maleArmatureRootName;
        [SerializeField] private string femaleArmatureRootName;

        [Header("Prefab")]
        [SerializeField] private string malePrefabName;
        [SerializeField] private string femalePrefabName;

        [Header("Personaggio predefinito nella scena di test")]
        [SerializeField] private PlayerCharacterVariant defaultCharacter;

        [Header("Nomenclatura delle parti")]
        [SerializeField] private string headRendererSuffix;
        [SerializeField] private string firstPersonArmsToken;
        [SerializeField] private string firstPersonHandsToken;
        [SerializeField] private string thirdPersonArmsToken;
        [SerializeField] private string thirdPersonHandsToken;
        [SerializeField] private string bodyRendererToken;
        [SerializeField, Range(1f, 89f)] private float firstPersonBodyRevealPitch;
        [SerializeField, Range(0f, 89f)] private float firstPersonBodyHidePitch;
        [SerializeField] private bool requireSeparateHead;
        [SerializeField] private bool requireFirstPersonArms;
        [SerializeField] private bool requireThirdPersonArms;

        [Header("Importazione FBX")]
        [SerializeField] private bool importAnimation;
        [SerializeField] private bool importMaterials;
        [SerializeField] private bool importCameras;
        [SerializeField] private bool importLights;
        [SerializeField] private bool optimizeGameObjects;
        [SerializeField] private bool importVisibility;

        public int SettingsVersion => settingsVersion;
        public PlayerCharacterVariant DefaultCharacter => defaultCharacter;
        public string MaleSourceAssetPath => maleSourceAssetPath;
        public string FemaleSourceAssetPath => femaleSourceAssetPath;
        public string MaleArmatureRootName => maleArmatureRootName;
        public string FemaleArmatureRootName => femaleArmatureRootName;
        public string MalePrefabName => malePrefabName;
        public string FemalePrefabName => femalePrefabName;
        public string HeadRendererSuffix => headRendererSuffix;
        public string FirstPersonArmsToken => firstPersonArmsToken;
        public string FirstPersonHandsToken => firstPersonHandsToken;
        public string ThirdPersonArmsToken => thirdPersonArmsToken;
        public string ThirdPersonHandsToken => thirdPersonHandsToken;
        public string BodyRendererToken => bodyRendererToken;
        public float FirstPersonBodyRevealPitch => firstPersonBodyRevealPitch;
        public float FirstPersonBodyHidePitch => firstPersonBodyHidePitch;
        public bool RequireSeparateHead => requireSeparateHead;
        public bool RequireFirstPersonArms => requireFirstPersonArms;
        public bool RequireThirdPersonArms => requireThirdPersonArms;
        public bool ImportAnimation => importAnimation;
        public bool ImportMaterials => importMaterials;
        public bool ImportCameras => importCameras;
        public bool ImportLights => importLights;
        public bool OptimizeGameObjects => optimizeGameObjects;
        public bool ImportVisibility => importVisibility;

        private void OnValidate()
        {
            maleSourceAssetPath = NormalizePath(maleSourceAssetPath);
            femaleSourceAssetPath = NormalizePath(femaleSourceAssetPath);
            maleArmatureRootName = Normalize(maleArmatureRootName);
            femaleArmatureRootName = Normalize(femaleArmatureRootName);
            malePrefabName = Normalize(malePrefabName);
            femalePrefabName = Normalize(femalePrefabName);
            headRendererSuffix = Normalize(headRendererSuffix);
            firstPersonArmsToken = Normalize(firstPersonArmsToken);
            firstPersonHandsToken = Normalize(firstPersonHandsToken);
            thirdPersonArmsToken = Normalize(thirdPersonArmsToken);
            thirdPersonHandsToken = Normalize(thirdPersonHandsToken);
            bodyRendererToken = Normalize(bodyRendererToken);
            firstPersonBodyRevealPitch = Mathf.Clamp(firstPersonBodyRevealPitch, 1f, 89f);
            firstPersonBodyHidePitch = Mathf.Clamp(firstPersonBodyHidePitch, 0f, firstPersonBodyRevealPitch);
        }

        private static string Normalize(string value) => value != null ? value.Trim() : string.Empty;
        private static string NormalizePath(string value) => Normalize(value).Replace("\\", "/");
    }
}