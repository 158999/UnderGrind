using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Animations;
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
            if (locomotionSettings == null || inventorySettings == null ||
                interactionSettings == null || inventoryUISettings == null)
            {
                EditorUtility.DisplayDialog("Configurazione Player mancante",
                    "Uno o più asset di configurazione Player non sono disponibili. La scena non è stata modificata.",
                    "OK");
                return;
            }

            AssetDatabase.ImportAsset(HumanModelPath,
                ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            var maleSource = ResolveHumanModel("male");
            var femaleSource = ResolveHumanModel("female");
            if (maleSource == null || femaleSource == null)
            {
                const string message = "Unity deve importare entrambe le mesh 'male' e 'female' da humans.blend. " +
                    "La scena esistente non è stata modificata e non verrà creato un manichino sostitutivo. " +
                    "Controlla la Console e Unity > Preferences > External Tools.";
                Debug.LogError("UnderGrind: " + message + " Percorso: " + HumanModelPath);
                EditorUtility.DisplayDialog("Modelli umani non disponibili", message, "OK");
                return;
            }

            var malePrefab = CreateHumanPrefab(maleSource, "Human_Male", locomotionSettings.CharacterHeight);
            var femalePrefab = CreateHumanPrefab(femaleSource, "Human_Female", locomotionSettings.FemaleCharacterHeight);
            if (malePrefab == null || femalePrefab == null)
            {
                Debug.LogError("UnderGrind: impossibile generare entrambi i prefab dai modelli originali.");
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateLighting();
            CreateCourse();
            CreatePlayer(malePrefab, locomotionSettings, inputSettings, inventorySettings, interactionSettings, inventoryUISettings);

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

        private static void CreatePlayer(GameObject human, PlayerLocomotionSettings locomotionSettings, PlayerInputSettings inputSettings, InventorySettings inventorySettings, PlayerInteractionSettings interactionSettings, InventoryUISettings inventoryUISettings)
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
            ConfigureAnimations(instance);
            Debug.Log("UnderGrind: modello umano importato da " + HumanModelPath);

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

        private static void ApplyPlayerMaterial(GameObject model)
        {
            var playerMaterial = MakeMaterial("Human Base", new Color(0.43f, 0.61f, 0.68f));
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                if (materials.Length == 0)
                    renderer.sharedMaterial = playerMaterial;
                else
                {
                    for (var i = 0; i < materials.Length; i++)
                        materials[i] = playerMaterial;
                    renderer.sharedMaterials = materials;
                }
            }
        }

        private static void ConfigureAnimations(GameObject model)
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(HumanModelPath).OfType<AnimationClip>()
                .Where(x => !x.name.StartsWith("__preview", StringComparison.OrdinalIgnoreCase)).ToArray();
            if (clips.Length == 0) return;
            bool HasToken(AnimationClip clip, string token) =>
                clip.name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;
            AnimationClip FindLocomotion(params string[] tokens) => clips
                .Where(clip => !HasToken(clip, "crouch") && tokens.Any(token => HasToken(clip, token)))
                .OrderBy(clip => tokens.Any(token => string.Equals(clip.name, token, StringComparison.OrdinalIgnoreCase)) ? 0 : 1)
                .FirstOrDefault();
            AnimationClip FindAll(string[] tokens) => clips.FirstOrDefault(clip =>
                tokens.All(token => HasToken(clip, token)));

            var idle = FindLocomotion("idle", "standing");
            var walk = FindLocomotion("walk", "walking");
            var run = FindLocomotion("run", "running");
            var jump = FindLocomotion("jump");
            var crouchIdle = FindAll(new[] { "crouch", "idle" });
            var crouchWalk = FindAll(new[] { "crouch", "walk" });
            if (idle == null && walk == null && run == null) return;

            var controllerPath = GeneratedPath + "/LocomotionTest.controller";
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath) != null)
                AssetDatabase.DeleteAsset(controllerPath);
            var animatorController = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            animatorController.AddParameter("Speed", AnimatorControllerParameterType.Float);
            animatorController.AddParameter("MoveX", AnimatorControllerParameterType.Float);
            animatorController.AddParameter("MoveY", AnimatorControllerParameterType.Float);
            animatorController.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
            animatorController.AddParameter("VerticalSpeed", AnimatorControllerParameterType.Float);
            animatorController.AddParameter("Running", AnimatorControllerParameterType.Bool);
            animatorController.AddParameter("Crouching", AnimatorControllerParameterType.Bool);
            animatorController.AddParameter("Jumping", AnimatorControllerParameterType.Bool);
            animatorController.AddParameter("Falling", AnimatorControllerParameterType.Bool);
            var machine = animatorController.layers[0].stateMachine;

            AnimatorState AddState(string name, AnimationClip clip, Vector3 position)
            {
                if (clip == null) return null;
                var state = machine.AddState(name, position);
                state.motion = clip;
                return state;
            }

            var idleState = AddState("Idle", idle, new Vector3(250, 100));
            var walkState = AddState("Walk", walk, new Vector3(500, 100));
            var runState = AddState("Run", run, new Vector3(750, 100));
            var jumpState = AddState("Jump", jump, new Vector3(500, 300));
            var crouchIdleState = AddState("Crouch Idle", crouchIdle, new Vector3(250, 500));
            var crouchWalkState = AddState("Crouch Walk", crouchWalk, new Vector3(500, 500));
            machine.defaultState = idleState ?? walkState ?? runState ?? crouchIdleState ?? jumpState;

            void Transition(AnimatorState from, AnimatorState to, string parameter, AnimatorConditionMode mode, float threshold = 0f)
            {
                if (from == null || to == null || from == to) return;
                var transition = from.AddTransition(to);
                transition.hasExitTime = false;
                transition.duration = 0.12f;
                transition.AddCondition(mode, threshold, parameter);
            }

            Transition(idleState, walkState, "Speed", AnimatorConditionMode.Greater, 0.1f);
            Transition(idleState, runState, "Speed", AnimatorConditionMode.Greater, 1.6f);
            Transition(walkState, idleState, "Speed", AnimatorConditionMode.Less, 0.1f);
            Transition(walkState, runState, "Speed", AnimatorConditionMode.Greater, 1.6f);
            Transition(runState, walkState, "Speed", AnimatorConditionMode.Less, 1.6f);
            Transition(runState, idleState, "Speed", AnimatorConditionMode.Less, 0.1f);

            foreach (var state in new[] { idleState, walkState, runState })
            {
                if (state == null || crouchIdleState == null) continue;
                var toCrouch = state.AddTransition(crouchIdleState);
                toCrouch.hasExitTime = false;
                toCrouch.duration = 0.12f;
                toCrouch.AddCondition(AnimatorConditionMode.If, 0f, "Crouching");
            }

            if (crouchIdleState != null && crouchWalkState != null)
            {
                var startMove = crouchIdleState.AddTransition(crouchWalkState);
                startMove.hasExitTime = false;
                startMove.duration = 0.12f;
                startMove.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
                var stopMove = crouchWalkState.AddTransition(crouchIdleState);
                stopMove.hasExitTime = false;
                stopMove.duration = 0.12f;
                stopMove.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

                void AddCrouchExit(AnimatorState from, AnimatorState to, AnimatorConditionMode speedMode, float threshold)
                {
                    if (from == null || to == null) return;
                    var transition = from.AddTransition(to);
                    transition.hasExitTime = false;
                    transition.duration = 0.12f;
                    transition.AddCondition(AnimatorConditionMode.IfNot, 0f, "Crouching");
                    transition.AddCondition(speedMode, threshold, "Speed");
                }

                AddCrouchExit(crouchIdleState, idleState, AnimatorConditionMode.Less, 0.1f);
                AddCrouchExit(crouchIdleState, walkState, AnimatorConditionMode.Greater, 0.1f);
                AddCrouchExit(crouchWalkState, idleState, AnimatorConditionMode.Less, 0.1f);
                AddCrouchExit(crouchWalkState, walkState, AnimatorConditionMode.Greater, 0.1f);
            }

            if (jumpState != null)
            {
                var any = machine.AddAnyStateTransition(jumpState);
                any.hasExitTime = false;
                any.duration = 0.08f;
                any.canTransitionToSelf = false;
                any.AddCondition(AnimatorConditionMode.If, 0f, "Jumping");
                foreach (var state in new[] { idleState, walkState, runState, crouchIdleState, crouchWalkState })
                    Transition(jumpState, state, "Grounded", AnimatorConditionMode.If);
            }

            var animator = model.GetComponentInChildren<Animator>();
            var avatar = AssetDatabase.LoadAllAssetsAtPath(HumanModelPath).OfType<Avatar>().FirstOrDefault();
            if (animator == null && avatar != null)
                animator = model.AddComponent<Animator>();
            if (animator != null && animator.avatar == null && avatar != null)
                animator.avatar = avatar;
            if (animator == null || animator.avatar == null)
            {
                AssetDatabase.DeleteAsset(controllerPath);
                Debug.LogWarning("UnderGrind: clip trovate, ma nessun Avatar valido nel modello. Animazioni non collegate.");
                return;
            }
            animator.runtimeAnimatorController = animatorController;
            animator.applyRootMotion = false;
            animator.SetBool("Grounded", true);
        }

        private static void CreateFallbackHumanoid(Transform parent)
        {
            var skin = MakeMaterial("Fallback Skin", new Color(0.72f, 0.55f, 0.43f));
            var shirt = MakeMaterial("Fallback Shirt", new Color(0.28f, 0.34f, 0.38f));
            var trousers = MakeMaterial("Fallback Trousers", new Color(0.18f, 0.20f, 0.23f));
            Part("Torso", PrimitiveType.Capsule, parent, new Vector3(0f, 1.18f, 0f), new Vector3(0.36f, 0.35f, 0.22f), shirt);
            Part("Head", PrimitiveType.Sphere, parent, new Vector3(0f, 1.65f, 0f), new Vector3(0.23f, 0.26f, 0.22f), skin);
            Part("Pelvis", PrimitiveType.Cube, parent, new Vector3(0f, 0.88f, 0f), new Vector3(0.34f, 0.25f, 0.20f), trousers);
            Limb("Left Upper Arm", parent, new Vector3(-0.22f, 1.43f, 0f), new Vector3(-0.34f, 1.13f, 0f), 0.105f, shirt);
            Limb("Right Upper Arm", parent, new Vector3(0.22f, 1.43f, 0f), new Vector3(0.34f, 1.13f, 0f), 0.105f, shirt);
            Limb("Left Forearm", parent, new Vector3(-0.34f, 1.13f, 0f), new Vector3(-0.36f, 0.88f, 0.03f), 0.075f, skin);
            Limb("Right Forearm", parent, new Vector3(0.34f, 1.13f, 0f), new Vector3(0.36f, 0.88f, 0.03f), 0.075f, skin);
            Limb("Left Thigh", parent, new Vector3(-0.10f, 0.82f, 0f), new Vector3(-0.12f, 0.46f, 0f), 0.115f, trousers);
            Limb("Right Thigh", parent, new Vector3(0.10f, 0.82f, 0f), new Vector3(0.12f, 0.46f, 0f), 0.115f, trousers);
            Limb("Left Shin", parent, new Vector3(-0.12f, 0.46f, 0f), new Vector3(-0.12f, 0.08f, 0.02f), 0.075f, trousers);
            Limb("Right Shin", parent, new Vector3(0.12f, 0.46f, 0f), new Vector3(0.12f, 0.08f, 0.02f), 0.075f, trousers);
        }

        private static void Limb(string name, Transform parent, Vector3 a, Vector3 b, float radius, Material material)
        {
            var part = Part(name, PrimitiveType.Capsule, parent, (a + b) * 0.5f,
                new Vector3(radius * 2f, Vector3.Distance(a, b) * 0.5f, radius * 2f), material);
            part.transform.localRotation = Quaternion.FromToRotation(Vector3.up, b - a);
        }

        private static GameObject Part(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var item = GameObject.CreatePrimitive(type);
            item.name = name;
            item.transform.SetParent(parent, false);
            item.transform.localPosition = position;
            item.transform.localScale = scale;
            item.GetComponent<Renderer>().sharedMaterial = material;
            var collider = item.GetComponent<Collider>();
            if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
            return item;
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