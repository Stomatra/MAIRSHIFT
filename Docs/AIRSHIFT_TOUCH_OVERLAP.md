# AIRSHIFT exact overlapping TOUCH pairs — integration draft

Stacked on PR #10. Does not modify public main or add private media.

## Chart syntax

Allowed in AIRSHIFT only, on one sensor in one beat:
- `LA1/RA1`
- `RB3/LB3`
- `LC/RC`

The original parser still receives `A1/A1`, `B3/B3` or `C/C`.
Each matching note retains its own L/R binding, sensor queue, and judgment.
No same-hand twins, mixed marked/unmarked duplicates, three-or-more duplicates,
firework/mine/break variants, or duplicate TOUCH HOLD.

## Independent hands

The Game loader identifies only plain, identical-sensor and identical-time
opposite-hand TOUCH pool entries with adjacent queue indexes. The NoteManager
permits either member to judge first, but blocks later notes in the same sensor
queue until both members have finished. Other notes and STANDARD mode retain FIFO.

The existing AIRSHIFT touch input buffer records (sensor, hand) events
independently. The TOUCH's existing group auto-completion is skipped for
required-hand notes.

## Appearance

Every paired TOUCH keeps six original sprites (four moving fans, point,
border) with the original skin, per-hand tints, and animated alpha. A dedicated
Resources material clips every child renderer against one *world-space* split
plane at the parent TOUCH center, instead of clipping every fan at its own
animated local center. Left remains blue; right remains red; a thin yellow
divider follows the center line through visible sprite pixels.

A judged/destroyed member exits the visual registry; the survivor restores
all original materials/property blocks and becomes a full-color TOUCH.
Nothing changes the imported skin sprite or shared original material.

## Validation before merging

Unity 6000.3.17f1 EditMode: AirshiftChartPlayTests + NoteLoaderTests.
Unity PlayMode: NoteManagerTests + TouchDropTests, plus the previously passed
TapDropTests. Verify A/B/C areas, hand-first order, a single hand then miss,
subsequent ordinary TOUCH, fade/animation, and STANDARD appearance.
Test actual Game with local `maidata.txt` (`LA1/RA1`, `LC/RC`) and
screenshot the rendered split. Player/Android rendering is not yet verified.
This feature is **unverified until these tests and a real-scene check pass**.
