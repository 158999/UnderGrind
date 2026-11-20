using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnderGrind.Locomotion;

namespace UnderGrind.Editor
{
    public static class PlayerAnimationControllerBuilder
    {
        private const string SettingsPath = "Assets/UnderGrind/Settings/PlayerAnimationSettings.asset";
        private const string AnimationFolder = "Assets/UnderGrind/Animations";
        private const string ControllerFolder = "Assets/UnderGrind/Generated/Animation";
        private const string ControllerPath = ControllerFolder + "/PlayerAnimator.controller";

        [MenuItem("UnderGrind/Player/Refresh Clips and Build Animator Controller")]
        public static void BuildFromSettings()
        {
            PlayerGameplayAssetBuilder.EnsureAssets();
            var settings = AssetDatabase.LoadAssetAtPath<PlayerAnimationSettings>(SettingsPath);
            if (settings == null)
            {
                Debug.LogError("UnderGrind: PlayerAnimationSettings non disponibile.");
                return;
            }

            PrepareAnimationImports(settings);
            RefreshClipReferences(settings);
            var missing = settings.GetMissingRequiredClipNames();
            if (missing.Count > 0)
            {
                var message = "Impossibile costruire l'Animator Controller. Mancano queste clip:\n- " +
                              string.Join("\n- ", missing) +
                              "\n\nImportare i file FBX nella cartella Animations e ripetere l'operazione.";
                Debug.LogWarning("UnderGrind: " + message);
                EditorUtility.DisplayDialog("Clip Player mancanti", message, "OK");
                return;
            }

            var invalidParameters = GetInvalidAnimatorParameters(settings);
            if (invalidParameters.Count > 0)
            {
                var message = "Impossibile costruire l'Animator Controller. Correggere i parametri configurati:\n- " +
                              string.Join("\n- ", invalidParameters);
                Debug.LogError("UnderGrind: " + message);
                EditorUtility.DisplayDialog("Parametri Animator non validi", message, "OK");
                return;
            }

            EnsureFolder("Assets/UnderGrind/Generated");
            EnsureFolder(ControllerFolder);
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null)
            {
                if (!EditorUtility.DisplayDialog(
                    "Sostituire l'Animator Controller?",
                    "Il controller generato esistente verrà eliminato e ricreato. Eventuali modifiche manuali a stati, transizioni e layer andranno perse.",
                    "Sostituisci controller", "Annulla"))
                    return;

                AssetDatabase.DeleteAsset(ControllerPath);
            }

            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            if (controller == null)
            {
                Debug.LogError("UnderGrind: creazione Animator Controller fallita.");
                return;
            }

            AddParameters(controller, settings);
            BuildStateMachine(controller.layers[0].stateMachine, settings);
            var serialized = new SerializedObject(settings);
            serialized.FindProperty("animatorController").objectReferenceValue = controller;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = settings;
            Debug.Log("UnderGrind: Animator Controller creato in " + ControllerPath);
        }

        private sealed class AnimationClipSource
        {
            public AnimationClip Clip;
            public string AssetName;
        }

