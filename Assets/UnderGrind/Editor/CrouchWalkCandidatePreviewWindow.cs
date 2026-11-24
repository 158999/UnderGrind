using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnderGrind.Locomotion;

namespace UnderGrind.Editor
{
    /// <summary>
    /// Previews crouch-walk clips on temporary instances of the production Humanoid prefabs.
    /// No production prefab, Animator Controller, or PlayerAnimationSettings asset is modified.
    /// </summary>
    public sealed class CrouchWalkCandidatePreviewWindow : EditorWindow
    {
        private const string CandidateFolder = "Assets/UnderGrind/Animations/Candidates/PINOC_CrouchWalk";
        private const string AnimationSettingsPath = "Assets/UnderGrind/Settings/PlayerAnimationSettings.asset";
        private const string RigSettingsPath = "Assets/UnderGrind/Settings/PlayerRigSettings.asset";

        private enum CharacterVariant
        {
            Male,
            Female
        }

        private enum CameraAngle
        {
            Front,
            ThreeQuarter,
            Side,
            Rear
        }

        private sealed class ClipOption
        {
            public string Label;
            public AnimationClip Clip;
        }

        private readonly List<ClipOption> _clipOptions = new List<ClipOption>();
        private PreviewRenderUtility _preview;
        private GameObject _previewInstance;
        private Animator _animator;
        private PlayableGraph _graph;
        private AnimationClipPlayable _clipPlayable;
        private Bounds _modelBounds;
        private CharacterVariant _character;
        private CameraAngle _cameraAngle = CameraAngle.ThreeQuarter;
        private int _clipIndex;
        private float _time;
        private float _playbackSpeed = 1f;
        private float _cameraDistance = 3.8f;
        private bool _isPlaying = true;
        private bool _loop = true;
        private bool _hasGraph;
        private double _lastUpdateTime;
        private string _status;
        private bool _showInspectionOptions;
        private int _inspectionSampleRate = 30;
        private int _inspectionFrameCount = 12;
        private int _inspectionCellWidth = 192;
        private int _inspectionCellHeight = 320;
        private int _inspectionMaxSamples = 12000;
        private bool _inspectionIncludeCurveKeys = true;
        private bool _inspectionCaptureGeometry = true;
        private float _inspectionContactHeight = 0.04f;
        private float _inspectionContactSpeed = 0.15f;
        private float _inspectionCrossingTolerance = 0.02f;

        [MenuItem("UnderGrind/Animation/Crouch Walk Candidate Preview")]
        public static void Open()
        {
            var window = GetWindow<CrouchWalkCandidatePreviewWindow>();
            window.titleContent = new GUIContent("Crouch Walk Preview");
            window.minSize = new Vector2(560f, 760f);
            window.Show();
        }

        private void OnEnable()
        {
            _preview = new PreviewRenderUtility();
            _preview.cameraFieldOfView = 30f;
            _preview.camera.nearClipPlane = 0.01f;
            _preview.camera.farClipPlane = 100f;
            ReloadClipOptions();
            RebuildCharacter();
            EditorApplication.update += EditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= EditorUpdate;
            DestroyGraph();
            DestroyPreviewInstance();
            if (_preview != null)
            {
                _preview.Cleanup();
                _preview = null;
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Anteprima animazione accovacciata", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Questa finestra anima un'istanza temporanea del prefab selezionato. " +
                "Non modifica prefab, controller Animator o PlayerAnimationSettings.",
                MessageType.Info);

            DrawSelectionControls();
            DrawPlaybackControls();
            DrawInspectionExportControls();
            DrawPreview();
            if (!string.IsNullOrEmpty(_status))
                EditorGUILayout.HelpBox(_status, MessageType.Warning);
        }

