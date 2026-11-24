using System;
using UnityEditor;
using UnityEngine;
using UnderGrind.Locomotion;

namespace UnderGrind.Editor
{
    /// <summary>
    /// Creates the player Animator Controller automatically when the imported
    /// motion clips are available but the generated controller is missing.
    /// </summary>
    [InitializeOnLoad]
    internal static class PlayerAnimationControllerBootstrap
    {
        private const string SettingsPath = "Assets/UnderGrind/Settings/PlayerAnimationSettings.asset";
        private const string ControllerPath = "Assets/UnderGrind/Generated/Animation/PlayerAnimator.controller";
        private const string AnimationFolder = "Assets/UnderGrind/Animations";

        static PlayerAnimationControllerBootstrap()
        {
            EditorApplication.delayCall += EnsureController;
        }

        private static void EnsureController()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += EnsureController;
                return;
            }

            var settings = AssetDatabase.LoadAssetAtPath<PlayerAnimationSettings>(SettingsPath);
            if (settings == null || settings.AnimatorController != null &&
                AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath) != null)
                return;

            var requiredFiles = new[]
            {
                "Standing_Idle.fbx", "Walking.fbx", "Running.fbx", "Jump.fbx",
                "Fall_down.fbx", "Hard_Landing.fbx", "Crouching_Idle.fbx"
            };

            foreach (var file in requiredFiles)
            {
                var path = AnimationFolder + "/" + file;
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
                {
                    Debug.Log("UnderGrind: Animator Controller bootstrap attende l'importazione di " + path);
                    return;
                }
            }

            var configuredCrouchWalkPath = settings.CrouchWalkClip != null
                ? AssetDatabase.GetAssetPath(settings.CrouchWalkClip)
                : null;
            if (string.IsNullOrEmpty(configuredCrouchWalkPath) ||
                AssetDatabase.LoadAssetAtPath<GameObject>(configuredCrouchWalkPath) == null)
            {
                Debug.Log("UnderGrind: Animator Controller bootstrap attende l'importazione della clip Crouch Walk configurata.");
                return;
            }

            try
            {
                PlayerAnimationControllerBuilder.BuildFromSettingsBatch();
            }
            catch (Exception exception)
            {
                Debug.LogError("UnderGrind: generazione automatica Animator Controller fallita. " + exception);
            }
        }
    }
}
