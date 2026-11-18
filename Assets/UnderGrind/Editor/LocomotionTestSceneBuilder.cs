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
        private const string GrayboxScenePath = "Assets/UnderGrind/Scenes/LocomotionGraybox.unity";
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
                HumanoidRigPrefabBuilder.BuildFromConfiguredSources();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                malePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(maleRiggedPath);
                femalePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(femaleRiggedPath);
            }

            var maleValid = ValidateRiggedPrefab(malePrefab, "maschile", rigSettings, out var maleError);
            var femaleValid = ValidateRiggedPrefab(femalePrefab, "femminile", rigSettings, out var femaleError);
            if (!maleValid || !femaleValid)
            {
                var message = !maleValid ? maleError : femaleError;
                Debug.LogError("UnderGrind: " + message);
                EditorUtility.DisplayDialog("Prefab Humanoid non valido",
                    message + "\n\nLa scena non è stata creata. Genera e verifica entrambi i prefab Humanoid prima di procedere.",
                    "OK");
                return;
            }
            var useFemale = rigSettings.DefaultCharacter == PlayerCharacterVariant.Female;
            var selectedPrefab = useFemale ? femalePrefab : malePrefab;
            var selectedHeight = useFemale
                ? locomotionSettings.FemaleCharacterHeight
                : locomotionSettings.CharacterHeight;
            var selectedVariant = useFemale ? "Female" : "Male";

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateLighting(grayboxSettings);
            CreateCourse(grayboxSettings);
            CreatePlayer(selectedPrefab, selectedHeight, selectedVariant, rigSettings.DefaultCharacter, locomotionSettings, inputSettings,
                inventorySettings, interactionSettings, inventoryUISettings, rigSettings, animationSettings, grayboxSettings);

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
                "Scena creata: " + ScenePath + "\nPersonaggio: " + selectedVariant + "\n\nApri la scena e premi Play. Controlli: WASD, mouse, SHIFT, SPAZIO, C, F5, E, G, TAB, [ e ].",
                "OK");
        }

        [MenuItem("UnderGrind/Build Graybox Locomotion Test (Temporary Proxy)")]
        public static void BuildGrayboxScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(GrayboxScenePath) != null &&
                !EditorUtility.DisplayDialog(
                    "Sostituire la scena Graybox?",
                    "La scena LocomotionGraybox.unity verrà ricreata. Il Player proxy temporaneo non è il personaggio definitivo.",
                    "Sostituisci scena", "Annulla"))
                return;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            EnsureFolder("Assets/UnderGrind/Scenes");
            EnsureFolder(GeneratedPath);
            PlayerInputAssetBuilder.EnsureAssets();
            PlayerGameplayAssetBuilder.EnsureAssets();

            var inputSettings = AssetDatabase.LoadAssetAtPath<PlayerInputSettings>(
                "Assets/UnderGrind/Settings/PlayerInputSettings.asset");
            var locomotionSettings = AssetDatabase.LoadAssetAtPath<PlayerLocomotionSettings>(LocomotionSettingsPath);
            var inventorySettings = AssetDatabase.LoadAssetAtPath<InventorySettings>(
                "Assets/UnderGrind/Settings/PlayerInventorySettings.asset");
            var interactionSettings = AssetDatabase.LoadAssetAtPath<PlayerInteractionSettings>(
                "Assets/UnderGrind/Settings/PlayerInteractionSettings.asset");
            var inventoryUISettings = AssetDatabase.LoadAssetAtPath<InventoryUISettings>(
                "Assets/UnderGrind/Settings/PlayerInventoryUISettings.asset");
            var rigSettings = AssetDatabase.LoadAssetAtPath<PlayerRigSettings>(
                "Assets/UnderGrind/Settings/PlayerRigSettings.asset");
            var grayboxSettings = AssetDatabase.LoadAssetAtPath<PlayerGrayboxSettings>(
                "Assets/UnderGrind/Settings/PlayerGrayboxSettings.asset");

            if (inputSettings == null || locomotionSettings == null || inventorySettings == null ||
                interactionSettings == null || inventoryUISettings == null || rigSettings == null ||
                grayboxSettings == null)
            {
                EditorUtility.DisplayDialog("Configurazione Graybox mancante",
                    "Uno o più asset di configurazione non sono disponibili. La scena non è stata modificata.",
                    "OK");
                return;
            }

            var useFemale = rigSettings.DefaultCharacter == PlayerCharacterVariant.Female;
            var characterHeight = useFemale
                ? locomotionSettings.FemaleCharacterHeight
                : locomotionSettings.CharacterHeight;
            var characterVariant = useFemale ? "Female" : "Male";

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateLighting(grayboxSettings);
            CreateCourse(grayboxSettings);
            CreateGrayboxProxyPlayer(characterHeight, characterVariant, rigSettings.DefaultCharacter, locomotionSettings,
                inputSettings, inventorySettings, interactionSettings, inventoryUISettings, grayboxSettings);

            EditorSceneManager.SaveScene(scene, GrayboxScenePath);
            var buildScenes = EditorBuildSettings.scenes.ToList();
            if (buildScenes.All(x => x.path != GrayboxScenePath))
            {
                buildScenes.Add(new EditorBuildSettingsScene(GrayboxScenePath, true));
                EditorBuildSettings.scenes = buildScenes.ToArray();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("UnderGrind Graybox",
                "Scena creata: " + GrayboxScenePath + "\nPlayer: proxy temporaneo " + characterVariant +
                "\n\nIl proxy serve solo a provare locomozione, camera, collisioni e percorso. Non sostituisce il prefab Humanoid definitivo.",
                "OK");
        }

        private static void CreateGrayboxProxyPlayer(float characterHeight, string characterVariant,
            PlayerCharacterVariant characterVariantId, PlayerLocomotionSettings locomotionSettings, PlayerInputSettings inputSettings,
            InventorySettings inventorySettings, PlayerInteractionSettings interactionSettings,
            InventoryUISettings inventoryUISettings, PlayerGrayboxSettings grayboxSettings)
        {
            var player = new GameObject("Player - GRAYBOX PROXY - " + characterVariant);
            player.transform.position = grayboxSettings.PlayerSpawnPosition;
            var controller = player.AddComponent<CharacterController>();
            controller.height = characterHeight;
            controller.radius = locomotionSettings.CapsuleRadius;
            controller.center = Vector3.up * (controller.height * 0.5f);
            controller.stepOffset = Mathf.Min(locomotionSettings.CharacterStepOffset,
                controller.height * locomotionSettings.CharacterStepOffsetHeightRatio);
            controller.slopeLimit = locomotionSettings.CharacterSlopeLimit;
            controller.minMoveDistance = locomotionSettings.CharacterMinMoveDistance;
            controller.skinWidth = locomotionSettings.CharacterSkinWidth;
            controller.detectCollisions = locomotionSettings.CharacterDetectCollisions;
            controller.enableOverlapRecovery = locomotionSettings.CharacterEnableOverlapRecovery;

            var proxy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            proxy.name = "GRAYBOX BODY PROXY - NOT FINAL CHARACTER";
            proxy.transform.SetParent(player.transform, false);
            proxy.transform.localPosition = Vector3.up * (characterHeight * 0.5f);
            proxy.transform.localScale = new Vector3(
                locomotionSettings.CapsuleRadius * 2f,
                characterHeight * 0.5f,
                locomotionSettings.CapsuleRadius * 2f);
            UnityEngine.Object.DestroyImmediate(proxy.GetComponent<Collider>());
            var proxyRenderer = proxy.GetComponent<Renderer>();
            proxyRenderer.sharedMaterial = MakeMaterial("Graybox Player Proxy", grayboxSettings.AccentColor);

            var visualRig = player.AddComponent<PlayerVisualRig>();
            visualRig.Configure(Array.Empty<Renderer>(), new[] { proxyRenderer },
                Array.Empty<Renderer>(), Array.Empty<Renderer>());

            var pivot = new GameObject("View Pivot").transform;
            pivot.SetParent(player.transform, false);
            pivot.localPosition = Vector3.up * locomotionSettings.GetStandingViewHeight(characterVariantId);

            var cameraObject = new GameObject("Player Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(pivot, false);
            var camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = locomotionSettings.CameraFieldOfView;
            camera.nearClipPlane = locomotionSettings.CameraNearClipPlane;
            cameraObject.AddComponent<AudioListener>();

            var inventory = player.AddComponent<PlayerInventory>();
            inventory.Configure(inventorySettings);
            var rebindingController = player.AddComponent<PlayerInputRebindingController>();
            rebindingController.Configure(inputSettings);

            var movement = player.AddComponent<HumanLocomotionController>();
            movement.ConfigureCharacterVariant(characterVariantId);
            movement.ConfigureSettings(locomotionSettings);
            movement.ConfigureInputSettings(inputSettings);
            movement.ConfigureViewRig(pivot, camera);

            var inventoryUI = player.AddComponent<PlayerInventoryUI>();
            inventoryUI.Configure(inventoryUISettings, inputSettings, inventory, movement);
            var interaction = player.AddComponent<PlayerInteractionController>();
            interaction.Configure(inputSettings, interactionSettings, inventory, movement, inventoryUI, camera);
            player.AddComponent<LocomotionDebugOverlay>();

            Debug.LogWarning("UnderGrind: creato Player proxy temporaneo per la scena Graybox. Non è un prefab/personaggio definitivo.");
        }

        private static void CreatePlayer(GameObject human, float characterHeight, string characterVariant,
            PlayerCharacterVariant characterVariantId, PlayerLocomotionSettings locomotionSettings, PlayerInputSettings inputSettings, InventorySettings inventorySettings,
            PlayerInteractionSettings interactionSettings, InventoryUISettings inventoryUISettings,
            PlayerRigSettings rigSettings, PlayerAnimationSettings animationSettings,
            PlayerGrayboxSettings grayboxSettings)
        {
            var player = new GameObject("Player - Human " + characterVariant + " " + characterHeight.ToString("0.00") + "m");
            player.transform.position = grayboxSettings.PlayerSpawnPosition;
            var controller = player.AddComponent<CharacterController>();
            controller.height = characterHeight;
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
            var visualRig = instance.GetComponentInChildren<PlayerVisualRig>(true);
            if (visualRig == null || !visualRig.IsConfigured)
                throw new InvalidOperationException("Il prefab " + human.name + " non contiene un PlayerVisualRig configurato.");
            Debug.Log("UnderGrind: prefab Humanoid selezionato: " + human.name);

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

            var rebindingController = player.AddComponent<PlayerInputRebindingController>();
            rebindingController.Configure(inputSettings);

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

        private static bool ValidateRiggedPrefab(GameObject prefab, string characterName,
            PlayerRigSettings settings, out string error)
        {
            if (prefab == null)
            {
                error = "Prefab " + characterName + " mancante. Verifica i due percorsi FBX in PlayerRigSettings.";
                return false;
            }

            var animator = prefab.GetComponentInChildren<Animator>(true);
            if (animator == null || animator.avatar == null || !animator.avatar.isValid || !animator.avatar.isHuman)
            {
                error = "Il prefab " + characterName + " non contiene un Animator con Avatar Humanoid valido.";
                return false;
            }

            if (prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length == 0)
            {
                error = "Il prefab " + characterName + " non contiene SkinnedMeshRenderer.";
                return false;
            }

            var visualRig = prefab.GetComponentInChildren<PlayerVisualRig>(true);
            if (visualRig == null || !visualRig.IsConfigured || visualRig.HeadRendererCount == 0 ||
                visualRig.BodyRendererCount == 0 ||
                (settings.RequireFirstPersonArms && visualRig.FirstPersonRendererCount == 0) ||
                (settings.RequireThirdPersonArms && visualRig.ThirdPersonRendererCount == 0))
            {
                error = "Il prefab " + characterName + " non soddisfa i gruppi renderer richiesti in PlayerRigSettings.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        private static void ConfigureAnimations(GameObject model, PlayerAnimationSettings settings)
        {
            var animator = model.GetComponentInChildren<Animator>(true);
            if (animator == null || animator.avatar == null || !animator.avatar.isValid || !animator.avatar.isHuman)
                throw new InvalidOperationException("Il modello deve avere un Avatar Humanoid valido prima di configurare le animazioni.");

            if (settings.AnimatorController != null)
                animator.runtimeAnimatorController = settings.AnimatorController;
            else
                Debug.LogWarning("UnderGrind: Avatar Humanoid valido, ma AnimatorController non ancora generato. Importa le otto clip e usa Refresh Clips and Build Animator Controller.", model);

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