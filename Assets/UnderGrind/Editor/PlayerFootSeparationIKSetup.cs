using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnderGrind.Locomotion;

namespace UnderGrind.Editor
{
    public static class PlayerFootSeparationIKSetup
    {
        [MenuItem("UnderGrind/Player/Enable Foot Separation IK on Selected Animator", true)]
        private static bool ValidateEnable()
        {
            return Selection.activeGameObject != null &&
                   Selection.activeGameObject.GetComponent<Animator>() != null;
        }

        [MenuItem("UnderGrind/Player/Enable Foot Separation IK on Selected Animator")]
        private static void EnableOnSelectedAnimator()
        {
            var target = Selection.activeGameObject;
            var animator = target.GetComponent<Animator>();
            var controller = animator.runtimeAnimatorController as AnimatorController;
            if (controller == null)
            {
                EditorUtility.DisplayDialog("UnderGrind",
                    "L'Animator selezionato deve usare un AnimatorController modificabile.", "OK");
                return;
            }

            var layers = controller.layers;
            for (var i = 0; i < layers.Length; i++)
            {
                layers[i].iKPass = true;
                controller.SetLayer(i, layers[i]);
            }

            if (target.GetComponent<PlayerFootSeparationIK>() == null)
                Undo.AddComponent<PlayerFootSeparationIK>(target);

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            Debug.Log("UnderGrind: IK Pass abilitato e PlayerFootSeparationIK aggiunto a " + target.name, target);
        }
    }
}
