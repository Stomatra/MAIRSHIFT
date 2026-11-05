# AIRSHIFT phase-one integration audit (2026-11-05)

## Verified by the user's Unity test runner and actual Game playback

The live Game feature chain is:
- PR #5 `feature/airshift-lr-chart-prefix` — L/R STAR + TOUCH input
- PR #9 `feature/airshift-same-position-hands` — overlapping STAR queue
- PR #10 `feature/airshift-pair-end-to-end` — overlapping STAR shader integration
- PR #11 `feature/airshift-overlapping-touch-pair` — dual-hand TOUCH
- PR #12 `feature/airshift-lr-straight-slide` — straight SLIDE
- PR #13 `feature/airshift-lr-complex-slide` — curves, bends, chains
- PR #14 `feature/airshift-wifi-dual-hand` — WIFI dual-hand ownership
- PR #15 `feature/airshift-lr-parallel-slide-pairs` — two distinct L/R SLIDE branches

All stated edit/play regression suites were reported passing by the
user. STAR, TOUCH, simple/complex SLIDEs and the PR #15 test chart
have been run and passed in the actual Editor Game scene. The native
Simai '*' slide expansion count mismatch (S:A1: 14 vs 15) was fixed
and retested in PR #15. Keep the full sensor-path slide judge:
the user explicitly rejected a one-flick simplification.

The user has NOT yet reported simultaneous hardware dual-controller
WIFI or paired-SLIDE physical verification. Those remain open.

## Dependencies and missing side branches

All listed PRs remain Draft as of this review. PR #9–#15 form an
ancestry chain, while PR #8's STAR split implementation was integrated
as equivalent new files in PR #10 (avoid merging the old branch again).
PR #6 (yellow EACH outline) and PR #7 (Windows Unity local audio
handling, stacked on #6) are on a separate branch. GitHub compare
showed PR #15 diverging from #6/#7 after PR #5; they are not
automatically part of the tested #15 tree.

This branch reintroduces ONLY PR #7's three changed files:
- AudioManager.cs — absolute file URI construction for Unity backend
- UnityAudioSample.cs — URI-preserving requests, decode/error checks
- AudioPlayModeTests.cs — local WAV path containing spaces and '+' regression

Exact preimage blob SHAs on PR #15 matched PR #5; all three complete
files were copied from PR #7, so no AIRSHIFT gameplay files are
altered in this integration step.

## Deferred visual branch (#6)

PR #6's yellow EACH outline touches TapDrop, TouchDrop, TouchBorder
and NoteLoader; these classes changed substantially in the later
STAR/TOUCH overlap and Slide branches. Never overwrite the tested
PR #15 classes with old PR #6 whole-file copies. Adapt EACH outline
incrementally, with tests for:
- ordinary EACH STAR/TOUCH with custom skin
- coincident L/R STAR and TOUCH with yellow central separator
- non-EACH note appearance, alpha fades and pooled renderer reuse
- STANDARD unchanged

PR #8 is not a missing step: the STAR-split visual implementation
was carried into PR #10 and passed real Game visual testing.

## Open release gates

1. This PR #16 must pass EditMode AIRSHIFT regressions and PlayMode
   AudioPlayModeTests on Windows Unity backend. Check local audio
   paths with spaces/plus, missing file handling, and actual song playback.
2. Adapt PR #6 yellow EACH via a separate Draft branch and rerun
   all STAR/TOUCH visual + judge suites.
3. Run a full cross-feature Game test with mixed L/R STAR, TOUCH,
   straight/complex/parallel SLIDE, WIFI and STANDARD charts.
4. Test the two real ESP32/IMU input devices simultaneously for
   WIFI and parallel SLIDE, including left/right swapping.
5. Inspect Android build/test on target tablet after release
   functionality is frozen.
6. Only after all release gates consider merging to `main`.
   No changes to `main` are performed by this review.

## CI availability

No GitHub Actions workflow runs or combined commit-status checks
were returned for the PR #15 head during the audit. Unity tests
reported by the user are therefore the current primary evidence.

This audit is a source review, not a claimed Unity compilation.
