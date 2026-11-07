# AIRSHIFT / MAIRSHIFT v0.4

基于 [MajdataPlay](https://github.com/TeamMajdata/MajdataPlay) 的 Unity 音游项目。
保留 STANDARD 模式，并提供面向双手挥动输入的 AIRSHIFT 模式。

## 当前可玩版本

v0.4 已接入 Android 平板摄像头，在设备上识别红、蓝目标并驱动双光标。
蓝色对应左手，红色对应右手。摄像头输入可进入正式谱面的共享 Game 场景，
沿用 AIRSHIFT 的 TAP、HOLD、TOUCH、SLIDE、WIFI 等判定路径和模式隔离。

- Android 完整游戏构建入口从 Init 启动，而非只启动摄像头诊断场景。
- 光标半径从 v0.3 的 0.05 调整为 0.125，即原来的 2.5 倍。
- 摄像头映射操作范围采用原来的 85%，不缩小游戏圆盘或传感器几何。
- 保留桌面鼠标模拟、双光标、指定手音符及既有 SLIDE/WIFI 机制。
- STANDARD 和 AIRSHIFT 由模式标志隔离，AIRSHIFT 不写入 STANDARD 正式成绩。

这是公开测试阶段的开发版本。现有 APK 的构建与试玩情况由项目所有者提供；
本仓库同步的重点是可复现的源码，不代表所有设备均通过兼容性和性能验证。
ESP32/IMU 无线双棒属于后续硬件方案，不能与已实现的摄像头光学识别混为一谈。

## 克隆与开发

使用 **Unity 6000.3.17f1**。这是 Unity 工程，不能用生成的 .sln/.csproj 代替 Unity 构建。

```sh
git lfs install
git clone --recurse-submodules https://github.com/Stomatra/MAIRSHIFT.git MAIRSHIFT
cd MAIRSHIFT
git lfs pull
git submodule update --init --recursive
git lfs fsck
```

安装 Android Build Support（SDK、NDK、OpenJDK），然后使用 Unity Hub 打开项目。
Android Java wrapper 的生成／重建步骤见
[平台说明](Assets/Plugins/MajdataPlay/Platform/Android/README.md) 和
[wrapper 工具说明](Tools/AndroidJavaWrapper/README.md)。

完整 Android 构建使用 **AIRSHIFT > Build Android Full Game APK**；
摄像头诊断构建使用 **AIRSHIFT > Build Android Camera Test APK**。
构建产物保存在忽略的 Builds/Android 目录，不加入源码提交。
详细设置、权限、启动场景、调参和验证边界见
[Android v0.4 构建说明](Docs/AIRSHIFT_ANDROID_V04.md)。

## 素材与可复现范围

**公开源码不包含本地完整素材包。** 当前音乐、曲目、背景视频、语音、部分皮肤、
Live2D 模型／SDK、BASS 原生库和部分字体仍需单独确认再分发权限。
“可以使用”不等同于“允许上传到公开 GitHub 仓库或嵌入公开 APK”。
不能把本项目的 GPL-3.0 错误地套用于这些第三方内容。

克隆者可以检查和编译公开代码；要获得完整游戏体验，必须自行提供合法授权的
资源与必要依赖。缺少素材可能导致字体／场景引用缺失、无声音或无法完成游玩。
Live2D 已可选化，缺少 SDK 不会让基础编辑器程序集固定依赖它。

- [资源授权及第三方政策](Docs/PUBLIC_RESOURCE_POLICY.md)
- [公开源码范围及检查记录](PUBLICATION.md)
- [原始许可证](LICENSE)

当前未把本地 APK 作为 GitHub Release 附件发布，因为其嵌入资源的公开再分发
证据尚不完整。请勿从“源码已公开”推断所有本地测试包也可公开分发。

## 参与与反馈

欢迎谱面设计、试玩反馈和开发协作。问题请提交到
[GitHub Issues](https://github.com/Stomatra/MAIRSHIFT/issues)，说明设备、版本和复现步骤，
分享日志前先移除个人信息和凭据。提交素材前须确认原作者、来源、公开再分发权和署名要求。

## 来源、许可与声明

本项目基于 TeamMajdata/MajdataPlay 及其原有贡献开发，保留原项目和第三方版权声明。
相应衍生代码按原有 GPL-3.0 条款发布，修改以 Git 提交记录为准。
字体、Skia、FFmpeg、NuGet 依赖及子模块按各自附带许可声明处理。
ManagedBass 开源包装层的许可不等于 BASS 原生库的许可。

AIRSHIFT / MAIRSHIFT 是独立爱好者项目，不代表 MajdataPlay 官方，亦不代表任何
商业街机游戏权利人。本项目不承诺所有硬件、谱面或输入条件下的完整兼容性。

## English

AIRSHIFT v0.4 is a Unity rhythm-game project derived from MajdataPlay. Android
on-device red/blue camera tracking feeds two cursors into the existing chart
play/judgement paths. Blue represents the left hand and red the right hand.
The full-game Android entry boots through Init; the diagnostic camera entry is
separate. STANDARD remains mode-gated from AIRSHIFT.

Use Unity 6000.3.17f1, Git LFS and all six pinned submodules. Install Android Build
Support and follow the linked Android v0.4 and Java-wrapper setup instructions.
The v0.4 normalized cursor radius is 0.125 (2.5 times v0.3), and camera movement
mapping uses a physical reach scale of 0.85. This is an experimental public-test
source version; device compatibility and actual camera performance still need
hardware verification. ESP32/IMU controllers are a separate future direction.

The source checkout omits media and proprietary resources without confirmed
public redistribution rights. Obtain properly licensed local resources and
native dependencies for a complete playable package. A successful compile is
not a complete game distribution. Existing local APKs are not uploaded as
Release assets while their embedded resources lack redistribution evidence.

Covered derivative code remains under GPL-3.0 with upstream credits and notices
retained. Songs, charts, images, fonts, models, voices and proprietary libraries
have separate rights; code licensing does not grant their redistribution.

## Upstream acknowledgements retained

MajdataPlay is based on [LeZi9916](https://github.com/LeZi9916)'s DJAuto branch of [MajdataView](https://github.com/LingFeng-bbben/MajdataView). Simai was developed by [Celeca](https://twitter.com/formiku39854).

## References

- [istareatscreens/MychIO](https://github.com/istareatscreens/MychIO)
  - Special thanks to istareatscreens for the early I/O solution for this project!
- [Cysharp/UniTask](https://github.com/Cysharp/UniTask)
- [Cysharp/ZString](https://github.com/Cysharp/ZString)
- [IntergatedCircuits/HidSharp](https://github.com/IntergatedCircuits/HidSharp)
- [ManagedBass/ManagedBass](https://github.com/ManagedBass/ManagedBass)
- [un4seen/bass](https://www.un4seen.com/)
- [mono/SkiaSharp](https://github.com/mono/SkiaSharp)
- [ammariqais/SkiaForUnity](https://github.com/ammariqais/SkiaForUnity)
- [videolan/vlc-unity](https://github.com/videolan/vlc-unity)
