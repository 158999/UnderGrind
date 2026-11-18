using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnderGrind.Locomotion;
using UnderGrind.Inventory;

namespace UnderGrind.Editor
{
    public static class LocomotionTestSceneBuilder
    {
        private const string ScenePath = "Assets/UnderGrind/Scenes/LocomotionTest.unity";
        private const string HumanModelPath = "Assets/UnderGrind/Art/Source/humans.blend";
        private const string GeneratedPath = "Assets/UnderGrind/Generated";
        private const string LocomotionSettingsPath = "Assets/UnderGrind/Settings/PlayerLocomotionSettings.asset";

        [MenuItem("UnderGrind/Build First Locomotion Test Scene")]
        public static void BuildScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null &&
                !EditorUtility.DisplayDialog(
                    "Sostituire la scena di locomozione?",
                    "La scena LocomotionTest.unity verrà ricreata. La gerarchia e le modifiche manuali presenti nella scena verranno sostituite. I materiali generati del percorso di test possono essere aggiornati.",
                    "Sostituisci scena", "Annulla"))
                return;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            EnsureFolder("Assets/UnderGrind/Scenes");
            EnsureFolder("Assets/UnderGrind/Art/Source");
            EnsureFolder(GeneratedPath);
            PlayerInputAssetBuilder.EnsureAssets();
            PlayerGameplayAssetBuilder.EnsureAssets();

            var inputSettings = AssetDatabase.LoadAssetAtPath<PlayerInputSettings>(
                "Assets/UnderGrind/Settings/PlayerInputSettings.asset");
            if (inputSettings == null)
            {
                EditorUtility.DisplayDialog("Configurazione input mancante",
                    "Non è stato possibile creare o caricare PlayerInputSettings. La scena non è stata modificata.",
                    "OK");
                return;
            }

            var locomotionSettings = AssetDatabase.LoadAssetAtPath<PlayerLocomotionSettings>(LocomotionSettingsPath);
            var inventorySettings = AssetDatabase.LoadAssetAtPath<InventorySettings>(
                "Assets/UnderGrind/Settings/PlayerInventorySettings.asset");
            var interactionSettings = AssetDatabase.LoadAssetAtPath<PlayerInteractionSettings>(
                "Assets/UnderGrind/Settings/PlayerInteractionSettings.asset");
            var inventoryUISettings = AssetDatabase.LoadAssetAtPath<InventoryUISettings>(
                "Assets/UnderGrind/Settings/PlayerInventoryUISettings.asset");
            var rigSettings = AssetDatabase.LoadAssetAtPath<PlayerRigSettings>(
                "Assets/UnderGrind/Settings/PlayerRigSettings.asset");
            var animationSettings = AssetDatabase.LoadAssetAtPath<PlayerAnimationSettings>(
                "Assets/UnderGrind/Settings/PlayerAnimationSettings.asset");
            if (locomotionSettings == null || inventorySettings == null ||
                interactionSettings == null || inventoryUISettings == null ||
                rigSettings == null || animationSettings == null)
            {
                EditorUtility.DisplayDialog("Configurazione Player mancante",
                    "Uno o più asset di configurazione Player non sono disponibili. La scena non è stata modificata.",
                    "OK");
                return;
            }

            var maleRiggedPath = GeneratedPath + "/Prefabs/" + rigSettings.MalePrefabName + ".prefab";
            var femaleRiggedPath = GeneratedPath + "/Prefabs/" + rigSettings.FemalePrefabName + ".prefab";
            var malePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(maleRiggedPath);
            var femalePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(femaleRiggedPath);

