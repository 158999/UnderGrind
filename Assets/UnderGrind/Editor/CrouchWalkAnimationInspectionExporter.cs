using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using UnderGrind.Locomotion;

namespace UnderGrind.Editor
{
    /// <summary>
    /// Exports a reproducible, frame-indexed inspection package for Humanoid animation retargeting.
    /// All sampling and rendering happen on temporary preview-scene instances.
    /// </summary>
    public static class CrouchWalkAnimationInspectionExporter
    {
        private const int SchemaVersion = 1;
        private const string PlayerAnimationSettingsPath = "Assets/UnderGrind/Settings/PlayerAnimationSettings.asset";
        private static readonly HumanBodyBones[] InspectedBones = Enum.GetValues(typeof(HumanBodyBones))
            .Cast<HumanBodyBones>()
            .Where(bone => bone != HumanBodyBones.LastBone)
            .ToArray();

        [Serializable]
        public sealed class ExportOptions
        {
            public int sampleRateHz = 30;
            public int contactSheetFrames = 24;
            public int cellWidth = 192;
            public int cellHeight = 320;
            public int maxPoseSamples = 12000;
            public bool includeCurveKeyTimes = true;
            public bool captureSkinnedMeshDiagnostics = true;
            public float contactHeightToleranceMeters = 0.04f;
            public float contactSpeedToleranceMetersPerSecond = 0.15f;
            public float crossingToleranceMeters = 0.02f;
        }

        [Serializable]
        private sealed class BundleManifest
        {
            public int schemaVersion;
            public string generatedUtc;
            public string unityVersion;
            public string operatingSystem;
            public string processor;
            public string graphicsDevice;
            public string graphicsApi;
            public string renderPipeline;
            public string colorSpace;
            public string playerAnimationSettingsPath;
            public string playerAnimationSettingsGuid;
            public string playerAnimationSettingsDependencyHash;
            public string playerAnimationSettingsSha256;
            public string assignedCrouchWalkClipName;
            public string assignedCrouchWalkClipPath;
            public string assignedCrouchWalkClipGuid;
            public string assignedCrouchWalkClipSha256;
            public string playerRigSettingsPath;
            public string playerRigSettingsGuid;
            public string playerRigSettingsDependencyHash;
            public string playerRigSettingsSha256;
            public ExportOptions options;
            public List<BundleEntry> entries = new List<BundleEntry>();
            public List<string> warnings = new List<string>();
        }

        [Serializable]
        private sealed class BundleEntry
        {
            public string clipName;
            public string clipAssetPath;
            public string character;
            public string reportPath;
            public string contactSheetPath;
            public string poseCsvPath;
            public string transformCsvPath;
            public string curveCsvPath;
            public string geometryCsvPath;
            public int poseSampleCount;
            public int curveBindingCount;
            public int curveKeyCount;
            public float minimumFootSeparation;
            public int footCrossingSamples;
            public float minimumKneeSeparation;
            public int kneeCrossingSamples;
            public float loopPositionError;
            public float loopRotationErrorDegrees;
            public float leftFootPathLengthMeters;
            public float rightFootPathLengthMeters;
            public float leftFootMaximumSpeedMetersPerSecond;
            public float rightFootMaximumSpeedMetersPerSecond;
            public float leftFootEstimatedContactRatio;
            public float rightFootEstimatedContactRatio;
            public float maximumHipsDriftMeters;
            public float maximumTorsoLeanDegrees;
            public float restFootSeparationMeters;
            public float restKneeSeparationMeters;
            public float crossingToleranceMeters;
            public Vector3 restAnatomicalLateralAxisLocal;
            public float minimumStaticAxisFootSeparationMeters;
            public int staticAxisFootCrossingSampleCount;
            public List<float> staticAxisFootCrossingTimesSeconds = new List<float>();
            public float minimumStaticAxisKneeSeparationMeters;
            public int staticAxisKneeCrossingSampleCount;
            public List<float> staticAxisKneeCrossingTimesSeconds = new List<float>();
            public float loopWorldBonePositionErrorMeters;
            public float minimumFootSeparationTimeSeconds;
            public float minimumKneeSeparationTimeSeconds;
            public List<float> contactSheetTimes = new List<float>();
            public List<string> contactSheetColumns = new List<string>();
        }

        [Serializable]
        private sealed class InspectionReport
        {
            public int schemaVersion;
            public string generatedUtc;
            public string unityVersion;
            public string clipName;
            public string clipAssetPath;
            public string clipGuid;
            public string clipDependencyHash;
            public string clipSha256;
            public long clipFileBytes;
            public ClipMetadata clip;
            public ImporterMetadata clipImporter;
            public AvatarMetadata clipSourceAvatar;
            public PrefabMetadata playerPrefab;
            public AvatarMetadata playerAvatar;
            public List<RendererMetadata> renderers = new List<RendererMetadata>();
            public List<CurveBindingMetadata> curveBindings = new List<CurveBindingMetadata>();
            public List<AnimationEventMetadata> events = new List<AnimationEventMetadata>();
            public List<PoseFrame> poseFrames = new List<PoseFrame>();
            public MotionSummary motion = new MotionSummary();
            public List<string> warnings = new List<string>();
            public string contactSheet;
            public List<float> contactSheetTimes = new List<float>();
            public List<string> contactSheetColumns = new List<string>();
            public string poseCsv;
            public string transformCsv;
            public string curveCsv;
            public string geometryCsv;
            public string samplingDescription;
            public string contactHeuristicDescription;
        }

        [Serializable]
        private sealed class ClipMetadata
        {
            public float lengthSeconds;
            public float frameRate;
            public int estimatedFrameCount;
            public bool legacy;
            public bool humanMotion;
            public bool empty;
            public bool hasGenericRootTransform;
            public bool hasMotionCurves;
            public bool hasMotionFloatCurves;
            public bool hasRootCurves;
            public string wrapMode;
            public string localBounds;
            public int floatCurveBindingCount;
            public int objectCurveBindingCount;
            public int totalFloatKeyCount;
            public int totalObjectKeyCount;
            public int eventCount;
            public float averageSampleIntervalSeconds;
            public float minimumSampleIntervalSeconds;
            public float maximumSampleIntervalSeconds;
            public bool curveKeyTimesIncluded;
            public bool sampleLimitReached;
        }

        [Serializable]
        private sealed class ImporterMetadata
        {
            public string importerType;
            public string animationType;
            public string avatarSetup;
            public string sourceAvatar;
            public bool importAnimation;
            public bool resampleCurves;
            public bool optimizeGameObjects;
            public bool importMaterials;
            public bool importCameras;
            public bool importLights;
            public bool importVisibility;
            public string animationCompression;
            public string animationWrapMode;
            public string motionNodeName;
            public float globalScale;
            public bool bakeIK;
            public bool importBlendShapes;
            public bool importConstraints;
            public bool importBlendShapeDeformPercent;
            public bool importVertexColors;
            public bool importUVs;
            public bool preserveHierarchy;
            public bool bakeAxisConversion;
            public bool optimizeBones;
            public bool isReadable;
            public bool removeConstantScaleCurves;
            public int maxBonesPerVertex;
            public float minBoneWeight;
            public float humanoidOversampling;
            public float animationPositionError;
            public float animationRotationError;
            public float animationScaleError;
            public string meshCompression;
            public string importNormals;
            public string importTangents;
            public string importBlendShapeNormals;
            public string indexFormat;
            public List<ImporterProperty> serializedProperties = new List<ImporterProperty>();
        }

        [Serializable]
        private sealed class ImporterProperty
        {
            public string name;
            public string type;
            public string value;
        }

        [Serializable]
        private sealed class AvatarMetadata
        {
            public string name;
            public bool isValid;
            public bool isHuman;
            public float feetSpacing;
            public float armStretch;
            public float legStretch;
            public float upperArmTwist;
            public float lowerArmTwist;
            public float upperLegTwist;
            public float lowerLegTwist;
            public bool hasTranslationDoF;
            public string rootMotionBoneName;
            public List<HumanBoneMapping> humanMappings = new List<HumanBoneMapping>();
            public List<SkeletonBoneMetadata> skeleton = new List<SkeletonBoneMetadata>();
        }

        [Serializable]
        private sealed class HumanBoneMapping
        {
            public string humanName;
            public string sourceBoneName;
            public bool mappedTransformFoundOnPlayer;
            public string playerTransformPath;
        }

        [Serializable]
        private sealed class SkeletonBoneMetadata
        {
            public string name;
            public string parentName;
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 scale;
        }

        [Serializable]
        private sealed class PrefabMetadata
        {
            public string assetPath;
            public string guid;
            public string dependencyHash;
            public string sha256;
            public long fileBytes;
            public string instanceName;
            public string animatorPath;
            public string animatorCullingMode;
            public bool applyRootMotion;
            public string updateMode;
            public Vector3 rootPosition;
            public Quaternion rootRotation;
            public Vector3 rootScale;
            public int hierarchyTransformCount;
            public int skinnedRendererCount;
        }

        [Serializable]
        private sealed class RendererMetadata
        {
            public string name;
            public string hierarchyPath;
            public string meshName;
            public int vertexCount;
            public int triangleCount;
            public int blendShapeCount;
            public bool enabled;
            public string shadowCastingMode;
            public string bounds;
            public string localBounds;
            public List<string> materials = new List<string>();
            public List<string> blendShapes = new List<string>();
        }

        [Serializable]
        private sealed class CurveBindingMetadata
        {
            public string path;
            public string property;
            public string componentType;
            public bool objectReferenceCurve;
            public bool rootOrMotionRelated;
            public int keyCount;
            public float firstKeyTime;
            public float lastKeyTime;
            public float minimumValue;
            public float maximumValue;
            public string preWrapMode;
            public string postWrapMode;
        }

        [Serializable]
        private sealed class AnimationEventMetadata
        {
            public float time;
            public string functionName;
            public string stringParameter;
            public float floatParameter;
            public int intParameter;
            public string objectReference;
            public string messageOptions;
        }

        [Serializable]
        private sealed class PoseFrame
        {
            public int index;
            public float timeSeconds;
            public float normalizedTime;
            public Vector3 rootWorldPosition;
            public Quaternion rootWorldRotation;
            public Vector3 rootLocalPosition;
            public Vector3 modelForwardWorld;
            public Vector3 modelRightWorld;
            public Vector3 hipsWorldPosition;
            public Quaternion hipsWorldRotation;
            public Vector3 hipsRootLocalPosition;
            public Vector3 anatomicalLateralAxisLocal;
            public Vector3 restAnatomicalLateralAxisLocal;
            public Vector3 leftFootRelativeToHips;
            public Vector3 rightFootRelativeToHips;
            public Quaternion leftFootRelativeRotationToHips;
            public Quaternion rightFootRelativeRotationToHips;
            public float hipsDriftFromStartMeters;
            public float torsoLeanDegrees;
            public float leftFootSignedLateralSeparation;
            public float leftRightFootDistanceMeters;
            public float leftRightFootLateralSeparationMeters;
            public float leftRightFootStaticAxisSeparationSigned;
            public float leftRightKneeSignedLateralSeparation;
            public float leftRightKneeDistanceMeters;
            public float leftRightKneeLateralSeparationMeters;
            public float leftRightKneeStaticAxisSeparationSigned;
            public float leftFootHeightInRootSpace;
            public float rightFootHeightInRootSpace;
            public float leftFootHeightAboveObservedMinimum;
            public float rightFootHeightAboveObservedMinimum;
            public float leftFootSpeedMetersPerSecond;
            public float rightFootSpeedMetersPerSecond;
            public float leftFootAccelerationMetersPerSecondSquared;
            public float rightFootAccelerationMetersPerSecondSquared;
            public bool leftFootEstimatedContact;
            public bool rightFootEstimatedContact;
            public bool leftFootEstimatedSliding;
            public bool rightFootEstimatedSliding;
            public float leftKneeAngleDegrees;
            public float rightKneeAngleDegrees;
            public float leftHipAngleDegrees;
            public float rightHipAngleDegrees;
            public List<BonePose> bones = new List<BonePose>();
            public List<TransformPose> transforms = new List<TransformPose>();
        }

        [Serializable]
        private sealed class TransformPose
        {
            public string hierarchyPath;
            public string transformName;
            public string parentPath;
            public string tag;
            public int layer;
            public string layerName;
            public bool activeSelf;
            public bool activeInHierarchy;
            public Vector3 localPosition;
            public Quaternion localRotation;
            public Vector3 localEulerAngles;
            public Vector3 localScale;
            public Vector3 worldPosition;
            public Quaternion worldRotation;
            public Vector3 worldEulerAngles;
            public Vector3 lossyScale;
            public Vector3 linearVelocity;
            public float linearSpeed;
            public float linearAcceleration;
            public float angularVelocityDegreesPerSecond;
        }

        [Serializable]
        private sealed class BonePose
        {
            public string humanBone;
            public string transformName;
            public string hierarchyPath;
            public string parentName;
            public Vector3 localPosition;
            public Quaternion localRotation;
            public Vector3 localEulerAngles;
            public Vector3 localScale;
            public Vector3 worldPosition;
            public Quaternion worldRotation;
            public Vector3 worldEulerAngles;
            public Vector3 linearVelocity;
            public float linearSpeed;
            public float linearAcceleration;
            public float angularVelocityDegreesPerSecond;
        }

