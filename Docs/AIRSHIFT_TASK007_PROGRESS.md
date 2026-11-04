# maimai AIR · Task 007 进度

## 当前目标

Task 007 以直接复用 MajdataPlay STANDARD 的场景视觉、显示布局和设置系统为最高优先级。AIRSHIFT 不再发展一套外观近似但结构不同的正式 UI；只有输入、判定、成绩和场景路由确实不兼容时，才保留最小适配层。

本文件已经纳入以下补充要求：

- 选曲、打歌、结算尽可能复用 STANDARD 的原场景视觉资源、Prefab、层级、字体和动画。
- 主屏、Sub Monitor、相机、Canvas、遮挡和缩放统一复用 `CanvasTemplate.prefab` 及 `ScreenPos` 布局系统。
- AIRSHIFT 直接进入原版 `Setting.unity`，复用 `SettingManager`、`GameSetting`、菜单、动画和持久化。
- 设置退出时按来源返回 `List` 或 `AirshiftList`，校准往返与其他合法入口保持原行为。
- F6 仅控制 AIRSHIFT 调试覆盖层；覆盖层不得改变主屏和副屏布局。
- 不启动 STANDARD 专属输入、判定、成绩写入或游戏路由控制器。

## 已验证的仓库事实

- `Assets/Prefabs/CanvasTemplate.prefab` 已被 STANDARD 的 `List.unity`、`Game.unity`、`Result.unity` 和 `Setting.unity` 使用。
- `CanvasTemplate.prefab` 包含 `Main_Display`、`Sub_Display` 和 `ScreenPosCanvasLayoutApplier`；唯一的 `ScreenPosCanvasLayoutProvider` 仍由 Init 创建的持久全局 Canvas 提供，AIRSHIFT 不创建第二个 Provider。
- STANDARD 显示布局由以下现有实现共同负责，AIRSHIFT 不应另写窗口比例算法：
  - `Assets/Scripts/Misc/ScreenPos/ScreenPosCanvasLayout.cs`
  - `Assets/Scripts/Misc/ScreenPos/ScreenPosCanvasLayoutProvider.cs`
  - `Assets/Scripts/Misc/ScreenPos/ScreenPosCanvasLayoutApplier.cs`
  - `Assets/Scripts/Misc/ScreenPos/ScreenPosCameraMover.cs`
  - `Assets/Scripts/Misc/ScreenPos/RectCornerPositionUpdater.cs`
- `SettingManager` 的正常退出当前固定返回 `List`；`RunningMode.View` 单独返回 `View`。
- `SettingManager` 进入校准时切换到 `Calibrator`，`CalibratorManager` 退出时返回 `Setting`。来源上下文必须跨这次往返保留。
- 当前 AIRSHIFT List/Game/Result 正式画面仍以轻量 IMGUI 为主，尚未达到“完整复用 STANDARD 视觉”的验收目标。

## 实施状态

### 已完成：最小安全基础层

- [x] 新增设置返回上下文，只记录合法来源场景，不保存游戏会话或成绩数据。
- [x] AirshiftList 使用原版 `Setting.unity`，A7/F1 进入，退出后返回 AirshiftList。
- [x] STANDARD List 和 View 保留现有返回行为，并清除不适用的 AIRSHIFT 返回上下文。
- [x] 来源上下文跨 Setting → Calibrator → Setting 保留，在正常退出时一次性消费。
- [x] 为来源路由添加 EditMode 测试。
- [x] AIRSHIFT 三张场景各接入一份原版 `CanvasTemplate.prefab` 和一份 `Main Camera.prefab`；原 AIRSHIFT Manager 保持原对象，场景未重新生成。
- [x] AIRSHIFT 光标、输入尺度和绘制区域跟随 `Main_Display` 的实际 RectTransform，不再根据全窗口宽高推导圆盘。
- [x] Sub Monitor 跟随 `Sub_Display`，并直接显示当前 STANDARD Skin 的 `SubDisplay` Sprite。
- [x] F6 显示/隐藏调试覆盖层，默认正式画面不被调试明细替代。
- [x] AIRSHIFT 游戏原型直接借用当前 STANDARD Skin 的 TAP、HOLD、TOUCH、TOUCH HOLD、SLIDE 与 STAR Sprite；未启动 STANDARD NoteManager 或对象池。
- [x] AirshiftList 复用原版 `coverbigbg.png`，AirshiftResult 复用原版 `ResultBackground.png` 与 `ResultPanel.png`。第一轮曾把 `1080Circle.png` 作为 AirshiftGame 常驻圆盘；第二轮确认它是结算/全连类白色圆形覆盖资源，不是游戏背景，运行时代码已停止绘制该资源。
- [x] 增加场景依赖、单一 Canvas/Camera 和禁止 STANDARD Manager 混入的 EditMode 覆盖。

