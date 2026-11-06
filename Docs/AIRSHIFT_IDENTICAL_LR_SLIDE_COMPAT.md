# AIRSHIFT identical-path L/R SLIDE compatibility

This PR extracts **gameplay compatibility only** from the abandoned visual
experiment PR #20, based on the verified mainline through PR #19.

## Accepted chart and gameplay semantics

- `L1-4[8:1]*R1-4[8:1]` and its `R...*L...` order are valid.
- Both notes have the same start position, complete trajectory and timing,
  but represent two **independently judged and independently scored**
  notes, each bound to its original physical hand (L or R).
- A one-hand-only duplicate `L...*L...`, wrong start position,
  invalid WIFI hand tags and ambiguous three-way pairs remain rejected.
- Different trajectories sharing a start position continue to work.
- When MajSimai yields two identical raw SLIDE objects, AIRSHIFT's
  `NoteFolding` must preserve the two hand tags: it folds by
  (original note geometry, hand), yielding one L and one R object.
- In STANDARD the original folding and visual rules remain untouched.
  Unfolded AIRSHIFT notes also preserve their original hand identities.

## Intentionally **not** included

No new split shader, material, overlap registry, clipping, half-color track
or moving STAR rendering changes. The two existing L/R renderers may
visually overlap based on native ordering; that is the accepted behavior.
The current STAR/TOUCH/SLIDE EACH silhouettes and other hand-bound paths
are unchanged. The previous full-overlap rendering experiment PR #20
remains closed and unmerged.

## Files

- `AirshiftHandedSimaiSyntax.cs`: accept matching paths only when one is L,
  the other R.
- `NoteLoader.cs`: preserve original hand identity during AIRSHIFT-specific
  duplicate folding, including folding disabled.
- `AirshiftChartPlayTests.cs`: parser/binder tests for L/R and R/L ordering,
  retain same-hand invalid cases.
- `NoteLoaderTests.cs`: two independent folded objects, reversed ordering,
  STANDARD multiplicity unchanged, and unrelated path preservation.

## Unity acceptance gate

1. **EditMode** `AirshiftChartPlayTests` and `NoteLoaderTests`:
   no exceptions or unintended behavior changes.
2. **PlayMode** `SlideDropTests`, `ExtendSlideDropTests`,
   `SlideBaseTests`: hand binding and full-path judgment unaffected.
3. In AIRSHIFT Game, play a same-path pair such as
   `L1-4[8:1]*R1-4[8:1]`. Verify that it loads, that there are two
   independently judged SLIDEs, and either hand can complete only its
   respective note. No special blue/red splitter is expected.
4. A different-path pair `L1-4[8:1]*R1-6[8:1]` must still work.
   Try STANDARD for regression.
5. Keep `main` unchanged until Unity acceptance, then merge normally.

Audio issues, including `track_start_3.wav`, remain deferred.
