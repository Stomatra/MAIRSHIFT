using MajdataPlay.Scenes.Airshift;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

#nullable enable
namespace MajdataPlay.Editor
{
    /// <summary>
    /// Generates the minimal AIRSHIFT list, gameplay, and result scenes through Unity's serialization APIs.
    /// </summary>
    public static class AirshiftSceneGenerator
    {
        /// <summary>The project-relative AIRSHIFT list scene path.</summary>
        private const string ListScenePath = "Assets/Scenes/AirshiftList.unity";
        /// <summary>The project-relative AIRSHIFT gameplay scene path.</summary>
        private const string GameScenePath = "Assets/Scenes/AirshiftGame.unity";
        /// <summary>The project-relative AIRSHIFT result scene path.</summary>
        private const string ResultScenePath = "Assets/Scenes/AirshiftResult.unity";
        /// <summary>The original STANDARD gameplay scene used as the visual hierarchy source.</summary>
        private const string StandardGameScenePath = "Assets/Scenes/Game.unity";
        /// <summary>The exact STANDARD canvas/layout prefab used by List, Game, Result, and Setting.</summary>
        private const string CanvasTemplatePath = "Assets/Prefabs/CanvasTemplate.prefab";
        /// <summary>The exact STANDARD scene camera prefab used by List, Game, and Result.</summary>
        private const string MainCameraPath = "Assets/Prefabs/Main Camera.prefab";
        private const string ListCoverBackgroundPath = "Assets/Sprites/List/coverbigbg.png";
        private const string GameBackgroundPath = "Assets/Sprites/Default_Background.png";
        private const string ResultBackgroundPath = "Assets/Sprites/Result/ResultBackground.png";
        private const string ResultPanelPath = "Assets/Sprites/Result/ResultPanel.png";


