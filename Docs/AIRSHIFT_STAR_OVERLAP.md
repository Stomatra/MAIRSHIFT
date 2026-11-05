# AIRSHIFT coincident opposite-hand STAR split

This change is deliberately independent of the unpublished L/R chart parser PR (#5)
and yellow Each silhouette PR (#6).

## Behavior

- In AIRSHIFT only, *exactly two* STAR instances with the same controller,
  key position, judgement time, approach speed, star rotation speed, and
  selected skin sprite can pair when one requires Left and the other Right.
- Both original STAR note objects retain their own hit windows, queued input,
  judgement, score, sound, and lifetime. No note is hidden or merged.
- Each sprite draws only its local left/right half using its existing renderer
  tint (blue for Left, red for Right). A thin yellow stripe marks the middle.
  Because split clipping uses object-local X, it follows STAR rotation.
- When one note is judged/pooled/destroyed, the remaining note restores its
  original skin material and shows as a full single-color STAR.
- A third coincident note, wrong hand, mismatched timing, position, velocity,
  sprite, BREAK/EX/MINE, or STANDARD mode keeps the original appearance.
- The material is a Resources asset so Player shader stripping cannot remove
  it by assuming Shader.Find from an unused runtime-only string.

## Limits and next steps

- PR #5 presently rejects repeated same-beat/same-area hand-prefixed tokens.
  Chart-authored L1$/R1$ overlaps will require a separate parser update
  before the new rendering can be exercised through ordinary chart loading.
- Verify the renderer in Unity 6000.3.17f1 in the actual Game scene: custom
  skin alpha, sprite-mask clipping, sorting, two-hand judgement and rotating
  STAR trajectories, plus Android Player. Static source review alone is not a
  rendering or gameplay pass.
- No code or assets from existing PR #5/#6 were copied into this branch.
- Check how yellow Each-outline PR #6 composes with the split material before
  merging both PRs. They affect distinct material layers but touch TapDrop.

## Focused validation

In Unity Test Runner, run the PlayMode fixture TapDropTests, especially
the four AirshiftOverlappingStars / StandardOverlappingStars cases.
No automated Unity test run has been performed by this GitHub-only change.