### 尚未完成：STANDARD 视觉适配

- [ ] 选曲：复用 List 的背景、封面、分类/难度显示和切换动画；以 AIRSHIFT 选择数据驱动，不启动 `ListManager` 的 STANDARD 路由。
- [ ] 游戏：复用 Game 的背景、圆形区域、音符/SLIDE/TOUCH 皮肤、HUD 和 Sub Monitor 资源；不启用 `GamePlayManager`、`NoteManager`、`ObjectCounter` 或 STANDARD 成绩写入。
- [ ] 结算：复用 Result 的背景、字体、动画和显示组件；由 `AirshiftScoreSummary` 适配数据，不启用 `ResultScreenManager` 的 STANDARD 保存流程。
- [ ] 对强耦合 MonoBehaviour 建立小型数据适配器；无法安全拆分的部分在最终报告中明确列为未完全复用。

当前没有把 `ListManager`、`GamePlayManager`、`NoteManager`、`ObjectCounter` 或 `ResultScreenManager` 放入 AIRSHIFT 场景。原版 List/Game/Result 的大量视觉对象与这些控制器直接耦合，因此本轮只复用了已确认安全的场景壳、显示布局、相机、Sub Monitor 皮肤和音符皮肤资源。选曲动画、正式 HUD、判定特效和结算动画仍需后续逐项适配，不能标记为完成。

## 第二轮：STANDARD 完整视觉还原

### 原版层级分类结论

#### Game

- 可直接复用的原场景对象：
  - `CanvasTemplate/Main_Display/Background`：包含原版 `BGManager`、背景遮罩材质、封面 Image、视频 RawImage 和 FFmpeg 播放组件。
  - `CanvasTemplate/Main_Display/CenterInfoDisplayer`：纯 TMP 数值和标题显示。
  - `CanvasTemplate/Main_Display/SecondaryInfoDisplayer`：纯 TMP 数值和标题显示。
  - `CanvasTemplate/Sub_Display/Static/ChartInfo`：`ChartInfoDisplayer` 及其曲名、艺术家、谱师、等级和封面控件。
- 展示数据驱动、可用适配器复用：`BGManager`、`ChartInfoDisplayer`、两个 TMP Info Displayer。
- 与 STANDARD 强耦合、禁止直接启动：`GamePlayManager`、`NoteManager`、`ObjectCounter`、`ScoreManager`、`NoteEffectManager`、`TimeDisplayer` 及各音符对象池。后两者分别直接依赖 STANDARD `InputManager` 或 `GameInfo` / `INoteController`。

#### List

- `CanvasTemplate`、`Main Camera`、背景 Sprite、字体及 UI Sprite 可独立复用。
- `Assets/Prefabs/UI/EmbeddedSongCoverDisplayer.prefab` 是纯 UI，可作为后续安全复用单元。
- `Assets/Prefabs/List/Main_Display_ribbon.prefab` 内含 `ListManager`、`CoverListManager`、`CenterCoverDisplayer`、预览音频和大量 STANDARD 状态机引用，且当前 Prefab 已有 Task 002 的用户修改，不能整体复制到 AirshiftList。
- 原版 `SongCoverDisplayer` 的封面异步任务会写入 `ListManager.AllBackgroundTasks`；原版 `CenterCoverDisplayer` 还依赖 STANDARD 成绩、收藏、在线信息、PreviewSoundPlayer 和难度状态，因此不能只复制外观后直接启用脚本。

