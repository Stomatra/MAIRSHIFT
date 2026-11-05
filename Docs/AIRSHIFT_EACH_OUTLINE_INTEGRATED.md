# AIRSHIFT yellow EACH silhouettes on current STAR/TOUCH pipeline (Draft)

Integration source: the earlier PR #6 silhouette implementation, now
adapted *incrementally* on top of the user's tested PR #15 code. No
audio PR #16 changes, no merges into main, no motion/input changes.

## Visual rules

- AIRSHIFT multi-press (Each) STAR and TOUCH retain their current
  ordinary custom-skin sprite and left-blue/right-red body tint.
- Yellow is a separate 1.09x enlarged alpha silhouette, behind the
  source sprite, without recoloring the body or modifying its skin
  texture/material. It is an approximate yellow edge silhouette,
  not a constant-width contour shader.
- The STAR outline follows its single body sprite (including
  double/slide-head STARs); TOUCH has six independent silhouette
  renderers: four moving fans, center point, and the just border.
- Each fan's animated fade alpha is copied to its silhouette. The
  outline is lazily created and reused across note pool cycles.
- Existing red-blue overlapping STAR half-split and TOUCH six-layer
  world-space split are retained on their *source* renderers.
  Yellow silhouette children do not participate in split registration
  or input/score processing. Overlapped Each still shows both halves.
- In AIRSHIFT, the original imported Each STAR/TOUCH sprite variants,
  shared TOUCH Each border variant and original STAR Each connector
  are suppressed; a yellow silhouette replaces their visual cue.
- STANDARD keeps all original Each skins, lines and handling.
  Non-EACH STAR/TOUCH do not create new silhouette layers.

## Files

- `Assets/Scripts/Scenes/Airshift/AirshiftEachOutline.cs`
- `Assets/Rendering/Shaders/Notes/AirshiftEachSilhouette.shader`
- `Assets/Resources/Airshift/EachSilhouette.mat`
- `TapDrop.cs`: STAR outline, normal body sprite and pooled enable.
- `TouchDrop.cs`: six-part outline, fade, normal body sprite,
  retain overlap registration.
- `TouchBorder.cs`: classic Each border only in STANDARD.
- `NoteLoader.cs`: suppress STAR Each line only in AIRSHIFT.
- `TapDropTests.cs` and `TouchDropTests.cs`: six new PlayMode tests.

## Unity validation

Run after checking for compile errors:
1. PlayMode `TapDropTests` and `TouchDropTests`: verify new
   silhouette tests and all existing split/skin/judgment regressions.
2. PlayMode `NoteManagerTests`, `SlideBaseTests`,
   `ExtendSlideDropTests`, `WifiDropTests`, `SlideDropTests`.
3. EditMode `AirshiftChartPlayTests`, `NoteLoaderTests`.
4. Actual Game in AIRSHIFT: simultaneous normal and L/R STARs
   and TOUCHes, same-position blue-left/red-right Each pairs,
   combined slide heads, custom skin shapes, fading, sequential
   reuse, and ordinary non-Each notes.
5. Switch to STANDARD and verify standard Each skins are unchanged.

For mixed-color overlapping notes, the original split shader must
stay active on the original six TOUCH layers and the STAR body.
Yellow outline is behind both and must remain a contour rather than
obscuring the blue/red interior. If the silhouette size or
transparency looks wrong with an imported skin, adjust it after
actual Game review; the integration is deliberately still Draft.

This feature has not been compiled or executed by the assistant in
Unity. Audio PR #16 remains separate and deferred by user request.