        [Serializable]
        private sealed class MotionSummary
        {
            public int sampleCount;
            public float sampleRateHz;
            public float minimumSampleIntervalSeconds;
            public float maximumSampleIntervalSeconds;
            public float minimumFootSeparationMeters;
            public float maximumFootSeparationMeters;
            public float meanFootSeparationMeters;
            public int footCrossingSampleCount;
            public List<float> footCrossingTimesSeconds = new List<float>();
            public float minimumKneeSeparationMeters;
            public float maximumKneeSeparationMeters;
            public int kneeCrossingSampleCount;
            public List<float> kneeCrossingTimesSeconds = new List<float>();
            public float leftFootPathLengthMeters;
            public float rightFootPathLengthMeters;
            public float leftFootMaximumSpeedMetersPerSecond;
            public float rightFootMaximumSpeedMetersPerSecond;
            public float leftFootMaximumEstimatedContactSlideSpeed;
            public float rightFootMaximumEstimatedContactSlideSpeed;
            public float leftFootEstimatedContactRatio;
            public float rightFootEstimatedContactRatio;
            public float maximumHipsDriftMeters;
            public float restFootSeparationMeters;
            public float restKneeSeparationMeters;
            public float crossingToleranceMeters;
            public Vector3 restAnatomicalLateralAxisLocal;
            public float minimumStaticAxisFootSeparationMeters;
            public int staticAxisFootCrossingSampleCount;
            public List<float> staticAxisFootCrossingTimesSeconds = new List<float>();
            public float minimumStaticAxisKneeSeparationMeters;
            public int staticAxisKneeCrossingSampleCount;
            public List<float> staticAxisKneeCrossingTimesSeconds = new List<float>();
            public float loopPositionErrorMeters;
            public float loopWorldBonePositionErrorMeters;
            public float loopRotationErrorDegrees;
            public float minimumFootSeparationTimeSeconds;
            public float minimumKneeSeparationTimeSeconds;
            public float maximumTorsoLeanDegrees;
            public float minimumTorsoLeanDegrees;
        }

        [Serializable]
        private sealed class GeometryFrame
        {
            public int contactSheetRowIndex;
            public float timeSeconds;
            public List<RendererGeometrySample> renderers = new List<RendererGeometrySample>();
        }

        [Serializable]
        private sealed class RendererGeometrySample
        {
            public string rendererName;
            public string hierarchyPath;
            public string worldBounds;
            public string bakedLocalBounds;
            public Vector3 bakedVertexCentroid;
            public Vector3 bakedVertexMinimum;
            public Vector3 bakedVertexMaximum;
            public float maximumVertexRadius;
            public int vertexCount;
            public int nonFiniteVertexCount;
            public string blendShapeWeights;
        }

        private sealed class PreviewRig : IDisposable
        {
            public PreviewRenderUtility Preview;
            public GameObject Instance;
            public Animator Animator;
            public PlayableGraph Graph;
            public AnimationClipPlayable ClipPlayable;
            public Bounds Bounds;
            public string PrefabPath;
            public string CharacterName;

            public void Dispose()
            {
                if (Graph.IsValid())
                    Graph.Destroy();
                if (Instance != null)
                    UnityEngine.Object.DestroyImmediate(Instance);
                if (Preview != null)
                {
                    Preview.Cleanup();
                    Preview = null;
                }
            }
        }

        public static string Export(
            IReadOnlyList<AnimationClip> clips,
            IReadOnlyList<string> labels,
            PlayerRigSettings rigSettings,
            ExportOptions options,
            string destinationDirectory)
        {
            if (clips == null || clips.Count == 0)
                throw new ArgumentException("Nessuna clip da esportare.", nameof(clips));
            if (rigSettings == null)
                throw new ArgumentNullException(nameof(rigSettings));
            if (options == null)
                throw new ArgumentNullException(nameof(options));
            if (string.IsNullOrWhiteSpace(destinationDirectory))
                throw new ArgumentException("Cartella di destinazione non valida.", nameof(destinationDirectory));

            ValidateOptions(options);
            Directory.CreateDirectory(destinationDirectory);

            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture);
            var packageName = "UnderGrind-Animation-Inspection-" + timestamp;
            var packageDirectory = Path.Combine(destinationDirectory, packageName);
            Directory.CreateDirectory(packageDirectory);

            var manifest = CreateManifest(options, rigSettings);
            var characterDefinitions = new[]
            {
                new { Name = "Male", PrefabName = rigSettings.MalePrefabName },
                new { Name = "Female", PrefabName = rigSettings.FemalePrefabName }
            };

