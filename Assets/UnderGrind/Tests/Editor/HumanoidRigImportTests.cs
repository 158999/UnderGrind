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
            Assert.That(importer.optimizeGameObjects, Is.False,
                "L'ottimizzazione deve restare disattivata finché il binding dei renderer non è verificato.");

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

            foreach (var bone in requiredBones)
                Assert.That(avatar.GetBoneTransform(bone), Is.Not.Null,
                    "Mapping Humanoid mancante per " + bone + " nel rig " + label);

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
                Assert.That(renderer.bones, Is.Not.Empty, renderer.name + " non ha riferimenti alle ossa.");
                Assert.That(renderer.bones.All(bone => bone != null), Is.True,
                    renderer.name + " contiene riferimenti a ossa mancanti.");
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