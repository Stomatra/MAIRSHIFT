# AIRSHIFT L/R chart syntax (v1)

AIRSHIFT is a play style within the **original Game scene**, not a separate
gameplay scene. The shared GamePlayManager and NoteLoader are responsible for
chart loading, visuals, judgement and results in both modes.

## Hand prefixes

When AIRSHIFT is enabled, the chart fumen accepts the following additions to
ordinary Simai notes:

| Fumen | Meaning |
| --- | --- |
| `L1$` | Blue standalone STAR at A1, left hand required |
| `R5$` | Red standalone STAR at A5, right hand required |
| `L1$$` | Blue rotating STAR (subject to parser compatibility) |
| `LA1` | Blue TOUCH at A1, left hand required |
| `RB3` | Red TOUCH at B3, right hand required |
| `LC` | Blue center TOUCH, left hand required |
| `1$`, `A1`, `C` | Original Simai notes, either hand |

Example Master chart:

```text
&title=AIRSHIFT L-R Test
&artist=Test
&des_5=MAIRSHIFT
&lv_5=1
&inote_5=(120){4}
L1$,R5$,LA1,RB3,
1$,A1,LC,
E
```

## Shared Game integration

1. Regular song selection enters the **same** `Game` scene with either play style.
2. `GamePlayManager.ParseChart()` checks the active mod:
   - STANDARD: pass the original fumen directly to MajSimai.
   - AIRSHIFT: strip supported L/R prefixes with
     `AirshiftHandedSimaiSyntax.Preprocess` before MajSimai parsing.
3. `BindGameNotes` resolves tags by TAP/TOUCH kind, sensor position, and
   chronological occurrence directly on the original parsed `SimaiNote` objects.
4. These note objects survive `NoteLoader.NoteFolding` for TAP/TOUCH.
   `NoteLoader` transfers `RequiredHand` to the existing
   `TapPoolingInfo`/`TouchPoolingInfo`.
5. Existing Game STAR and TOUCH drop implementations tint notes while the
   existing AIRSHIFT input bridge enforces matching hands.

The existing `AirshiftList`/`AirshiftGame` prototype files remain in the
repository but are **not used by the shared Game entry path**. This PR does not
modify the STANDARD game's scene, skin resources, or score storage.

## Current v1 restrictions

- Supported new tokens are `L/R + 1$`, `1$$` or basic TOUCH such as `LA1`,
  `RB3`, `LC`. Unprefixed notes retain ordinary Simai semantics.
- No handed SLIDE head (`L1-5[8:1]`), HOLD, TOUCH HOLD, or MINE yet.
- Hand-marked identical notes in the same beat such as `L1$/R1$` are
  rejected pending stable same-position event identity mapping. Different
  positions in the same beat are supported.
- With L/R tags, **Game Mirror must be Off** in v1. Rotation and random
  placement need explicit mapping verification before enabling mirror.
- Complex unmarked slide-head mixtures sharing the same position as handed
  stars may be rejected if source/parsed note counts differ.
- STANDARD does not parse L/R tokens: choose an AIRSHIFT-only chart when
  using prefixes; an ordinary Simai chart remains playable in both modes.

## Before merging

- Unity 6000.3.17f1 EditMode: run
  `MajdataPlay.Tests.Game.Scenes.Airshift.AirshiftChartPlayTests`.
- Run the existing AIRSHIFT input bridge and Game PlayMode note tests.
- Manual smoke test using `MAIRSHIFT-Verify` in `Game` with AIRSHIFT enabled:
  note colors, correct/wrong hand, unprefixed notes, and simultaneous
  left/right notes at distinct positions.
- Verify STANDARD launches the same Game scene with ordinary Simai.
- The sanitized public checkout omits BASS and some font/SFX assets;
  configure the built-in **Unity audio backend** for local testing.
  Never upload proprietary binaries or fonts just to make tests pass.

Note: Unity compile/runtime verification for this shared-Game rewrite has
**not yet been performed**; keep PR as Draft until these checks pass.
