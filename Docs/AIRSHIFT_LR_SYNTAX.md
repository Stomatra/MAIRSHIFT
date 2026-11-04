# AIRSHIFT v1 hand-prefixed Simai notes

AIRSHIFT reuses the existing `maidata.txt` fumen format but adds an optional
**left/right hand prefix immediately before an eligible note token**.
These prefixes are recognized **only in the independent AIRSHIFT chart player**.
They are stripped before the fumen reaches the unmodified MajSimai parser and
then attached to the resulting AIRSHIFT runtime chart notes.

| Fumen | AIRSHIFT meaning |
| --- | --- |
| `L1$` | Left-hand-only (blue) standalone star TAP at A1 |
| `R5$` | Right-hand-only (red) standalone star TAP at A5 |
| `L1$$` | Left-hand-only rotating star |
| `LA1` | Left-hand-only (blue) TOUCH at A1 |
| `RB3` | Right-hand-only (red) TOUCH at B3 |
| `LC` | Left-hand-only TOUCH at the center |
| `1$`, `A1`, `C` | Ordinary Simai, either hand |

Example fumen:

```text
(120){4}
L1$,R5$,LA1,RB3,
1$,A1,LC,
E
```

The official Simai standalone star notation is `1$`, not `1*`.
The usual slide notation `1-5[8:1]` has a slide head, but
**`L1-5[8:1]` / `R1-5[8:1]` is not supported in v1**.
Hand-marked HOLD, TOUCH HOLD, MINE, and mixed star modifiers are also outside
the first implementation.

V1 supports separate notes and simultaneous notes on **distinct positions**.
To avoid incorrectly assigning hands, the first implementation rejects a
hand-marked pair of identical raw note tokens in the same beat, such as
`L1$/R1$`. Stable event-ID matching for that special case is future work.

## Implementation flow

1. `AirshiftHandedSimaiSyntax.Preprocess` strips L/R from eligible note tokens.
2. The original MajSimai parser and MajRadar adapter process valid Simai data.
3. `AirshiftChartConverter` produces independent AIRSHIFT chart objects.
4. `AirshiftHandedSimaiSyntax.Apply` attaches required-hand metadata by
   normalized raw token + occurrence order, or rejects an unmatched token.
5. `AirshiftChartJudge` checks hand identity when consuming TAP/TOUCH actions.
6. `AirshiftChartView` colors those notes using the shared left-blue/right-red palette.

**Note:** The parser/adapter may normalize raw tokens differently depending
on the pinned MajSimai/MajRadar versions. The integrated EditMode test must
pass on the merged project's pinned submodules before enabling these notes in
production; an unmatched tag fails chart loading rather than applying it to
the wrong note.

STANDARD mode is unchanged; use a separate AIRSHIFT difficulty/chart if
STANDARD play must remain compatible with ordinary Simai tools. The changes
do not alter the MajSimai submodule, STANDARD note loader, or score database.

## Test checklist

- EditMode: `MajdataPlay.Tests.Game.Scenes.Airshift.AirshiftChartPlayTests`
- EditMode: `MajdataPlay.Tests.EditMode.Game.Scenes.Airshift.AirshiftInputBridgeTests`
- Unity import + chart play: load fumen shown above in AIRSHIFT mode.
- Wrong hand cannot hit red/blue note; correct hand can.
- Ordinary star and TOUCH accept either hand.
- STANDARD mode and older charts remain unchanged.
- Inspect sprite coloration on a real Unity Game view and test reused notes.
