using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnderGrind.Locomotion;

namespace UnderGrind.Tests
{
    public sealed class HumanoidRigImportTests
    {
        private const string SettingsPath = "Assets/UnderGrind/Settings/PlayerRigSettings.asset";
        private const string MaleFbxPath = "Assets/UnderGrind/Art/Source/Rigged/Human_Male_Rigged.fbx";
        private const string FemaleFbxPath = "Assets/UnderGrind/Art/Source/Rigged/Human_Female_Rigged.fbx";

        private PlayerRigSettings _settings;

        [SetUp]
        public void SetUp()
        {
            _settings = AssetDatabase.LoadAssetAtPath<PlayerRigSettings>(SettingsPath);
            Assert.That(_settings, Is.Not.Null, "PlayerRigSettings deve essere presente nel progetto.");
        }

        [TestCase(MaleFbxPath, "maschile")]
        [TestCase(FemaleFbxPath, "femminile")]
        public void ImportedFbxHasValidHumanoidAvatarAndSkinnedParts(string assetPath, string label)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            Assert.That(model, Is.Not.Null, "FBX " + label + " non trovato: " + assetPath);

            var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            Assert.That(importer, Is.Not.Null, "ModelImporter non disponibile per " + assetPath);
            Assert.That(importer.animationType, Is.EqualTo(ModelImporterAnimationType.Human));
            Assert.That(importer.avatarSetup, Is.EqualTo(ModelImporterAvatarSetup.CreateFromThisModel));
            Assert.That(importer.optimizeGameObjects, Is.EqualTo(_settings.OptimizeGameObjects),
                "L'ottimizzazione deve rispettare PlayerRigSettings.");
            Assert.That(importer.importAnimation, Is.EqualTo(_settings.ImportAnimation),
                "L'importazione delle animazioni deve rispettare PlayerRigSettings.");
            Assert.That(importer.importCameras, Is.EqualTo(_settings.ImportCameras),
                "L'importazione delle camere deve rispettare PlayerRigSettings.");
            Assert.That(importer.importLights, Is.EqualTo(_settings.ImportLights),
                "L'importazione delle luci deve rispettare PlayerRigSettings.");
            Assert.That(importer.importVisibility, Is.EqualTo(_settings.ImportVisibility),
                "L'importazione della visibilità deve rispettare PlayerRigSettings.");

            var expectedMaterialMode = _settings.ImportMaterials
                ? ModelImporterMaterialImportMode.ImportStandard
                : ModelImporterMaterialImportMode.None;
            Assert.That(importer.materialImportMode, Is.EqualTo(expectedMaterialMode),
                "L'importazione dei materiali deve rispettare PlayerRigSettings.");

            var avatar = AssetDatabase.LoadAllAssetsAtPath(assetPath)
                .OfType<Avatar>()
                .FirstOrDefault(candidate => candidate != null && candidate.isHuman && candidate.isValid);
            Assert.That(avatar, Is.Not.Null, "Unity non ha generato un Avatar Humanoid valido per " + label);

            var requiredBones = new[]
            {
                HumanBodyBones.Hips,
                HumanBodyBones.Spine,
                HumanBodyBones.Head,
                HumanBodyBones.LeftUpperArm,
                HumanBodyBones.RightUpperArm,
                HumanBodyBones.LeftLowerArm,
                HumanBodyBones.RightLowerArm,
                HumanBodyBones.LeftUpperLeg,
                HumanBodyBones.RightUpperLeg,
                HumanBodyBones.LeftLowerLeg,
                HumanBodyBones.RightLowerLeg,
                HumanBodyBones.LeftFoot,
                HumanBodyBones.RightFoot
            };

            var armatureName = assetPath == MaleFbxPath
                ? _settings.MaleArmatureRootName
                : _settings.FemaleArmatureRootName;
            var armature = FindDeepChild(model.transform, armatureName);
            Assert.That(armature, Is.Not.Null, "Armature root '" + armatureName + "' mancante nel rig " + label);

