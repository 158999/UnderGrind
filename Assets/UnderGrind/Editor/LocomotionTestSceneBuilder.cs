using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnderGrind.Locomotion;
using UnderGrind.Inventory;
using UnderGrind.Testing;

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
            var grayboxSettings = AssetDatabase.LoadAssetAtPath<PlayerGrayboxSettings>(
                "Assets/UnderGrind/Settings/PlayerGrayboxSettings.asset");
            if (locomotionSettings == null || inventorySettings == null ||
                interactionSettings == null || inventoryUISettings == null ||
                rigSettings == null || animationSettings == null || grayboxSettings == null)
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
            CreateLighting(grayboxSettings);
            CreateCourse(grayboxSettings);
            CreatePlayer(malePrefab, locomotionSettings, inputSettings, inventorySettings,
                interactionSettings, inventoryUISettings, rigSettings, animationSettings, grayboxSettings);

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
                "Scena creata: " + ScenePath + "\n\nApri la scena e premi Play. Controlli: WASD, mouse, SHIFT, SPAZIO, C, F5, E, G, TAB, [ e ].",
                "OK");
        }

        private static void CreatePlayer(GameObject human, PlayerLocomotionSettings locomotionSettings,
            PlayerInputSettings inputSettings, InventorySettings inventorySettings,
            PlayerInteractionSettings interactionSettings, InventoryUISettings inventoryUISettings,
            PlayerRigSettings rigSettings, PlayerAnimationSettings animationSettings,
            PlayerGrayboxSettings grayboxSettings)
        {
            var player = new GameObject("Player - Human " + locomotionSettings.CharacterHeight.ToString("0.00") + "m");
            player.transform.position = grayboxSettings.PlayerSpawnPosition;
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
            instance.name = "Human Base - " + human.name;
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

            player.AddComponent<PlayerInputRebindingController>();

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

        private static void CreateCourse(PlayerGrayboxSettings settings)
        {
            var ground = MakeMaterial("Test Ground", settings.GroundColor);
            var concrete = MakeMaterial("Test Concrete", settings.ConcreteColor);
            var marker = MakeMaterial("Measurement Marker", settings.MarkerColor);
            var accent = MakeMaterial("Test Accent", settings.AccentColor);

            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Test Floor " + settings.FloorSize.x.ToString("0.#") + "m x " + settings.FloorSize.y.ToString("0.#") + "m";
            floor.transform.position = settings.FloorPosition;
            floor.transform.localScale = new Vector3(settings.FloorSize.x / 10f, 1f, settings.FloorSize.y / 10f);
            floor.GetComponent<Renderer>().sharedMaterial = ground;

            if (settings.MarkerInterval > 0f)
            {
                for (var distance = settings.MarkerInterval; distance <= settings.MarkerMaxDistance + 0.0001f;
                     distance += settings.MarkerInterval)
                {
                    var line = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    line.name = "Distance " + distance.ToString("0.##") + "m";
                    line.transform.position = new Vector3(settings.MarkerLineX,
                        settings.FloorPosition.y + settings.MarkerSurfaceOffset, distance);
                    line.transform.localScale = new Vector3(settings.MarkerLineWidth,
                        settings.MarkerLineHeight, settings.MarkerLineDepth);
                    line.GetComponent<Renderer>().sharedMaterial = marker;
                    UnityEngine.Object.DestroyImmediate(line.GetComponent<Collider>());

                    var label = new GameObject(distance.ToString("0.##") + " m");
                    label.transform.position = new Vector3(settings.MarkerLineX + settings.MarkerLabelOffset.x,
                        settings.FloorPosition.y + settings.MarkerLabelOffset.y,
                        distance + settings.MarkerLabelOffset.z);
                    var text = label.AddComponent<TextMesh>();
                    text.text = distance.ToString("0.##") + " m";
                    text.characterSize = settings.MarkerTextCharacterSize;
                    text.fontSize = settings.MarkerTextFontSize;
                    text.anchor = settings.MarkerTextAlignment;
                    text.color = settings.MarkerTextColor;
                }
            }

            foreach (var block in settings.Blocks)
            {
                var material = ResolveGrayboxMaterial(block.MaterialKind, concrete, accent, marker);
                CreateBlock(block.ObjectName, block.Position, block.Size, material, block.ColliderEnabled);
            }

            foreach (var rampSettings in settings.Ramps)
            {
                var ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ramp.name = rampSettings.ObjectName;
                ramp.transform.position = rampSettings.Position;
                ramp.transform.localScale = rampSettings.Size;
                ramp.transform.rotation = Quaternion.Euler(rampSettings.RotationEuler);
                ramp.GetComponent<Renderer>().sharedMaterial =
                    ResolveGrayboxMaterial(rampSettings.MaterialKind, concrete, accent, marker);
                if (!rampSettings.ColliderEnabled)
                    UnityEngine.Object.DestroyImmediate(ramp.GetComponent<Collider>());
            }
        }

        private static Material ResolveGrayboxMaterial(GrayboxMaterialKind kind, Material concrete,
            Material accent, Material marker)
        {
            return kind switch
            {
                GrayboxMaterialKind.Accent => accent,
                GrayboxMaterialKind.Marker => marker,
                _ => concrete
            };
        }

        private static void CreateBlock(string name, Vector3 position, Vector3 size, Material material, bool colliderEnabled)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.position = position;
            block.transform.localScale = size;
            block.GetComponent<Renderer>().sharedMaterial = material;
            if (!colliderEnabled)
                UnityEngine.Object.DestroyImmediate(block.GetComponent<Collider>());
        }

        private static void CreateLighting(PlayerGrayboxSettings settings)
        {
            var lightObject = new GameObject("Directional Light");
            lightObject.transform.rotation = Quaternion.Euler(settings.DirectionalLightEuler);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = settings.DirectionalLightIntensity;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = settings.AmbientColor;
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