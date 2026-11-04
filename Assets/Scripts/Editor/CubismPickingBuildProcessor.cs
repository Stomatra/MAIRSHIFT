using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

#nullable enable
namespace MajdataPlay.Editor
{
    /// <summary>Removes optional Cubism picking geometry from player scene copies.</summary>
    [BuildCallbackVersion(1)]
    class CubismPickingBuildProcessor : IProcessSceneWithReport
    {
        /// <summary>Gets the installed SDK renderer type, or null when Cubism is not installed.</summary>
        internal static Type? CubismRendererType => Type.GetType(
            "Live2D.Cubism.Rendering.CubismRenderer, Live2D.Cubism", throwOnError: false);

        /// <summary>Runs picking cleanup before other scene processors.</summary>
        public int callbackOrder => 0;

        /// <summary>Processes scene copies only while Unity is building a player.</summary>
        /// <param name="scene">The scene copy being processed.</param>
        /// <param name="report">The current player build report.</param>
        public void OnProcessScene(Scene scene, BuildReport report)
        {
            // Unity also invokes this callback when entering Editor Play Mode.
            // Strip picking geometry only from the scene copies used by player builds,
            // including Development builds.
            if (!BuildPipeline.isBuildingPlayer)
            {
                return;
            }

            StripPickingGeometry(scene, CubismRendererType);
        }

        /// <summary>Removes picking mesh filters associated with the available renderer type.</summary>
        /// <param name="scene">The player scene copy to inspect.</param>
        /// <param name="rendererType">The installed renderer component type, or null when unavailable.</param>
        internal static void StripPickingGeometry(Scene scene, Type? rendererType)
        {
            if (rendererType is null)
            {
                return;
            }

            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var renderer in root.GetComponentsInChildren(rendererType, true))
                {
                    if (renderer.TryGetComponent<MeshFilter>(out var meshFilter))
                    {
                        // Cubism draws its own Mesh through a command buffer. This
                        // MeshFilter exists for Scene View picking and otherwise submits
                        // an empty transparent draw. Keep MeshRenderer: Cubism uses its
                        // enabled state, sorting settings and material property block.
                        UnityEngine.Object.DestroyImmediate(meshFilter);
                    }
                }
            }
        }
    }
}
