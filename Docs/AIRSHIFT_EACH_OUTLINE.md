# AIRSHIFT STAR/TOUCH multi-press yellow outline — experimental

This change is intentionally **stacked on PR #5** (`feature/airshift-lr-chart-prefix`).
It does not change the shared `Game` entry, chart parsing, note timing, judgment,
scoring, original sprites, or the STANDARD play style.

## Visual rules

- STAR and TOUCH in AIRSHIFT use **the active custom skin's ordinary body**;
  the `Each` skin variants are not selected, even if the note is part of
  a simultaneous group.
- Preserve the existing per-renderer left-blue/right-red RGB multiplication.
  No guarantee of mathematically pure blue/red is needed.
- If (and only if) `IsEach` is true, create a yellow silhouette **behind**
  the ordinary STAR/TOUCH body. This works for every imported Sprite without
  requiring dedicated skin assets.
- STANDARD continues using `Star_Each`, `Touch_Each`,
  `TouchPoint_Each`, standard Each touch border sprites, and original each
  connector lines. Ordinary TAP/HOLD are not modified.
- AIRSHIFT keeps `IsEach` for timing/judgment/grouping, but uses standard
  STAR/TOUCH body sprites and suppresses the original STAR Each connector,
  TOUCH Each fan/point/border sprite choices.

## Implementation

- `AirshiftEachOutline.Sync` attaches a lazy, reusable child SpriteRenderer
  to the source. For STAR, the source is the existing `TapDrop` star renderer;
  for TOUCH, the sources are its four fan renderers plus center point.
- The child copies the *same* `Sprite`, inherits rotation and movement from
  the original transform, and renders a 9% enlarged **alpha silhouette** in
  yellow behind it, using the shared Resources material and one unlit shader.
  The shader ignores skin RGB but respects imported sprite alpha, and the
  TOUCH silhouette follows the fan's fade-alpha animation.
- This is an **approximate expanded silhouette**, not a mathematically
  constant-pixel-width contour. It may appear thicker around complex or
  asymmetric sprites, and translucent sprite interiors can show some yellow
  from behind. Inspect the look on several popular imported skins before
  treating it as final.
- No source PNGs are written, and no per-note dynamic Material allocations
  occur. An outline GameObject is created only for an AIRSHIFT Each note and
  reused for that pooled note's subsequent appearances.

## Validation (not yet run in Unity)

1. Confirm PR #5 passes the EditMode suite (56/56 passed on its baseline).
2. Fetch the stacked feature branch into a clean verification workspace.
3. Run PlayMode `MajdataPlay.Tests.Game.Notes.PlayMode.TapDropTests` and
   `MajdataPlay.Tests.Game.Notes.PlayMode.TouchDropTests`.
4. In the **original Game scene**, test simultaneous ordinary/blue/red STAR
   and TOUCH, ordinary solo notes, and STANDARD Each variants.
5. Check custom skins with irregular silhouettes, TOUCH moving fans/fade,
   STAR rotation, note pool reuse, and overlapping notes.
6. Check GPU/Material compatibility with Unity 6000.3.17f1 and
   Android. These tests have not yet been executed by the assistant.

The public repository intentionally excludes some proprietary music, fonts and
native audio resources. This feature adds only new source code/shader/material.

Do **not merge this Draft PR** until Unity compile, PlayMode and actual Game
rendering validation are complete.
