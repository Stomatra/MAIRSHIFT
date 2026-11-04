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
   semantic note kind + sensor position + chronological occurrence order, verifying that
   the parsed candidate count matches the simple source-note count.
5. `AirshiftChartJudge` checks hand identity when consuming TAP/TOUCH actions.
6. `AirshiftChartView` colors those notes using the shared left-blue/right-red palette.

**Note:** MajSimai/MajRadar can normalize raw tokens (e.g. a standalone star
need not retain a literal `# AIRSHIFT v1 hand-prefixed Simai notes

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
   semantic note kind + sensor position + chronological occurrence order, verifying that
   the parsed candidate count matches the simple source-note count.
5. `AirshiftChartJudge` checks hand identity when consuming TAP/TOUCH actions.
6. `AirshiftChartView` colors those notes using the shared left-blue/right-red palette.

 in its `RawToken`). We deliberately do not
match on `RawToken`. The initial mapper uses the note family, physical area,
and order in the parsed chart. If the number of source notes and corresponding
adapter events differs, the chart fails explicitly instead of assigning hands
to the wrong note. In particular, complex slide-head mixtures at the same
position may not be usable with handed notes in v1.

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