            var totalOperations = clips.Count * characterDefinitions.Length;
            var completedOperations = 0;
            try
            {
                foreach (var character in characterDefinitions)
                {
                    var prefabPath = "Assets/UnderGrind/Generated/Prefabs/" + character.PrefabName + ".prefab";
                    using (var rig = CreatePreviewRig(prefabPath, character.Name, rigSettings))
                    {
                        for (var clipIndex = 0; clipIndex < clips.Count; clipIndex++)
                        {
                            var clip = clips[clipIndex];
                            if (clip == null)
                            {
                                manifest.warnings.Add("Clip nulla ignorata all'indice " + clipIndex + ".");
                                completedOperations++;
                                continue;
                            }

                            var label = labels != null && clipIndex < labels.Count && !string.IsNullOrWhiteSpace(labels[clipIndex])
                                ? labels[clipIndex]
                                : clip.name;
                            EditorUtility.DisplayProgressBar(
                                "UnderGrind — Analisi animazioni",
                                character.Name + " / " + label,
                                (float)completedOperations / totalOperations);

                            var entry = ExportClipForCharacter(
                                clip, label, rig, rigSettings, options, packageDirectory);
                            manifest.entries.Add(entry);
                            completedOperations++;
                        }
                    }
                }

                var manifestPath = Path.Combine(packageDirectory, "manifest.json");
                File.WriteAllText(manifestPath, JsonUtility.ToJson(manifest, true), new UTF8Encoding(false));
                WriteHtmlIndex(packageDirectory, manifest);
                WriteReadme(packageDirectory, options, manifest);

                var zipPath = Path.Combine(destinationDirectory, packageName + ".zip");
                CreateZip(packageDirectory, zipPath);
                EditorUtility.RevealInFinder(zipPath);
                Debug.Log("UnderGrind: pacchetto di ispezione animazioni creato: " + zipPath);
                return zipPath;
            }
            catch
            {
                manifest.warnings.Add("Esportazione interrotta: consultare la Console Unity per lo stack trace.");
                throw;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private static void ValidateOptions(ExportOptions options)
        {
            options.sampleRateHz = Mathf.Clamp(options.sampleRateHz, 5, 120);
            options.contactSheetFrames = Mathf.Clamp(options.contactSheetFrames, 4, 24);
            options.cellWidth = Mathf.Clamp(options.cellWidth, 128, 512);
            options.cellHeight = Mathf.Clamp(options.cellHeight, 192, 768);
            options.maxPoseSamples = Mathf.Clamp(options.maxPoseSamples, 100, 50000);
            options.contactHeightToleranceMeters = Mathf.Max(0f, options.contactHeightToleranceMeters);
            options.contactSpeedToleranceMetersPerSecond = Mathf.Max(0f, options.contactSpeedToleranceMetersPerSecond);
            options.crossingToleranceMeters = Mathf.Max(0f, options.crossingToleranceMeters);
        }

        private static BundleManifest CreateManifest(ExportOptions options, PlayerRigSettings rigSettings)
        {
            var pipeline = GraphicsSettings.currentRenderPipeline;
            var animationSettings = AssetDatabase.LoadAssetAtPath<PlayerAnimationSettings>(PlayerAnimationSettingsPath);
            var assignedClip = animationSettings == null ? null : animationSettings.CrouchWalkClip;
            var rigSettingsPath = AssetDatabase.GetAssetPath(rigSettings);
            var assignedClipPath = assignedClip == null ? string.Empty : AssetDatabase.GetAssetPath(assignedClip);
            return new BundleManifest
            {
                schemaVersion = SchemaVersion,
                generatedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                unityVersion = Application.unityVersion,
                operatingSystem = SystemInfo.operatingSystem,
                processor = SystemInfo.processorType,
                graphicsDevice = SystemInfo.graphicsDeviceName,
                graphicsApi = SystemInfo.graphicsDeviceType.ToString(),
                renderPipeline = pipeline == null ? "Built-in" : pipeline.GetType().FullName,
                colorSpace = QualitySettings.activeColorSpace.ToString(),
                playerAnimationSettingsPath = PlayerAnimationSettingsPath,
                playerAnimationSettingsGuid = AssetDatabase.AssetPathToGUID(PlayerAnimationSettingsPath),
                playerAnimationSettingsDependencyHash = AssetDatabase.GetAssetDependencyHash(PlayerAnimationSettingsPath).ToString(),
                playerAnimationSettingsSha256 = GetSha256(PlayerAnimationSettingsPath),
                assignedCrouchWalkClipName = assignedClip == null ? string.Empty : assignedClip.name,
                assignedCrouchWalkClipPath = assignedClipPath,
                assignedCrouchWalkClipGuid = string.IsNullOrEmpty(assignedClipPath) ? string.Empty : AssetDatabase.AssetPathToGUID(assignedClipPath),
                assignedCrouchWalkClipSha256 = string.IsNullOrEmpty(assignedClipPath) ? string.Empty : GetSha256(assignedClipPath),
                playerRigSettingsPath = rigSettingsPath,
                playerRigSettingsGuid = AssetDatabase.AssetPathToGUID(rigSettingsPath),
                playerRigSettingsDependencyHash = AssetDatabase.GetAssetDependencyHash(rigSettingsPath).ToString(),
                playerRigSettingsSha256 = GetSha256(rigSettingsPath),
                options = options
            };
        }

        private static PreviewRig CreatePreviewRig(string prefabPath, string characterName, PlayerRigSettings settings)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
                throw new InvalidOperationException("Prefab non trovato: " + prefabPath);

            var rig = new PreviewRig
            {
                Preview = new PreviewRenderUtility(),
                PrefabPath = prefabPath,
                CharacterName = characterName
            };
            rig.Preview.cameraFieldOfView = 30f;
            rig.Preview.camera.nearClipPlane = 0.01f;
            rig.Preview.camera.farClipPlane = 100f;
            rig.Preview.camera.clearFlags = CameraClearFlags.SolidColor;
            rig.Preview.camera.backgroundColor = new Color(0.13f, 0.15f, 0.18f, 1f);

            var previewScene = rig.Preview.camera.gameObject.scene;
            if (!previewScene.IsValid() || !previewScene.isLoaded)
            {
                rig.Dispose();
                throw new InvalidOperationException("Scena temporanea PreviewRenderUtility non valida.");
            }

            rig.Instance = PrefabUtility.InstantiatePrefab(prefab, previewScene) as GameObject;
            if (rig.Instance == null)
            {
                rig.Dispose();
                throw new InvalidOperationException("Impossibile istanziare in preview scene: " + prefabPath);
            }

            rig.Animator = rig.Instance.GetComponentInChildren<Animator>(true);
            if (rig.Animator == null || rig.Animator.avatar == null ||
                !rig.Animator.avatar.isHuman || !rig.Animator.avatar.isValid)
            {
                rig.Dispose();
                throw new InvalidOperationException("Prefab " + prefabPath + " senza Avatar Humanoid valido.");
            }

            foreach (var behaviour in rig.Instance.GetComponentsInChildren<MonoBehaviour>(true))
                if (behaviour != null) behaviour.enabled = false;

            foreach (var renderer in rig.Instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var rendererName = renderer.name;
                var firstPerson =
                    (!string.IsNullOrEmpty(settings.FirstPersonArmsToken) &&
                     rendererName.IndexOf(settings.FirstPersonArmsToken, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (!string.IsNullOrEmpty(settings.FirstPersonHandsToken) &&
                     rendererName.IndexOf(settings.FirstPersonHandsToken, StringComparison.OrdinalIgnoreCase) >= 0);
                renderer.enabled = !firstPerson;
            }

            rig.Animator.enabled = true;
            rig.Animator.runtimeAnimatorController = null;
            rig.Animator.applyRootMotion = false;
            rig.Animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            rig.Animator.Rebind();

            var renderers = rig.Instance.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled)
                .ToArray();
            if (renderers.Length == 0)
            {
                rig.Dispose();
                throw new InvalidOperationException("Prefab senza renderer visibili: " + prefabPath);
            }

            rig.Bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
                rig.Bounds.Encapsulate(renderers[index].bounds);

            return rig;
        }

        private static BundleEntry ExportClipForCharacter(
            AnimationClip clip,
            string label,
            PreviewRig rig,
            PlayerRigSettings rigSettings,
            ExportOptions options,
            string packageDirectory)
        {
            var clipPath = AssetDatabase.GetAssetPath(clip);
            var clipSlug = MakeSlug(Path.GetFileNameWithoutExtension(clipPath));
            var characterSlug = rig.CharacterName.ToLowerInvariant();
            var relativeDirectory = Path.Combine(clipSlug, characterSlug);
            var outputDirectory = Path.Combine(packageDirectory, relativeDirectory);
            Directory.CreateDirectory(outputDirectory);

            if (rig.Graph.IsValid())
                rig.Graph.Destroy();

            rig.Graph = PlayableGraph.Create("UnderGrind Inspection / " + rig.CharacterName + " / " + clip.name);
            rig.Graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            var output = AnimationPlayableOutput.Create(rig.Graph, "Animation Inspection", rig.Animator);
            rig.ClipPlayable = AnimationClipPlayable.Create(rig.Graph, clip);
            rig.ClipPlayable.SetApplyFootIK(false);
            rig.ClipPlayable.SetApplyPlayableIK(false);
            rig.ClipPlayable.SetSpeed(0d);
            output.SetSourcePlayable(rig.ClipPlayable);
            rig.Graph.Play();

            var report = new InspectionReport
            {
                schemaVersion = SchemaVersion,
                generatedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                unityVersion = Application.unityVersion,
                clipName = clip.name,
                clipAssetPath = clipPath,
                clipGuid = AssetDatabase.AssetPathToGUID(clipPath),
                clipDependencyHash = AssetDatabase.GetAssetDependencyHash(clipPath).ToString(),
                clipSha256 = GetSha256(clipPath),
                clipFileBytes = GetFileSize(clipPath),
                clip = CreateClipMetadata(clip, options),
                clipImporter = CreateImporterMetadata(AssetImporter.GetAtPath(clipPath) as ModelImporter),
                clipSourceAvatar = CreateSourceAvatarMetadata(clipPath),
                playerPrefab = CreatePrefabMetadata(rig),
                playerAvatar = CreateAvatarMetadata(rig.Animator.avatar, rig.Animator),
                samplingDescription = "Campionamento deterministico tramite AnimationClipPlayable su PlayableGraph manuale. Ogni frame registra i Transform Humanoid risolti dall'Animator Player.",
                contactHeuristicDescription =
                    "Il contatto è una stima, non un ground truth: piede entro " +
                    options.contactHeightToleranceMeters.ToString("F3", CultureInfo.InvariantCulture) +
                    " m dal minimo osservato nel ciclo e velocità <= " +
                    options.contactSpeedToleranceMetersPerSecond.ToString("F3", CultureInfo.InvariantCulture) +
                    " m/s. La soglia non sostituisce la valutazione visiva o il grounded state del controller. " +
                    "Gli incroci usano l'asse anatomico dinamico delle anche e una tolleranza di " +
                    options.crossingToleranceMeters.ToString("F3", CultureInfo.InvariantCulture) + " m."
            };

            report.renderers = CreateRendererMetadata(rig.Instance);
            report.curveBindings = CreateCurveMetadata(clip);
            report.events = CreateEventMetadata(clip);

            var sampleTimes = BuildSampleTimes(clip, options, out var sampleLimitReached);
            report.clip.sampleLimitReached = sampleLimitReached;
            if (sampleLimitReached)
                report.warnings.Add("Il limite Max Pose Samples è stato raggiunto: la sequenza temporale è stata ridotta in modo uniforme e alcuni tempi keyframe potrebbero non essere inclusi.");
            report.clip.averageSampleIntervalSeconds = CalculateAverageInterval(sampleTimes);
            report.clip.minimumSampleIntervalSeconds = CalculateMinimumInterval(sampleTimes);
            report.clip.maximumSampleIntervalSeconds = CalculateMaximumInterval(sampleTimes);

            report.poseFrames = SamplePoses(rig, clip, sampleTimes);
            CalculatePoseDerivatives(report.poseFrames);
            CalculateFootMetrics(report.poseFrames, report.motion, options);
            report.motion.sampleCount = report.poseFrames.Count;
            report.motion.sampleRateHz = options.sampleRateHz;
            report.motion.minimumSampleIntervalSeconds = report.clip.minimumSampleIntervalSeconds;
            report.motion.maximumSampleIntervalSeconds = report.clip.maximumSampleIntervalSeconds;
            report.motion.loopPositionErrorMeters = CalculateLoopPositionError(report.poseFrames);
            report.motion.loopWorldBonePositionErrorMeters = CalculateLoopWorldBonePositionError(report.poseFrames);
            report.motion.loopRotationErrorDegrees = CalculateLoopRotationError(report.poseFrames);
            report.motion.maximumHipsDriftMeters = report.poseFrames.Count == 0
                ? 0f
                : report.poseFrames.Max(frame => frame.hipsDriftFromStartMeters);
            report.motion.maximumTorsoLeanDegrees = report.poseFrames.Count == 0
                ? 0f
                : report.poseFrames.Max(frame => frame.torsoLeanDegrees);
            report.motion.minimumTorsoLeanDegrees = report.poseFrames.Count == 0
                ? 0f
                : report.poseFrames.Min(frame => frame.torsoLeanDegrees);

            var sheetPath = Path.Combine(outputDirectory, "contact-sheet.png");
            var sheetTimes = BuildContactSheetTimes(clip, options.contactSheetFrames);
            report.contactSheetTimes.AddRange(sheetTimes);
            report.contactSheetColumns.AddRange(new[] { "Frontale", "Tre quarti", "Laterale", "Posteriore" });
            CaptureContactSheet(rig, clip, sheetTimes, options, sheetPath);
            report.contactSheet = "contact-sheet.png";

            if (options.captureSkinnedMeshDiagnostics)
            {
                var geometryFrames = CaptureGeometryFrames(rig, clip, sheetTimes);
                var geometryPath = Path.Combine(outputDirectory, "geometry-samples.csv");
                WriteGeometryCsv(geometryPath, geometryFrames);
                report.geometryCsv = "geometry-samples.csv";
            }

            var poseCsvPath = Path.Combine(outputDirectory, "pose-samples.csv");
            WritePoseCsv(poseCsvPath, report.poseFrames);
            report.poseCsv = "pose-samples.csv";

            var transformCsvPath = Path.Combine(outputDirectory, "transform-hierarchy-samples.csv");
            WriteTransformCsv(transformCsvPath, report.poseFrames);
            report.transformCsv = "transform-hierarchy-samples.csv";

            var curveCsvPath = Path.Combine(outputDirectory, "curve-keys.csv");
            WriteCurveCsv(curveCsvPath, clip);
            report.curveCsv = "curve-keys.csv";

            var reportPath = Path.Combine(outputDirectory, "report.json");
            File.WriteAllText(reportPath, JsonUtility.ToJson(report, true), new UTF8Encoding(false));

            return new BundleEntry
            {
                clipName = label,
                clipAssetPath = clipPath,
                character = rig.CharacterName,
                reportPath = ToPackagePath(relativeDirectory, "report.json"),
                contactSheetPath = ToPackagePath(relativeDirectory, "contact-sheet.png"),
                poseCsvPath = ToPackagePath(relativeDirectory, "pose-samples.csv"),
                transformCsvPath = ToPackagePath(relativeDirectory, "transform-hierarchy-samples.csv"),
                curveCsvPath = ToPackagePath(relativeDirectory, "curve-keys.csv"),
                geometryCsvPath = options.captureSkinnedMeshDiagnostics
                    ? ToPackagePath(relativeDirectory, "geometry-samples.csv")
                    : string.Empty,
                poseSampleCount = report.poseFrames.Count,
                curveBindingCount = report.curveBindings.Count,
                curveKeyCount = report.clip.totalFloatKeyCount + report.clip.totalObjectKeyCount,
                minimumFootSeparation = report.motion.minimumFootSeparationMeters,
                footCrossingSamples = report.motion.footCrossingSampleCount,
                minimumKneeSeparation = report.motion.minimumKneeSeparationMeters,
                kneeCrossingSamples = report.motion.kneeCrossingSampleCount,
                loopPositionError = report.motion.loopPositionErrorMeters,
                loopRotationErrorDegrees = report.motion.loopRotationErrorDegrees,
                restFootSeparationMeters = report.motion.restFootSeparationMeters,
                restKneeSeparationMeters = report.motion.restKneeSeparationMeters,
                crossingToleranceMeters = report.motion.crossingToleranceMeters,
                restAnatomicalLateralAxisLocal = report.motion.restAnatomicalLateralAxisLocal,
                minimumStaticAxisFootSeparationMeters = report.motion.minimumStaticAxisFootSeparationMeters,
                staticAxisFootCrossingSampleCount = report.motion.staticAxisFootCrossingSampleCount,
                staticAxisFootCrossingTimesSeconds = new List<float>(report.motion.staticAxisFootCrossingTimesSeconds),
                minimumStaticAxisKneeSeparationMeters = report.motion.minimumStaticAxisKneeSeparationMeters,
                staticAxisKneeCrossingSampleCount = report.motion.staticAxisKneeCrossingSampleCount,
                staticAxisKneeCrossingTimesSeconds = new List<float>(report.motion.staticAxisKneeCrossingTimesSeconds),
                loopWorldBonePositionErrorMeters = report.motion.loopWorldBonePositionErrorMeters,
                minimumFootSeparationTimeSeconds = report.motion.minimumFootSeparationTimeSeconds,
                minimumKneeSeparationTimeSeconds = report.motion.minimumKneeSeparationTimeSeconds,
                leftFootPathLengthMeters = report.motion.leftFootPathLengthMeters,
                rightFootPathLengthMeters = report.motion.rightFootPathLengthMeters,
                leftFootMaximumSpeedMetersPerSecond = report.motion.leftFootMaximumSpeedMetersPerSecond,
                rightFootMaximumSpeedMetersPerSecond = report.motion.rightFootMaximumSpeedMetersPerSecond,
                leftFootEstimatedContactRatio = report.motion.leftFootEstimatedContactRatio,
                rightFootEstimatedContactRatio = report.motion.rightFootEstimatedContactRatio,
                maximumHipsDriftMeters = report.motion.maximumHipsDriftMeters,
                maximumTorsoLeanDegrees = report.motion.maximumTorsoLeanDegrees,
                contactSheetTimes = new List<float>(report.contactSheetTimes),
                contactSheetColumns = new List<string>(report.contactSheetColumns)
            };
        }

        private static ClipMetadata CreateClipMetadata(AnimationClip clip, ExportOptions options)
        {
            var floatBindings = AnimationUtility.GetCurveBindings(clip);
            var objectBindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
            var floatKeys = floatBindings.Sum(binding =>
            {
                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                return curve == null ? 0 : curve.length;
            });
            var objectKeys = objectBindings.Sum(binding =>
            {
                var keys = AnimationUtility.GetObjectReferenceCurve(clip, binding);
                return keys == null ? 0 : keys.Length;
            });

            return new ClipMetadata
            {
                lengthSeconds = clip.length,
                frameRate = clip.frameRate,
                estimatedFrameCount = Mathf.CeilToInt(clip.length * clip.frameRate),
                legacy = clip.legacy,
                humanMotion = clip.humanMotion,
                empty = clip.empty,
                hasGenericRootTransform = clip.hasGenericRootTransform,
                hasMotionCurves = clip.hasMotionCurves,
                hasMotionFloatCurves = clip.hasMotionFloatCurves,
                hasRootCurves = clip.hasRootCurves,
                wrapMode = clip.wrapMode.ToString(),
                localBounds = clip.localBounds.ToString(),
                floatCurveBindingCount = floatBindings.Length,
                objectCurveBindingCount = objectBindings.Length,
                totalFloatKeyCount = floatKeys,
                totalObjectKeyCount = objectKeys,
                eventCount = AnimationUtility.GetAnimationEvents(clip).Length,
                curveKeyTimesIncluded = options.includeCurveKeyTimes
            };
        }

        private static ImporterMetadata CreateImporterMetadata(ModelImporter importer)
        {
            if (importer == null)
                return new ImporterMetadata { importerType = "Non disponibile" };

            var metadata = new ImporterMetadata
            {
                importerType = importer.GetType().FullName,
                animationType = importer.animationType.ToString(),
                avatarSetup = importer.avatarSetup.ToString(),
                sourceAvatar = importer.sourceAvatar == null ? string.Empty : AssetDatabase.GetAssetPath(importer.sourceAvatar) + "|" + importer.sourceAvatar.name,
                importAnimation = importer.importAnimation,
                resampleCurves = importer.resampleCurves,
                optimizeGameObjects = importer.optimizeGameObjects,
                importMaterials = importer.materialImportMode != ModelImporterMaterialImportMode.None,
                importCameras = importer.importCameras,
                importLights = importer.importLights,
                importVisibility = importer.importVisibility,
                animationCompression = importer.animationCompression.ToString(),
                animationWrapMode = importer.animationWrapMode.ToString(),
                motionNodeName = importer.motionNodeName,
                globalScale = importer.globalScale,
                bakeIK = importer.bakeIK,
                importBlendShapes = importer.importBlendShapes,
                importConstraints = importer.importConstraints,
                importBlendShapeDeformPercent = importer.importBlendShapeDeformPercent,
                importVertexColors = importer.importVertexColors,
                importUVs = importer.importUVs,
                preserveHierarchy = importer.preserveHierarchy,
                bakeAxisConversion = importer.bakeAxisConversion,
                optimizeBones = importer.optimizeBones,
                isReadable = importer.isReadable,
                removeConstantScaleCurves = importer.removeConstantScaleCurves,
                maxBonesPerVertex = importer.maxBonesPerVertex,
                minBoneWeight = importer.minBoneWeight,
                humanoidOversampling = importer.humanoidOversampling,
                animationPositionError = importer.animationPositionError,
                animationRotationError = importer.animationRotationError,
                animationScaleError = importer.animationScaleError,
                meshCompression = importer.meshCompression.ToString(),
                importNormals = importer.importNormals.ToString(),
                importTangents = importer.importTangents.ToString(),
                importBlendShapeNormals = importer.importBlendShapeNormals.ToString(),
                indexFormat = importer.indexFormat.ToString()
            };

            var serialized = new SerializedObject(importer);
            var property = serialized.GetIterator();
            while (property.NextVisible(true))
            {
                var value = GetSerializedPropertyValue(property);
                if (value == null)
                    continue;
                metadata.serializedProperties.Add(new ImporterProperty
                {
                    name = property.propertyPath,
                    type = property.propertyType.ToString(),
                    value = value
                });
            }

            return metadata;
        }

        private static string GetSerializedPropertyValue(SerializedProperty property)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer: return property.intValue.ToString(CultureInfo.InvariantCulture);
                case SerializedPropertyType.Boolean: return property.boolValue.ToString();
                case SerializedPropertyType.Float: return property.floatValue.ToString("R", CultureInfo.InvariantCulture);
                case SerializedPropertyType.String: return property.stringValue;
                case SerializedPropertyType.Enum:
                    return property.enumDisplayNames != null &&
                           property.enumValueIndex >= 0 &&
                           property.enumValueIndex < property.enumDisplayNames.Length
                        ? property.enumDisplayNames[property.enumValueIndex]
                        : property.enumValueIndex.ToString(CultureInfo.InvariantCulture);
                case SerializedPropertyType.ObjectReference:
                    return property.objectReferenceValue == null
                        ? string.Empty
                        : AssetDatabase.GetAssetPath(property.objectReferenceValue) + "|" + property.objectReferenceValue.name;
                case SerializedPropertyType.Vector2: return property.vector2Value.ToString("R", CultureInfo.InvariantCulture);
                case SerializedPropertyType.Vector3: return property.vector3Value.ToString("R", CultureInfo.InvariantCulture);
                case SerializedPropertyType.Vector4: return property.vector4Value.ToString("R", CultureInfo.InvariantCulture);
                case SerializedPropertyType.Quaternion: return property.quaternionValue.ToString("R", CultureInfo.InvariantCulture);
                case SerializedPropertyType.Color: return property.colorValue.ToString();
                case SerializedPropertyType.Bounds: return property.boundsValue.ToString();
                case SerializedPropertyType.Rect: return property.rectValue.ToString();
                case SerializedPropertyType.ArraySize: return property.intValue.ToString(CultureInfo.InvariantCulture);
                case SerializedPropertyType.LayerMask: return property.intValue.ToString(CultureInfo.InvariantCulture);
                default: return null;
            }
        }

