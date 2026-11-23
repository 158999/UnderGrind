using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnderGrind.Locomotion;

namespace UnderGrind.Editor
{
    /// <summary>
    /// Imports FBX Humanoid avatars while preserving valid manual pose corrections.
    /// If Unity's automatic map is invalid, repairs mappings by exact transform names
    /// before falling back to a clean automatic import.
    /// </summary>
    public static class HumanoidAvatarImporter
    {
        private const string RigSettingsPath = "Assets/UnderGrind/Settings/PlayerRigSettings.asset";
        private const string HumanBonesPropertyPath = "m_HumanDescription.m_Human";
        private const string SkeletonBonesPropertyPath = "m_HumanDescription.m_Skeleton";

        public static bool EnsureValidAvatar(string assetPath, string armatureRootName)
        {
            if (string.IsNullOrWhiteSpace(assetPath) || string.IsNullOrWhiteSpace(armatureRootName))
                return false;

            var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (importer == null || AssetDatabase.LoadAssetAtPath<GameObject>(assetPath) == null)
            {
                Debug.LogError("UnderGrind: ModelImporter o FBX non disponibile: " + assetPath);
                return false;
            }

            var description = importer.humanDescription;
            var hasManualMapping = description.human != null && description.human.Length > 0 ||
                                  description.skeleton != null && description.skeleton.Length > 0;
            var importerSettingsChanged = importer.animationType != ModelImporterAnimationType.Human ||
                                          importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel;

            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;

            if (importerSettingsChanged)
            {
                importer.SaveAndReimport();
                importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
                if (importer == null)
                {
                    Debug.LogError("UnderGrind: ModelImporter non disponibile dopo la reimportazione: " + assetPath);
                    return false;
                }
            }

            var avatar = LoadValidAvatar(assetPath);
            if (avatar == null)
            {
                // Unity's auto-mapper can assign a real RightHand transform to RightLowerArm.
                // Rebuild the Humanoid map by exact transform names while preserving the
                // importer-generated skeleton and its sampled pose.
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (TryBuildExactNameMapping(model, description, out var exactDescription))
                {
                    importer.humanDescription = exactDescription;
                    importer.SaveAndReimport();
                    importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
                    avatar = LoadValidAvatar(assetPath);
                }
            }

            if (avatar == null && hasManualMapping)
            {
                // Unity's Avatar Configuration writes pose corrections into humanDescription.
                // Keep those corrections whenever they produce a valid Avatar; only discard
                // an invalid manual description before falling back to automatic mapping.
                description = importer.humanDescription;
                description.human = Array.Empty<HumanBone>();
                description.skeleton = Array.Empty<SkeletonBone>();
                importer.humanDescription = description;
                importer.SaveAndReimport();
                avatar = LoadValidAvatar(assetPath);
            }

            if (avatar == null)
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                var currentDescription = importer.humanDescription;
                var mappedBones = currentDescription.human == null
                    ? "nessun mapping Humanoid serializzato"
                    : string.Join(", ", currentDescription.human.Select(item =>
                        item.humanName + " -> " + item.boneName));
                var skeletonBones = currentDescription.skeleton == null
                    ? "nessuno skeleton serializzato"
                    : string.Join(", ", currentDescription.skeleton.Select(item =>
                        item.name));
                var hierarchy = model == null
                    ? "FBX hierarchy non disponibile"
                    : string.Join(Environment.NewLine, model.GetComponentsInChildren<Transform>(true)
                        .Select(GetTransformPath));

                Debug.LogError("UnderGrind: Avatar Humanoid non valido: " + assetPath +
                               Environment.NewLine + "Mapping importer: " + mappedBones +
                               Environment.NewLine + "Skeleton importer: " + skeletonBones +
                               Environment.NewLine + "Transform FBX importati:" +
                               Environment.NewLine + hierarchy +
                               Environment.NewLine + "Root armature configurata: " + armatureRootName);
                return false;
            }