            if (malePrefab == null || femalePrefab == null)
            {
                if (System.IO.File.Exists(HumanModelPath))
                    AssetDatabase.ImportAsset(HumanModelPath,
                        ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                if (malePrefab == null)
                {
                    var maleSource = ResolveHumanModel("male");
                    if (maleSource != null)
                        malePrefab = CreateHumanPrefab(maleSource, "Human_Male", locomotionSettings.CharacterHeight);
                }
                if (femalePrefab == null)
                {
                    var femaleSource = ResolveHumanModel("female");
                    if (femaleSource != null)
                        femalePrefab = CreateHumanPrefab(femaleSource, "Human_Female", locomotionSettings.FemaleCharacterHeight);
                }
            }

            if (malePrefab == null)
            {
                const string message = "Non è disponibile il prefab maschile riggato e Unity non riesce a importare " +
                    "la mesh maschile da humans.blend. La scena esistente non è stata modificata.";
                Debug.LogError("UnderGrind: " + message + " Percorso: " + HumanModelPath);
                EditorUtility.DisplayDialog("Modello maschile non disponibile", message, "OK");
                return;
            }
            if (femalePrefab == null)
                Debug.LogWarning("UnderGrind: prefab femminile non disponibile. La scena di test maschile verrà comunque creata.");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateLighting();
            CreateCourse();
            CreatePlayer(malePrefab, locomotionSettings, inputSettings, inventorySettings, interactionSettings, inventoryUISettings, rigSettings, animationSettings);

            EditorSceneManager.SaveScene(scene, ScenePath);
            var buildScenes = EditorBuildSettings.scenes.ToList();
            if (buildScenes.All(x => x.path != ScenePath))
            {
                buildScenes.Add(new EditorBuildSettingsScene(ScenePath, true));
                EditorBuildSettings.scenes = buildScenes.ToArray();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("UnderGrind",
                "Scena creata: " + ScenePath + "\n\nApri la scena e premi Play. Controlli: WASD, SHIFT, SPAZIO, F5.",
                "OK");
        }

        private static void CreatePlayer(GameObject human, PlayerLocomotionSettings locomotionSettings, PlayerInputSettings inputSettings, InventorySettings inventorySettings, PlayerInteractionSettings interactionSettings, InventoryUISettings inventoryUISettings, PlayerRigSettings rigSettings, PlayerAnimationSettings animationSettings)
        {
            var player = new GameObject("Player - Human " + locomotionSettings.CharacterHeight.ToString("0.00") + "m");
            player.transform.position = new Vector3(0f, 0.05f, 0f);
            var controller = player.AddComponent<CharacterController>();
            controller.height = locomotionSettings.CharacterHeight;
            controller.radius = locomotionSettings.CapsuleRadius;
            controller.center = Vector3.up * (controller.height * 0.5f);
            controller.stepOffset = Mathf.Min(locomotionSettings.CharacterStepOffset,
                controller.height * locomotionSettings.CharacterStepOffsetHeightRatio);
            controller.slopeLimit = locomotionSettings.CharacterSlopeLimit;
            controller.minMoveDistance = locomotionSettings.CharacterMinMoveDistance;
            controller.skinWidth = locomotionSettings.CharacterSkinWidth;
            controller.detectCollisions = locomotionSettings.CharacterDetectCollisions;
            controller.enableOverlapRecovery = locomotionSettings.CharacterEnableOverlapRecovery;

            var visualRoot = new GameObject("Visual");
            visualRoot.transform.SetParent(player.transform, false);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(human);
            if (instance == null) instance = UnityEngine.Object.Instantiate(human);
            instance.name = "Human Base - imported from humans.blend";
            instance.transform.SetParent(visualRoot.transform, false);
            ConfigureAnimations(instance, animationSettings);
            if (instance.GetComponentInChildren<PlayerVisualRig>(true) == null)
            {
                var visualRig = player.AddComponent<PlayerVisualRig>();
                if (!visualRig.ConfigureFromHierarchy(instance.transform, rigSettings))
                    Debug.LogWarning("UnderGrind: il prefab corrente non espone renderer separati secondo PlayerRigSettings.", instance);
            }
            Debug.Log("UnderGrind: prefab umano selezionato: " + human.name);

            var pivot = new GameObject("View Pivot").transform;
            pivot.SetParent(player.transform, false);
            pivot.localPosition = Vector3.up * locomotionSettings.StandingViewHeight;

            var cameraObject = new GameObject("Player Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(pivot, false);
            var camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = locomotionSettings.CameraFieldOfView;
            camera.nearClipPlane = locomotionSettings.CameraNearClipPlane;
            cameraObject.AddComponent<AudioListener>();

            var inventory = player.AddComponent<PlayerInventory>();
            inventory.Configure(inventorySettings);

            var movement = player.AddComponent<HumanLocomotionController>();
            movement.ConfigureSettings(locomotionSettings);
            movement.ConfigureInputSettings(inputSettings);
            movement.ConfigureAnimationSettings(animationSettings);
            movement.ConfigureViewRig(pivot, camera);

            var inventoryUI = player.AddComponent<PlayerInventoryUI>();
            inventoryUI.Configure(inventoryUISettings, inputSettings, inventory, movement);
            var interaction = player.AddComponent<PlayerInteractionController>();
            interaction.Configure(inputSettings, interactionSettings, inventory, movement, inventoryUI, camera);
            player.AddComponent<LocomotionDebugOverlay>();
        }

        private static GameObject CreateHumanPrefab(GameObject source, string prefabName, float targetHeight)
        {
            var prefabPath = GeneratedPath + "/Prefabs/" + prefabName + ".prefab";
            EnsureFolder(GeneratedPath + "/Prefabs");
            var temporary = UnityEngine.Object.Instantiate(source);
            temporary.name = prefabName;
            FitModelToHeight(temporary, targetHeight);
            var prefab = PrefabUtility.SaveAsPrefabAsset(temporary, prefabPath);
            UnityEngine.Object.DestroyImmediate(temporary);
            if (prefab == null)
                Debug.LogError("UnderGrind: salvataggio prefab fallito: " + prefabPath);
            else
                Debug.Log("UnderGrind: prefab creato: " + prefabPath + " | altezza " + targetHeight + " m");
            return prefab;
        }

        private static GameObject ResolveHumanModel(string sex)
        {
            // Inspect the imported hierarchy, but only consider subtrees with human-sized
            // bounds. This avoids choosing the whole Blender scene (which may contain more
            // than one body) or an isolated limb/torso as the player model.
            var candidates = AssetDatabase.LoadAllAssetsAtPath(HumanModelPath).OfType<GameObject>()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true).Select(t => t.gameObject))
                .Distinct()
                .Where(x => x.GetComponentInChildren<Renderer>(true) != null)
                .Where(x =>
                {
                    var size = GetCombinedBounds(x).size;
                    return size.y >= 0.5f && size.y <= 250f;
                }).ToList();
            if (candidates.Count == 0) return null;

            bool MatchesSex(GameObject item)
            {
                var name = item.name.ToLowerInvariant();
                if (sex == "female")
                    return name == "female" || name.Contains("female") || name.Contains("woman") || name.Contains("femmina");
                return name == "male" || (name.Contains("male") && !name.Contains("female")) ||
                    name.Contains("maschio") || name.Contains("uomo");
            }

            int Score(GameObject item)
            {
                var name = item.name.ToLowerInvariant();
                if (!MatchesSex(item)) return int.MinValue;
                var bounds = GetCombinedBounds(item);
                var score = name == sex ? 1000 : 100;
                if (name.Contains("human") || name.Contains("person") || name.Contains("character")) score += 20;
                if (bounds.size.y >= 0.5f && bounds.size.y <= 4f) score += 20;
                return score;
            }

            var selected = candidates.Where(MatchesSex).OrderByDescending(Score)
                .ThenByDescending(x => GetCombinedBounds(x).size.y).FirstOrDefault();
            if (selected != null)
                Debug.Log("UnderGrind: candidato umano selezionato: " + selected.name + " | bounds " + GetCombinedBounds(selected).size);
            return selected;
        }

