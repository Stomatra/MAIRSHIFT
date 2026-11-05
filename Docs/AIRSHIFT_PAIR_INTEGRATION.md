# AIRSHIFT L/R STAR integration verification

This branch is an **integration-only draft**, stacked on PR #9 (which is stacked
on PR #5). It brings in the independently tested split visual resources from
PR #8. It does not merge any PR into public `main`.

## Intended end-to-end path

1. AIRSHIFT fumen `(120){4}L1$/R1$,E`.
2. `AirshiftHandedSimaiSyntax` removes the two prefixes and keeps separate
   Left/Right markers.
3. MajSimai keeps two standalone STAR objects at one timing/key.
4. `NoteLoader` transfers each hand marker to its own `TapPoolingInfo`,
   registering the matched pair in `NoteManager`.
5. `TapDrop.Init` registers both visible bodies and loads the split shader:
   blue on the left, red on the right, thin yellow middle.
6. Either hand may judge first. The later same-key note must wait until *both*
   pair members resolve. Each STAR keeps its own score and lifetime.
7. STANDARD charts, other STAR types and unmarked notes remain unchanged.

## Testing

Previously reported local results (before combining the branches):
- PR #8: TapDropTests PlayMode passed.
- PR #9: AirshiftChartPlayTests and NoteLoaderTests EditMode, plus
  NoteManagerTests PlayMode, passed after corrected test cases.

**These are not an integrated Unity test run.** On the new integration branch,
run these suites again in Unity 6000.3.17f1:
- EditMode: AirshiftChartPlayTests
- EditMode: NoteLoaderTests
- PlayMode: NoteManagerTests
- PlayMode: TapDropTests (includes a chart-to-split-renderer bridge test)

Finally run the actual Game scene in AIRSHIFT mode with an isolated test fumen.
Verify both entry orders (L first / R first), one-hand-only then miss,
a third later note at the same key, rotation, custom STAR skin, and yellow
divider visibility. Repeat a STANDARD chart without L/R tags.
Android controller/player remains untested until performed.

## Source and resource boundaries

Retain the existing Unity .meta GUIDs. Do not add private BASS, audio, fonts,
skins or assets to the public Git branch. Permission for media mentioned in a
QQ group should be separately documented in writing before redistribution.
