// Optional AIRSHIFT Android diagnostic APK build command.
// Menu: AIRSHIFT > Build Android Camera Test APK
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class AirshiftAndroidCameraBuilder
{
    private const string CameraScene = "Assets/Scenes/AirshiftGame.unity";

    [MenuItem("AIRSHIFT/Build Android Camera Test APK")]
    public static void Build()
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
        {
            EditorUtility.DisplayDialog("Android build target required",
                "Switch to Android in File > Build Profiles first, then run this menu again.", "OK");
            return;
        }
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        if (!File.Exists(Path.Combine(projectRoot, CameraScene)))
        {
            Debug.LogError("[AIRSHIFT ANDROID BUILD] Scene not found: " + CameraScene);
            EditorUtility.DisplayDialog("AIRSHIFT scene missing", CameraScene, "OK");
            return;
        }

        var scenes = new List<string> { CameraScene };
        foreach (var scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled && scene.path != CameraScene && File.Exists(Path.Combine(projectRoot, scene.path)))
                scenes.Add(scene.path);
        }
        string outputDir = Path.Combine(projectRoot, "Builds", "Android");
        Directory.CreateDirectory(outputDir);
        string output = Path.Combine(outputDir, "AIRSHIFT-Android-Camera-Debug.apk");
        bool oldBundle = EditorUserBuildSettings.buildAppBundle;
        string oldIdentifier = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);
        string oldProductName = PlayerSettings.productName;
        try
        {
            EditorUserBuildSettings.buildAppBundle = false;
            // Separate debug package: do not replace an installed STANDARD/MajdataPlay app.
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.stomatra.airshiftcameratest");
            PlayerSettings.productName = "AIRSHIFT Camera Test";
            Debug.Log("[AIRSHIFT ANDROID BUILD] Building scenes: " + string.Join(", ", scenes));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes.ToArray(),
                target = BuildTarget.Android,
                locationPathName = output,
                options = BuildOptions.Development | BuildOptions.AllowDebugging
            });
            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log("[AIRSHIFT ANDROID BUILD] SUCCESS: " + output);
                EditorUtility.RevealInFinder(output);
            }
            else
            {
                Debug.LogError("[AIRSHIFT ANDROID BUILD] FAILED: " + report.summary.result);
            }
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            throw;
        }
        finally
        {
            EditorUserBuildSettings.buildAppBundle = oldBundle;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, oldIdentifier);
            PlayerSettings.productName = oldProductName;
        }
    }

    // Full MAIRSHIFT boot flow. Unlike the camera diagnostic target, Init must be scene 0.
    [MenuItem("AIRSHIFT/Build Android Full Game APK")]
    public static void BuildFullGame()
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
        {
            EditorUtility.DisplayDialog("Android build target required",
                "Switch to Android in File > Build Profiles first.", "OK");
            return;
        }

        const string initScene = "Assets/Scenes/Init.unity";
        const string titleScene = "Assets/Scenes/Title.unity";
        const string listScene = "Assets/Scenes/List.unity";
        const string standardGameScene = "Assets/Scenes/Game.unity";
        const string airshiftGameScene = "Assets/Scenes/AirshiftGame.unity";
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        var scenes = new List<string>();
        foreach (var scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled && File.Exists(Path.Combine(projectRoot, scene.path)) &&
                !scenes.Contains(scene.path))
            {
                scenes.Add(scene.path);
            }
        }

        string[] requiredScenes = { initScene, titleScene, listScene, standardGameScene, airshiftGameScene };
        foreach (var requiredScene in requiredScenes)
        {
            if (!scenes.Contains(requiredScene))
            {
                Debug.LogError("[AIRSHIFT FULL BUILD] Required enabled scene missing: " + requiredScene);
                EditorUtility.DisplayDialog("Scene missing from Build Profiles",
                    "Enable this scene in Build Profiles > Scene List:\n" + requiredScene, "OK");
                return;
            }
        }

        scenes.Remove(initScene);
        scenes.Insert(0, initScene);
        string outputDir = Path.Combine(projectRoot, "Builds", "Android");
        Directory.CreateDirectory(outputDir);
        string output = Path.Combine(outputDir, "MAIRSHIFT-Android-Full-Debug.apk");
        bool oldBundle = EditorUserBuildSettings.buildAppBundle;
        string oldIdentifier = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);
        string oldProductName = PlayerSettings.productName;
        try
        {
            EditorUserBuildSettings.buildAppBundle = false;
            // Separate package for testing so the working camera-only APK remains installed.
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.stomatra.mairshiftfulltest");
            PlayerSettings.productName = "MAIRSHIFT Full Test";
            Debug.Log("[AIRSHIFT FULL BUILD] Boot scene: " + scenes[0]);
            Debug.Log("[AIRSHIFT FULL BUILD] Included scenes: " + string.Join(", ", scenes));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes.ToArray(),
                target = BuildTarget.Android,
                locationPathName = output,
                options = BuildOptions.Development | BuildOptions.AllowDebugging
            });
            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log("[AIRSHIFT FULL BUILD] SUCCESS: " + output);
                EditorUtility.RevealInFinder(output);
            }
            else
            {
                Debug.LogError("[AIRSHIFT FULL BUILD] FAILED: " + report.summary.result);
            }
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            throw;
        }
        finally
        {
            EditorUserBuildSettings.buildAppBundle = oldBundle;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, oldIdentifier);
            PlayerSettings.productName = oldProductName;
        }
    }

}
#endif
