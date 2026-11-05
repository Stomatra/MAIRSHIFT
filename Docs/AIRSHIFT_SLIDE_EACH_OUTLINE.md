# AIRSHIFT SLIDE multi-press (EACH) yellow outlines — PR #18 Draft

This branch is stacked directly on the user's verified PR #17
(STAR/TOUCH yellow outlines), **not** PR #16 (audio) or main.

## Gameplay rule

The existing original `NoteLoader` already sets `IsEach` on
native, translated, connected and WIFI slide instances when the
corresponding Simai timing has simultaneous SLIDEs. No syntax or
judgment/score/timing logic changes are made here. In particular,
the user explicitly prefers the existing full-path sensor judgment;
one-flick direction completion is not part of this work.

## Visual rule

- AIRSHIFT EACH slide arrows use their normal custom skin sprite and
  any original left-blue/right-red tint. An independently drawn yellow
  1.09x enlarged alpha silhouette appears behind each source arrow.
- The follower STAR also uses the ordinary custom-skin sprite plus
  the same yellow outline. WIFI has three moving follower STARs.
- The existing leading STAR head is a TapDrop, already outlined by
  PR #17. Do not modify its yellow/red-blue split pipeline.
- Every yellow arrow outline copies its associated source's fade alpha;
  when an arrow is consumed by SlideBase.HideBar, its outline is
  disabled in the same method. Finishing the slide removes the
  outlines alongside the native visuals.
- Moving STAR outline alpha follows SingleSlideBase/WifiDrop's
  animation and follows the source transform without modifying
  pose, materials or note judgment.
- Non-EACH and STANDARD slides keep their original behavior and
  do not create new overlay GameObjects. Existing overlay instances
  are disabled if a renderer is reused without EACH.
- STANDARD `Slide_Each`/`Wifi_Each` and `Star_Each` skins are
  retained. Only AIRSHIFT EACH switches to normal body sprites.
- Break and mine visual behavior remains original. The actual
  border is the same approximate silhouette shader as PR #17, not a
  true uniform-pixel outline; test irregular skins and transparency.

## Files changed

- `SlideBase.cs`: pooled arrow and guide STAR silhouette arrays,
  synchronizing all enabled/fade states including hiding and reuse.
- `SingleSlideBase.cs`: suppress old EACH skin only in AIRSHIFT,
  update moving guide STAR opacity outline.
- `WifiDrop.cs`: same normal-skin substitution and triple-star alpha.
- `SlideBaseTests.cs`: arrow fade/progressive hiding, reusable
  enable state, guide STAR opacity, STANDARD/non-EACH behavior.
- `SlideDropTests.cs`: native skin/outline verification.
- `ExtendSlideDropTests.cs`: translated SLIDE Game path verification.
- `WifiDropTests.cs`: three-star WIFI and arrow outline verification.

This work **reuses** `AirshiftEachOutline.cs`,
`AirshiftEachSilhouette.shader` and its Resources material from PR #17.
No new shader or downloaded assets.

## Local Unity test gate

1. Confirm no compilation errors on Unity 6000.3.17f1.
2. PlayMode: SlideBaseTests, SlideDropTests,
   ExtendSlideDropTests, WifiDropTests. Note that the existing
   `SlideDropTests.Awake_RejectsUnknownSlideType` intentionally
   expects its MissingComponentException log.
3. PlayMode regression: TapDropTests, TouchDropTests,
   NoteManagerTests.
4. EditMode regression: AirshiftChartPlayTests, NoteLoaderTests.
5. Game in AIRSHIFT: simultaneous ordinary and hand-tagged straight
   slides; curved/turning slides; chains with disappearing bars;
   two independent opposite-hand parallel paths `L1-4*R1-6`;
   unmarked WIFI plus a second same-beat note. Inspect head STAR,
   follower STAR and arrow EACH contour; check for yellow remnants
   after a consumed arrow and after pooled reuse.
6. Game in STANDARD: original Each skins and the ordinary
   FULL-PATH slide judgment should remain unchanged.
7. For final acceptance inspect visually with custom skins and
   eventually two real simultaneous input devices.

This is a source-backed Draft. The assistant has NOT run Unity or
played the chart, so actual Game rendering remains to be confirmed.
Audio PR #16 remains explicitly deferred by the user.
