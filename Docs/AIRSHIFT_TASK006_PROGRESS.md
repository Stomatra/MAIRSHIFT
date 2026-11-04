# AIRSHIFT Task 006 Progress

## Implemented stages

- Task 002-005 baseline is present and preserved.
- Stage A: implemented. `AirshiftList` is a Unity-generated manager-only scene with a local-only song/difficulty selector, cover preview, AIRSHIFT identity, retained session selection, STANDARD return, and Task 005 F7 entry.
- Stage B: implemented. A selected local `ISongDetail` loads its existing `maidata.txt`, parses with `SimaiParser`, adapts with `MajSimaiChartAdapter` plus the existing extended-slide provider, copies stable events into an AIRSHIFT model, and plays the existing audio track as the judgement clock.
- Stage C: implemented as a lightweight prototype renderer. TAP/star heads, HOLD, SLIDE checkpoints, TOUCH, TOUCH HOLD, BREAK, and EX are visually distinct and synchronized to audio time. It does not instantiate STANDARD note prefabs.
- Stage D: implemented as independent prototype judgement for TAP, HOLD, TOUCH, TOUCH HOLD, and ordered SLIDE checkpoints. Task 005 gestures are reused; inner touch regions use read-only raycasts against the original persistent sensor colliders. Parameters are AIRSHIFT prototypes, not official maimai values.
- Stage E: implemented. AIRSHIFT score/combo/timing counts, versioned JSON bests/play counts, and the independent result screen are connected without `ScoreManager` or the STANDARD result database.
- Stage F: targeted EditMode coverage is implemented and executed; Unity PlayMode/manual song validation remains.

## Verified

- Branch is maimai-air.
- Existing dirty assets, Android wrapper output, Task 002-005 scripts, scenes, prefabs, fonts, animations, and models remain present.
- MajSimaiChartAdapter already provides a stable event conversion layer for parsed SimaiChart, including slide heads, paths, timing, special flags, and stable event IDs.
- Unity 6000.3.17f1 compiled the final runtime and EditMode test assemblies successfully.
- Unity Editor serialization created `Assets/Scenes/AirshiftList.unity`; Build Settings now contain AirshiftList, AirshiftGame, and AirshiftResult in order.
- The targeted AIRSHIFT EditMode run completed with 39 passed, 0 failed, and 0 skipped tests. The temporary automatic test hook was removed and a final compile succeeded.
- The generated `AirshiftList` scene contains `AirshiftListManager`; the accepted AirshiftGame and AirshiftResult scenes were not regenerated.

## Open issues and deliberate prototype limits

- PlayMode and real-song manual verification have not been performed. Validate MAJTITLE plus charts containing HOLD, TOUCH/TOUCH HOLD, ordinary/multi-segment/WIFI/no-head/BREAK slides before calling the feature production-ready.
- Complex slide curves are judged as ordered physical A-area checkpoints and rendered as straight checkpoint segments. Shape-specific continuous curvature, WIFI branch completeness, and official direction/speed tolerances remain degraded.
- TOUCH detection uses the original Collider geometry, but the lightweight IMGUI note centers are schematic rather than a reused production skin layout.
- MINE notes are retained by conversion but explicitly reject chart startup with a clear unsupported message; they are never silently treated as normal notes.
- BREAK uses an explicit 5x prototype score weight and EX upgrades a successful hit to PERFECT. These are AIRSHIFT prototype rules, not claims of official equivalence.
- Inner TOUCH currently triggers on a sufficiently fast region entry. A separate rearmed in-region B/C/D/E strike recognizer is not yet implemented.
- The result JSON writer is versioned and backup-based, but crash-level atomic replacement across every storage backend still needs platform validation.

## Next entry point

1. In Unity PlayMode, enter AIRSHIFT from STANDARD List, select MAJTITLE and a difficulty, and verify audio/visible notes/timing/result/return.
2. Repeat with local charts covering HOLD, TOUCH, TOUCH HOLD, ordinary SLIDE, multi-segment, WIFI, no-head, BREAK, and EX.
3. Tune normalized gesture, hold-coverage, and slide-checkpoint thresholds from recorded manual results.
4. Replace schematic slide/touch rendering and add shape-specific continuous slide geometry only after the vertical slice is verified.

## Current uncommitted scope

- Pre-existing Task 002-005 and user asset changes shown by git status --short.
- Task 006 AIRSHIFT list scene/controller, chart model/player/view/judge, sensor tracker, independent score persistence/result, routing/build registration, tests, and this progress document.
- No STANDARD gameplay judgement, STANDARD score storage, accepted AirshiftGame/AirshiftResult scene YAML, or user asset changes were overwritten.
- No commit has been created.
