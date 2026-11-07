# Public resource and third-party policy

This repository contains MAIRSHIFT / AIRSHIFT source derived from MajdataPlay.
The project code remains under the existing GPL-3.0 terms in [LICENSE](../LICENSE).
Original copyright notices, third-party licenses, submodule references and LFS
rules are retained. The GPL for code does not grant rights to unrelated artwork,
music, recordings, voices, models, fonts or proprietary native libraries.

## Evidence reviewed for the v0.4 source publication

The project owner reports a MajdataPlay QQ-group statement permitting use of
resources other than Live2D models and voices. That statement is recorded as
context, not as a public redistribution grant. This audit has no per-file written
grant establishing the speaker's rights, the upstream creators' rights, and the
right to publish copies on GitHub or include them in a downloadable APK.
Consequently it does not authorize uploading the local resource collection.

No new third-party media was approved during this synchronization. New camera
code, editor build code, configuration and scene changes are included as project
source. Resource hash manifests contain paths/checksums, not the media itself.

Historical artwork already present on public main is not treated as proof of
ownership or public redistribution rights. The local audit separately inventories
tracked media and its available notices; assets without per-file provenance remain
pending review. This synchronization introduces no new version of those media
files and does not rewrite the existing public Git history. It is not a blanket
copyright clearance of the repository's historical artwork.

## Included license evidence

| Component | Evidence retained in the repository |
| --- | --- |
| MajdataPlay-derived project code | Root LICENSE and existing upstream attribution |
| Gen Jyuu GothicX, Jua, Source Han Sans | Embedded declarations and full OFL texts under Assets/Fonts/General |
| Noto Sans Symbols / Symbols 2 | OFL.txt beside each included font package |
| Liberation Sans / TextMesh Pro fonts | Existing LiberationSans - OFL.txt and package notices |
| FFmpeg and native bridges | Per-target COPYING/license/manifests; Native/SOURCES.md links exact source, patches and build scripts |
| SkiaForUnity / SkiaSharp | ThirdParty/SkiaForUnity/LICENSE and THIRD-PARTY-NOTICES |
| NuGet dependencies | Existing package license and third-party notice files under Assets/Packages |
| Git submodules | Existing pinned revisions and upstream license files; ManagedBass wrappers do not license BASS native binaries |

Existing approved font/asset files were not replaced by locally regenerated
atlases just to normalize line endings. No existing license text was removed.

## Withheld from GitHub and Release attachments

| Local content | Publication decision |
| --- | --- |
| StreamingAssets/MaiCharts, MovieBG, SFX and Skins | Withhold actual charts/audio/video/artwork until asset-specific redistribution rights and provenance are established |
| StreamingAssets/Voice | Withhold unlicensed voices; the reported group statement expressly excludes them |
| Live2D models and SDK/Core | Models are excluded; SDK/Core distribution and compatibility with this public distribution remain unestablished |
| Un4seen.Bass native libraries | Withhold pending exact package redistribution terms and distribution compatibility; the open-source wrapper is separate |
| Alimama FangYuanTi, Minimoon, rounded-x-mplus and derived TMP assets | Exact local font-package redistribution evidence remains incomplete |
| ArtMaterials, inherited Doc assets, optional Skia test images/fonts | No asset-specific provenance/redistribution evidence sufficient for this publication |
| Recovery scenes, temporary source backups, personal settings, databases and logs | Local data, backups or potentially sensitive information |
| APK/AAB files | Build artifacts; current local packages may embed the withheld content above and are not uploaded as source or Release assets |

The absence of evidence is a publication hold, not a claim that every listed
resource is prohibited from all use. These files remain in the owner's complete
local project. Public checkout placeholders retain the relevant directory GUIDs.
The detailed local pending-file inventory is kept outside the source repository
in the publication audit, rather than uploading private files or chat records.

Before adding an item, establish the exact file/version, original creator and
source, an applicable public redistribution grant, required attribution and any
restrictions on modification or APK embedding. Keep its license and Unity .meta
with the asset. Obtain separate permission where the purported licensor cannot
grant rights for another contributor's work.

See [PUBLICATION.md](../PUBLICATION.md) for build and validation limits.