            var rigSettings = AssetDatabase.LoadAssetAtPath<PlayerRigSettings>(RigSettingsPath);
            if ((rigSettings == null || rigSettings.EnforceHumanoidTPose) && !EnsureTPose(assetPath))
                return false;

            return true;
        }

        public static bool IsTPoseValid(string assetPath)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (model == null || importer == null) return false;

            var setupToolType = GetAvatarSetupToolType();
            if (setupToolType == null)
            {
                Debug.LogError("UnderGrind: AvatarSetupTool non disponibile nella versione Unity corrente.");
                return false;
            }

            var isPoseValidOnInstance = setupToolType.GetMethod("IsPoseValidOnInstance",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, null,
                new[] { typeof(GameObject), typeof(SerializedObject) }, null);
            if (isPoseValidOnInstance == null)
            {
                Debug.LogError("UnderGrind: API Unity AvatarSetupTool.IsPoseValidOnInstance non disponibile.");
                return false;
            }

            using (var serialized = new SerializedObject(importer))
            {
                try
                {
                    return (bool)isPoseValidOnInstance.Invoke(null, new object[] { model, serialized });
                }
                catch (TargetInvocationException exception)
                {
                    Debug.LogException(exception.InnerException ?? exception);
                    return false;
                }
            }
        }

        private static bool EnsureTPose(string assetPath)
        {
            if (IsTPoseValid(assetPath)) return true;

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (model == null || importer == null)
            {
                Debug.LogError("UnderGrind: impossibile applicare la T-Pose; FBX o ModelImporter non disponibili: " + assetPath);
                return false;
            }

            var setupToolType = typeof(Editor).Assembly.GetType("UnityEditor.AvatarSetupTool", false);
            if (setupToolType == null)
            {
                Debug.LogError("UnderGrind: AvatarSetupTool non disponibile. In Unity usa Avatar Configuration > Pose > Enforce T-Pose > Apply > Done.");
                return false;
            }

            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            var getModelBones = FindStaticMethod(setupToolType, "GetModelBones", 3, flags);
            var getHumanBones = setupToolType.GetMethod("GetHumanBones", flags, null,
                new[] { typeof(SerializedProperty), typeof(Dictionary<Transform, bool>) }, null);
            var transferDescriptionToPose = setupToolType.GetMethod("TransferDescriptionToPose", flags, null,
                new[] { typeof(SerializedObject), typeof(Transform) }, null);
            var isPoseValid = FindStaticMethod(setupToolType, "IsPoseValid", 1, flags);
            var makePoseValid = FindStaticMethod(setupToolType, "MakePoseValid", 1, flags);
            var transferPoseToDescription = setupToolType.GetMethod("TransferPoseToDescription", flags, null,
                new[] { typeof(SerializedProperty), typeof(Transform) }, null);

            if (getModelBones == null || getHumanBones == null || transferDescriptionToPose == null ||
                isPoseValid == null || makePoseValid == null || transferPoseToDescription == null)
            {
                Debug.LogError("UnderGrind: API Unity necessarie per Enforce T-Pose non disponibili. " +
                               "Usa Avatar Configuration > Pose > Enforce T-Pose > Apply > Done.");
                return false;
            }

            using (var serialized = new SerializedObject(importer))
            {
                var humanBonesProperty = serialized.FindProperty(HumanBonesPropertyPath);
                var skeletonBonesProperty = serialized.FindProperty(SkeletonBonesPropertyPath);
                if (humanBonesProperty == null || skeletonBonesProperty == null)
                {
                    Debug.LogError("UnderGrind: proprietà HumanDescription mancanti nel ModelImporter: " + assetPath);
                    return false;
                }

                var instance = UnityEngine.Object.Instantiate(model);
                instance.hideFlags = HideFlags.HideAndDontSave;
                try
                {
                    var modelBones = getModelBones.Invoke(null, new object[] { instance.transform, false, null });
                    if (modelBones == null)
                    {
                        Debug.LogError("UnderGrind: Unity non ha individuato le ossa del modello: " + assetPath);
                        return false;
                    }

                    var humanBones = getHumanBones.Invoke(null, new object[] { humanBonesProperty, modelBones });
                    if (humanBones == null)
                    {
                        Debug.LogError("UnderGrind: Unity non ha costruito il mapping Humanoid: " + assetPath);
                        return false;
                    }

                    transferDescriptionToPose.Invoke(null, new object[] { serialized, instance.transform });
                    if ((bool)isPoseValid.Invoke(null, new object[] { humanBones }))
                        return true;

                    makePoseValid.Invoke(null, new object[] { humanBones });
                    transferPoseToDescription.Invoke(null, new object[] { skeletonBonesProperty, instance.transform });
                    serialized.ApplyModifiedProperties();
                }
                catch (Exception exception)
                {
                    var cause = exception is TargetInvocationException invocation && invocation.InnerException != null
                        ? invocation.InnerException : exception;
                    Debug.LogException(cause);
                    return false;
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(instance);
                }
            }

            importer.SaveAndReimport();
            var avatar = LoadValidAvatar(assetPath);
            if (avatar == null || !IsTPoseValid(assetPath))
            {
                Debug.LogError("UnderGrind: correzione T-Pose non verificata dopo la reimportazione: " + assetPath);
                return false;
            }

            Debug.Log("UnderGrind: T-Pose Humanoid corretta e salvata nel ModelImporter: " + assetPath);
            return true;
        }

        private static Type GetAvatarSetupToolType()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType("UnityEditor.AvatarSetupTool", false))
                .FirstOrDefault(type => type != null);
        }

        private static MethodInfo FindStaticMethod(Type type, string name, int parameterCount, BindingFlags flags)
        {
            return type.GetMethods(flags).FirstOrDefault(method =>
                method.Name == name && method.GetParameters().Length == parameterCount);
        }

        private static bool TryBuildExactNameMapping(
            GameObject model,
            HumanDescription sourceDescription,
            out HumanDescription correctedDescription)
        {
            correctedDescription = sourceDescription;
            if (model == null || sourceDescription.skeleton == null || sourceDescription.skeleton.Length == 0)
                return false;

            var transforms = model.GetComponentsInChildren<Transform>(true)
                .GroupBy(transform => transform.name, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

            var requiredBones = new[]
            {
                "Hips", "Spine", "Chest", "Neck", "Head",
                "LeftShoulder", "RightShoulder",
                "LeftUpperArm", "RightUpperArm",
                "LeftLowerArm", "RightLowerArm",
                "LeftHand", "RightHand",
                "LeftUpperLeg", "RightUpperLeg",
                "LeftLowerLeg", "RightLowerLeg",
                "LeftFoot", "RightFoot"
            };

            if (requiredBones.Any(boneName => !transforms.ContainsKey(boneName)))
            {
                Debug.LogError("UnderGrind: mapping Humanoid esplicito impossibile; ossa mancanti: " +
                               string.Join(", ", requiredBones.Where(boneName => !transforms.ContainsKey(boneName))));
                return false;
            }

            var mappings = HumanTrait.BoneName
                .Where(boneName => !string.IsNullOrEmpty(boneName) && transforms.ContainsKey(boneName))
                .Select(boneName => new HumanBone
                {
                    humanName = boneName,
                    boneName = boneName,
                    limit = new HumanLimit { useDefaultValues = true }
                })
                .ToArray();

            correctedDescription.human = mappings;
            return requiredBones.All(required =>
                mappings.Any(mapping => mapping.humanName == required && mapping.boneName == required));
        }

        private static string GetTransformPath(Transform transform)
        {
            var segments = new System.Collections.Generic.Stack<string>();
            while (transform != null)
            {
                segments.Push(transform.name);
                transform = transform.parent;
            }

            return string.Join("/", segments);
        }

        private static Avatar LoadValidAvatar(string assetPath)
        {
            return AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Avatar>()
                .FirstOrDefault(item => item != null && item.isHuman && item.isValid);
        }
    }
}