using MajdataPlay.Scenes.Airshift;
using NUnit.Framework;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MajdataPlay.Tests.Game.Scenes.Airshift
{
    /// <summary>Verifies that AIRSHIFT consumes the STANDARD display layout instead of deriving its own.</summary>
    public class AirshiftStandardLayoutTests
    {
        private const string CanvasTemplatePath = "Assets/Prefabs/CanvasTemplate.prefab";
        private const string MainCameraPath = "Assets/Prefabs/Main Camera.prefab";
        private const string StandardGameScenePath = "Assets/Scenes/Game.unity";

        [Test]
        public void ScreenCorners_AreConvertedToTopLeftGuiCoordinates()
        {
            var rect = AirshiftStandardLayoutBridge.FromScreenCorners(
                new Vector2(120f, 300f),
                new Vector2(660f, 840f),
                1080f);

            Assert.That(rect, Is.EqualTo(new Rect(120f, 240f, 540f, 540f)));
        }

        [TestCase("Assets/Scenes/AirshiftList.unity")]
        [TestCase("Assets/Scenes/AirshiftGame.unity")]
        [TestCase("Assets/Scenes/AirshiftResult.unity")]
        public void AirshiftScene_DependsOnStandardSceneShell(string scenePath)
        {
            var dependencies = AssetDatabase.GetDependencies(scenePath, true);

            Assert.That(dependencies.Contains(CanvasTemplatePath), Is.True);
            Assert.That(dependencies.Contains(MainCameraPath), Is.True);
        }

        [TestCase("Assets/Scenes/AirshiftList.unity")]
        [TestCase("Assets/Scenes/AirshiftGame.unity")]
        [TestCase("Assets/Scenes/AirshiftResult.unity")]
        public void AirshiftScene_HasOneSharedShellAndNoStandardGameplayManagers(string scenePath)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            try
            {
                var roots = scene.GetRootGameObjects();
                Assert.That(roots.Count(root => root.name == "CanvasTemplate"), Is.EqualTo(1));
                Assert.That(roots.Count(root => root.name == "Main Camera"), Is.EqualTo(1));

                var componentNames = roots
                    .SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true))
                    .Where(component => component != null)
                    .Select(component => component.GetType().Name)
                    .ToArray();
                Assert.That(componentNames, Does.Not.Contain("GamePlayManager"));
                Assert.That(componentNames, Does.Not.Contain("NoteManager"));
                Assert.That(componentNames, Does.Not.Contain("ObjectCounter"));
                Assert.That(componentNames, Does.Not.Contain("ResultScreenManager"));
                Assert.That(componentNames, Does.Not.Contain("ListManager"));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void StandardGameVisualSources_DoNotContainStandardGameplayManagers()
        {
            var scene = EditorSceneManager.OpenScene(StandardGameScenePath, OpenSceneMode.Additive);
            try
            {
                var sourcePaths = new[]
                {
                    "CanvasTemplate/Main_Display/Background",
                    "CanvasTemplate/Main_Display/CenterInfoDisplayer",
                    "CanvasTemplate/Main_Display/SecondaryInfoDisplayer",
                    "CanvasTemplate/Sub_Display/Static/ChartInfo",
                };
                var componentNames = sourcePaths
                    .Select(path => FindSceneTransform(scene, path))
                    .SelectMany(transform => transform.GetComponentsInChildren<MonoBehaviour>(true))
                    .Where(component => component != null)
                    .Select(component => component.GetType().Name)
                    .ToArray();

                Assert.That(componentNames, Does.Contain("BGManager"));
                Assert.That(componentNames, Does.Contain("ChartInfoDisplayer"));
                Assert.That(componentNames, Does.Not.Contain("GamePlayManager"));
                Assert.That(componentNames, Does.Not.Contain("NoteManager"));
                Assert.That(componentNames, Does.Not.Contain("ObjectCounter"));
                Assert.That(componentNames, Does.Not.Contain("ScoreManager"));
                Assert.That(componentNames, Does.Not.Contain("NoteEffectManager"));
                Assert.That(componentNames, Does.Not.Contain("TimeDisplayer"));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        /// <summary>Finds one exact transform path inside an opened scene.</summary>
        /// <param name="scene">The scene containing the expected hierarchy.</param>
        /// <param name="path">The slash-separated path starting with a root object name.</param>
        /// <returns>The transform at the requested path.</returns>
        private static Transform FindSceneTransform(Scene scene, string path)
        {
            var segments = path.Split('/');
            var current = scene.GetRootGameObjects()
                .Single(root => root.name == segments[0])
                .transform;
            for (var i = 1; i < segments.Length; i++)
            {
                current = current.Find(segments[i]);
                Assert.That(current, Is.Not.Null, $"Missing STANDARD visual source {path}.");
            }
            return current;
        }
    }
}
