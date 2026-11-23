using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
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
            Assert.That(EnsureHumanoidAvatar(MaleFbxPath, _settings.MaleArmatureRootName), Is.True,
                "Impossibile generare l'Avatar Humanoid maschile.");
            Assert.That(EnsureHumanoidAvatar(FemaleFbxPath, _settings.FemaleArmatureRootName), Is.True,
                "Impossibile generare l'Avatar Humanoid femminile.");
        }

        [Test]
        public void EnsureValidAvatarPreservesValidManualPoseCorrections()
        {
            var importer = AssetImporter.GetAtPath(MaleFbxPath) as ModelImporter;
            Assert.That(importer, Is.Not.Null, "ModelImporter maschile non disponibile.");

            var originalDescription = importer.humanDescription;
            var originalAnimationType = importer.animationType;
            var originalAvatarSetup = importer.avatarSetup;
            var avatar = LoadValidAvatar(MaleFbxPath);
            var poseDescription = avatar.humanDescription;

            Assert.That(poseDescription.human, Is.Not.Empty,
                "L'Avatar valido deve esporre il mapping Humanoid.");
            Assert.That(poseDescription.skeleton, Is.Not.Empty,
                "L'Avatar valido deve esporre la posa dello skeleton.");

            try
            {
                importer.humanDescription = poseDescription;
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.SaveAndReimport();

                Assert.That(EnsureHumanoidAvatar(MaleFbxPath, _settings.MaleArmatureRootName), Is.True,
                    "L'Avatar con correzioni manuali valide deve restare valido.");

                var persistedImporter = AssetImporter.GetAtPath(MaleFbxPath) as ModelImporter;
                Assert.That(persistedImporter, Is.Not.Null);
                Assert.That(persistedImporter.humanDescription.human.Length,
                    Is.EqualTo(poseDescription.human.Length),
                    "Il mapping Humanoid manuale non deve essere cancellato.");
                Assert.That(persistedImporter.humanDescription.skeleton.Length,
                    Is.EqualTo(poseDescription.skeleton.Length),
                    "La posa corretta manualmente non deve essere cancellata.");
            }
            finally
            {
                var restoreImporter = AssetImporter.GetAtPath(MaleFbxPath) as ModelImporter;
                if (restoreImporter != null)
                {
                    restoreImporter.humanDescription = originalDescription;
                    restoreImporter.animationType = originalAnimationType;
                    restoreImporter.avatarSetup = originalAvatarSetup;
                    restoreImporter.SaveAndReimport();
                }
            }
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
                ? ModelImporterMaterialImportMode.ImportViaMaterialDescription
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
                HumanBodyBones.LeftHand,
                HumanBodyBones.RightHand,
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
        public void ConfiguredModelYawAlignsAuthoredForwardWithPlayerForward()
        {
            var alignedForward = Quaternion.Euler(0f, _settings.ModelYawOffset, 0f) * Vector3.left;
            Assert.That(Vector3.Dot(alignedForward, Vector3.forward), Is.GreaterThan(0.999f),
                "Il forward sorgente -X deve essere allineato al forward Player +Z tramite PlayerRigSettings.");
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

        [Test]
        public void HumanoidTPoseCorrectionIsInspectorConfigurableAndEnabledByDefault()
        {
            var settings = AssetDatabase.LoadAssetAtPath<PlayerRigSettings>(SettingsPath);
            Assert.That(settings, Is.Not.Null);
            var serialized = new SerializedObject(settings);
            Assert.That(serialized.FindProperty("enforceHumanoidTPose"), Is.Not.Null,
                "La correzione T-Pose deve essere configurabile da PlayerRigSettings nell'Inspector.");

            var defaultsPath = "Assets/UnderGrind/Settings/Defaults/PlayerRigSettings.json";
            var defaults = System.IO.File.ReadAllText(defaultsPath);
            StringAssert.Contains("\"enforceHumanoidTPose\": true", defaults,
                "La correzione T-Pose deve essere attiva nella configurazione predefinita.");
        }

        [TestCase(MaleFbxPath)]
        [TestCase(FemaleFbxPath)]
        public void ImportedHumanoidAvatarHasValidTPose(string assetPath)
        {
            var settings = AssetDatabase.LoadAssetAtPath<PlayerRigSettings>(SettingsPath);
            if (settings != null && !settings.EnforceHumanoidTPose)
                Assert.Ignore("La correzione T-Pose è disabilitata in PlayerRigSettings.");

            Assert.That(IsTPoseValid(assetPath), Is.True,
                "La posa Humanoid deve essere valida dopo EnsureValidAvatar: " + assetPath);
        }

        [Test]
        public void LocomotionClipsDoNotCrossFeetOnSourceAndPlayerAvatars()
        {
            var clipPaths = new[]
            {
                "Assets/UnderGrind/Animations/Walking.fbx",
                "Assets/UnderGrind/Animations/Running.fbx",
                "Assets/UnderGrind/Animations/Crouch_Walk_Forward.fbx"
            };
            var failures = new System.Collections.Generic.List<string>();

            foreach (var clipPath in clipPaths)
            {
                var clip = AssetDatabase.LoadAllAssetsAtPath(clipPath)
                    .OfType<AnimationClip>()
                    .FirstOrDefault(candidate => candidate != null &&
                        candidate.humanMotion &&
                        !candidate.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase));
                Assert.That(clip, Is.Not.Null,
                    "Clip Humanoid mancante o non importata: " + clipPath);

                var sourceModel = AssetDatabase.LoadAssetAtPath<GameObject>(clipPath);
                Assert.That(sourceModel, Is.Not.Null, "Modello sorgente Mixamo mancante: " + clipPath);
                var sourceAvatar = LoadValidAvatar(clipPath);
                RecordFootCrossingResult("Mixamo source", sourceModel, sourceAvatar, clip, failures);

                var maleModel = AssetDatabase.LoadAssetAtPath<GameObject>(MaleFbxPath);
                var maleAvatar = LoadValidAvatar(MaleFbxPath);
                RecordFootCrossingResult("Player male", maleModel, maleAvatar, clip, failures);

                var femaleModel = AssetDatabase.LoadAssetAtPath<GameObject>(FemaleFbxPath);
                var femaleAvatar = LoadValidAvatar(FemaleFbxPath);
                RecordFootCrossingResult("Player female", femaleModel, femaleAvatar, clip, failures);
            }

            Assert.That(failures, Is.Empty,
                "Rilevati incroci laterali dei piedi durante il retarget Humanoid:" +
                Environment.NewLine + string.Join(Environment.NewLine, failures));
        }

        private sealed class FootCrossingResult
        {
            public float RestSeparation;
            public float MinimumSeparation;
            public float WorstTime;
            public int CrossingSamples;
            public int SampleCount;
        }

        private static void RecordFootCrossingResult(
            string avatarLabel,
            GameObject model,
            Avatar avatar,
            AnimationClip clip,
            System.Collections.Generic.ICollection<string> failures)
        {
            var result = MeasureFootCrossing(model, avatar, clip);
            var summary = avatarLabel + " / " + System.IO.Path.GetFileNameWithoutExtension(
                AssetDatabase.GetAssetPath(clip)) +
                " / " + clip.name +
                ": rest=" + result.RestSeparation.ToString("F3") + "m" +
                ", min=" + result.MinimumSeparation.ToString("F3") + "m" +
                " a " + result.WorstTime.ToString("F2") + "s" +
                ", incroci=" + result.CrossingSamples + "/" + result.SampleCount;
            Debug.Log("UnderGrind foot-crossing diagnostic: " + summary);

            if (result.RestSeparation <= 0f || result.CrossingSamples > 0)
                failures.Add(summary);
        }

        private static FootCrossingResult MeasureFootCrossing(
            GameObject model,
            Avatar avatar,
            AnimationClip clip)
        {
            const int sampleRate = 30;
            const float crossingToleranceMeters = 0.02f;

            var instance = UnityEngine.Object.Instantiate(model);
            PlayableGraph graph = default;
            try
            {
                instance.name = model.name + "_FootCrossingDiagnostic";
                var animator = instance.GetComponent<Animator>();
                if (animator == null) animator = instance.AddComponent<Animator>();
                animator.avatar = avatar;
                animator.runtimeAnimatorController = null;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.Rebind();
                animator.Update(0f);

                var leftHip = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
                var rightHip = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
                var leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
                var rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
                Assert.That(leftHip, Is.Not.Null, model.name + ": LeftUpperLeg non mappata.");
                Assert.That(rightHip, Is.Not.Null, model.name + ": RightUpperLeg non mappata.");
                Assert.That(leftFoot, Is.Not.Null, model.name + ": LeftFoot non mappata.");
                Assert.That(rightFoot, Is.Not.Null, model.name + ": RightFoot non mappata.");

                var lateral = instance.transform.InverseTransformDirection(
                    leftHip.position - rightHip.position).normalized;
                Assert.That(lateral.sqrMagnitude, Is.GreaterThan(0.9f),
                    model.name + ": asse laterale delle anche non valido.");

                var restLeft = instance.transform.InverseTransformPoint(leftFoot.position);
                var restRight = instance.transform.InverseTransformPoint(rightFoot.position);
                var restSeparation = Vector3.Dot(restLeft - restRight, lateral);

                graph = PlayableGraph.Create("FootCrossing_" + clip.name);
                graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                var output = AnimationPlayableOutput.Create(graph, "FootCrossing", animator);
                var playable = AnimationClipPlayable.Create(graph, clip);
                playable.SetApplyFootIK(false);
                playable.SetApplyPlayableIK(false);
                playable.SetSpeed(0d);
                output.SetSourcePlayable(playable);
                graph.Play();

                var sampleCount = Mathf.Max(2, Mathf.CeilToInt(clip.length * sampleRate) + 1);
                var minimumSeparation = float.PositiveInfinity;
                var worstTime = 0f;
                var crossingSamples = 0;

                for (var sample = 0; sample < sampleCount; sample++)
                {
                    var normalizedTime = sample / (float)(sampleCount - 1);
                    var sampleTime = Mathf.Min(clip.length * normalizedTime,
                        Mathf.Max(0f, clip.length - 0.0001f));
                    playable.SetTime(sampleTime);
                    graph.Evaluate(0f);

                    var leftPosition = instance.transform.InverseTransformPoint(leftFoot.position);
                    var rightPosition = instance.transform.InverseTransformPoint(rightFoot.position);
                    var separation = Vector3.Dot(leftPosition - rightPosition, lateral);
                    if (separation < minimumSeparation)
                    {
                        minimumSeparation = separation;
                        worstTime = sampleTime;
                    }

                    if (separation < -crossingToleranceMeters)
                        crossingSamples++;
                }

                return new FootCrossingResult
                {
                    RestSeparation = restSeparation,
                    MinimumSeparation = minimumSeparation,
                    WorstTime = worstTime,
                    CrossingSamples = crossingSamples,
                    SampleCount = sampleCount
                };
            }
            finally
            {
                if (graph.IsValid()) graph.Destroy();
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        private static bool IsTPoseValid(string assetPath)
        {
            var importerType = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType("UnderGrind.Editor.HumanoidAvatarImporter", false))
                .FirstOrDefault(type => type != null);
            Assert.That(importerType, Is.Not.Null, "HumanoidAvatarImporter deve essere caricato nell'assembly Editor.");

            var method = importerType.GetMethod("IsTPoseValid",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            Assert.That(method, Is.Not.Null, "Metodo IsTPoseValid non trovato.");
            return (bool)method.Invoke(null, new object[] { assetPath });
        }

        private static bool EnsureHumanoidAvatar(string assetPath, string armatureRootName)
        {
            var importerType = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType("UnderGrind.Editor.HumanoidAvatarImporter", false))
                .FirstOrDefault(type => type != null);
            Assert.That(importerType, Is.Not.Null, "HumanoidAvatarImporter deve essere caricato nell'assembly Editor.");

            var method = importerType.GetMethod("EnsureValidAvatar",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            Assert.That(method, Is.Not.Null, "Metodo EnsureValidAvatar non trovato.");
            return (bool)method.Invoke(null, new object[] { assetPath, armatureRootName });
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