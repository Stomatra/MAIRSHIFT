# AIRSHIFT WIFI any-hand compatibility for concurrent WIFI — PR #19

## Why PR #14's two-hand ownership is removed

The PR #14 WIFI policy bound the two outer rails of *each* WIFI
slide to two distinct physical hands. One WIFI then monopolized both
controllers, making two concurrent WIFI slides impractical. High
density maimai charts may contain two WIFI notes simultaneously.
The earlier policy also rejected single-hand sweeping across the
three sensor areas, despite the original sensor-driven gameplay
permitting the same physical hand to hit both outer rails.

This branch is stacked on the user's fully gameplay-verified PR #18.
It does not include the deferred Windows audio PR #16.

## New rule

- An untagged WIFI (`1w5[8:1]`) keeps all three independently
  ordered original sensor-path judge queues.
- Either real hand can press the next required area on either
  outer rail. A different hand may take over the same rail; one
  hand may also visit both outer rails in sequence.
- Each WIFI note remains a distinct note instance. Two same-beat
  notes (`1w5[8:1]/5w1[8:1]`) never claim exclusive hands.
- Center-rail virtual midpoint behavior remains as before, as
  does physical contact by either hand. This is *not* a bypass
  of the complete path or timing windows.
- No L/R prefix for WIFI is introduced. Hand-specific straight,
  curved, chained and paired parallel SLIDEs remain unchanged.
- STANDARD is unchanged, as are WIFI visuals / EACH yellow
  outlines from PR #18. No scoring or audio changes.

This makes the sensor-contact filter stateless. `WifiDrop.Init`
no longer resets a now-nonexistent owner mapping.

## Regression tests

- EditMode `AirshiftWifiLaneHandPolicyTests`: both-hand acceptance,
  one-hand two-rail sweeping, mid-rail hand switching, two
  concurrent WIFI contact streams, empty regions and lane bounds.
- EditMode `AirshiftChartPlayTests`: two same-beat WIFI notes
  survive preprocessing and MajSimai parsing separately.
- PlayMode `WifiDropTests`: two same-timing WIFI instances
  maintain their own three-lane judge queues, plus existing
  WIFI animation and yellow silhouette tests.
- Run existing EditMode `NoteLoaderTests` and PlayMode
  `SlideBaseTests`, `SlideDropTests`,
  `ExtendSlideDropTests`, `TapDropTests`, `TouchDropTests`,
  `NoteManagerTests` for mode and shape regression.

## Manual Game acceptance

1. Single WIFI from A1: one hand may visit required left and
   right rails in sequence; two hands together should still work.
2. Two simultaneous WIFI notes on different start keys:
   `1w5[8:1]/5w1[8:1]`. Each hand traces a different WIFI.
   Verify **both independent scores/queues** progress as allowed
   by the full-path sensor contacts, including center segments.
3. Repeat using reversed physical hands, and an ordinary
   single SLIDE and an L/R constrained parallel SLIDE nearby.
4. Toggle AIRSHIFT to STANDARD: original note behavior remains.
5. Verify all WIFI yellow EACH outlines still follow arrow
   fade and disappear after completed segments.

The user cannot fully demonstrate two concurrent physical hands
with the Editor's one-mouse Tab simulation. Acceptance of real
dual-controller simultaneous interactions remains a later gate.

No Unity compiler or Game runtime execution was performed while
authoring this source change.