        /// <summary>
        /// Regenerates both AIRSHIFT placeholder scenes and registers them in player build settings.
        /// </summary>
        [MenuItem("MajdataPlay/AIRSHIFT/Generate Placeholder Scenes")]
        public static void Generate()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }
            if (!EditorUtility.DisplayDialog(
                "Regenerate AIRSHIFT placeholder scenes?",
                "This replaces AirshiftGame.unity and AirshiftResult.unity with their minimal controller-only versions.",
                "Regenerate",
                "Cancel"))
            {
                return;
            }
            var returnScenePath = SceneManager.GetActiveScene().path;
            CreateListScene();
            CreateGameScene();
            CreateResultScene();
            RegisterBuildScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!string.IsNullOrEmpty(returnScenePath))
            {
                EditorSceneManager.OpenScene(returnScenePath, OpenSceneMode.Single);
            }
        }

        /// <summary>
        /// Generates only the AIRSHIFT list scene without replacing accepted gameplay or result scenes.
        /// </summary>
        [MenuItem("MajdataPlay/AIRSHIFT/Generate List Scene")]
        public static void GenerateListScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ListScenePath) != null &&
                !EditorUtility.DisplayDialog(
                    "Replace AIRSHIFT list scene?",
                    "This replaces only AirshiftList.unity with its lightweight controller scene.",
                    "Replace",
                    "Cancel"))
            {
                return;
            }
            var returnScenePath = SceneManager.GetActiveScene().path;
            CreateListScene();
            RegisterBuildScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!string.IsNullOrEmpty(returnScenePath))
            {
                EditorSceneManager.OpenScene(returnScenePath, OpenSceneMode.Single);
            }
        }

        /// <summary>
        /// Adds the STANDARD CanvasTemplate to existing AIRSHIFT scenes without regenerating their managers.
        /// </summary>
        [MenuItem("MajdataPlay/AIRSHIFT/Attach STANDARD Scene Shell to Existing Scenes")]
        public static void AttachStandardLayoutToExistingScenes()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }
            if (!EditorUtility.DisplayDialog(
                "Attach STANDARD scene shell to AIRSHIFT scenes?",
                "This preserves the existing AIRSHIFT managers and adds the shared CanvasTemplate and Main Camera prefabs only when they are missing.",
                "Attach",
                "Cancel"))
            {
                return;
            }

            var returnScenePath = SceneManager.GetActiveScene().path;
            foreach (var scenePath in new[] { ListScenePath, GameScenePath, ResultScenePath })
            {
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                var changed = AttachStandardSceneShell(scene);
                changed |= ConfigureStandardVisualReferences(scene);
                if (changed)
                {
                    SaveScene(scene, scenePath);
                }
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!string.IsNullOrEmpty(returnScenePath))
            {
                EditorSceneManager.OpenScene(returnScenePath, OpenSceneMode.Single);
            }
        }

        /// <summary>
        /// Creates the AIRSHIFT local-song selector scene.
        /// </summary>
        private static void CreateListScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            var manager = new GameObject(nameof(AirshiftListManager));
            SceneManager.MoveGameObjectToScene(manager, scene);
            manager.AddComponent<AirshiftListManager>();
            AttachStandardSceneShell(scene);
            ConfigureStandardVisualReferences(scene);
            SaveScene(scene, ListScenePath);
            EditorSceneManager.CloseScene(scene, true);
        }

        /// <summary>
        /// Creates the AIRSHIFT local-test gameplay scene.
        /// </summary>
        private static void CreateGameScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject(nameof(AirshiftGameManager)).AddComponent<AirshiftGameManager>();
            AttachStandardSceneShell(scene);
            ConfigureStandardVisualReferences(scene);
            SaveScene(scene, GameScenePath);
        }

        /// <summary>
        /// Creates the AIRSHIFT placeholder result scene.
        /// </summary>
        private static void CreateResultScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject(nameof(AirshiftResultManager)).AddComponent<AirshiftResultManager>();
            AttachStandardSceneShell(scene);
            ConfigureStandardVisualReferences(scene);
            SaveScene(scene, ResultScenePath);
        }

        /// <summary>
        /// Non-interactive entry point for CI and one-time safe migration of the existing AIRSHIFT scenes.
        /// </summary>
        public static void ApplyStandardSceneShellBatch()
        {
            foreach (var scenePath in new[] { ListScenePath, GameScenePath, ResultScenePath })
            {
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                var changed = AttachStandardSceneShell(scene);
                changed |= ConfigureStandardVisualReferences(scene);
                if (changed)
                {
                    SaveScene(scene, scenePath);
                }
            }
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Copies the independently reusable STANDARD gameplay visuals into the existing AIRSHIFT scene.
        /// </summary>
        /// <remarks>
        /// The copied objects are the original background renderer, chart metadata panel, and the two
        /// text-only main-screen information displayers. STANDARD gameplay, note, input, and score
        /// controllers are deliberately excluded.
        /// </remarks>
        [MenuItem("MajdataPlay/AIRSHIFT/Attach STANDARD Game Visual Hierarchy")]
        public static void AttachStandardGameVisualHierarchy()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }
            if (!EditorUtility.DisplayDialog(
                "Attach STANDARD gameplay visuals?",
                "This adds only the original background, chart metadata, and text-only HUD objects to the existing AirshiftGame scene.",
                "Attach",
                "Cancel"))
            {
                return;
            }
            ApplyStandardGameVisualHierarchyBatch();
        }

        /// <summary>Non-interactive entry point for the safe, one-time STANDARD gameplay visual migration.</summary>
        public static void ApplyStandardGameVisualHierarchyBatch()
        {
            var previousSetup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var sourceScene = EditorSceneManager.OpenScene(StandardGameScenePath, OpenSceneMode.Single);
                var targetScene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Additive);
                var changed = false;
                changed |= CloneVisualObjectOnce(
                    sourceScene,
                    targetScene,
                    "CanvasTemplate/Main_Display/Background",
                    "CanvasTemplate/Main_Display",
                    "StandardGameBackground");
                changed |= CloneVisualObjectOnce(
                    sourceScene,
                    targetScene,
                    "CanvasTemplate/Main_Display/CenterInfoDisplayer",
                    "CanvasTemplate/Main_Display",
                    "StandardGameCenterInfo");
                changed |= CloneVisualObjectOnce(
                    sourceScene,
                    targetScene,
                    "CanvasTemplate/Main_Display/SecondaryInfoDisplayer",
                    "CanvasTemplate/Main_Display",
                    "StandardGameSecondaryInfo");
                changed |= CloneVisualObjectOnce(
                    sourceScene,
                    targetScene,
                    "CanvasTemplate/Sub_Display/Static/ChartInfo",
                    "CanvasTemplate/Sub_Display",
                    "StandardGameChartInfo");
                changed |= ConfigureStandardVisualReferences(targetScene);
                if (changed)
                {
                    SaveScene(targetScene, GameScenePath);
                }
                AssetDatabase.SaveAssets();
            }
            finally
            {
                EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
            }
        }

        /// <summary>Adds the same layout canvas and scene camera shell used by STANDARD scenes.</summary>
        private static bool AttachStandardSceneShell(Scene scene)
        {
            var changed = AttachStandardCanvas(scene);
            changed |= AttachPrefabRootOnce(scene, MainCameraPath, "Main Camera");
            return changed;
        }

        /// <summary>Assigns original STANDARD visual assets to the lightweight AIRSHIFT adapters.</summary>
        private static bool ConfigureStandardVisualReferences(Scene scene)
        {
            var changed = false;
            foreach (var root in scene.GetRootGameObjects())
            {
                var listManager = root.GetComponentInChildren<AirshiftListManager>(true);
                if (listManager != null)
                {
                    changed |= SetSpriteReference(listManager, "_coverBackground", ListCoverBackgroundPath);
                }
                var gameManager = root.GetComponentInChildren<AirshiftGameManager>(true);
                if (gameManager != null)
                {
                    changed |= SetSpriteReference(gameManager, "_gameBackground", GameBackgroundPath);
                }
                var resultManager = root.GetComponentInChildren<AirshiftResultManager>(true);
                if (resultManager != null)
                {
                    changed |= SetSpriteReference(resultManager, "_resultBackground", ResultBackgroundPath);
                    changed |= SetSpriteReference(resultManager, "_resultPanel", ResultPanelPath);
                }
            }
            return changed;
        }

        /// <summary>Assigns one Sprite reference through Unity serialization without changing other fields.</summary>
        private static bool SetSpriteReference(MonoBehaviour target, string propertyName, string assetPath)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite == null)
            {
                throw new UnityException($"Missing STANDARD sprite at {assetPath}.");
            }
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                throw new UnityException($"Missing serialized property {propertyName} on {target.GetType().Name}.");
            }
            if (property.objectReferenceValue == sprite)
            {
                return false;
            }
            property.objectReferenceValue = sprite;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }

        /// <summary>
        /// Instantiates the shared STANDARD CanvasTemplate once in the supplied scene.
        /// </summary>
        /// <param name="scene">The scene that needs the shared layout.</param>
        /// <returns>Whether a prefab instance was added.</returns>
        private static bool AttachStandardCanvas(Scene scene)
        {
            return AttachPrefabRootOnce(scene, CanvasTemplatePath, "CanvasTemplate");
        }

        /// <summary>Instantiates one named prefab root only when the scene does not already contain it.</summary>
        private static bool AttachPrefabRootOnce(Scene scene, string prefabPath, string rootName)
        {
            if (scene.GetRootGameObjects().Any(root => root.name == rootName))
            {
                return false;
            }
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                throw new UnityException($"Missing STANDARD prefab at {prefabPath}.");
            }
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.name = rootName;
            return true;
        }

        /// <summary>Clones one exact scene visual into the matching CanvasTemplate display.</summary>
        private static bool CloneVisualObjectOnce(
            Scene sourceScene,
            Scene targetScene,
            string sourcePath,
            string targetParentPath,
            string targetName)
        {
            var targetParent = FindSceneTransform(targetScene, targetParentPath);
            if (targetParent.Find(targetName) != null)
            {
                return false;
            }
            var source = FindSceneTransform(sourceScene, sourcePath);
            var clone = Object.Instantiate(source.gameObject);
            clone.name = targetName;
            SceneManager.MoveGameObjectToScene(clone, targetScene);
            clone.transform.SetParent(targetParent, false);
            CopyLocalTransform(source, clone.transform);
            return true;
        }

        /// <summary>Finds one transform by a root-relative scene path and fails on hierarchy drift.</summary>
        private static Transform FindSceneTransform(Scene scene, string path)
        {
            var separatorIndex = path.IndexOf('/');
            var rootName = separatorIndex < 0 ? path : path[..separatorIndex];
            var childPath = separatorIndex < 0 ? string.Empty : path[(separatorIndex + 1)..];
            var root = scene.GetRootGameObjects().SingleOrDefault(candidate => candidate.name == rootName);
            if (root == null)
            {
                throw new UnityException($"Missing scene root {rootName} in {scene.path}.");
            }
            var transform = string.IsNullOrEmpty(childPath) ? root.transform : root.transform.Find(childPath);
            if (transform == null)
            {
                throw new UnityException($"Missing visual hierarchy {path} in {scene.path}.");
            }
            return transform;
        }

        /// <summary>Preserves the source object's exact local layout after moving it between equivalent displays.</summary>
        private static void CopyLocalTransform(Transform source, Transform target)
        {
            target.localPosition = source.localPosition;
            target.localRotation = source.localRotation;
            target.localScale = source.localScale;
            if (source is RectTransform sourceRect && target is RectTransform targetRect)
            {
                targetRect.anchorMin = sourceRect.anchorMin;
                targetRect.anchorMax = sourceRect.anchorMax;
                targetRect.anchoredPosition = sourceRect.anchoredPosition;
                targetRect.sizeDelta = sourceRect.sizeDelta;
                targetRect.pivot = sourceRect.pivot;
            }
        }

        /// <summary>
        /// Saves a generated scene and throws if Unity rejects the target path.
        /// </summary>
        /// <param name="scene">The scene to save.</param>
        /// <param name="path">The project-relative scene path.</param>
        private static void SaveScene(Scene scene, string path)
        {
            if (!EditorSceneManager.SaveScene(scene, path))
            {
                throw new UnityException($"Failed to save AIRSHIFT scene at {path}.");
            }
        }

        /// <summary>
        /// Inserts the AIRSHIFT scenes after the existing STANDARD result scene without reordering other entries.
        /// </summary>
        private static void RegisterBuildScenes()
        {
            var scenes = EditorBuildSettings.scenes
                .Where(scene =>
                    scene.path != ListScenePath &&
                    scene.path != GameScenePath &&
                    scene.path != ResultScenePath)
                .ToList();
            var insertionIndex = scenes.FindIndex(scene => scene.path == "Assets/Scenes/Result.unity") + 1;
            if (insertionIndex <= 0)
            {
                insertionIndex = scenes.Count;
            }
            scenes.InsertRange(insertionIndex, new List<EditorBuildSettingsScene>
            {
                new(ListScenePath, true),
                new(GameScenePath, true),
                new(ResultScenePath, true),
            });
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
