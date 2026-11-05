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
| `L1-5[8:1]` | Blue straight SLIDE, left hand required |
| `R3-7[8:1]` | Red straight SLIDE, right hand required |
| `L1p4[8:1]` | Blue curved SLIDE, left hand required |
| `R1V35[8:1]` | Red turning SLIDE, right hand required |
| `L1-4[8:1]q7[8:1]` | Blue chained SLIDE, left hand required |
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

## Skin compatibility and hand colors

AIRSHIFT **does not replace the user's note skin**. The shared Game scene uses
the normal `SkinManager.GetStarSkin()` and `GetTouchSkin()` selection, and
`TapDrop` / `TouchDrop` continue to render those same skin sprites.

Current implementation (from PR #3):
- An L/R STAR tints the main STAR SpriteRenderer blue/red.
- An L/R TOUCH tints its fan sprites, center point, and judgment border,
  while preserving each fan's animated alpha.
- Unmarked notes and STANDARD mode keep normal skin colors.
- Tint is per-note renderer state, reset on pooled-note initialization, and
  never modifies source PNGs, shared Sprite objects, or shared materials.

**Deliberate palette rule:** multiplying the imported skin's RGB toward
left-blue or right-red is the entire requirement. There is NO requirement to
make the result pure blue or pure red. A blue source may become dark blue when
left-tinted, or even greenish when right-tinted. That is expected. Preserve the
original skin artwork, texture, materials and transparency; do not rewrite it
to a fixed palette.

### AIRSHIFT multi-note (Each) presentation — planned visual follow-up

The AIRSHIFT mode should use one consistent rule for ALL STAR and TOUCH notes,
regardless of whether they have L/R hand prefixes:

- Ordinary single note: render the current custom skin normally, and apply
  L/R tint if required.
- Simultaneous multi-note (existing `IsEach` flag): render the regular
  STAR/TOUCH *body* variant rather than the imported skin's `Each` variant.
  In particular, use STAR `Normal` / `Double` as appropriate instead of
  `Each` / `EachDouble`; use TOUCH `Normal` and `Point_Normal` instead
  of `Each` and `Point_Each`.
- The simultaneous-note cue is a **separate yellow silhouette border** around
  the current STAR/TOUCH graphic, on top of whichever original skin and hand
  tint is in use. The border must not recolor the note's interior.
- Do not change the `IsEach` gameplay flag, note time, judgment, score, or
  simultaneous grouping. Only suppress its legacy body/guide/ex-effect visual
  hint on STAR and TOUCH. Keep special BREAK/MINE/EX behavior intact.
- STANDARD must retain all original Each sprites and appearance. Other note
  families (such as TAP or HOLD) are outside this STAR/TOUCH-only policy.
- Border follows rotation, scaling, pool reuse and TOUCH fan fade/position
  animation. It should be constructed from the current skin's sprite alpha
  so no dedicated red/blue/Each texture is required.

This is **a design requirement, not a feature already implemented** by PR #5;
the border renderer and the Each-sprite replacement need separate Unity
implementation and PlayMode validation.

Slide-head STARs use the same general STAR visual mechanism; **L/R-prefixed
SLIDE syntax is not yet supported**, so they cannot be hand-tagged by the
current chart preprocessor. Adding that syntax is a separate gameplay change.

## Current v1 restrictions

- Supported new tokens are `L/R + 1$`, `1$$` or basic TOUCH such as `LA1`,
  `RB3`, `LC`. Unprefixed notes retain ordinary Simai semantics.
- Straight slides (`L1-5[8:1]`) and curved / turning / sequential chained slides (`L1p4[8:1]`, `R1V35[8:1]`, `L1-4[8:1]q7[8:1]`) support hand assignment. Head STAR and guide preserve hand tint and only the assigned cursor advances the existing path judge. Hand-prefixed WIFI and parallel * branches are intentionally rejected; unmarked WIFI is unchanged. Directional one-flick, HOLD, TOUCH HOLD and MINE remain outside scope.
- Same-position/same-beat **opposite-hand standalone STAR pairs** are now
  permitted by the stacked overlap PR: `L1$/R1$`, `R1$/L1$`, and matching
  `L1$$/R1$$`. Only exactly two **identical STAR variants**, one L and
  one R, qualify. These become two independently judged notes at one key.
  The game's queue manager admits either hand first and holds later notes
  until both notes finish.
- Same-hand STAR twins, mixed `# AIRSHIFT L/R chart syntax (v1)

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
| `L1-5[8:1]` | Blue straight SLIDE, left hand required |
| `R3-7[8:1]` | Red straight SLIDE, right hand required |
| `L1p4[8:1]` | Blue curved SLIDE, left hand required |
| `R1V35[8:1]` | Red turning SLIDE, right hand required |
| `L1-4[8:1]q7[8:1]` | Blue chained SLIDE, left hand required |
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

## Skin compatibility and hand colors

AIRSHIFT **does not replace the user's note skin**. The shared Game scene uses
the normal `SkinManager.GetStarSkin()` and `GetTouchSkin()` selection, and
`TapDrop` / `TouchDrop` continue to render those same skin sprites.

Current implementation (from PR #3):
- An L/R STAR tints the main STAR SpriteRenderer blue/red.
- An L/R TOUCH tints its fan sprites, center point, and judgment border,
  while preserving each fan's animated alpha.
- Unmarked notes and STANDARD mode keep normal skin colors.
- Tint is per-note renderer state, reset on pooled-note initialization, and
  never modifies source PNGs, shared Sprite objects, or shared materials.

**Deliberate palette rule:** multiplying the imported skin's RGB toward
left-blue or right-red is the entire requirement. There is NO requirement to
make the result pure blue or pure red. A blue source may become dark blue when
left-tinted, or even greenish when right-tinted. That is expected. Preserve the
original skin artwork, texture, materials and transparency; do not rewrite it
to a fixed palette.

### AIRSHIFT multi-note (Each) presentation — planned visual follow-up

The AIRSHIFT mode should use one consistent rule for ALL STAR and TOUCH notes,
regardless of whether they have L/R hand prefixes:

- Ordinary single note: render the current custom skin normally, and apply
  L/R tint if required.
- Simultaneous multi-note (existing `IsEach` flag): render the regular
  STAR/TOUCH *body* variant rather than the imported skin's `Each` variant.
  In particular, use STAR `Normal` / `Double` as appropriate instead of
  `Each` / `EachDouble`; use TOUCH `Normal` and `Point_Normal` instead
  of `Each` and `Point_Each`.
- The simultaneous-note cue is a **separate yellow silhouette border** around
  the current STAR/TOUCH graphic, on top of whichever original skin and hand
  tint is in use. The border must not recolor the note's interior.
- Do not change the `IsEach` gameplay flag, note time, judgment, score, or
  simultaneous grouping. Only suppress its legacy body/guide/ex-effect visual
  hint on STAR and TOUCH. Keep special BREAK/MINE/EX behavior intact.
- STANDARD must retain all original Each sprites and appearance. Other note
  families (such as TAP or HOLD) are outside this STAR/TOUCH-only policy.
- Border follows rotation, scaling, pool reuse and TOUCH fan fade/position
  animation. It should be constructed from the current skin's sprite alpha
  so no dedicated red/blue/Each texture is required.

This is **a design requirement, not a feature already implemented** by PR #5;
the border renderer and the Each-sprite replacement need separate Unity
implementation and PlayMode validation.

Slide-head STARs use the same general STAR visual mechanism; **L/R-prefixed
SLIDE syntax is not yet supported**, so they cannot be hand-tagged by the
current chart preprocessor. Adding that syntax is a separate gameplay change.

## Current v1 restrictions

- Supported new tokens are `L/R + 1$`, `1$$` or basic TOUCH such as `LA1`,
  `RB3`, `LC`. Unprefixed notes retain ordinary Simai semantics.
- Straight slides (`L1-5[8:1]`) and curved / turning / sequential chained slides (`L1p4[8:1]`, `R1V35[8:1]`, `L1-4[8:1]q7[8:1]`) support hand assignment. Head STAR and guide preserve hand tint and only the assigned cursor advances the existing path judge. Hand-prefixed WIFI and parallel * branches are intentionally rejected; unmarked WIFI is unchanged. Directional one-flick, HOLD, TOUCH HOLD and MINE remain outside scope.
- Same-position/same-beat **opposite-hand standalone STAR pairs** are now
  permitted by the stacked overlap PR: `L1$/R1$`, `R1$/L1$`, and matching
  `L1$$/R1$$`. Only exactly two **identical STAR variants**, one L and
  one R, qualify. These become two independently judged notes at one key.
  The game's queue manager admits either hand first and holds later notes
  until both notes finish.
/`$` STAR twins, and three or more
  copies remain rejected. AIRSHIFT now also supports exactly two plain
  opposite-hand TOUCHes at one sensor and beat (`LA1/RA1`, `LC/RC`).
  Same-hand, unmarked, special TOUCH, or three-plus TOUCH duplicates are
  still rejected. Different positions in the same beat still work.
- With L/R tags, **Game Mirror must be Off** in v1. Rotation and random
  placement need explicit mapping verification before enabling mirror.
- Complex unmarked slide-head mixtures sharing the same position as handed
  stars may be rejected if source/parsed note counts differ.
- STANDARD does not parse L/R tokens: choose an AIRSHIFT-only chart when
  using prefixes; an ordinary Simai chart remains playable in both modes.

## Dependency and pairing verification

This overlap extension is based on PR #5, **not** the public `main`.
It must be reviewed and tested alongside that unmerged L/R parser.
PR #8 provides the independent two-color split shader but is not included
in this parser branch; testing syntax and judging does not prove that split
visuals render. Integrate only after both branches pass their targeted tests.

Additional tests for this extension:
- EditMode `AirshiftChartPlayTests`: duplicate parsing, identity mapping,
  and ambiguous input rejection.
- EditMode `NoteLoaderTests`: two distinct `TapPoolingInfo` records
  and per-key queue indexes from the existing Game loader.
- PlayMode `NoteManagerTests`: right-first, left-first, later-note blocking,
  reset and invalid pairing.
- Real Game scene: simultaneous left/right input at a single position,
  both orders; one-hand-only and miss cases; next notes must remain blocked
  until both STARs resolve. STANDARD sequential notes must remain unchanged.
- Android PlayMode and actual controller order still need confirmation.

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
