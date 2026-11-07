# AIRSHIFT v0.4 source publication checks

Use Unity 6000.3.17f1 with Android Build Support. The shared input regression tests
include `AirshiftCameraInputTests`, `AirshiftInputBridgeTests`,
`AirshiftInputPrototypeTests`, `AirshiftDiscContactTrackerTests` and
`AirshiftOuterDiscContactTests` in Assets/Tests/EditMode/Game/Scenes/Airshift.
Run them in EditMode through the normal Unity Test Framework.

For an Android-only managed compilation check, temporarily copy
CompileAndroidPlayerScripts.cs into Assets/Editor in an isolated checkout, then
run the editor in batch mode with:

```text
-buildTarget Android -executeMethod MajdataPlay.Tests.Publication.CompileAndroidPlayerScripts.Run
-airshiftScriptOutput <external-output-directory> -quit -logFile <external-log>
```

Create the output directory first. Move the temporary editor script and its .meta
out of Assets after Unity exits; do not commit that fixture copy. The tool source
in this directory is outside Assets and does not join production assemblies.
It calls Unity's real PlayerBuildInterface.CompilePlayerScripts API, so the
`UNITY_ANDROID && !UNITY_EDITOR` camera implementation is compiled. No APK,
native Player, camera emulation, third-party media or release upload is produced.

A successful managed compilation does not establish camera permission behavior,
frame rate, detection, GPU/audio compatibility or device judgement correctness.
Keep logs/results outside Temp, which Unity can clear between runs.
