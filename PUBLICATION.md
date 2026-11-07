# Public source publication — AIRSHIFT v0.4

This checkout contains MAIRSHIFT project source, Unity configuration, scenes,
tests, submodule references and selected dependencies. It is not a complete,
verified game distribution. Use **Unity 6000.3.17f1** as pinned in ProjectSettings.

The 2026-11-07 synchronization includes on-device Android red/blue camera input,
the full-game Android build entry, shared chart gameplay integration, and the
v0.4 cursor/reach tuning. See [Android setup](Docs/AIRSHIFT_ANDROID_V04.md) and
[resource rights policy](Docs/PUBLIC_RESOURCE_POLICY.md). No additional local
third-party media or local APK was approved for public redistribution.

## Included dependencies

- All nine FFmpeg production targets, their Unity bridges, build manifests,
  import metadata, license notices and applied source patches. Exact upstream
  source downloads and build instructions are listed beside the binaries in
  [FFmpeg source notices](Assets/Plugins/MajdataPlay/FFmpeg/Native/SOURCES.md).
- Existing networking and SkiaForUnity dependencies retain their notices.
- Gen Jyuu GothicX Bold/Medium, Jua, Source Han Sans Medium, Noto Sans Symbols
  and Noto Sans Symbols 2, with complete OFL texts and existing applicable TMP
  assets. Font licenses remain separate from the project GPL.
- Original Git submodule revisions and Git LFS rules are retained.

## Intentionally absent

- Bundled charts, music, videos, SFX, voices and skins in StreamingAssets.
  Placeholders preserve their directory metadata, but provide no playable media.
- Un4seen BASS native libraries and the Live2D SDK/Core. Their redistribution
  terms and compatibility with this distribution have not been established.
  ManagedBass's open source wrapper is a separate submodule; its presence does
  not grant permission for BASS binaries.
- Alimama FangYuanTi, Minimoon and rounded-x-mplus fonts and their TMP atlases,
  pending verification of the exact packages' redistribution terms.
- Optional art/editor originals, inherited presentation assets and Skia test
  content whose individual media/font provenance was not established.

No replacement GUIDs or substitute assets have been inserted. Existing scenes
and scripts can therefore retain references to these absent dependencies.
Expect missing assets and possibly compilation failures until authorized
dependencies are supplied locally or the code is adapted to omit them.
Do not interpret successful repository cloning or native import checks as a
successful full-project build.

## Reproduce setup

```sh
git lfs install
git clone --recurse-submodules https://github.com/Stomatra/MAIRSHIFT.git MAIRSHIFT
cd MAIRSHIFT
git lfs pull
git submodule update --init --recursive
git lfs fsck
```

Follow the README's Android Java binding generation instructions with the exact
Unity editor path and Android build support. The complete project must be built
by Unity, rather than by its generated .csproj or solution files.
Supply only locally authorized resources for the omitted items above.

## 2026-11-07 v0.4 source checks

In an isolated public-source checkout, Unity 6000.3.17f1 completed Editor import
with exit code 0. The relevant EditMode suite passed **41 tests, 0 failures,
0 skipped**, including camera hand/source identity, reacquisition/loss boundaries,
per-hand disc contacts and existing STANDARD/input isolation checks.

Unity's real Android Player script compilation passed with **67 assemblies**.
The compiled Player core contains AirshiftAndroidCameraSource; the Editor core
does not. Thus the Android-only branch was compiled, rather than treating Editor
compilation as Android coverage. The reusable check source is under
Tools/Tests/Modules/AirshiftV04Publication.

These checks used no local Live2D SDK, BASS binaries, music or voice pack. They do
not establish a complete playable APK, native/audio/GPU integration or real camera
behavior on a tablet. No APK was built or distributed by this synchronization.
The owner's existing local package remains separate from these source checks.

The current source keeps the existing LFS rules and six submodule revisions.
No new third-party media was introduced. Historical tracked media without
asset-specific provenance remains a separate rights-review item, not implicitly
approved by the source code license. Existing warnings and the historical results
below are not relabeled as successful fresh platform/device validation.

## Historical 2026-11-04 publication checks

The 2026-11-04 audit verified all nine FFmpeg target manifests, native hashes,
machine types, license notices and Android 16 KiB alignment. Unity 6000.3.17f1
also passed the isolated check of 24 Windows x86/x64/ARM64 native importers.
The artifact regression suite passed 50 tests. The build-script regression suite
passed 30 of 31 tests; one pre-existing assertion expects the former importer
YAML format, while the real Unity importer check accepts the current format.
An independent fresh clone retrieved the Git LFS object and all six pinned
submodules. Its native audit also loaded the Windows x64 bridge (ABI 4).
The standalone I/O harness passed 315 assertions. The standalone settings harness
could not compile because its source list omits existing SettingTranslation,
SettingVisibility and SettingValueSelection sources; this harness failure is
separate from Unity project compilation.

The initial November 4 import failed with a missing Live2D namespace. That
specific blocker was subsequently resolved by the optional integration under
Assets/Scripts/Editor; the current base editor assembly has no fixed SDK
reference. The earlier audit did not perform a complete Player build or runtime
test. The historical import also reported existing
nullable/compiler warnings and duplicate System.Runtime.CompilerServices.Unsafe
versions; these were not silently counted as successful validation.

Generated/local settings, credentials, databases and recovery files are excluded.
The release audit did not identify credential values in the selected text files;
pattern scanning is not a guarantee that every historical file is secret-free.
GitHub Actions is inherited and does not automatically build pushes to main.
No full multi-platform player verification is claimed by this publication.