        private void DrawSelectionControls()
        {
            var previousCharacter = _character;
            var previousClipIndex = _clipIndex;
            _character = (CharacterVariant)EditorGUILayout.EnumPopup("Personaggio", _character);
            var labels = _clipOptions.Select(option => option.Label).ToArray();
            if (labels.Length == 0) labels = new[] { "Nessuna clip disponibile" };
            _clipIndex = Mathf.Clamp(_clipIndex, 0, labels.Length - 1);
            _clipIndex = EditorGUILayout.Popup("Animazione", _clipIndex, labels);
            _cameraAngle = (CameraAngle)EditorGUILayout.EnumPopup("Inquadratura", _cameraAngle);
            _cameraDistance = EditorGUILayout.Slider("Distanza camera", _cameraDistance, 1.5f, 8f);

            if (_character != previousCharacter)
                RebuildCharacter();
            else if (_clipIndex != previousClipIndex)
                RebuildGraph();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Ricarica candidati"))
                {
                    ReloadClipOptions();
                    RebuildGraph();
                }
                if (GUILayout.Button("Ricarica personaggio"))
                    RebuildCharacter();
            }
        }

        private void DrawPlaybackControls()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(_isPlaying ? "Pausa" : "Riproduci", GUILayout.Width(90f)))
                {
                    _isPlaying = !_isPlaying;
                    _lastUpdateTime = EditorApplication.timeSinceStartup;
                }

                if (GUILayout.Button("Inizio", GUILayout.Width(70f)))
                {
                    _time = 0f;
                    EvaluateAtCurrentTime();
                    _isPlaying = false;
                }

                _loop = GUILayout.Toggle(_loop, "Loop", "Button", GUILayout.Width(65f));
                GUILayout.FlexibleSpace();
                _playbackSpeed = EditorGUILayout.Slider(_playbackSpeed, 0.1f, 2f, GUILayout.Width(150f));
            }

            var clip = GetSelectedClip();
            if (clip == null)
            {
                EditorGUILayout.HelpBox("Selezionare una clip importata valida.", MessageType.Warning);
                return;
            }

            var normalizedTime = clip.length > 0f ? _time / clip.length : 0f;
            EditorGUI.BeginChangeCheck();
            normalizedTime = EditorGUILayout.Slider("Tempo normalizzato", normalizedTime, 0f, 1f);
            if (EditorGUI.EndChangeCheck())
            {
                _time = normalizedTime * clip.length;
                EvaluateAtCurrentTime();
                _isPlaying = false;
            }

            EditorGUILayout.LabelField(
                string.Format("{0:0.000}s / {1:0.000}s", _time, clip.length),
                EditorStyles.miniLabel);
        }

        private void DrawInspectionExportControls()
        {
            EditorGUILayout.Space(8f);
            _showInspectionOptions = EditorGUILayout.Foldout(
                _showInspectionOptions, "Report diagnostico ultra dettagliato", true);
            if (!_showInspectionOptions)
                return;

            EditorGUILayout.HelpBox(
                "Il report campiona entrambi gli Avatar Player, registra le pose di tutte le ossa Humanoid, " +
                "le curve/keyframe, le derivate cinematiche, le metriche di piedi e ginocchia, le mesh skinned " +
                "e una contact sheet con quattro angolazioni. Il pacchetto viene compresso in ZIP.",
                MessageType.Info);

            _inspectionSampleRate = EditorGUILayout.IntSlider("Campionamento pose (Hz)", _inspectionSampleRate, 5, 120);
            _inspectionFrameCount = EditorGUILayout.IntSlider("Istantanee per clip", _inspectionFrameCount, 4, 24);
            _inspectionCellWidth = EditorGUILayout.IntSlider("Larghezza immagine (px)", _inspectionCellWidth, 128, 512);
            _inspectionCellHeight = EditorGUILayout.IntSlider("Altezza immagine (px)", _inspectionCellHeight, 192, 768);
            _inspectionMaxSamples = EditorGUILayout.IntSlider("Limite campioni posa", _inspectionMaxSamples, 100, 50000);
            _inspectionIncludeCurveKeys = EditorGUILayout.Toggle("Includi tempi di tutti i keyframe", _inspectionIncludeCurveKeys);
            _inspectionCaptureGeometry = EditorGUILayout.Toggle("Analizza mesh skinned deformate", _inspectionCaptureGeometry);
            _inspectionContactHeight = EditorGUILayout.FloatField("Tolleranza altezza contatto (m)", _inspectionContactHeight);
            _inspectionContactSpeed = EditorGUILayout.FloatField("Velocità massima contatto (m/s)", _inspectionContactSpeed);
            _inspectionCrossingTolerance = EditorGUILayout.FloatField("Tolleranza incrocio anatomico (m)", _inspectionCrossingTolerance);

            EditorGUILayout.Space(4f);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (GUILayout.Button("Esporta clip selezionata — entrambi i personaggi", GUILayout.Height(30f)))
                {
                    var clip = GetSelectedClip();
                    if (clip == null)
                        EditorUtility.DisplayDialog("Animation Inspection", "Nessuna clip selezionata.", "OK");
                    else
                    {
                        var label = _clipOptions[_clipIndex].Label;
                        ExportInspection(new[] { clip }, new[] { label });
                    }
                }

                if (GUILayout.Button("Esporta originale + tutti i candidati — entrambi i personaggi", GUILayout.Height(34f)))
                {
                    var clips = _clipOptions.Where(option => option.Clip != null).Select(option => option.Clip).ToArray();
                    var labels = _clipOptions.Where(option => option.Clip != null).Select(option => option.Label).ToArray();
                    if (clips.Length == 0)
                        EditorUtility.DisplayDialog("Animation Inspection", "Non sono state trovate clip da esportare.", "OK");
                    else
                        ExportInspection(clips, labels);
                }
            }
        }

        private void ExportInspection(AnimationClip[] clips, string[] labels)
        {
            var settings = AssetDatabase.LoadAssetAtPath<PlayerRigSettings>(RigSettingsPath);
            if (settings == null)
            {
                EditorUtility.DisplayDialog("Animation Inspection", "PlayerRigSettings.asset non è disponibile.", "OK");
                return;
            }

            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            var defaultFolder = Path.Combine(projectRoot, "TestResults", "AnimationInspection");
            Directory.CreateDirectory(defaultFolder);
            var destination = EditorUtility.SaveFolderPanel(
                "Destinazione pacchetto Animation Inspection", defaultFolder, string.Empty);
            if (string.IsNullOrEmpty(destination))
                return;

            var options = new CrouchWalkAnimationInspectionExporter.ExportOptions
            {
                sampleRateHz = _inspectionSampleRate,
                contactSheetFrames = _inspectionFrameCount,
                cellWidth = _inspectionCellWidth,
                cellHeight = _inspectionCellHeight,
                maxPoseSamples = _inspectionMaxSamples,
                includeCurveKeyTimes = _inspectionIncludeCurveKeys,
                captureSkinnedMeshDiagnostics = _inspectionCaptureGeometry,
                contactHeightToleranceMeters = _inspectionContactHeight,
                contactSpeedToleranceMetersPerSecond = _inspectionContactSpeed,
                crossingToleranceMeters = _inspectionCrossingTolerance
            };

            try
            {
                var zipPath = CrouchWalkAnimationInspectionExporter.Export(
                    clips, labels, settings, options, destination);
                _status = "Pacchetto diagnostico creato: " + zipPath;
                EditorUtility.DisplayDialog(
                    "Animation Inspection completata",
                    "Pacchetto creato. Allega qui il file ZIP:\n\n" + zipPath,
                    "OK");
            }
            catch (Exception exception)
            {
                _status = "Esportazione non riuscita: " + exception.Message;
                Debug.LogException(exception);
                EditorUtility.DisplayDialog(
                    "Animation Inspection — errore",
                    exception.Message + "\n\nDettagli completi nella Console Unity.",
                    "OK");
            }
        }

        private void DrawPreview()
        {
            var rect = GUILayoutUtility.GetRect(100f, 390f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, new Color(0.16f, 0.16f, 0.16f));

            if (_previewInstance == null || _preview == null)
            {
                GUI.Label(rect, "Prefab non disponibile", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            _preview.camera.aspect = Mathf.Max(0.1f, rect.width / Mathf.Max(1f, rect.height));
            ConfigureCamera();
            _preview.BeginPreview(rect, GUIStyle.none);
            _preview.Render(true);
            var texture = _preview.EndPreview();
            GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, false);
        }

        private void ConfigureCamera()
        {
            var direction = _cameraAngle switch
            {
                CameraAngle.Front => Vector3.back,
                CameraAngle.ThreeQuarter => new Vector3(-1f, 0f, -1f).normalized,
                CameraAngle.Side => Vector3.right,
                CameraAngle.Rear => Vector3.forward,
                _ => Vector3.back
            };

            var target = _modelBounds.center + Vector3.up * (_modelBounds.extents.y * 0.08f);
            var cameraPosition = target + direction * _cameraDistance +
                                Vector3.up * (_modelBounds.extents.y * 0.12f);
            _preview.camera.transform.position = cameraPosition;
            _preview.camera.transform.LookAt(target);
        }

        private void ReloadClipOptions()
        {
            _clipOptions.Clear();
            var animationSettings = AssetDatabase.LoadAssetAtPath<PlayerAnimationSettings>(AnimationSettingsPath);
            if (animationSettings != null && animationSettings.CrouchWalkClip != null)
            {
                _clipOptions.Add(new ClipOption
                {
                    Label = "Originale assegnata — " + animationSettings.CrouchWalkClip.name,
                    Clip = animationSettings.CrouchWalkClip
                });
            }

            var modelGuids = AssetDatabase.FindAssets("t:Model", new[] { CandidateFolder });
            var candidateOptions = new List<ClipOption>();
            foreach (var guid in modelGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
                    continue;

                var clip = AssetDatabase.LoadAllAssetsAtPath(path)
                    .OfType<AnimationClip>()
                    .FirstOrDefault(candidate => candidate != null && candidate.length > 0f);
                if (clip == null)
                    continue;

                candidateOptions.Add(new ClipOption
                {
                    Label = Path.GetFileNameWithoutExtension(path),
                    Clip = clip
                });
            }

            _clipOptions.AddRange(candidateOptions.OrderBy(option => option.Label, StringComparer.OrdinalIgnoreCase));
            if (_clipOptions.Count == 0)
                _status = "Non sono state trovate clip candidate nella cartella " + CandidateFolder + ".";
            else
                _status = null;

            _clipIndex = Mathf.Clamp(_clipIndex, 0, Mathf.Max(0, _clipOptions.Count - 1));
        }

        private AnimationClip GetSelectedClip()
        {
            if (_clipOptions.Count == 0 || _clipIndex < 0 || _clipIndex >= _clipOptions.Count)
                return null;
            return _clipOptions[_clipIndex].Clip;
        }

        private void RebuildCharacter()
        {
            DestroyGraph();
            DestroyPreviewInstance();
            _animator = null;
            if (_preview == null)
                return;

            var rigSettings = AssetDatabase.LoadAssetAtPath<PlayerRigSettings>(RigSettingsPath);
            if (rigSettings == null)
            {
                _status = "PlayerRigSettings.asset non è disponibile.";
                return;
            }

            var prefabName = _character == CharacterVariant.Male
                ? rigSettings.MalePrefabName
                : rigSettings.FemalePrefabName;
            var prefabPath = "Assets/UnderGrind/Generated/Prefabs/" + prefabName + ".prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                _status = "Prefab non trovato: " + prefabPath;
                return;
            }

            var previewScene = _preview.camera.gameObject.scene;
            if (!previewScene.IsValid() || !previewScene.isLoaded)
            {
                _status = "La scena temporanea di anteprima non è disponibile.";
                return;
            }

            _previewInstance = PrefabUtility.InstantiatePrefab(prefab, previewScene) as GameObject;
            if (_previewInstance == null)
            {
                _status = "Impossibile creare l'istanza temporanea di " + prefabName + ".";
                return;
            }
            _animator = _previewInstance.GetComponentInChildren<Animator>(true);
            if (_animator == null || _animator.avatar == null || !_animator.avatar.isHuman || !_animator.avatar.isValid)
            {
                _status = "Il prefab selezionato non contiene un Animator con Avatar Humanoid valido.";
                return;
            }

            foreach (var behaviour in _previewInstance.GetComponentsInChildren<MonoBehaviour>(true))
                if (behaviour != null) behaviour.enabled = false;

            foreach (var renderer in _previewInstance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var name = renderer.name;
                var isFirstPersonPart =
                    name.IndexOf(rigSettings.FirstPersonArmsToken, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf(rigSettings.FirstPersonHandsToken, StringComparison.OrdinalIgnoreCase) >= 0;
                renderer.enabled = !isFirstPersonPart;
            }

            _animator.enabled = true;
            _animator.runtimeAnimatorController = null;
            _animator.applyRootMotion = false;
            _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            var renderers = _previewInstance.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled)
                .ToArray();
            if (renderers.Length == 0)
            {
                _status = "Il prefab non contiene renderer visibili per l'anteprima.";
                return;
            }

            _modelBounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
                _modelBounds.Encapsulate(renderers[i].bounds);

            _previewInstance.transform.position -= _modelBounds.center;
            _modelBounds.center = Vector3.zero;
            _status = null;
            RebuildGraph();
        }

        private void RebuildGraph()
        {
            DestroyGraph();
            var clip = GetSelectedClip();
            if (_animator == null || clip == null)
            {
                if (clip == null) _status = "La clip selezionata non è stata importata correttamente.";
                return;
            }

            _graph = PlayableGraph.Create("UnderGrind Crouch Walk Candidate Preview");
            _graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            var output = AnimationPlayableOutput.Create(_graph, "Crouch Walk Preview", _animator);
            _clipPlayable = AnimationClipPlayable.Create(_graph, clip);
            _clipPlayable.SetApplyFootIK(false);
            _clipPlayable.SetApplyPlayableIK(false);
            _clipPlayable.SetSpeed(0d);
            output.SetSourcePlayable(_clipPlayable);
            _graph.Play();

            _time = 0f;
            _hasGraph = true;
            _isPlaying = true;
            _lastUpdateTime = EditorApplication.timeSinceStartup;
            EvaluateAtCurrentTime();
            _status = null;
        }

        private void EditorUpdate()
        {
            if (!_hasGraph || !_graph.IsValid() || !_isPlaying)
                return;

            var now = EditorApplication.timeSinceStartup;
            var delta = Mathf.Max(0f, (float)(now - _lastUpdateTime));
            _lastUpdateTime = now;

            var clip = GetSelectedClip();
            if (clip == null || clip.length <= 0f)
                return;

            _time += delta * _playbackSpeed;
            if (_loop)
                _time %= clip.length;
            else if (_time >= clip.length)
            {
                _time = clip.length;
                _isPlaying = false;
            }

            EvaluateAtCurrentTime();
            Repaint();
        }

        private void EvaluateAtCurrentTime()
        {
            if (!_hasGraph || !_graph.IsValid())
                return;

            var clip = GetSelectedClip();
            if (clip == null)
                return;

            _clipPlayable.SetTime(Mathf.Clamp(_time, 0f, clip.length));
            _graph.Evaluate(0f);
        }

        private void DestroyGraph()
        {
            if (_graph.IsValid())
                _graph.Destroy();
            _hasGraph = false;
        }

        private void DestroyPreviewInstance()
        {
            if (_previewInstance != null)
            {
                DestroyImmediate(_previewInstance);
                _previewInstance = null;
            }
        }
    }
}