            var avatarInstance = UnityEngine.Object.Instantiate(model);
            try
            {
                var animator = avatarInstance.GetComponent<Animator>();
                if (animator == null) animator = avatarInstance.AddComponent<Animator>();
                animator.avatar = avatar;

                var instanceArmature = FindDeepChild(avatarInstance.transform, armatureName);
                Assert.That(instanceArmature, Is.Not.Null,
                    "Armature root '" + armatureName + "' mancante nell'istanza " + label);

                foreach (var bone in requiredBones)
                {
                    var mappedBone = animator.GetBoneTransform(bone);
                    Assert.That(mappedBone, Is.Not.Null,
                        "Mapping Humanoid mancante per " + bone + " nel rig " + label);
                    Assert.That(mappedBone == instanceArmature || mappedBone.IsChildOf(instanceArmature), Is.True,
                        "Il mapping Humanoid di " + bone + " non appartiene all'armatura configurata.");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(avatarInstance);
            }

            var renderers = model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            Assert.That(renderers, Has.Length.EqualTo(6),
                "Il rig " + label + " deve contenere Body, Head, FP Arms/Hands e TP Arms/Hands.");

            var requiredPartTokens = new[]
            {
                _settings.BodyRendererToken,
                _settings.HeadRendererSuffix,
                _settings.FirstPersonArmsToken,
                _settings.FirstPersonHandsToken,
                _settings.ThirdPersonArmsToken,
                _settings.ThirdPersonHandsToken
            };

            foreach (var token in requiredPartTokens)
            {
                Assert.That(renderers.Any(renderer =>
                        renderer.name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0),
                    Is.True, "Parte renderer '" + token + "' mancante nel rig " + label);
            }

            foreach (var renderer in renderers)
            {
                Assert.That(renderer.sharedMesh, Is.Not.Null, renderer.name + " non ha una mesh.");
                Assert.That(renderer.sharedMesh.bindposes, Is.Not.Empty,
                    renderer.name + " non contiene bind pose.");
                Assert.That(renderer.rootBone, Is.Not.Null, renderer.name + " non ha rootBone.");
                Assert.That(renderer.rootBone == armature || renderer.rootBone.IsChildOf(armature), Is.True,
                    renderer.name + " usa un rootBone esterno all'armatura configurata.");
                Assert.That(renderer.bones, Is.Not.Empty, renderer.name + " non ha riferimenti alle ossa.");
                Assert.That(renderer.bones.All(bone => bone != null), Is.True,
                    renderer.name + " contiene riferimenti a ossa mancanti.");
                Assert.That(renderer.bones.All(bone => bone == armature || bone.IsChildOf(armature)), Is.True,
                    renderer.name + " contiene ossa non appartenenti all'armatura configurata.");
            }
        }

        [Test]
        public void MaleAndFemaleFbxUseDistinctHumanoidAvatars()
        {
            var maleAvatar = LoadValidAvatar(MaleFbxPath);
            var femaleAvatar = LoadValidAvatar(FemaleFbxPath);

            Assert.That(maleAvatar, Is.Not.SameAs(femaleAvatar),
                "I due personaggi devono mantenere Avatar Humanoid distinti.");
        }

        [Test]
        public void BothImportedModelsBindToConfiguredVisualRigParts()
        {
            AssertVisualRigBindings(MaleFbxPath, "maschile");
            AssertVisualRigBindings(FemaleFbxPath, "femminile");
        }

        private static Transform FindDeepChild(Transform root, string targetName)
        {
            if (root.name.Equals(targetName, StringComparison.OrdinalIgnoreCase)) return root;
            foreach (Transform child in root)
            {
                var found = FindDeepChild(child, targetName);
                if (found != null) return found;
            }
            return null;
        }

        private static Avatar LoadValidAvatar(string assetPath)
        {
            var avatar = AssetDatabase.LoadAllAssetsAtPath(assetPath)
                .OfType<Avatar>()
                .FirstOrDefault(candidate => candidate != null && candidate.isHuman && candidate.isValid);
            Assert.That(avatar, Is.Not.Null, "Avatar Humanoid non valido: " + assetPath);
            return avatar;
        }

        private void AssertVisualRigBindings(string assetPath, string label)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            Assert.That(source, Is.Not.Null, "FBX " + label + " non trovato.");

            var instance = UnityEngine.Object.Instantiate(source);
            try
            {
                var rig = instance.AddComponent<PlayerVisualRig>();
                Assert.That(rig.ConfigureFromHierarchy(instance.transform, _settings), Is.True,
                    "PlayerVisualRig non riesce a riconoscere le parti configurate del personaggio " + label + ".");
                Assert.That(rig.HeadRendererCount, Is.EqualTo(1));
                Assert.That(rig.BodyRendererCount, Is.EqualTo(1));
                Assert.That(rig.FirstPersonRendererCount, Is.EqualTo(2));
                Assert.That(rig.ThirdPersonRendererCount, Is.EqualTo(2));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }
    }
}