        private static AvatarMetadata CreateSourceAvatarMetadata(string clipPath)
        {
            var sourceAvatar = AssetDatabase.LoadAllAssetsAtPath(clipPath)
                .OfType<Avatar>()
                .FirstOrDefault(avatar => avatar != null && avatar.isHuman);
            return CreateAvatarMetadata(sourceAvatar, null);
        }

        private static AvatarMetadata CreateAvatarMetadata(Avatar avatar, Animator playerAnimator)
        {
            if (avatar == null)
                return new AvatarMetadata { name = "Avatar sorgente non disponibile" };

            var metadata = new AvatarMetadata
            {
                name = avatar.name,
                isValid = avatar.isValid,
                isHuman = avatar.isHuman,
                rootMotionBoneName = string.Empty
            };

            if (!avatar.isValid || !avatar.isHuman)
                return metadata;

            var description = avatar.humanDescription;
            metadata.feetSpacing = description.feetSpacing;
            metadata.armStretch = description.armStretch;
            metadata.legStretch = description.legStretch;
            metadata.upperArmTwist = description.upperArmTwist;
            metadata.lowerArmTwist = description.lowerArmTwist;
            metadata.upperLegTwist = description.upperLegTwist;
            metadata.lowerLegTwist = description.lowerLegTwist;
            metadata.hasTranslationDoF = description.hasTranslationDoF;
            metadata.rootMotionBoneName = description.rootMotionBoneName;

            foreach (var mapping in description.human ?? Array.Empty<HumanBone>())
            {
                var playerBone = playerAnimator == null
                    ? null
                    : Enum.GetValues(typeof(HumanBodyBones))
                        .Cast<HumanBodyBones>()
                        .Where(bone => bone != HumanBodyBones.LastBone)
                        .Select(bone => new { Bone = bone, Transform = SafeGetBoneTransform(playerAnimator, bone) })
                        .FirstOrDefault(item => item.Transform != null && item.Transform.name == mapping.boneName);
                metadata.humanMappings.Add(new HumanBoneMapping
                {
                    humanName = mapping.humanName,
                    sourceBoneName = mapping.boneName,
                    mappedTransformFoundOnPlayer = playerBone != null,
                    playerTransformPath = playerBone == null ? string.Empty : GetHierarchyPath(playerBone.Transform)
                });
            }

            foreach (var bone in description.skeleton ?? Array.Empty<SkeletonBone>())
            {
                metadata.skeleton.Add(new SkeletonBoneMetadata
                {
                    name = bone.name,
                    parentName = bone.parentName,
                    position = bone.position,
                    rotation = bone.rotation,
                    scale = bone.scale
                });
            }

            return metadata;
        }

        private static PrefabMetadata CreatePrefabMetadata(PreviewRig rig)
        {
            var path = rig.PrefabPath;
            var root = rig.Instance.transform;
            return new PrefabMetadata
            {
                assetPath = path,
                guid = AssetDatabase.AssetPathToGUID(path),
                dependencyHash = AssetDatabase.GetAssetDependencyHash(path).ToString(),
                sha256 = GetSha256(path),
                fileBytes = GetFileSize(path),
                instanceName = rig.Instance.name,
                animatorPath = GetHierarchyPath(rig.Animator.transform),
                animatorCullingMode = rig.Animator.cullingMode.ToString(),
                applyRootMotion = rig.Animator.applyRootMotion,
                updateMode = rig.Animator.updateMode.ToString(),
                rootPosition = root.position,
                rootRotation = root.rotation,
                rootScale = root.lossyScale,
                hierarchyTransformCount = rig.Instance.GetComponentsInChildren<Transform>(true).Length,
                skinnedRendererCount = rig.Instance.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length
            };
        }

        private static List<RendererMetadata> CreateRendererMetadata(GameObject instance)
        {
            var result = new List<RendererMetadata>();
            foreach (var renderer in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var mesh = renderer.sharedMesh;
                var item = new RendererMetadata
                {
                    name = renderer.name,
                    hierarchyPath = GetHierarchyPath(renderer.transform),
                    meshName = mesh == null ? string.Empty : mesh.name,
                    vertexCount = mesh == null ? 0 : mesh.vertexCount,
                    triangleCount = GetTriangleCount(mesh),
                    blendShapeCount = mesh == null ? 0 : mesh.blendShapeCount,
                    enabled = renderer.enabled,
                    shadowCastingMode = renderer.shadowCastingMode.ToString(),
                    bounds = renderer.bounds.ToString(),
                    localBounds = renderer.localBounds.ToString()
                };

                foreach (var material in renderer.sharedMaterials)
                    item.materials.Add(material == null ? "<null>" : material.name);

                if (mesh != null)
                    for (var index = 0; index < mesh.blendShapeCount; index++)
                        item.blendShapes.Add(mesh.GetBlendShapeName(index));

                result.Add(item);
            }
            return result;
        }

