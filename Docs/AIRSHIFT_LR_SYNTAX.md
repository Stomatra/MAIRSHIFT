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

The standalone STAR notation is `1$`, not `1*`.
The ordinary slide notation `1-5[8:1]` has a slide head, but
**`L1-5[8:1]` / `R1-5[8:1]` are not supported in v1**.
Hand-marked HOLD, TOUCH HOLD, MINE, and mixed star modifiers are
also outside the first implementation.

V1 supports separate notes and simultaneous notes on **distinct positions**.
To avoid ambiguous mapping, it rejects a hand-marked pair at the same
position in one beat, such as `L1$/R1$`. The adapter's event-ID-level
mapping for identical simultaneous notes is future work.

## Implementation flow

1. `AirshiftHandedSimaiSyntax.Preprocess` strips L/R from eligible note tokens.
2. The original MajSimai parser and MajRadar adapter process valid Simai data.
3. `AirshiftChartConverter` produces independent AIRSHIFT chart objects.
4. `AirshiftHandedSimaiSyntax.Apply` attaches required-hand metadata by
   semantic note kind + sensor position + chronological occurrence order.
   It rejects charts if the parsed candidate count differs from source.
5. `AirshiftChartJudge` checks hand identity when consuming TAP/TOUCH actions.
6. `AirshiftChartView` colors those notes using the shared left-blue/right-red palette.

**Note:** MajSimai/MajRadar can normalize raw tokens (e.g. a standalone star
need not retain a literal `$` in its `RawToken`). We deliberately do not
match on `RawToken`. If the parsed count differs from the source-note count,
loading fails explicitly rather than assigning hands to the wrong note.
Complex slide-head mixtures at the same position may not work in v1.

STANDARD mode is unchanged; use a separate AIRSHIFT difficulty/chart if
STANDARD play must remain compatible with ordinary Simai tools.
Neither the MajSimai submodule nor the STANDARD note loader and score
database has been changed by this PR.

## Manual verification

Use the 120 BPM test fumen in the example above, or the dedicated
`AIRSHIFT-LR-Check` local test package.
Check L/R STAR and TOUCH colors, wrong-hand rejection, ordinary note
acceptance, and two notes at different positions in the same beat.
Also check that STANDARD still plays an ordinary Simai chart.