#### Result

- `CanvasTemplate`、`Main Camera`、`EmbeddedSongCoverDisplayer`、背景/面板 Sprite、字体可安全复用。
- 正式成绩面板、动画和场景对象由 `ResultScreenManager` 集中绑定 STANDARD `GameInfo`、成绩保存及结算流程，当前不能整体启用；需要后续把纯视觉子树与数据控制器拆成最小适配层。

#### Setting

- AirshiftList 已直接进入原版 `Setting.unity`，复用 `SettingManager`、`GameSetting`、菜单 Prefab、动画和持久化。
- `SettingReturnContext` 只增加来源场景感知；STANDARD List、View 及 Setting → Calibrator → Setting 的既有分支保持原行为。

### 本轮已经完成的代码

- [x] 定位白色主屏原因：`1080Circle.png` 被错误当成常驻游戏圆盘；已从 `AirshiftGameManager` / `AirshiftChartView` 的正式绘制路径移除。
- [x] 后备背景改为原版 `Assets/Sprites/Default_Background.png`，不再使用固定色方形或错误的白色覆盖圆。
- [x] 新增幂等 Editor 迁移：`MajdataPlay/AIRSHIFT/Attach STANDARD Game Visual Hierarchy`。它只从原版 `Game.unity` 复制上面列出的四个安全视觉子树，不重新生成 AirshiftGame 场景。
- [x] 新增 `AirshiftStandardGameVisualAdapter`，使用 AIRSHIFT 会话、独立分数和 Combo 驱动原版背景、曲目信息及主屏 Info Displayer。
- [x] 支持原版 `BackgroundDim`、`BGInfo` 和 `SecondaryBGInfo` 中可准确映射的选项；不兼容的选项隐藏，避免显示伪造数据。
- [x] AirshiftChartView 检测到原版视觉层级后停止绘制旧的方形背景和重复 Sub Monitor HUD；F6 调试覆盖层及双光标仍留在同一主屏坐标系。
- [x] 增加所选原版视觉源不含 STANDARD 核心 Manager 的 EditMode 回归检查，并为 AIRSHIFT 实时 Combo 暴露只读适配数据。

### 场景迁移状态

- [ ] 当前主工作区的 `AirshiftGame.unity` 尚未执行上述一次性菜单。尝试使用正在运行的 Unity Editor 执行时检测到用户输入，自动化已按安全规则停止；没有改写场景 YAML，也没有启动第二个 Editor 强行覆盖。
- [ ] 在 Unity 中执行 `MajdataPlay > AIRSHIFT > Attach STANDARD Game Visual Hierarchy` 后，应确认场景各出现一次 `StandardGameBackground`、`StandardGameCenterInfo`、`StandardGameSecondaryInfo`、`StandardGameChartInfo`，然后保存场景。
- [ ] 场景迁移完成前，代码会自动回退到 `Default_Background.png` 和旧的轻量 Sub Monitor HUD；这不是本轮最终视觉验收状态。

### 设置兼容矩阵

- 已直接生效：主/副屏位置、缩放和偏移（ScreenPos）；当前 STANDARD Skin 的 TAP/HOLD/TOUCH/TOUCH HOLD/SLIDE/STAR Sprite；背景亮度；BGInfo/SecondaryBGInfo 的 Combo、CP/P Combo、Achievement、DXScore 映射；语言字体由原版字体/Setting 管理；共享音量设置继续由原版音频管理器持有。
- 尚未完整接入：AIRSHIFT 音符运动仍使用独立原型时间与 IMGUI 绘制，尚未读取 STANDARD 音符流速；判定文字显示选项和原版判定特效/音效未接入；背景视频未由 AIRSHIFT 会话加载。
- 不允许假装生效：依赖 STANDARD `GameInfo`、音符对象池、`InputManager` 或成绩数据库的设置及显示项。