        private static int GetTriangleCount(Mesh mesh)
        {
            if (mesh == null)
                return 0;

            ulong indexCount = 0;
            for (var subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
                if (mesh.GetTopology(subMesh) == MeshTopology.Triangles)
                    indexCount += mesh.GetIndexCount(subMesh);

            return (int)(indexCount / 3UL);
        }

        private static List<CurveBindingMetadata> CreateCurveMetadata(AnimationClip clip)
        {
            var result = new List<CurveBindingMetadata>();
            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
            {
                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                var keys = curve == null ? Array.Empty<Keyframe>() : curve.keys;
                result.Add(new CurveBindingMetadata
                {
                    path = binding.path,
                    property = binding.propertyName,
                    componentType = binding.type == null ? string.Empty : binding.type.FullName,
                    objectReferenceCurve = false,
                    rootOrMotionRelated = IsRootOrMotionBinding(binding.path, binding.propertyName),
                    keyCount = keys.Length,
                    firstKeyTime = keys.Length == 0 ? 0f : keys[0].time,
                    lastKeyTime = keys.Length == 0 ? 0f : keys[keys.Length - 1].time,
                    minimumValue = keys.Length == 0 ? 0f : keys.Min(key => key.value),
                    maximumValue = keys.Length == 0 ? 0f : keys.Max(key => key.value),
                    preWrapMode = curve == null ? string.Empty : curve.preWrapMode.ToString(),
                    postWrapMode = curve == null ? string.Empty : curve.postWrapMode.ToString()
                });
            }

            foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
            {
                var keys = AnimationUtility.GetObjectReferenceCurve(clip, binding) ?? Array.Empty<ObjectReferenceKeyframe>();
                result.Add(new CurveBindingMetadata
                {
                    path = binding.path,
                    property = binding.propertyName,
                    componentType = binding.type == null ? string.Empty : binding.type.FullName,
                    objectReferenceCurve = true,
                    rootOrMotionRelated = IsRootOrMotionBinding(binding.path, binding.propertyName),
                    keyCount = keys.Length,
                    firstKeyTime = keys.Length == 0 ? 0f : keys[0].time,
                    lastKeyTime = keys.Length == 0 ? 0f : keys[keys.Length - 1].time
                });
            }

            return result;
        }

        private static List<AnimationEventMetadata> CreateEventMetadata(AnimationClip clip)
        {
            var result = new List<AnimationEventMetadata>();
            foreach (var animationEvent in AnimationUtility.GetAnimationEvents(clip))
            {
                result.Add(new AnimationEventMetadata
                {
                    time = animationEvent.time,
                    functionName = animationEvent.functionName,
                    stringParameter = animationEvent.stringParameter,
                    floatParameter = animationEvent.floatParameter,
                    intParameter = animationEvent.intParameter,
                    objectReference = animationEvent.objectReferenceParameter == null
                        ? string.Empty
                        : AssetDatabase.GetAssetPath(animationEvent.objectReferenceParameter) + "|" +
                          animationEvent.objectReferenceParameter.name,
                    messageOptions = animationEvent.messageOptions.ToString()
                });
            }
            return result;
        }

        private static List<float> BuildSampleTimes(AnimationClip clip, ExportOptions options, out bool limitReached)
        {
            var lastSampleTime = Mathf.Max(0f, clip.length - 0.0001f);
            var times = new SortedSet<float> { 0f, lastSampleTime };
            var interval = 1f / options.sampleRateHz;
            var uniformCount = Mathf.CeilToInt(clip.length * options.sampleRateHz);
            for (var index = 1; index < uniformCount; index++)
                times.Add(Mathf.Min(lastSampleTime, index * interval));

            if (options.includeCurveKeyTimes)
            {
                foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                {
                    var curve = AnimationUtility.GetEditorCurve(clip, binding);
                    if (curve == null) continue;
                    foreach (var key in curve.keys)
                        if (key.time >= 0f && key.time <= clip.length)
                            times.Add(Mathf.Min(key.time, lastSampleTime));
                }

                foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
                {
                    var keys = AnimationUtility.GetObjectReferenceCurve(clip, binding);
                    if (keys == null) continue;
                    foreach (var key in keys)
                        if (key.time >= 0f && key.time <= clip.length)
                            times.Add(Mathf.Min(key.time, lastSampleTime));
                }
            }

            limitReached = times.Count > options.maxPoseSamples;
            if (!limitReached)
                return times.ToList();

            var allTimes = times.ToArray();
            var reduced = new SortedSet<float> { 0f, lastSampleTime };
            var retainedInteriorCount = Mathf.Max(0, options.maxPoseSamples - 3);
            var stride = retainedInteriorCount <= 1
                ? 1f
                : (float)(allTimes.Length - 1) / (retainedInteriorCount - 1);
            for (var index = 0; index < retainedInteriorCount; index++)
                reduced.Add(allTimes[Mathf.Clamp(Mathf.RoundToInt(index * stride), 0, allTimes.Length - 1)]);
            return reduced.ToList();
        }

        private static float CalculateAverageInterval(IReadOnlyList<float> times)
        {
            if (times.Count < 2) return 0f;
            return (times[times.Count - 1] - times[0]) / (times.Count - 1);
        }

        private static float CalculateMinimumInterval(IReadOnlyList<float> times)
        {
            if (times.Count < 2) return 0f;
            var min = float.MaxValue;
            for (var index = 1; index < times.Count; index++)
                min = Mathf.Min(min, times[index] - times[index - 1]);
            return min;
        }

        private static float CalculateMaximumInterval(IReadOnlyList<float> times)
        {
            if (times.Count < 2) return 0f;
            var max = 0f;
            for (var index = 1; index < times.Count; index++)
                max = Mathf.Max(max, times[index] - times[index - 1]);
            return max;
        }

        private static List<PoseFrame> SamplePoses(
            PreviewRig rig,
            AnimationClip clip,
            IReadOnlyList<float> times)
        {
            var frames = new List<PoseFrame>(times.Count);
            var root = rig.Instance.transform;
            var hips = SafeGetBoneTransform(rig.Animator, HumanBodyBones.Hips);
            var spine = SafeGetBoneTransform(rig.Animator, HumanBodyBones.UpperChest) ??
                        SafeGetBoneTransform(rig.Animator, HumanBodyBones.Chest) ??
                        SafeGetBoneTransform(rig.Animator, HumanBodyBones.Spine);
            var leftFoot = SafeGetBoneTransform(rig.Animator, HumanBodyBones.LeftFoot);
            var rightFoot = SafeGetBoneTransform(rig.Animator, HumanBodyBones.RightFoot);
            var leftKnee = SafeGetBoneTransform(rig.Animator, HumanBodyBones.LeftLowerLeg);
            var rightKnee = SafeGetBoneTransform(rig.Animator, HumanBodyBones.RightLowerLeg);
            var leftUpperLeg = SafeGetBoneTransform(rig.Animator, HumanBodyBones.LeftUpperLeg);
            var rightUpperLeg = SafeGetBoneTransform(rig.Animator, HumanBodyBones.RightUpperLeg);
            var allTransforms = rig.Instance.GetComponentsInChildren<Transform>(true);

            if (hips == null || leftFoot == null || rightFoot == null ||
                leftKnee == null || rightKnee == null ||
                leftUpperLeg == null || rightUpperLeg == null)
                throw new InvalidOperationException(
                    rig.CharacterName + ": mapping Humanoid incompleto. Servono Hips, entrambe le anche, ginocchia e piedi.");

            Vector3 restLateralAxisLocal = Vector3.zero;
            for (var index = 0; index < times.Count; index++)
            {
                var time = times[index];
                if (index % 10 == 0 || index == times.Count - 1)
                    EditorUtility.DisplayProgressBar(
                        "UnderGrind — Analisi animazioni",
                        rig.CharacterName + " / " + clip.name + " — campionamento pose " +
                        (index + 1) + "/" + times.Count,
                        (float)(index + 1) / times.Count);
                Evaluate(rig, clip, time);

                // Match the project foot-crossing test: anatomical left-to-right axis is recalculated
                // from the two upper legs at every sample, so it follows pelvis rotation.
                var lateralAxisLocal = root.InverseTransformDirection(
                    leftUpperLeg.position - rightUpperLeg.position).normalized;
                if (lateralAxisLocal.sqrMagnitude < 0.9f)
                    throw new InvalidOperationException(
                        rig.CharacterName + ": asse anatomico delle anche non valido al tempo " +
                        time.ToString("F4", CultureInfo.InvariantCulture) + " s.");

                if (index == 0)
                    restLateralAxisLocal = lateralAxisLocal;

                var leftFootRootLocal = root.InverseTransformPoint(leftFoot.position);
                var rightFootRootLocal = root.InverseTransformPoint(rightFoot.position);
                var footDelta = leftFootRootLocal - rightFootRootLocal;
                var leftKneeRootLocal = root.InverseTransformPoint(leftKnee.position);
                var rightKneeRootLocal = root.InverseTransformPoint(rightKnee.position);
                var kneeDelta = leftKneeRootLocal - rightKneeRootLocal;
                var modelForward = root.TransformDirection(Vector3.left).normalized;
                var modelRight = Vector3.Cross(root.up, modelForward).normalized;

                var frame = new PoseFrame
                {
                    index = index,
                    timeSeconds = time,
                    normalizedTime = clip.length <= 0f ? 0f : time / clip.length,
                    rootWorldPosition = root.position,
                    rootWorldRotation = root.rotation,
                    rootLocalPosition = root.localPosition,
                    modelForwardWorld = modelForward,
                    modelRightWorld = modelRight,
                    hipsWorldPosition = hips.position,
                    hipsWorldRotation = hips.rotation,
                    hipsRootLocalPosition = root.InverseTransformPoint(hips.position),
                    anatomicalLateralAxisLocal = lateralAxisLocal,
                    restAnatomicalLateralAxisLocal = restLateralAxisLocal,
                    leftFootRelativeToHips = hips.InverseTransformPoint(leftFoot.position),
                    rightFootRelativeToHips = hips.InverseTransformPoint(rightFoot.position),
                    leftFootRelativeRotationToHips = Quaternion.Inverse(hips.rotation) * leftFoot.rotation,
                    rightFootRelativeRotationToHips = Quaternion.Inverse(hips.rotation) * rightFoot.rotation,
                    leftFootSignedLateralSeparation = Vector3.Dot(footDelta, lateralAxisLocal),
                    leftRightFootStaticAxisSeparationSigned = Vector3.Dot(footDelta, restLateralAxisLocal),
                    leftRightFootDistanceMeters = footDelta.magnitude,
                    leftRightFootLateralSeparationMeters = Mathf.Abs(Vector3.Dot(footDelta, lateralAxisLocal)),
                    leftRightKneeSignedLateralSeparation = Vector3.Dot(kneeDelta, lateralAxisLocal),
                    leftRightKneeStaticAxisSeparationSigned = Vector3.Dot(kneeDelta, restLateralAxisLocal),
                    leftRightKneeDistanceMeters = kneeDelta.magnitude,
                    leftRightKneeLateralSeparationMeters = Mathf.Abs(Vector3.Dot(kneeDelta, lateralAxisLocal)),
                    leftFootHeightInRootSpace = leftFootRootLocal.y,
                    rightFootHeightInRootSpace = rightFootRootLocal.y
                };

                if (frames.Count > 0)
                    frame.hipsDriftFromStartMeters = Vector3.Distance(
                        frame.hipsRootLocalPosition, frames[0].hipsRootLocalPosition);

                if (spine != null)
                {
                    var spineDirection = (spine.position - hips.position).normalized;
                    frame.torsoLeanDegrees = Vector3.Angle(root.up, spineDirection);
                }

                frame.leftKneeAngleDegrees = CalculateJointAngle(leftUpperLeg, leftKnee, leftFoot);
                frame.rightKneeAngleDegrees = CalculateJointAngle(rightUpperLeg, rightKnee, rightFoot);
                frame.leftHipAngleDegrees = CalculateJointAngle(hips, leftUpperLeg, leftKnee);
                frame.rightHipAngleDegrees = CalculateJointAngle(hips, rightUpperLeg, rightKnee);

                foreach (var humanBone in InspectedBones)
                {
                    var boneTransform = SafeGetBoneTransform(rig.Animator, humanBone);
                    if (boneTransform == null) continue;
                    frame.bones.Add(new BonePose
                    {
                        humanBone = humanBone.ToString(),
                        transformName = boneTransform.name,
                        hierarchyPath = GetHierarchyPath(boneTransform),
                        parentName = boneTransform.parent == null ? string.Empty : boneTransform.parent.name,
                        localPosition = boneTransform.localPosition,
                        localRotation = boneTransform.localRotation,
                        localEulerAngles = boneTransform.localEulerAngles,
                        localScale = boneTransform.localScale,
                        worldPosition = boneTransform.position,
                        worldRotation = boneTransform.rotation,
                        worldEulerAngles = boneTransform.eulerAngles
                    });
                }

                foreach (var transform in allTransforms)
                {
                    frame.transforms.Add(new TransformPose
                    {
                        hierarchyPath = GetHierarchyPath(transform),
                        transformName = transform.name,
                        parentPath = transform.parent == null ? string.Empty : GetHierarchyPath(transform.parent),
                        tag = transform.gameObject.tag,
                        layer = transform.gameObject.layer,
                        layerName = LayerMask.LayerToName(transform.gameObject.layer),
                        activeSelf = transform.gameObject.activeSelf,
                        activeInHierarchy = transform.gameObject.activeInHierarchy,
                        localPosition = transform.localPosition,
                        localRotation = transform.localRotation,
                        localEulerAngles = transform.localEulerAngles,
                        localScale = transform.localScale,
                        worldPosition = transform.position,
                        worldRotation = transform.rotation,
                        worldEulerAngles = transform.eulerAngles,
                        lossyScale = transform.lossyScale
                    });
                }

                frames.Add(frame);
            }

            return frames;
        }

        private static float CalculateJointAngle(Transform proximal, Transform joint, Transform distal)
        {
            if (proximal == null || joint == null || distal == null)
                return -1f;
            var first = proximal.position - joint.position;
            var second = distal.position - joint.position;
            if (first.sqrMagnitude < 0.000001f || second.sqrMagnitude < 0.000001f)
                return -1f;
            return Vector3.Angle(first, second);
        }

        private static void CalculatePoseDerivatives(List<PoseFrame> frames)
        {
            var previousVelocities = new Dictionary<string, Vector3>(StringComparer.Ordinal);
            var previousTransformVelocities = new Dictionary<string, Vector3>(StringComparer.Ordinal);
            for (var frameIndex = 0; frameIndex < frames.Count; frameIndex++)
            {
                var frame = frames[frameIndex];
                var previousFrame = frameIndex > 0 ? frames[frameIndex - 1] : null;
                var deltaTime = previousFrame == null ? 0f : frame.timeSeconds - previousFrame.timeSeconds;

                foreach (var bone in frame.bones)
                {
                    var key = bone.humanBone;
                    if (previousFrame != null && deltaTime > 0f)
                    {
                        var previousBone = previousFrame.bones.FirstOrDefault(item => item.humanBone == key);
                        if (previousBone != null)
                        {
                            bone.linearVelocity = (bone.worldPosition - previousBone.worldPosition) / deltaTime;
                            bone.linearSpeed = bone.linearVelocity.magnitude;
                            bone.angularVelocityDegreesPerSecond =
                                Quaternion.Angle(previousBone.worldRotation, bone.worldRotation) / deltaTime;
                            if (previousVelocities.TryGetValue(key, out var previousVelocity))
                                bone.linearAcceleration = (bone.linearVelocity - previousVelocity).magnitude / deltaTime;
                            previousVelocities[key] = bone.linearVelocity;
                        }
                    }
                }

                foreach (var transform in frame.transforms)
                {
                    var key = transform.hierarchyPath;
                    if (previousFrame != null && deltaTime > 0f)
                    {
                        var previousTransform = previousFrame.transforms.FirstOrDefault(item => item.hierarchyPath == key);
                        if (previousTransform != null)
                        {
                            transform.linearVelocity = (transform.worldPosition - previousTransform.worldPosition) / deltaTime;
                            transform.linearSpeed = transform.linearVelocity.magnitude;
                            transform.angularVelocityDegreesPerSecond =
                                Quaternion.Angle(previousTransform.worldRotation, transform.worldRotation) / deltaTime;
                            if (previousTransformVelocities.TryGetValue(key, out var previousVelocity))
                                transform.linearAcceleration = (transform.linearVelocity - previousVelocity).magnitude / deltaTime;
                            previousTransformVelocities[key] = transform.linearVelocity;
                        }
                    }
                }

                var leftFootPose = frame.bones.FirstOrDefault(item => item.humanBone == HumanBodyBones.LeftFoot.ToString());
                var rightFootPose = frame.bones.FirstOrDefault(item => item.humanBone == HumanBodyBones.RightFoot.ToString());
                if (leftFootPose != null)
                {
                    frame.leftFootSpeedMetersPerSecond = leftFootPose.linearSpeed;
                    frame.leftFootAccelerationMetersPerSecondSquared = leftFootPose.linearAcceleration;
                }
                if (rightFootPose != null)
                {
                    frame.rightFootSpeedMetersPerSecond = rightFootPose.linearSpeed;
                    frame.rightFootAccelerationMetersPerSecondSquared = rightFootPose.linearAcceleration;
                }
            }
        }

        private static void CalculateFootMetrics(
            List<PoseFrame> frames,
            MotionSummary summary,
            ExportOptions options)
        {
            if (frames.Count == 0) return;

            var first = frames[0];
            summary.restFootSeparationMeters = first.leftFootSignedLateralSeparation;
            summary.restKneeSeparationMeters = first.leftRightKneeSignedLateralSeparation;
            summary.crossingToleranceMeters = options.crossingToleranceMeters;
            summary.restAnatomicalLateralAxisLocal = first.restAnatomicalLateralAxisLocal;

            var minFootHeight = frames.Min(frame => Mathf.Min(
                frame.leftFootHeightInRootSpace, frame.rightFootHeightInRootSpace));
            var footSeparationSum = 0f;
            var leftContactCount = 0;
            var rightContactCount = 0;
            var leftFootPath = 0f;
            var rightFootPath = 0f;
            var leftMaxSpeed = 0f;
            var rightMaxSpeed = 0f;
            var leftMaxSlide = 0f;
            var rightMaxSlide = 0f;
            var previousLeft = Vector3.zero;
            var previousRight = Vector3.zero;
            var hasPreviousFeet = false;

            summary.minimumFootSeparationMeters = float.MaxValue;
            summary.minimumKneeSeparationMeters = float.MaxValue;
            summary.minimumStaticAxisFootSeparationMeters = float.MaxValue;
            summary.minimumStaticAxisKneeSeparationMeters = float.MaxValue;
            summary.minimumFootSeparationTimeSeconds = 0f;
            summary.minimumKneeSeparationTimeSeconds = 0f;

            foreach (var frame in frames)
            {
                frame.leftFootHeightAboveObservedMinimum = frame.leftFootHeightInRootSpace - minFootHeight;
                frame.rightFootHeightAboveObservedMinimum = frame.rightFootHeightInRootSpace - minFootHeight;

                var leftBone = frame.bones.FirstOrDefault(item => item.humanBone == HumanBodyBones.LeftFoot.ToString());
                var rightBone = frame.bones.FirstOrDefault(item => item.humanBone == HumanBodyBones.RightFoot.ToString());
                if (leftBone != null && rightBone != null)
                {
                    frame.leftFootEstimatedContact =
                        frame.leftFootHeightAboveObservedMinimum <= options.contactHeightToleranceMeters &&
                        leftBone.linearSpeed <= options.contactSpeedToleranceMetersPerSecond;
                    frame.rightFootEstimatedContact =
                        frame.rightFootHeightAboveObservedMinimum <= options.contactHeightToleranceMeters &&
                        rightBone.linearSpeed <= options.contactSpeedToleranceMetersPerSecond;
                    frame.leftFootEstimatedSliding = frame.leftFootEstimatedContact &&
                        leftBone.linearSpeed > options.contactSpeedToleranceMetersPerSecond * 0.5f;
                    frame.rightFootEstimatedSliding = frame.rightFootEstimatedContact &&
                        rightBone.linearSpeed > options.contactSpeedToleranceMetersPerSecond * 0.5f;

                    if (hasPreviousFeet)
                    {
                        leftFootPath += Vector3.Distance(previousLeft, leftBone.worldPosition);
                        rightFootPath += Vector3.Distance(previousRight, rightBone.worldPosition);
                    }
                    previousLeft = leftBone.worldPosition;
                    previousRight = rightBone.worldPosition;
                    hasPreviousFeet = true;
                }

                if (frame.leftFootEstimatedContact) leftContactCount++;
                if (frame.rightFootEstimatedContact) rightContactCount++;
                leftMaxSpeed = Mathf.Max(leftMaxSpeed, frame.leftFootSpeedMetersPerSecond);
                rightMaxSpeed = Mathf.Max(rightMaxSpeed, frame.rightFootSpeedMetersPerSecond);
                if (frame.leftFootEstimatedContact) leftMaxSlide = Mathf.Max(leftMaxSlide, frame.leftFootSpeedMetersPerSecond);
                if (frame.rightFootEstimatedContact) rightMaxSlide = Mathf.Max(rightMaxSlide, frame.rightFootSpeedMetersPerSecond);

                var absoluteFootSeparation = Mathf.Abs(frame.leftFootSignedLateralSeparation);
                var absoluteKneeSeparation = Mathf.Abs(frame.leftRightKneeSignedLateralSeparation);
                summary.minimumFootSeparationMeters = Mathf.Min(
                    summary.minimumFootSeparationMeters, frame.leftFootSignedLateralSeparation);
                summary.maximumFootSeparationMeters = Mathf.Max(
                    summary.maximumFootSeparationMeters, absoluteFootSeparation);
                footSeparationSum += absoluteFootSeparation;

                if (frame.leftFootSignedLateralSeparation < -options.crossingToleranceMeters)
                {
                    summary.footCrossingSampleCount++;
                    summary.footCrossingTimesSeconds.Add(frame.timeSeconds);
                }

                if (frame.leftRightFootStaticAxisSeparationSigned < -options.crossingToleranceMeters)
                {
                    summary.staticAxisFootCrossingSampleCount++;
                    summary.staticAxisFootCrossingTimesSeconds.Add(frame.timeSeconds);
                }

                summary.minimumKneeSeparationMeters = Mathf.Min(
                    summary.minimumKneeSeparationMeters, frame.leftRightKneeSignedLateralSeparation);
                summary.maximumKneeSeparationMeters = Mathf.Max(
                    summary.maximumKneeSeparationMeters, absoluteKneeSeparation);

                if (frame.leftRightKneeSignedLateralSeparation < -options.crossingToleranceMeters)
                {
                    summary.kneeCrossingSampleCount++;
                    summary.kneeCrossingTimesSeconds.Add(frame.timeSeconds);
                }

                if (frame.leftRightKneeStaticAxisSeparationSigned < -options.crossingToleranceMeters)
                {
                    summary.staticAxisKneeCrossingSampleCount++;
                    summary.staticAxisKneeCrossingTimesSeconds.Add(frame.timeSeconds);
                }

                summary.minimumStaticAxisFootSeparationMeters = Mathf.Min(
                    summary.minimumStaticAxisFootSeparationMeters,
                    frame.leftRightFootStaticAxisSeparationSigned);
                summary.minimumStaticAxisKneeSeparationMeters = Mathf.Min(
                    summary.minimumStaticAxisKneeSeparationMeters,
                    frame.leftRightKneeStaticAxisSeparationSigned);

                if (frame.leftFootSignedLateralSeparation == summary.minimumFootSeparationMeters)
                    summary.minimumFootSeparationTimeSeconds = frame.timeSeconds;
                if (frame.leftRightKneeSignedLateralSeparation == summary.minimumKneeSeparationMeters)
                    summary.minimumKneeSeparationTimeSeconds = frame.timeSeconds;
            }

            summary.meanFootSeparationMeters = footSeparationSum / frames.Count;
            summary.leftFootPathLengthMeters = leftFootPath;
            summary.rightFootPathLengthMeters = rightFootPath;
            summary.leftFootMaximumSpeedMetersPerSecond = leftMaxSpeed;
            summary.rightFootMaximumSpeedMetersPerSecond = rightMaxSpeed;
            summary.leftFootMaximumEstimatedContactSlideSpeed = leftMaxSlide;
            summary.rightFootMaximumEstimatedContactSlideSpeed = rightMaxSlide;
            summary.leftFootEstimatedContactRatio = (float)leftContactCount / frames.Count;
            summary.rightFootEstimatedContactRatio = (float)rightContactCount / frames.Count;
            if (summary.minimumFootSeparationMeters == float.MaxValue) summary.minimumFootSeparationMeters = 0f;
            if (summary.minimumKneeSeparationMeters == float.MaxValue) summary.minimumKneeSeparationMeters = 0f;
            if (summary.minimumStaticAxisFootSeparationMeters == float.MaxValue) summary.minimumStaticAxisFootSeparationMeters = 0f;
            if (summary.minimumStaticAxisKneeSeparationMeters == float.MaxValue) summary.minimumStaticAxisKneeSeparationMeters = 0f;
        }

        private static float CalculateLoopPositionError(List<PoseFrame> frames)
        {
            if (frames.Count < 2) return 0f;
            var first = frames[0];
            var endFrame = frames[frames.Count - 1];
            return Mathf.Max(
                Vector3.Distance(first.leftFootRelativeToHips, endFrame.leftFootRelativeToHips),
                Vector3.Distance(first.rightFootRelativeToHips, endFrame.rightFootRelativeToHips));
        }

        private static float CalculateLoopWorldBonePositionError(List<PoseFrame> frames)
        {
            if (frames.Count < 2) return 0f;
            var first = frames[0];
            var last = frames[frames.Count - 1];
            var names = new[] { "Hips", "LeftFoot", "RightFoot", "LeftLowerLeg", "RightLowerLeg" };
            var errors = new List<float>();
            foreach (var name in names)
            {
                var a = first.bones.FirstOrDefault(bone => bone.humanBone == name);
                var b = last.bones.FirstOrDefault(bone => bone.humanBone == name);
                if (a != null && b != null)
                    errors.Add(Vector3.Distance(a.worldPosition, b.worldPosition));
            }
            return errors.Count == 0 ? 0f : errors.Max();
        }

        private static float CalculateLoopRotationError(List<PoseFrame> frames)
        {
            if (frames.Count < 2) return 0f;
            var first = frames[0];
            var endFrame = frames[frames.Count - 1];
            return Mathf.Max(
                Quaternion.Angle(first.leftFootRelativeRotationToHips, endFrame.leftFootRelativeRotationToHips),
                Quaternion.Angle(first.rightFootRelativeRotationToHips, endFrame.rightFootRelativeRotationToHips));
        }

        private static List<float> BuildContactSheetTimes(AnimationClip clip, int frameCount)
        {
            var times = new List<float>(frameCount);
            for (var index = 0; index < frameCount; index++)
                times.Add(clip.length <= 0f ? 0f : clip.length * index / frameCount);
            return times;
        }

        private static void CaptureContactSheet(
            PreviewRig rig,
            AnimationClip clip,
            IReadOnlyList<float> times,
            ExportOptions options,
            string outputPath)
        {
            const int angleCount = 4;
            var sheet = new Texture2D(
                options.cellWidth * angleCount,
                options.cellHeight * times.Count,
                TextureFormat.RGBA32,
                false,
                false);

            try
            {
                for (var row = 0; row < times.Count; row++)
                {
                    EditorUtility.DisplayProgressBar(
                        "UnderGrind — Analisi animazioni",
                        rig.CharacterName + " / " + clip.name + " — contact sheet " +
                        (row + 1) + "/" + times.Count,
                        (float)(row + 1) / times.Count);
                    Evaluate(rig, clip, times[row]);
                    for (var column = 0; column < angleCount; column++)
                    {
                        ConfigureCamera(rig, column, options.cellWidth, options.cellHeight);
                        var rect = new Rect(0f, 0f, options.cellWidth, options.cellHeight);
                        Texture2D rendered = null;
                        var previewBegun = false;
                        try
                        {
                            rig.Preview.BeginStaticPreview(rect);
                            previewBegun = true;
                            var pixelScale = EditorGUIUtility.pixelsPerPoint;
                            rig.Preview.camera.pixelRect = new Rect(
                                0f, 0f, options.cellWidth * pixelScale, options.cellHeight * pixelScale);
                            rig.Preview.Render(true, false);
                            rendered = rig.Preview.EndStaticPreview();
                            previewBegun = false;
                            if (rendered == null)
                                throw new InvalidOperationException("PreviewRenderUtility non ha restituito un frame.");

                            var y = (times.Count - 1 - row) * options.cellHeight;
                            var pixels = GetScaledPixels(rendered, options.cellWidth, options.cellHeight);
                            sheet.SetPixels(column * options.cellWidth, y,
                                options.cellWidth, options.cellHeight, pixels);
                        }
                        finally
                        {
                            if (previewBegun)
                            {
                                try
                                {
                                    var abandoned = rig.Preview.EndStaticPreview();
                                    if (abandoned != null)
                                        UnityEngine.Object.DestroyImmediate(abandoned);
                                }
                                catch (Exception cleanupException)
                                {
                                    Debug.LogException(cleanupException);
                                }
                            }
                            if (rendered != null)
                                UnityEngine.Object.DestroyImmediate(rendered);
                        }
                    }
                }

                sheet.Apply(false, false);
                File.WriteAllBytes(outputPath, sheet.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sheet);
            }
        }

        private static Color[] GetScaledPixels(Texture2D source, int width, int height)
        {
            if (source.width == width && source.height == height)
                return source.GetPixels();

            var previous = RenderTexture.active;
            var target = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
            Texture2D resized = null;
            try
            {
                Graphics.Blit(source, target);
                RenderTexture.active = target;
                resized = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
                resized.ReadPixels(new Rect(0f, 0f, width, height), 0, 0, false);
                resized.Apply(false, false);
                return resized.GetPixels();
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
                if (resized != null)
                    UnityEngine.Object.DestroyImmediate(resized);
            }
        }

        private static void ConfigureCamera(PreviewRig rig, int angleIndex, int width, int height)
        {
            var camera = rig.Preview.camera;
            var root = rig.Animator.transform;
            var forward = root.TransformDirection(Vector3.left).normalized;
            var right = Vector3.Cross(root.up, forward).normalized;
            Vector3 direction;
            switch (angleIndex)
            {
                case 0: direction = forward; break;
                case 1: direction = (forward + right).normalized; break;
                case 2: direction = right; break;
                default: direction = -forward; break;
            }

            var modelUp = rig.Animator.transform.up;
            var target = rig.Bounds.center + modelUp * (rig.Bounds.extents.y * 0.03f);
            camera.aspect = (float)width / height;
            camera.transform.position = target + direction * 4f + modelUp * (rig.Bounds.extents.y * 0.04f);
            camera.transform.LookAt(target, modelUp);

            var boundsExtents = rig.Bounds.extents;
            var cameraRight = camera.transform.right;
            var cameraUp = camera.transform.up;
            var projectedHalfWidth =
                Mathf.Abs(boundsExtents.x * cameraRight.x) +
                Mathf.Abs(boundsExtents.y * cameraRight.y) +
                Mathf.Abs(boundsExtents.z * cameraRight.z);
            var projectedHalfHeight =
                Mathf.Abs(boundsExtents.x * cameraUp.x) +
                Mathf.Abs(boundsExtents.y * cameraUp.y) +
                Mathf.Abs(boundsExtents.z * cameraUp.z);
            var tangentVertical = Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            var tangentHorizontal = tangentVertical * camera.aspect;
            var distance = Mathf.Max(
                projectedHalfHeight / tangentVertical,
                projectedHalfWidth / tangentHorizontal) * 1.2f;
            camera.transform.position = target + direction * distance + modelUp * (rig.Bounds.extents.y * 0.04f);
            camera.transform.LookAt(target, modelUp);
        }

        private static List<GeometryFrame> CaptureGeometryFrames(
            PreviewRig rig,
            AnimationClip clip,
            IReadOnlyList<float> times)
        {
            var result = new List<GeometryFrame>();
            var renderers = rig.Instance.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Where(renderer => renderer.enabled && renderer.sharedMesh != null)
                .ToArray();

            for (var frameIndex = 0; frameIndex < times.Count; frameIndex++)
            {
                EditorUtility.DisplayProgressBar(
                    "UnderGrind — Analisi animazioni",
                    rig.CharacterName + " / " + clip.name + " — mesh deformate " +
                    (frameIndex + 1) + "/" + times.Count,
                    (float)(frameIndex + 1) / times.Count);
                Evaluate(rig, clip, times[frameIndex]);
                var frame = new GeometryFrame
                {
                    contactSheetRowIndex = frameIndex,
                    timeSeconds = times[frameIndex]
                };

                foreach (var renderer in renderers)
                {
                    var baked = new Mesh { name = "UnderGrind_Inspection_BakedMesh" };
                    try
                    {
                        renderer.BakeMesh(baked);
                        var vertices = baked.vertices;
                        var min = vertices.Length == 0 ? Vector3.zero : vertices[0];
                        var max = min;
                        var centroid = Vector3.zero;
                        var nonFinite = 0;
                        var maxRadius = 0f;
                        foreach (var vertex in vertices)
                        {
                            if (!IsFinite(vertex))
                            {
                                nonFinite++;
                                continue;
                            }
                            min = Vector3.Min(min, vertex);
                            max = Vector3.Max(max, vertex);
                            centroid += vertex;
                            maxRadius = Mathf.Max(maxRadius, vertex.magnitude);
                        }
                        if (vertices.Length > nonFinite)
                            centroid /= vertices.Length - nonFinite;

                        var blendShapeWeights = new StringBuilder();
                        if (renderer.sharedMesh != null)
                        {
                            for (var blendIndex = 0; blendIndex < renderer.sharedMesh.blendShapeCount; blendIndex++)
                            {
                                if (blendIndex > 0) blendShapeWeights.Append("; ");
                                blendShapeWeights.Append(renderer.sharedMesh.GetBlendShapeName(blendIndex))
                                    .Append("=")
                                    .Append(renderer.GetBlendShapeWeight(blendIndex).ToString("F4", CultureInfo.InvariantCulture));
                            }
                        }

                        frame.renderers.Add(new RendererGeometrySample
                        {
                            rendererName = renderer.name,
                            hierarchyPath = GetHierarchyPath(renderer.transform),
                            worldBounds = renderer.bounds.ToString(),
                            bakedLocalBounds = baked.bounds.ToString(),
                            bakedVertexCentroid = centroid,
                            bakedVertexMinimum = min,
                            bakedVertexMaximum = max,
                            maximumVertexRadius = maxRadius,
                            vertexCount = vertices.Length,
                            nonFiniteVertexCount = nonFinite,
                            blendShapeWeights = blendShapeWeights.ToString()
                        });
                    }
                    finally
                    {
                        UnityEngine.Object.DestroyImmediate(baked);
                    }
                }
                result.Add(frame);
            }

            return result;
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static void WritePoseCsv(string path, IReadOnlyList<PoseFrame> frames)
        {
            var builder = new StringBuilder();
            builder.AppendLine("frame,timeSeconds,normalizedTime,humanBone,transformName,hierarchyPath,parentName,rootWorldPosX,rootWorldPosY,rootWorldPosZ,rootWorldRotX,rootWorldRotY,rootWorldRotZ,rootWorldRotW,rootLocalPosX,rootLocalPosY,rootLocalPosZ,modelForwardX,modelForwardY,modelForwardZ,modelRightX,modelRightY,modelRightZ,hipsWorldPosX,hipsWorldPosY,hipsWorldPosZ,hipsWorldRotX,hipsWorldRotY,hipsWorldRotZ,hipsWorldRotW,hipsRootLocalX,hipsRootLocalY,hipsRootLocalZ,anatomicalLateralX,anatomicalLateralY,anatomicalLateralZ,restLateralX,restLateralY,restLateralZ,leftFootRelativeHipsX,leftFootRelativeHipsY,leftFootRelativeHipsZ,rightFootRelativeHipsX,rightFootRelativeHipsY,rightFootRelativeHipsZ,leftFootRelativeHipsRotX,leftFootRelativeHipsRotY,leftFootRelativeHipsRotZ,leftFootRelativeHipsRotW,rightFootRelativeHipsRotX,rightFootRelativeHipsRotY,rightFootRelativeHipsRotZ,rightFootRelativeHipsRotW,localPosX,localPosY,localPosZ,localRotX,localRotY,localRotZ,localRotW,localEulerX,localEulerY,localEulerZ,localScaleX,localScaleY,localScaleZ,worldPosX,worldPosY,worldPosZ,worldRotX,worldRotY,worldRotZ,worldRotW,worldEulerX,worldEulerY,worldEulerZ,velocityX,velocityY,velocityZ,speed,acceleration,angularVelocityDegPerSec,hipsDrift,torsoLean,leftFootSignedLateral,leftFootStaticAxisSeparation,footDistance,footLateralSeparation,kneeSignedLateral,kneeStaticAxisSeparation,kneeDistance,kneeLateralSeparation,leftFootHeight,rightFootHeight,leftFootHeightAboveMinimum,rightFootHeightAboveMinimum,leftFootSpeed,rightFootSpeed,leftFootAcceleration,rightFootAcceleration,leftFootContact,rightFootContact,leftFootSliding,rightFootSliding,leftKneeAngle,rightKneeAngle,leftHipAngle,rightHipAngle");

            foreach (var frame in frames)
            {
                foreach (var bone in frame.bones)
                {
                    builder.Append(Csv(frame.index)).Append(',')
                        .Append(F(frame.timeSeconds)).Append(',')
                        .Append(F(frame.normalizedTime)).Append(',')
                        .Append(Csv(bone.humanBone)).Append(',')
                        .Append(Csv(bone.transformName)).Append(',')
                        .Append(Csv(bone.hierarchyPath)).Append(',')
                        .Append(Csv(bone.parentName)).Append(',')
                        .Append(VectorCsv(frame.rootWorldPosition)).Append(',')
                        .Append(QuaternionCsv(frame.rootWorldRotation)).Append(',')
                        .Append(VectorCsv(frame.rootLocalPosition)).Append(',')
                        .Append(VectorCsv(frame.modelForwardWorld)).Append(',')
                        .Append(VectorCsv(frame.modelRightWorld)).Append(',')
                        .Append(VectorCsv(frame.hipsWorldPosition)).Append(',')
                        .Append(QuaternionCsv(frame.hipsWorldRotation)).Append(',')
                        .Append(VectorCsv(frame.hipsRootLocalPosition)).Append(',')
                        .Append(VectorCsv(frame.anatomicalLateralAxisLocal)).Append(',')
                        .Append(VectorCsv(frame.restAnatomicalLateralAxisLocal)).Append(',')
                        .Append(VectorCsv(frame.leftFootRelativeToHips)).Append(',')
                        .Append(VectorCsv(frame.rightFootRelativeToHips)).Append(',')
                        .Append(QuaternionCsv(frame.leftFootRelativeRotationToHips)).Append(',')
                        .Append(QuaternionCsv(frame.rightFootRelativeRotationToHips)).Append(',')
                        .Append(VectorCsv(bone.localPosition)).Append(',')
                        .Append(QuaternionCsv(bone.localRotation)).Append(',')
                        .Append(VectorCsv(bone.localEulerAngles)).Append(',')
                        .Append(VectorCsv(bone.localScale)).Append(',')
                        .Append(VectorCsv(bone.worldPosition)).Append(',')
                        .Append(QuaternionCsv(bone.worldRotation)).Append(',')
                        .Append(VectorCsv(bone.worldEulerAngles)).Append(',')
                        .Append(VectorCsv(bone.linearVelocity)).Append(',')
                        .Append(F(bone.linearSpeed)).Append(',')
                        .Append(F(bone.linearAcceleration)).Append(',')
                        .Append(F(bone.angularVelocityDegreesPerSecond)).Append(',')
                        .Append(F(frame.hipsDriftFromStartMeters)).Append(',')
                        .Append(F(frame.torsoLeanDegrees)).Append(',')
                        .Append(F(frame.leftFootSignedLateralSeparation)).Append(',')
                        .Append(F(frame.leftRightFootStaticAxisSeparationSigned)).Append(',')
                        .Append(F(frame.leftRightFootDistanceMeters)).Append(',')
                        .Append(F(frame.leftRightFootLateralSeparationMeters)).Append(',')
                        .Append(F(frame.leftRightKneeSignedLateralSeparation)).Append(',')
                        .Append(F(frame.leftRightKneeStaticAxisSeparationSigned)).Append(',')
                        .Append(F(frame.leftRightKneeDistanceMeters)).Append(',')
                        .Append(F(frame.leftRightKneeLateralSeparationMeters)).Append(',')
                        .Append(F(frame.leftFootHeightInRootSpace)).Append(',')
                        .Append(F(frame.rightFootHeightInRootSpace)).Append(',')
                        .Append(F(frame.leftFootHeightAboveObservedMinimum)).Append(',')
                        .Append(F(frame.rightFootHeightAboveObservedMinimum)).Append(',')
                        .Append(F(frame.leftFootSpeedMetersPerSecond)).Append(',')
                        .Append(F(frame.rightFootSpeedMetersPerSecond)).Append(',')
                        .Append(F(frame.leftFootAccelerationMetersPerSecondSquared)).Append(',')
                        .Append(F(frame.rightFootAccelerationMetersPerSecondSquared)).Append(',')
                        .Append(frame.leftFootEstimatedContact).Append(',')
                        .Append(frame.rightFootEstimatedContact).Append(',')
                        .Append(frame.leftFootEstimatedSliding).Append(',')
                        .Append(frame.rightFootEstimatedSliding).Append(',')
                        .Append(F(frame.leftKneeAngleDegrees)).Append(',')
                        .Append(F(frame.rightKneeAngleDegrees)).Append(',')
                        .Append(F(frame.leftHipAngleDegrees)).Append(',')
                        .Append(F(frame.rightHipAngleDegrees)).AppendLine();
                }
            }
            File.WriteAllText(path, builder.ToString(), new UTF8Encoding(false));
        }

        private static void WriteTransformCsv(string path, IReadOnlyList<PoseFrame> frames)
        {
            var builder = new StringBuilder();
            builder.AppendLine("frame,timeSeconds,normalizedTime,hierarchyPath,transformName,parentPath,tag,layer,layerName,activeSelf,activeInHierarchy,localPosX,localPosY,localPosZ,localRotX,localRotY,localRotZ,localRotW,localEulerX,localEulerY,localEulerZ,localScaleX,localScaleY,localScaleZ,worldPosX,worldPosY,worldPosZ,worldRotX,worldRotY,worldRotZ,worldRotW,worldEulerX,worldEulerY,worldEulerZ,lossyScaleX,lossyScaleY,lossyScaleZ,velocityX,velocityY,velocityZ,speed,acceleration,angularVelocityDegPerSec");

            foreach (var frame in frames)
            {
                foreach (var transform in frame.transforms)
                {
                    builder.Append(frame.index).Append(',')
                        .Append(F(frame.timeSeconds)).Append(',')
                        .Append(F(frame.normalizedTime)).Append(',')
                        .Append(Csv(transform.hierarchyPath)).Append(',')
                        .Append(Csv(transform.transformName)).Append(',')
                        .Append(Csv(transform.parentPath)).Append(',')
                        .Append(Csv(transform.tag)).Append(',')
                        .Append(transform.layer).Append(',')
                        .Append(Csv(transform.layerName)).Append(',')
                        .Append(transform.activeSelf).Append(',')
                        .Append(transform.activeInHierarchy).Append(',')
                        .Append(VectorCsv(transform.localPosition)).Append(',')
                        .Append(QuaternionCsv(transform.localRotation)).Append(',')
                        .Append(VectorCsv(transform.localEulerAngles)).Append(',')
                        .Append(VectorCsv(transform.localScale)).Append(',')
                        .Append(VectorCsv(transform.worldPosition)).Append(',')
                        .Append(QuaternionCsv(transform.worldRotation)).Append(',')
                        .Append(VectorCsv(transform.worldEulerAngles)).Append(',')
                        .Append(VectorCsv(transform.lossyScale)).Append(',')
                        .Append(VectorCsv(transform.linearVelocity)).Append(',')
                        .Append(F(transform.linearSpeed)).Append(',')
                        .Append(F(transform.linearAcceleration)).Append(',')
                        .Append(F(transform.angularVelocityDegreesPerSecond)).AppendLine();
                }
            }

            File.WriteAllText(path, builder.ToString(), new UTF8Encoding(false));
        }

        private static void WriteCurveCsv(string path, AnimationClip clip)
        {
            var builder = new StringBuilder();
            builder.AppendLine("curveKind,bindingPath,propertyName,componentType,keyIndex,time,value,inTangent,outTangent,inWeight,outWeight,weightedMode,objectReferencePath,objectReferenceName");

            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
            {
                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                if (curve == null) continue;
                var keys = curve.keys;
                for (var index = 0; index < keys.Length; index++)
                {
                    var key = keys[index];
                    builder.Append("Float,")
                        .Append(Csv(binding.path)).Append(',')
                        .Append(Csv(binding.propertyName)).Append(',')
                        .Append(Csv(binding.type == null ? string.Empty : binding.type.FullName)).Append(',')
                        .Append(index).Append(',')
                        .Append(F(key.time)).Append(',')
                        .Append(F(key.value)).Append(',')
                        .Append(F(key.inTangent)).Append(',')
                        .Append(F(key.outTangent)).Append(',')
                        .Append(F(key.inWeight)).Append(',')
                        .Append(F(key.outWeight)).Append(',')
                        .Append(Csv(key.weightedMode.ToString())).Append(",,")
                        .AppendLine();
                }
            }

            foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
            {
                var keys = AnimationUtility.GetObjectReferenceCurve(clip, binding);
                if (keys == null) continue;
                for (var index = 0; index < keys.Length; index++)
                {
                    var key = keys[index];
                    var value = key.value;
                    builder.Append("ObjectReference,")
                        .Append(Csv(binding.path)).Append(',')
                        .Append(Csv(binding.propertyName)).Append(',')
                        .Append(Csv(binding.type == null ? string.Empty : binding.type.FullName)).Append(',')
                        .Append(index).Append(',')
                        .Append(F(key.time)).Append(",,,,,,")
                        .Append(Csv(value == null ? string.Empty : AssetDatabase.GetAssetPath(value))).Append(',')
                        .Append(Csv(value == null ? string.Empty : value.name)).AppendLine();
                }
            }

            File.WriteAllText(path, builder.ToString(), new UTF8Encoding(false));
        }

        private static void WriteGeometryCsv(string path, IReadOnlyList<GeometryFrame> frames)
        {
            var builder = new StringBuilder();
            builder.AppendLine("contactSheetRowIndex,timeSeconds,rendererName,hierarchyPath,worldBounds,bakedLocalBounds,vertexCount,nonFiniteVertexCount,centroidX,centroidY,centroidZ,minX,minY,minZ,maxX,maxY,maxZ,maxVertexRadius,blendShapeWeights");
            foreach (var frame in frames)
            {
                foreach (var renderer in frame.renderers)
                {
                    builder.Append(frame.contactSheetRowIndex).Append(',')
                        .Append(F(frame.timeSeconds)).Append(',')
                        .Append(Csv(renderer.rendererName)).Append(',')
                        .Append(Csv(renderer.hierarchyPath)).Append(',')
                        .Append(Csv(renderer.worldBounds)).Append(',')
                        .Append(Csv(renderer.bakedLocalBounds)).Append(',')
                        .Append(renderer.vertexCount).Append(',')
                        .Append(renderer.nonFiniteVertexCount).Append(',')
                        .Append(VectorCsv(renderer.bakedVertexCentroid)).Append(',')
                        .Append(VectorCsv(renderer.bakedVertexMinimum)).Append(',')
                        .Append(VectorCsv(renderer.bakedVertexMaximum)).Append(',')
                        .Append(F(renderer.maximumVertexRadius)).Append(',')
                        .Append(Csv(renderer.blendShapeWeights)).AppendLine();
                }
            }
            File.WriteAllText(path, builder.ToString(), new UTF8Encoding(false));
        }

        private static void WriteHtmlIndex(string packageDirectory, BundleManifest manifest)
        {
            var html = new StringBuilder();
            html.AppendLine("<!doctype html><html lang=\"it\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\">");
            html.AppendLine("<title>UnderGrind — Animation Inspection</title><style>");
            html.AppendLine("body{font:14px/1.5 system-ui,sans-serif;margin:32px;background:#171a1f;color:#e9edf2}h1,h2{color:#fff}a{color:#8fc7ff}section{margin:28px 0;padding:20px;background:#222831;border-radius:8px}img{width:min(100%,768px);height:auto;image-rendering:auto;border:1px solid #59616b}table{border-collapse:collapse}td,th{padding:6px 10px;border:1px solid #48515c;text-align:left}.muted{color:#adb7c3}.grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(320px,1fr));gap:20px}</style></head><body>");
            html.AppendLine("<h1>UnderGrind — Animation Inspection</h1>");
            html.AppendLine("<p class=\"muted\">Generato UTC: " + Html(manifest.generatedUtc) + " · Unity " + Html(manifest.unityVersion) + "</p>");
            html.AppendLine("<p>Ogni contact sheet contiene 4 viste per riga: frontale, tre quarti, laterale, posteriore. Le righe seguono i tempi indicati sotto ogni immagine.</p>");

            foreach (var entry in manifest.entries)
            {
                var reportPath = entry.reportPath.Replace('\\', '/');
                var sheetPath = entry.contactSheetPath.Replace('\\', '/');
                var posePath = entry.poseCsvPath.Replace('\\', '/');
                var transformPath = entry.transformCsvPath.Replace('\\', '/');
                var curvePath = entry.curveCsvPath.Replace('\\', '/');
                var geometryPath = entry.geometryCsvPath.Replace('\\', '/');
                html.AppendLine("<section><h2>" + Html(entry.clipName) + " — " + Html(entry.character) + "</h2>");
                html.AppendLine("<p>Asset: <code>" + Html(entry.clipAssetPath) + "</code></p>");
                html.AppendLine("<p>Campioni: " + entry.poseSampleCount + " · Curve: " + entry.curveBindingCount +
                                " · Incroci anatomici dinamici: piedi " + entry.footCrossingSamples +
                                " / ginocchia " + entry.kneeCrossingSamples + "</p>");
                html.AppendLine("<p>Separazione firmata minima: piedi " + entry.minimumFootSeparation.ToString("F4", CultureInfo.InvariantCulture) +
                                " m a " + entry.minimumFootSeparationTimeSeconds.ToString("F3", CultureInfo.InvariantCulture) +
                                " s · ginocchia " + entry.minimumKneeSeparation.ToString("F4", CultureInfo.InvariantCulture) +
                                " m a " + entry.minimumKneeSeparationTimeSeconds.ToString("F3", CultureInfo.InvariantCulture) + " s</p>");
                html.AppendLine("<p>Asse anatomico iniziale (locale): " + Html(entry.restAnatomicalLateralAxisLocal.ToString("F4")) +
                                " · soglia incrocio: " + entry.crossingToleranceMeters.ToString("F3", CultureInfo.InvariantCulture) + " m</p>");
                html.AppendLine("<p>Confronto asse statico: piedi min " + entry.minimumStaticAxisFootSeparationMeters.ToString("F4", CultureInfo.InvariantCulture) +
                                " m / " + entry.staticAxisFootCrossingSampleCount + " campioni · ginocchia min " +
                                entry.minimumStaticAxisKneeSeparationMeters.ToString("F4", CultureInfo.InvariantCulture) +
                                " m / " + entry.staticAxisKneeCrossingSampleCount + " campioni</p>");
                html.AppendLine("<p>Rest separation: piedi " + entry.restFootSeparationMeters.ToString("F4", CultureInfo.InvariantCulture) +
                                " m · ginocchia " + entry.restKneeSeparationMeters.ToString("F4", CultureInfo.InvariantCulture) +
                                " m · loop piedi rispetto alle anche " + entry.loopPositionError.ToString("F4", CultureInfo.InvariantCulture) +
                                " m / " + entry.loopRotationErrorDegrees.ToString("F2", CultureInfo.InvariantCulture) +
                                "° · deriva world ossa " + entry.loopWorldBonePositionErrorMeters.ToString("F4", CultureInfo.InvariantCulture) + " m</p>");
                html.AppendLine("<img src=\"" + Html(sheetPath) + "\" alt=\"Contact sheet\">");
                html.AppendLine("<p><b>Colonne:</b> " + Html(string.Join(" · ", entry.contactSheetColumns)) + "</p>");
                html.AppendLine("<p><b>Tempi righe:</b> " + Html(string.Join(" · ", entry.contactSheetTimes.Select(time => time.ToString("F3", CultureInfo.InvariantCulture) + " s"))) + "</p>");
                html.AppendLine("<p>Traiettoria piedi: L " + entry.leftFootPathLengthMeters.ToString("F3", CultureInfo.InvariantCulture) +
                                " m / R " + entry.rightFootPathLengthMeters.ToString("F3", CultureInfo.InvariantCulture) +
                                " m · Velocità max: L " + entry.leftFootMaximumSpeedMetersPerSecond.ToString("F3", CultureInfo.InvariantCulture) +
                                " / R " + entry.rightFootMaximumSpeedMetersPerSecond.ToString("F3", CultureInfo.InvariantCulture) + " m/s</p>");
                html.AppendLine("<p>Contatto stimato: L " + (entry.leftFootEstimatedContactRatio * 100f).ToString("F1", CultureInfo.InvariantCulture) +
                                "% / R " + (entry.rightFootEstimatedContactRatio * 100f).ToString("F1", CultureInfo.InvariantCulture) +
                                "% · Deriva bacino max: " + entry.maximumHipsDriftMeters.ToString("F4", CultureInfo.InvariantCulture) +
                                " m · Inclinazione busto max: " + entry.maximumTorsoLeanDegrees.ToString("F1", CultureInfo.InvariantCulture) + "°</p>");
                html.AppendLine("<p><a href=\"" + Html(reportPath) + "\">Report JSON</a> · <a href=\"" + Html(posePath) +
                                "\">Pose Humanoid CSV</a> · <a href=\"" + Html(transformPath) +
                                "\">Hierarchy Transform CSV</a> · <a href=\"" + Html(curvePath) + "\">Curve CSV</a>");
                if (!string.IsNullOrEmpty(geometryPath))
                    html.AppendLine(" · <a href=\"" + Html(geometryPath) + "\">Geometry CSV</a>");
                html.AppendLine("</p></section>");
            }

            html.AppendLine("</body></html>");
            File.WriteAllText(Path.Combine(packageDirectory, "index.html"), html.ToString(), new UTF8Encoding(false));
        }

        private static void WriteReadme(string packageDirectory, ExportOptions options, BundleManifest manifest)
        {
            var text = new StringBuilder();
            text.AppendLine("UNDERGRIND — ANIMATION INSPECTION PACKAGE");
            text.AppendLine("Generated UTC: " + manifest.generatedUtc);
            text.AppendLine("Unity: " + manifest.unityVersion);
            text.AppendLine();
            text.AppendLine("CONTENTS");
            text.AppendLine("- manifest.json: ambiente, opzioni, elenco delle analisi e risultati sintetici.");
            text.AppendLine("- index.html: indice navigabile con contact sheet e link ai dati.");
            text.AppendLine("- <clip>/<male|female>/report.json: metadati, Avatar, importer, curve, eventi, pose e metriche.");
            text.AppendLine("- pose-samples.csv: un record per osso Humanoid e campione temporale, con trasformazioni e derivate.");
            text.AppendLine("- transform-hierarchy-samples.csv: un record per ogni Transform del prefab, inclusi nodi non Humanoid, gerarchia, tag/layer, stato attivo, trasformazioni locali/world e derivate cinematiche.");
            text.AppendLine("- curve-keys.csv: chiavi di tutte le curve float e object-reference.");
            text.AppendLine("- geometry-samples.csv: bounds e statistiche delle mesh skinned campionate ai tempi delle immagini.");
            text.AppendLine("- contact-sheet.png: quattro angolazioni per ciascun istante rappresentativo.");
            text.AppendLine();
            text.AppendLine("CAMPIONAMENTO");
            text.AppendLine("- Frequenza uniforme: " + options.sampleRateHz + " Hz.");
            text.AppendLine("- Frame immagine per clip: " + options.contactSheetFrames + ".");
            text.AppendLine("- Dimensione cella: " + options.cellWidth + " x " + options.cellHeight + " px.");
            text.AppendLine("- Inclusione tempi keyframe: " + options.includeCurveKeyTimes + ".");
            text.AppendLine("- Limite campioni posa: " + options.maxPoseSamples + ".");
            text.AppendLine("- Diagnostica mesh: " + options.captureSkinnedMeshDiagnostics + ".");
            text.AppendLine();
            text.AppendLine("LETTURA DELLE MISURE");
            text.AppendLine("- Le posizioni/rotazioni world sono misurate sull'istanza temporanea del prefab nel preview scene; quelle local sono relative al parent Transform.");
            text.AppendLine("- Velocità e accelerazioni sono differenze finite tra campioni; l'accuratezza dipende dalla frequenza e dai tempi keyframe inclusi.");
            text.AppendLine("- Il contatto del piede è una stima basata sull'altezza minima osservata nel ciclo e sulla velocità. La percentuale è una frazione dei campioni, non una durata ponderata; non equivale al grounded state del controller.");
            text.AppendLine("- Gli incroci dinamici usano lo stesso riferimento del test Unity: asse laterale ricalcolato a ogni campione dalla differenza LeftUpperLeg - RightUpperLeg, in coordinate locali del root. Un campione è incrociato quando la separazione firmata è inferiore a -crossingToleranceMeters.");
            text.AppendLine("- Viene registrata anche la separazione su un asse laterale statico ricavato dalla posa iniziale, così si può distinguere l'incrocio anatomico da un artefatto causato dalla rotazione del bacino.");
            text.AppendLine("- Il loop position error confronta la posizione dei piedi relativa alle anche tra t=0 e l'ultimo campione prima della fine (clip.length - 0.0001 s); è riportato separatamente anche l'errore world di ossa selezionate. Alcune clip non sono progettate per chiudersi in posa identica; interpretare insieme a loop settings e immagini.");
            text.AppendLine("- Le immagini sono viste statiche renderizzate dal preview scene, non catture del Game View né prova del controller/IK di produzione.");
            text.AppendLine("- L'Animator Controller e il root motion sono disattivati nell'anteprima: le curve root motion sono comunque elencate nel CSV delle curve.");
            text.AppendLine("- La mesh baked è una diagnostica geometrica, non sostituisce il rendering con materiali/luci di gioco.");
            text.AppendLine();
            text.AppendLine("LIMITI");
            text.AppendLine("- Nessuna interazione, fisica, collisione, foot-separation IK, Blend Tree, transizione o input del Player viene eseguita.");
            text.AppendLine("- Il retargeting Humanoid è valutato sui prefab Player, ma non viene eseguita la scena reale.");
            text.AppendLine("- Le soglie di contatto sono euristiche configurabili e non sono verdetti automatici di qualità.");
            text.AppendLine("- Se il report indica sampleLimitReached=true, i tempi sono stati ridotti per rispettare il limite configurato.");
            File.WriteAllText(Path.Combine(packageDirectory, "README.txt"), text.ToString(), new UTF8Encoding(false));
        }

        private static void CreateZip(string sourceDirectory, string zipPath)
        {
            if (File.Exists(zipPath))
                File.Delete(zipPath);
            using (var stream = new FileStream(zipPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Create))
            {
                foreach (var file in Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories))
                {
                    var relative = file.Substring(sourceDirectory.Length)
                        .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                        .Replace(Path.DirectorySeparatorChar, '/');
                    var entry = archive.CreateEntry(relative, CompressionLevel.Optimal);
                    using (var entryStream = entry.Open())
                    using (var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                        fileStream.CopyTo(entryStream);
                }
            }
        }

        private static void Evaluate(PreviewRig rig, AnimationClip clip, float time)
        {
            rig.ClipPlayable.SetTime(Mathf.Clamp(time, 0f, clip.length));
            rig.Graph.Evaluate(0f);
        }

        private static Transform SafeGetBoneTransform(Animator animator, HumanBodyBones bone)
        {
            if (animator == null || !animator.isHuman)
                return null;
            try { return animator.GetBoneTransform(bone); }
            catch (Exception) { return null; }
        }

        private static bool IsRootOrMotionBinding(string path, string property)
        {
            return string.IsNullOrEmpty(path) ||
                   path.IndexOf("root", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   property.IndexOf("Root", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   property.IndexOf("Motion", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   property.IndexOf("Position", StringComparison.OrdinalIgnoreCase) >= 0 &&
                   string.IsNullOrEmpty(path);
        }

        private static string GetHierarchyPath(Transform transform)
        {
            if (transform == null) return string.Empty;
            var parts = new Stack<string>();
            var current = transform;
            while (current != null)
            {
                parts.Push(current.name + "[" + current.GetSiblingIndex() + "]");
                current = current.parent;
            }
            return string.Join("/", parts);
        }

        private static string GetSha256(string assetPath)
        {
            var fullPath = GetAbsoluteAssetPath(assetPath);
            if (!File.Exists(fullPath)) return string.Empty;
            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(fullPath))
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty).ToLowerInvariant();
        }

        private static long GetFileSize(string assetPath)
        {
            var fullPath = GetAbsoluteAssetPath(assetPath);
            return File.Exists(fullPath) ? new FileInfo(fullPath).Length : 0L;
        }

        private static string GetAbsoluteAssetPath(string assetPath)
        {
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            return Path.GetFullPath(Path.Combine(projectRoot, assetPath));
        }

        private static string MakeSlug(string value)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var chars = value.Select(character =>
                invalid.Contains(character) || char.IsWhiteSpace(character) ? '-' : char.ToLowerInvariant(character)).ToArray();
            return new string(chars).Trim('-');
        }

        private static string ToPackagePath(string directory, string file)
        {
            return (directory + "/" + file).Replace('\\', '/');
        }

        private static string F(float value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        private static string VectorCsv(Vector3 value)
        {
            return F(value.x) + "," + F(value.y) + "," + F(value.z);
        }

        private static string QuaternionCsv(Quaternion value)
        {
            return F(value.x) + "," + F(value.y) + "," + F(value.z) + "," + F(value.w);
        }

        private static string Csv(string value)
        {
            if (value == null) return string.Empty;
            return "\"" + value.Replace("\"", "\"\"").Replace("\r", " ").Replace("\n", " ") + "\"";
        }

        private static string Html(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;")
                .Replace("\"", "&quot;").Replace("'", "&#39;");
        }
    }
}