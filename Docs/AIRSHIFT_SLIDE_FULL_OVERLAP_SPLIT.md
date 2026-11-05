# AIRSHIFT PR #20 — completely overlapped L/R SLIDE split

## Why this exists

PR #10's blue-left / red-right STAR splitter registers `TapDrop`
bodies only. Two fully overlapping L/R **SLIDEs** also have independently
animated *follower* STARs plus an entire trail of arrows; previously
those drew over one another and whichever renderer sorted on top won.

The requirement is stricter than intersecting, sharing a start key,
or briefly coinciding: two same-time SLIDEs must trace the **identical
complete path**, including geometry and every arrow.

## Visual rule

- Exactly one L and one R SLIDE with matching session, start/end keys,
  note timing, slide start time, full duration, speed, moving STAR
  sprite and **all follower path positions/rotations** and arrow
  poses/sprites/scales are eligible.
- Render both existing STAR and arrow sprites through a complementary
  half-clipping shader. In the **direction of travel**, blue occupies
  the left half, red the right half, and a narrow yellow line divides
  them. For travel visually **downwards** this is explicitly
  **screen-left blue / screen-right red**.
- Both follower STARs retain their original movement, spin and fade.
  The separator follows the actual moving path tangent, recalculated
  each frame on curved or turning paths. Every stationary arrow gets
  the tangent of the nearest segment of the full path.
- The existing PR #17/#18 EACH yellow *outer* silhouette remains an
  independent child behind each source sprite. The new yellow
  *middle* divider is inside the complementary sprite shader.
- Keep two separate original notes and their untouched
  `SlideBase` sensor-path judging, per-hand filtering, scoring and
  independent completion. Ending one restores the other to full color.
- Three or more identical paths are ambiguous: none should be
  arbitrarily paired. Intersecting/crossing, different-path, timing,
  speed, length, mirror-geometry or single-handed notes do not pair.
- STANDARD, ordinary and untinted SLIDEs remain entirely unchanged.
  Break/mine/EX and no-head/no-track slides are excluded from the
  initial visual-only split, to preserve their existing specialized
  materials and feedback. WIFI's three-star visualization is unchanged.

## Implementation

- `AirshiftSlideOverlapRegistry`: grouping per gameplay session and
  note time, full-path comparison, renderer material capture / restore,
  and property blocks for transverse clip axes.
- `AirshiftSlideSplit.shader` / `SlideSplit.mat`: two complementary
  halves, with yellow center stripe, no baked skin assumptions.
  Shader uses a **local-space axis derived from world path direction**,
  rather than raw sprite X or the fixed screen X.
- `SingleSlideBase`: read-only geometry access for visual matching,
  tangent computation, dynamic star clip rotation, unregister on End.
- `SlideDrop` and `ExtendSlideDrop`: register after initialization;
  unregister before freeing objects on destruction.
- `SlideBase`: restore split material if the effect pipeline
  temporarily replaces an arrow's material.
- PlayMode regression tests: identical native L/R arrows+stars,
  wrong duration, different actual geometry, triple overlap,
  restoration after End, down/right/up travel direction; identical
  translated `ExtendSlideDrop` instance pair.
- EditMode: identical-path L/R Simai chart still produces two
  distinct slide objects and their separate hand bindings.

## Local Unity validation

Run Unity 6000.3.17f1 **EditMode**
`AirshiftChartPlayTests`, `NoteLoaderTests`, then **PlayMode**
`SlideDropTests`, `ExtendSlideDropTests`,
`SlideBaseTests`, `TapDropTests`, `TouchDropTests`,
`WifiDropTests`, `NoteManagerTests`.

Actual AIRSHIFT Game tests:

1. Completely overlapping L/R paths:
   `L1-4[8:1]*R1-4[8:1]` (new target)
2. Same origin but **different** paths:
   `L1-4[8:1]*R1-6[8:1]` (must not split)
3. Same origin and path with **different durations**:
   `L1-4[8:1]*R1-4[4:1]` (must not split)
4. Curving and chained identical pairs to check a rotating yellow
   middle divider; curve transitions should never split the wrong
   transverse direction.
5. Complete either hand's slide first; the other must immediately
   return to full-color sprites, with its own judgement unaffected.
6. Verify the EACH outer yellow outline and STAR/TOP touch visuals,
   plus STANDARD chart playback, remain unchanged.

Do not confuse the source's separate center divider with its EACH
outer silhouette. Use the Game view to verify thickness with custom
skin sprites; an approximate divider thickness is currently based on
2.5% of the sprite's narrower local bound.

PR #19 WIFI-any-hand change remains preserved in main. PR #16 audio
is still deferred. This PR is Draft until the user's Unity and actual
Game visual tests confirm behavior.
