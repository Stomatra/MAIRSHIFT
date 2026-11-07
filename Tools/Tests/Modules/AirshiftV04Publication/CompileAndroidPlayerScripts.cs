using System;
using UnityEditor;
using UnityEditor.Build.Player;
using UnityEngine;

#nullable enable
namespace MajdataPlay.Tests.Publication
{
    /// <summary>Compiles the real Android Player branch without packaging any runtime media.</summary>
    public static class CompileAndroidPlayerScripts
    {
        /// <summary>Compiles Android scripts into the explicit external audit output directory.</summary>
        /// <exception cref="ArgumentException">The audit output argument is missing.</exception>
        /// <exception cref="InvalidOperationException">Unity returns no compiled assemblies.</exception>
        public static void Run()
        {
            var arguments = Environment.GetCommandLineArgs();
            var index = Array.IndexOf(arguments, "-airshiftScriptOutput");
            if (index < 0 || index + 1 >= arguments.Length)
            {
                throw new ArgumentException("Provide -airshiftScriptOutput with an external output directory.");
            }

            var settings = new ScriptCompilationSettings
            {
                target = BuildTarget.Android,
                group = BuildTargetGroup.Android,
                options = ScriptCompilationOptions.DevelopmentBuild
            };
            var result = PlayerBuildInterface.CompilePlayerScripts(settings, arguments[index + 1]);
            if (result.assemblies == null || result.assemblies.Count == 0)
            {
                throw new InvalidOperationException("Android script compilation returned no assemblies.");
            }
            Debug.Log("AIRSHIFT_ANDROID_SCRIPT_COMPILE_PASS: " + result.assemblies.Count + " assemblies");
        }
    }
}