        private static void PrepareAnimationImports(PlayerAnimationSettings settings)
        {
            if (!settings.ConfigureFbxImports) return;

            var guids = AssetDatabase.FindAssets("t:Model", new[] { AnimationFolder });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase)) continue;
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null) continue;

                var changed = false;
                var animationType = settings.FbxRigType == PlayerAnimationRigType.Humanoid
                    ? ModelImporterAnimationType.Human : ModelImporterAnimationType.Generic;
                var avatarSetup = settings.CreateAvatarFromFbx
                    ? ModelImporterAvatarSetup.CreateFromThisModel : ModelImporterAvatarSetup.NoAvatar;
                if (importer.animationType != animationType)
                {
                    importer.animationType = animationType;
                    changed = true;
                }
                if (importer.avatarSetup != avatarSetup)
                {
                    importer.avatarSetup = avatarSetup;
                    changed = true;
                }
                if (importer.importAnimation != settings.ImportAnimationClips)
                {
                    importer.importAnimation = settings.ImportAnimationClips;
                    changed = true;
                }
                if (importer.materialImportMode != (settings.ImportMaterials ? ModelImporterMaterialImportMode.ImportStandard : ModelImporterMaterialImportMode.None))
                {
                    importer.materialImportMode = settings.ImportMaterials ? ModelImporterMaterialImportMode.ImportStandard : ModelImporterMaterialImportMode.None;
                    changed = true;
                }
                if (importer.importCameras != settings.ImportCameras)
                {
                    importer.importCameras = settings.ImportCameras;
                    changed = true;
                }
                if (importer.importLights != settings.ImportLights)
                {
                    importer.importLights = settings.ImportLights;
                    changed = true;
                }
                if (importer.optimizeGameObjects != settings.OptimizeFbxGameObjects)
                {
                    importer.optimizeGameObjects = settings.OptimizeFbxGameObjects;
                    changed = true;
                }

                var loopMode = GetLoopMode(Normalize(System.IO.Path.GetFileNameWithoutExtension(path)), settings);
                if (loopMode.HasValue)
                {
                    var clipAnimations = importer.clipAnimations;
                    if (clipAnimations == null || clipAnimations.Length == 0)
                        clipAnimations = importer.defaultClipAnimations;
                    if (clipAnimations != null)
                    {
                        foreach (var clipAnimation in clipAnimations)
                        {
                            if (clipAnimation.loopTime != loopMode.Value)
                            {
                                clipAnimation.loopTime = loopMode.Value;
                                changed = true;
                            }
                            if (clipAnimation.loopPose != loopMode.Value)
                            {
                                clipAnimation.loopPose = loopMode.Value;
                                changed = true;
                            }
                        }
                        importer.clipAnimations = clipAnimations;
                    }
                }

                if (changed) importer.SaveAndReimport();

                if (settings.FbxRigType == PlayerAnimationRigType.Humanoid && settings.CreateAvatarFromFbx)
                {
                    var assets = AssetDatabase.LoadAllAssetsAtPath(path);
                    var hasValidAvatar = assets.OfType<Avatar>().Any(avatar => avatar != null && avatar.isHuman && avatar.isValid);
                    if (!hasValidAvatar)
                        Debug.LogWarning("UnderGrind: il file animazione non ha generato un Avatar Humanoid valido: " + path);
                }
            }
        }
        private static bool? GetLoopMode(string assetName, PlayerAnimationSettings settings)
        {
            if (assetName.Contains("standingidle")) return settings.IdleLoops;
            if (assetName.Contains("walking")) return settings.WalkLoops;
            if (assetName.Contains("running")) return settings.RunLoops;
            if (assetName.Contains("jump")) return settings.JumpLoops;
            if (assetName.Contains("fall")) return settings.FallLoops;
            if (assetName.Contains("landing")) return settings.LandLoops;
            if (assetName.Contains("crouchingidle")) return settings.CrouchIdleLoops;
            if (assetName.Contains("crouchwalk")) return settings.CrouchWalkLoops;
            return null;
        }

        private static void RefreshClipReferences(PlayerAnimationSettings settings)
        {
            var clips = new List<AnimationClipSource>();
            var guids = AssetDatabase.FindAssets(string.Empty, new[] { AnimationFolder });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var assetName = Normalize(System.IO.Path.GetFileNameWithoutExtension(path));
                foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                    if (asset is AnimationClip clip &&
                        !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
                        clips.Add(new AnimationClipSource { Clip = clip, AssetName = assetName });
            }

            var serialized = new SerializedObject(settings);
            AssignIfMissing(serialized, "idleClip", clips, "standingidle", "idle");
            AssignIfMissing(serialized, "walkClip", clips, "walking", "walk");
            AssignIfMissing(serialized, "runClip", clips, "running", "run");
            AssignIfMissing(serialized, "jumpClip", clips, "jump");
            AssignIfMissing(serialized, "fallClip", clips, "falling", "fall");
            AssignIfMissing(serialized, "landClip", clips, "hardlanding", "landing", "land");
            AssignIfMissing(serialized, "crouchIdleClip", clips, "crouchingidle", "crouchidle");
            AssignIfMissing(serialized, "crouchWalkClip", clips, "crouchwalkforward", "crouchwalk");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
        }

        private static void AssignIfMissing(SerializedObject serialized, string propertyName,
            List<AnimationClipSource> clips, params string[] names)
        {
            var property = serialized.FindProperty(propertyName);
            if (property == null || property.objectReferenceValue != null) return;

            foreach (var name in names)
            {
                var target = Normalize(name);
                var match = clips.Find(x => Normalize(x.Clip.name) == target);
                match ??= clips.Find(x => x.AssetName == target);
                match ??= clips.Find(x => Normalize(x.Clip.name).Contains(target));
                match ??= clips.Find(x => x.AssetName.Contains(target));
                if (match == null) continue;
                property.objectReferenceValue = match.Clip;
                return;
            }
        }

        private static List<string> GetInvalidAnimatorParameters(PlayerAnimationSettings settings)
        {
            var invalid = new List<string>();
            var names = new HashSet<string>(StringComparer.Ordinal);
            var parameters = new[]
            {
                new { Name = settings.SpeedParameterName, Label = "Speed" },
                new { Name = settings.MoveXParameterName, Label = "MoveX" },
                new { Name = settings.MoveYParameterName, Label = "MoveY" },
                new { Name = settings.GroundedParameterName, Label = "Grounded" },
                new { Name = settings.VerticalSpeedParameterName, Label = "VerticalSpeed" },
                new { Name = settings.RunningParameterName, Label = "Running" },
                new { Name = settings.CrouchingParameterName, Label = "Crouching" },
                new { Name = settings.JumpingParameterName, Label = "Jumping" },
                new { Name = settings.FallingParameterName, Label = "Falling" },
                new { Name = settings.LandedParameterName, Label = "Landed" }
            };

            foreach (var parameter in parameters)
            {
                if (string.IsNullOrWhiteSpace(parameter.Name))
                {
                    invalid.Add(parameter.Label + ": nome non configurato.");
                    continue;
                }
                if (!names.Add(parameter.Name))
                    invalid.Add(parameter.Label + ": il nome '" + parameter.Name + "' è già utilizzato da un altro parametro.");
            }
            return invalid;
        }

        private static void AddParameters(AnimatorController controller, PlayerAnimationSettings settings)
        {
            AddParameter(controller, settings.SpeedParameterName, AnimatorControllerParameterType.Float);
            AddParameter(controller, settings.MoveXParameterName, AnimatorControllerParameterType.Float);
            AddParameter(controller, settings.MoveYParameterName, AnimatorControllerParameterType.Float);
            AddParameter(controller, settings.GroundedParameterName, AnimatorControllerParameterType.Bool);
            AddParameter(controller, settings.VerticalSpeedParameterName, AnimatorControllerParameterType.Float);
            AddParameter(controller, settings.RunningParameterName, AnimatorControllerParameterType.Bool);
            AddParameter(controller, settings.CrouchingParameterName, AnimatorControllerParameterType.Bool);
            AddParameter(controller, settings.JumpingParameterName, AnimatorControllerParameterType.Bool);
            AddParameter(controller, settings.FallingParameterName, AnimatorControllerParameterType.Bool);
            AddParameter(controller, settings.LandedParameterName, AnimatorControllerParameterType.Bool);
        }

        private static void AddParameter(AnimatorController controller, string name,
            AnimatorControllerParameterType type)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            foreach (var parameter in controller.parameters)
                if (parameter.name == name) return;
            controller.AddParameter(name, type);
        }

        private static void BuildStateMachine(AnimatorStateMachine stateMachine, PlayerAnimationSettings settings)
        {
            var locomotionTree = CreateBlendTree("Locomotion Blend", settings.SpeedParameterName,
                settings.IdleClip, settings.IdleThreshold,
                settings.WalkClip, settings.WalkThreshold,
                settings.RunClip, settings.RunThreshold);
            AssetDatabase.AddObjectToAsset(locomotionTree, stateMachine);
            var locomotion = stateMachine.AddState("Locomotion");
            locomotion.motion = locomotionTree;
            stateMachine.defaultState = locomotion;

            var crouchTree = CreateBlendTree("Crouch Blend", settings.SpeedParameterName,
                settings.CrouchIdleClip, settings.IdleThreshold,
                settings.CrouchWalkClip, settings.CrouchWalkThreshold);
            AssetDatabase.AddObjectToAsset(crouchTree, stateMachine);
            var crouch = stateMachine.AddState("Crouch");
            crouch.motion = crouchTree;

            var jump = stateMachine.AddState("Jump");
            jump.motion = settings.JumpClip;
            var fall = settings.FallClip != null ? stateMachine.AddState("Fall") : null;
            if (fall != null) fall.motion = settings.FallClip;
            var land = settings.LandClip != null ? stateMachine.AddState("Land") : null;
            if (land != null) land.motion = settings.LandClip;

            AddConditionTransition(locomotion, crouch, settings, settings.CrouchingParameterName,
                AnimatorConditionMode.If, 0f);
            AddConditionTransition(crouch, locomotion, settings, settings.CrouchingParameterName,
                AnimatorConditionMode.IfNot, 0f);
            AddConditionTransition(stateMachine, jump, settings, settings.JumpingParameterName,
                AnimatorConditionMode.If, 0f);

            if (fall != null)
                AddConditionTransition(jump, fall, settings, settings.FallingParameterName,
                    AnimatorConditionMode.If, 0f);

            if (land != null)
            {
                var landingSource = fall != null ? fall : jump;
                AddConditionTransition(landingSource, land, settings, settings.LandedParameterName,
                    AnimatorConditionMode.If, 0f);
                var exit = land.AddTransition(locomotion);
                exit.hasExitTime = true;
                exit.exitTime = settings.LandingExitTime;
                exit.duration = settings.TransitionDuration;
            }
            else
            {
                var groundedSource = fall != null ? fall : jump;
                AddConditionTransition(groundedSource, locomotion, settings, settings.GroundedParameterName,
                    AnimatorConditionMode.If, 0f);
            }
        }

        private static BlendTree CreateBlendTree(string name, string parameterName,
            AnimationClip first, float firstThreshold, AnimationClip second, float secondThreshold,
            AnimationClip third = null, float thirdThreshold = 0f)
        {
            var tree = new BlendTree
            {
                name = name,
                blendType = BlendTreeType.Simple1D,
                blendParameter = parameterName,
                useAutomaticThresholds = false
            };
            if (first != null) tree.AddChild(first, firstThreshold);
            if (second != null) tree.AddChild(second, secondThreshold);
            if (third != null) tree.AddChild(third, thirdThreshold);
            return tree;
        }

        private static void AddConditionTransition(AnimatorState source, AnimatorState destination,
            PlayerAnimationSettings settings, string parameter, AnimatorConditionMode mode, float threshold)
        {
            if (source == null || destination == null || string.IsNullOrWhiteSpace(parameter)) return;
            var transition = source.AddTransition(destination);
            transition.hasExitTime = false;
            transition.duration = settings.TransitionDuration;
            transition.AddCondition(mode, threshold, parameter);
        }

        private static void AddConditionTransition(AnimatorStateMachine source, AnimatorState destination,
            PlayerAnimationSettings settings, string parameter, AnimatorConditionMode mode, float threshold)
        {
            if (destination == null || string.IsNullOrWhiteSpace(parameter)) return;
            var transition = source.AddAnyStateTransition(destination);
            transition.hasExitTime = false;
            transition.duration = settings.TransitionDuration;
            transition.canTransitionToSelf = false;
            transition.AddCondition(mode, threshold, parameter);
        }

        private static string Normalize(string value)
        {
            return value.Replace(" ", string.Empty).Replace("_", string.Empty)
                .Replace("-", string.Empty).ToLowerInvariant();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = System.IO.Path.GetDirectoryName(path)?.Replace("\\", "/");
            var name = System.IO.Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, name);
        }
    }
}