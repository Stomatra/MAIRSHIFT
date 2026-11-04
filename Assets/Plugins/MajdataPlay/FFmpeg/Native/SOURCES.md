# Native FFmpeg source and redistribution notices

The installed FFmpeg builds are GPL-3.0-or-later, with GPL components enabled and
nonfree components disabled. Preserve each target directory's COPYING files,
dependency licenses, configure.txt, build-manifest.json, bridge-manifest.json and
patches. This notice does not relicense FFmpeg or third-party code.

## Obtain the corresponding source

The exact upstream revisions and release hashes are pinned in
[ffmpeg.lock.json](../../../../../Tools/FFmpeg/ffmpeg.lock.json) and
[dependencies.lock.json](../../../../../Tools/FFmpeg/dependencies.lock.json).
Use the following upstream source downloads at no charge; full source trees,
rather than just public binding headers, are required.

| Component | Pinned source download |
| --- | --- |
| FFmpeg | [source archive](https://github.com/FFmpeg/FFmpeg/archive/bf1b838f2ab88b4f8fd83443325c782ea0e0f7fa.tar.gz) |
| dav1d | [source archive](https://downloads.videolan.org/pub/videolan/dav1d/1.5.3/dav1d-1.5.3.tar.xz) |
| vulkanHeaders | [source archive](https://github.com/KhronosGroup/Vulkan-Headers/archive/19725e4d48082fe78e26622b15d3080ccd54112b.tar.gz) |
| nvCodecHeaders | [source archive](https://github.com/FFmpeg/nv-codec-headers/archive/7c8e7fc751f803c672b42c37d9199c8a4f98b327.tar.gz) |
| amfHeaders | [source archive](https://github.com/GPUOpen-LibrariesAndSDKs/AMF/archive/eadd00804d5f7e5cd8c85d540073198312870776.tar.gz) |
| x264 | [source archive](https://code.videolan.org/videolan/x264/-/archive/b35605ace3ddf7c1a5d67a2eb553f034aef41d55/x264-b35605ace3ddf7c1a5d67a2eb553f034aef41d55.tar.gz) |
| x265 | [source archive](https://github.com/Multicorewareinc/x265/archive/1d117bed4747758b51bd2c124d738527e30392cb.tar.gz) |
| libvpx | [source archive](https://chromium.googlesource.com/webm/libvpx/+archive/6df3ec34557879fff673706f4a1d9fbd0f3a6f0e.tar.gz) |
| libaom | [source archive](https://aomedia.googlesource.com/aom/+archive/92d4c37fbdd08944a0e721bbaeb13318f10aebb0.tar.gz) |
| opus | [source archive](https://downloads.xiph.org/releases/opus/opus-1.6.1.tar.gz) |
| lame | [source archive](https://downloads.sourceforge.net/project/lame/lame/3.100/lame-3.100.tar.gz) |

The Opus 1.6.1 source archive was downloaded using a trusted Windows TLS client
and its complete SHA256 matched the lock file:
`6ffcb593207be92584df15b32466ed64bbec99109f007c82205f0194572411a1`.
Use the official [Xiph download mirror](https://downloads.xiph.org/releases/opus/)
and verify the locked hash. Do not disable certificate verification.

Apply the two committed patches under
[Tools/FFmpeg/patches](../../../../../Tools/FFmpeg/patches):
`amf-rate-control.patch` and `x265-parameter-check.patch`. Their exact copies and
hashes accompany applicable targets. The complete project bridge source and build
instructions are under [Tools/FFmpeg/Native](../../../../../Tools/FFmpeg/Native)
and [Tools/FFmpeg](../../../../../Tools/FFmpeg/README.md). Per-target manifests
record configure arguments, dependency builds, and source fingerprints.

Compiler runtimes and system libraries have their own notices. Preserve the
LLVM/MinGW or GCC runtime exception notices; Linux bundled libva/libdrm notices
remain beside those libraries. Platform SDKs, GPU drivers and Unity are obtained
separately under their respective terms.

GPLv3 section 6(d) permits source hosting on a different server with equivalent
copying facilities and clear directions alongside object code. The distributor
remains responsible for maintaining source availability; pinned external links
must be monitored or mirrored if upstream availability changes. This audit checks
source locations and the project's static manifest validation; it does not certify
a fresh native rebuild on every platform.

References: [GNU GPLv3](https://www.gnu.org/licenses/gpl-3.0.html),
[FFmpeg licensing](https://ffmpeg.org/legal.html).
