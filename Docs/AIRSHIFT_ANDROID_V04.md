# AIRSHIFT v0.4 Android source setup

Use Unity **6000.3.17f1** and Android Build Support (SDK, NDK and OpenJDK).
Clone with all six submodules and Git LFS as described in the root README.
The Android Java wrapper generation instructions remain under
`Assets/Plugins/MajdataPlay/Platform/Android/README.md` and
`Tools/AndroidJavaWrapper/README.md`. Use the actual installed Unity path.

## Camera input and gameplay

On Android Player builds, `AirshiftInputController` owns an on-device
`AirshiftAndroidCameraSource` and feeds the shared dual-cursor tracker used by
the existing TAP, HOLD, TOUCH, SLIDE and WIFI input paths. The List flow can enter
the shared Game scene with AIRSHIFT enabled. STANDARD retains its separate mode
gate and AIRSHIFT results are not treated as STANDARD ranked scores.

The camera requests Android camera permission. Use the on-screen diagnostics to
check detection, camera selection/mirroring and tracking. Blue is the left hand,
red the right hand. Image processing stays on the device; the Android source
does not need UDP or a browser. Camera loss/reacquisition suppresses synthetic
motion and stale hand contacts. Performance depends on the device and lighting.

The v0.4 cursor radius is `0.125` of the normalized playfield width, 2.5 times
the v0.3 value `0.05`. Camera mapping uses `PhysicalReachScale = 0.85`; it changes
camera movement mapping rather than shrinking note/sensor geometry.

Editor mouse simulation remains available. The diagnostic AirshiftGame scene
also supports the opt-in loopback UDP coordinate receiver on 127.0.0.1:19476;
the browser/sender is an external diagnostic tool and is not required by Android.

## Build entry points

1. Open the project in the pinned editor and switch the active target to Android.
2. Supply only authorized local runtime resources and required native dependencies.
3. Run **AIRSHIFT > Build Android Full Game APK** for the full Init boot flow.
4. The entry validates enabled Init, Title, List, Game and AirshiftGame scenes and
   puts Init first. It uses a separate development package identifier
   `com.stomatra.mairshiftfulltest`, includes debugging, and restores the temporary
   product name/identifier/bundle settings afterwards.
5. **AIRSHIFT > Build Android Camera Test APK** is the diagnostic entry, not the
   complete game boot flow. It uses `com.stomatra.airshiftcameratest`.

Outputs stay under ignored `Builds/Android`. Unity may put an ARM64 APK inside
the selected output directory. The full-game builder's selected path is
`Builds/Android/MAIRSHIFT-Android-Full-Debug.apk`.

The public repository intentionally omits unverified music, voices, skins,
models, BASS binaries and some fonts. Compilation is distinct from a complete
playable package: obtain properly licensed dependencies/resources locally or
provide authorized replacements before expecting the full runtime experience.
Do not upload a local APK just because the source code can be published.

## Validation boundaries

Editor tests can exercise shared tracker/contact/mode behavior. Editor-only
compilation does not compile `UNITY_ANDROID && !UNITY_EDITOR` code; Android
Player script compilation must also be checked. Neither check proves camera
permission flow, detection, latency, sustained performance, audio or judgement
on every device. Record the actual target and test results for each build.

The owner's existing v0.4 APK and device testing are separate evidence; this
source synchronization does not certify that package's public redistribution.