### 第二轮仍未完成

- [ ] Game：原版音符 Prefab 的运动动画、复杂 SLIDE 轨迹、判定文字、判定特效和音效仍未安全拆离 STANDARD 判定管线。
- [ ] List：尚未接入原版封面轮播、分类、难度动画和副屏信息；当前仍是 AIRSHIFT 独立数据驱动的轻量选择器。
- [ ] Result：尚未接入原版正式成绩面板和动画；当前仍使用原版 Sprite/字体的轻量结果界面。
- [ ] 三界面的同分辨率逐项截图对比尚未执行。不能把当前状态标记为“完整视觉一致”。

## 自动测试状态

- Unity 6000.3.17f1 BatchMode 脚本编译成功，无 Task 007 编译错误。
- `MajdataPlay.Tests.Game.Scenes.Airshift` 定向 EditMode：50/50 通过，0 失败、0 跳过。
- 第二轮运行时代码已由正在运行的 Unity Editor 完成脚本重载，Editor.log 未出现编译错误。测试文件修改后未抢占用户当前 Editor 触发刷新。
- 第二轮定向测试尝试使用 `Tools/Tests/Run-UnityTests.ps1` 的隔离副本运行，但脚本无条件注入 `com.unity.testtools.codecoverage`，UPM 访问 `packages.unity.com` 时 `ECONNRESET`；恢复原 manifest 后，隔离 UPM 仍停在初始化，已终止本次隔离进程。因此新增测试尚未执行，不能沿用 50/50 结果声称通过。
- 未运行 PlayMode、窗口比例视觉对比或 Android 设备测试。

## 安全边界

- 保留全部未提交修改，不执行 `git reset`、`git clean` 或 `git restore`。
- 不创建提交，不使用 `dotnet build` 编译 Unity 工程。
- 不重新实现已通过验收的 AIRSHIFT 输入、轨迹、动作识别和独立判定。
- 不修改 STANDARD 的核心判定与成绩保存。
- 场景变更只能通过 Unity Editor 安全生成/迁移流程完成，不盲写 `.unity` YAML。

## 验收清单

- [ ] 同一显示设置下，STANDARD 与 AIRSHIFT 的主屏/Sub Monitor 位置和比例一致。
- [ ] AIRSHIFT 选曲、游戏、结算使用原版视觉资源，不是近似重绘。
- [ ] AIRSHIFT 的 HUD 与结算数据来自 AIRSHIFT 会话和成绩。
- [ ] AirshiftList 进入原版设置界面，退出后返回 AirshiftList。
- [ ] STANDARD List、View、校准、保存、音量更新及销毁流程保持原行为。
- [ ] 音量、皮肤和适用显示设置能被 AIRSHIFT 读取并响应。
- [ ] F6 只切换调试覆盖层，不改变输入区域或显示布局。
- [ ] 纵向目标布局、宽屏、窗口缩放和 Android 上的行为与 STANDARD 一致。
- [ ] STANDARD 游玩与成绩存储不受影响。

## 当前已知风险

- STANDARD 的 List/Game/Result 视觉组件中有不少脚本直接依赖各自 Manager、输入单例或 STANDARD 数据模型，不能简单复制整个场景后只替换一个控制器。
- 原版音符和轨迹资源的加载路径可能依赖 `NoteManager` / 对象池；需要逐项验证哪些是纯视觉资源，避免误启动 STANDARD 判定。
- `CanvasTemplate` 含布局 Applier，唯一布局 Provider 仍来自 Init 创建的持久全局 Canvas；AIRSHIFT 每张场景只能实例化一份模板，避免重复 Canvas/Applier。
- EditMode 可以覆盖路由与纯数据适配，但窗口比例、动画、相机和 Android 布局必须由 Unity PlayMode/设备人工验收。
