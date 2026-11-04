# Public source publication

This checkout contains MAIRSHIFT project source, Unity configuration, scenes,
tests, submodule references and selected dependencies. It is not a complete,
verified game distribution. Use **Unity 6000.3.17f1** as pinned in ProjectSettings.

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

## Publication checks

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

A complete fresh import was attempted with Unity 6000.3.17f1. It exited with
compiler error CS0246 in `Assets/Scripts/Editor/CubismPickingBuildProcessor.cs:1`
because the intentionally withheld Live2D namespace is unavailable. No complete
Player build or runtime test was performed. Resolve this dependency under suitable
terms, or adapt the optional integration in a separate development change, before
claiming the public project builds successfully. The import also reported existing
nullable/compiler warnings and duplicate System.Runtime.CompilerServices.Unsafe
versions; these were not silently counted as successful validation.

Generated/local settings, credentials, databases and recovery files are excluded.
The release audit did not identify credential values in the selected text files;
pattern scanning is not a guarantee that every historical file is secret-free.
GitHub Actions is inherited and does not automatically build pushes to main.
No full multi-platform player verification is claimed by this publication.