        private static Bounds GetCombinedBounds(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return new Bounds(root.transform.position, Vector3.zero);
            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private static void FitModelToHeight(GameObject model, float targetHeight)
        {
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            var initial = GetCombinedBounds(model);
            if (initial.size.y < 0.05f) return;
            model.transform.localScale *= targetHeight / initial.size.y;
            var fitted = GetCombinedBounds(model);
            var parentPosition = model.transform.parent != null ? model.transform.parent.position : Vector3.zero;
            model.transform.position += new Vector3(parentPosition.x - fitted.center.x, parentPosition.y - fitted.min.y, parentPosition.z - fitted.center.z);
        }

        private static void ConfigureAnimations(GameObject model, PlayerAnimationSettings settings)
        {
            if (model == null || settings == null) return;

            var animator = model.GetComponentInChildren<Animator>(true);
            var avatar = AssetDatabase.LoadAllAssetsAtPath(HumanModelPath).OfType<Avatar>()
                .FirstOrDefault(candidate => candidate != null && candidate.isValid);

            if (animator == null && avatar != null)
                animator = model.AddComponent<Animator>();
            if (animator == null)
            {
                Debug.LogWarning("UnderGrind: il modello importato non contiene un Animator. " +
                    "Le animazioni saranno disponibili dopo l'importazione di un prefab riggato.", model);
                return;
            }

            if (animator.avatar == null && avatar != null) animator.avatar = avatar;
            if (animator.avatar == null || !animator.avatar.isValid)
            {
                Debug.LogWarning("UnderGrind: nessun Avatar valido sul modello. " +
                    "Il PlayerAnimationDriver non potrà animare questo modello.", model);
                return;
            }

            if (settings.AnimatorController != null)
                animator.runtimeAnimatorController = settings.AnimatorController;
            animator.applyRootMotion = false;
        }

        private static Material MakeMaterial(string name, Color color)
        {
            var safeName = string.Concat(name.Select(c => char.IsLetterOrDigit(c) ? c : '_'));
            var path = GeneratedPath + "/" + safeName + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var activePipeline = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
                var shader = activePipeline != null
                    ? Shader.Find("Universal Render Pipeline/Lit")
                    : Shader.Find("Standard");
                shader ??= Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                    throw new InvalidOperationException("Nessuno shader Lit compatibile è disponibile nel progetto.");
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                var activePipeline = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
                var compatibleShader = activePipeline != null
                    ? Shader.Find("Universal Render Pipeline/Lit")
                    : Shader.Find("Standard");
                compatibleShader ??= Shader.Find("Universal Render Pipeline/Lit");
                if (compatibleShader != null && material.shader != compatibleShader)
                    material.shader = compatibleShader;
            }
            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void CreateCourse()
        {
            var ground = MakeMaterial("Test Ground", new Color(0.24f, 0.27f, 0.28f));
            var concrete = MakeMaterial("Test Concrete", new Color(0.43f, 0.46f, 0.47f));
            var marker = MakeMaterial("Measurement Marker", new Color(0.83f, 0.69f, 0.34f));
            var accent = MakeMaterial("Test Accent", new Color(0.30f, 0.52f, 0.49f));

            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Test Floor - 50m x 50m";
            floor.transform.position = new Vector3(0f, -0.05f, 10f);
            floor.transform.localScale = new Vector3(5f, 1f, 5f);
            floor.GetComponent<Renderer>().sharedMaterial = ground;

            for (var i = 1; i <= 10; i++)
            {
                var distance = i * 2f;
                var line = GameObject.CreatePrimitive(PrimitiveType.Cube);
                line.name = "Distance " + distance + "m";
                line.transform.position = new Vector3(0f, 0.006f, distance);
                line.transform.localScale = new Vector3(3f, 0.012f, 0.025f);
                line.GetComponent<Renderer>().sharedMaterial = marker;
                UnityEngine.Object.DestroyImmediate(line.GetComponent<Collider>());
                var label = new GameObject(distance + " m");
                label.transform.position = new Vector3(1.8f, 0.05f, distance);
                var text = label.AddComponent<TextMesh>();
                text.text = distance + " m";
                text.characterSize = 0.12f;
                text.fontSize = 48;
                text.anchor = TextAnchor.MiddleLeft;
                text.color = Color.white;
            }

            CreateBlock("Jump step 0.20m", new Vector3(4f, 0.10f, 5f), new Vector3(1.2f, 0.20f, 1.2f), concrete);
            CreateBlock("Jump step 0.40m", new Vector3(6f, 0.20f, 5f), new Vector3(1.2f, 0.40f, 1.2f), concrete);
            CreateBlock("Jump step 0.60m", new Vector3(8f, 0.30f, 5f), new Vector3(1.2f, 0.60f, 1.2f), concrete);
            for (var i = 0; i < 5; i++)
                CreateBlock("Stair " + (i + 1), new Vector3(-4f, 0.08f + i * 0.16f, 5f + i * 0.48f),
                    new Vector3(1.5f, 0.16f + i * 0.16f, 0.5f), accent);

            CreateBlock("Wall 0.80m", new Vector3(-6f, 0.40f, 10f), new Vector3(2.4f, 0.80f, 0.18f), concrete);
            CreateBlock("Wall 1.00m", new Vector3(-2.5f, 0.50f, 10f), new Vector3(2.4f, 1.00f, 0.18f), concrete);
            CreateBlock("Wall 1.20m", new Vector3(1f, 0.60f, 10f), new Vector3(2.4f, 1.20f, 0.18f), concrete);

            var ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ramp.name = "Ramp 15 degrees";
            ramp.transform.position = new Vector3(6f, 0.35f, 11f);
            ramp.transform.localScale = new Vector3(2.5f, 0.18f, 4f);
            ramp.transform.rotation = Quaternion.Euler(-15f, 0f, 0f);
            ramp.GetComponent<Renderer>().sharedMaterial = accent;

            CreateBlock("Platform A", new Vector3(10f, 0.25f, 16f), new Vector3(2f, 0.5f, 2f), concrete);
            CreateBlock("Platform B", new Vector3(10f, 0.25f, 19.5f), new Vector3(2f, 0.5f, 2f), concrete);
            CreateBlock("Platform C", new Vector3(10f, 0.25f, 23f), new Vector3(2f, 0.5f, 2f), concrete);
        }

        private static void CreateBlock(string name, Vector3 position, Vector3 size, Material material)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.position = position;
            block.transform.localScale = size;
            block.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static void CreateLighting()
        {
            var lightObject = new GameObject("Directional Light");
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.43f, 0.46f, 0.50f);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = System.IO.Path.GetDirectoryName(path)?.Replace("\\", "/");
            var folder = System.IO.Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            if (!string.IsNullOrEmpty(parent)) AssetDatabase.CreateFolder(parent, folder);
        }

    }
}