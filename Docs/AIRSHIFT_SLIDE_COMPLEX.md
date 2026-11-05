# AIRSHIFT L/R complex SLIDE expansion — draft

Stacked on PR #12. Preserves the independently played STAR, TOUCH and
straight SLIDE code; not merged to main.

## Supported

AIRSHIFT prefixes for valid non-WIFI simai slide paths:
- `L1p4[8:1]` and `R1q4[8:1]` (curved paths)
- `L1V35[8:1]` and `R1v3[8:1]` (turning/bent paths)
- `L1pp6[8:1]` (wide curve)
- `L1-4[8:1]q7[8:1]` and `R1-3[8:1]-5[8:1]` (sequential chain)

L/R is stripped before MajSimai; markers are bound to the original parsed
simai object. The existing Game loader transfers this label across
NoteFolding, then to ExtendSlideDrop or native subslides and the leading
STAR. The SlideBase already filters per-hand sensor samples and tints
arrows/follower stars. No secondary judgment system added.

All shapes must be valid according to the upstream MajSimai parser and
SlideTable translator. The parser tests exercise the supported examples
above. A duration is required. Complex shape validation is delegated to
MajSimai/NoteLoader, so malformed notes that pass the lightweight
preprocessor must still fail the normal chart load.

## Exclusions

WIFI `1w5` is intrinsically a two-hand gesture (two outer lanes and
virtual midpoint). Assigning a single mandatory hand to all three lanes
would make an otherwise valid WIFI impossible to judge. Accordingly
`L1w5[8:1]` and `R3w7[8:1]` are **explicitly rejected** in this
version; ordinary untagged WIFI remains unchanged and functional.
Parallel `*` branch syntax is also excluded until we can map every
branch independently. No one-flick shortcut is introduced.

## Testing

EditMode:
- AirshiftChartPlayTests (preprocessor, parsed hand binding, invalid cases)
- NoteLoaderTests (folding identity for curves/chains)

PlayMode:
- SlideBaseTests (hand sensor filtering)
- ExtendSlideDropTests (translated path and guide)
- SlideDropTests, WifiDropTests (STANDARD/unprefixed regression)
- TapDropTests (head STAR regressions)

Then run real Editor Game with alternating curved, V-turn and chain
L/R charts and one unmarked WIFI chart. Check correct/incorrect hand,
appearance and judging after both curve and chain segments. Keep mirror,
random and autoplay off for the first pass. This branch has not been
compiled or executed in Unity, keep Draft until validated.
