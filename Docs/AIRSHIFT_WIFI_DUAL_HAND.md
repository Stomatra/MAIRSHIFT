# AIRSHIFT WIFI dual-hand policy — PR #14 draft

WIFI remains `1w5[8:1]` (no L/R prefix). It is a three-lane,
two-hand gesture. The existing full-path judgment, virtual midpoint,
and UI remain unchanged. AIRSHIFT now dynamically assigns the two outer
lanes to separate physical hands. Shared initial key does not claim.
First unambiguous non-start sensor entry claims that lane; the second
lane requires the other hand. Either physical orientation is valid.
An already claimed lane cannot be advanced by the other hand, and
ambiguous same-sensor entries do not claim.

Only AIRSHIFT WIFI gets this policy: STANDARD and all ordinary single
SLIDEs remain unchanged. Tagged WIFI `L1w5` / `R3w7` and parallel
asterisk syntax remain unsupported, per PR #13.

The user confirmed full-trajectory SLIDE input feels excellent; do not
implement the formerly proposed one-flick simplification.

Validation pending: Unity EditMode AirshiftWifiLaneHandPolicyTests,
AirshiftChartPlayTests and NoteLoaderTests; PlayMode WifiDropTests,
SlideBaseTests, SlideDropTests, ExtendSlideDropTests and TapDropTests.
Test real scene WIFI at multiple start keys. Reject one-hand tracing
both outer branches and accept either swapped physical hand orientation.
Editor Tab-switched mouse cannot simulate fully simultaneous hands;
true dual-controller WIFI remains a hardware validation gate.
