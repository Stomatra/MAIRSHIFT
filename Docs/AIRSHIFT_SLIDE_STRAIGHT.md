# AIRSHIFT L/R straight SLIDE — staged Draft

Stacked on PR #11, not main. No private media is included.

## Syntax and source identity

L1-5[8:1] and R3-7[8:1] are the only newly accepted shapes.
AIRSHIFT strips the L/R hand prefix before MajSimai; STANDARD is untouched.
The parsed Slide note is bound to a RequiredHand by source start/ordinal
with exact RawContent. The original Game NoteLoader folds Slide objects,
so the required hand is matched to the folded note and rejected if a
duplicate folded slide would make its assignment ambiguous. The head
TapPoolingInfo and SlideBase path both receive the same hand.

## Behaviour

The existing AIRSHIFT sensor path judge checks ONLY the assigned hand
for tagged straight slides. An ordinary slide still accepts either hand.
The follower star and slide arrows are tinted blue (left) or red (right)
while retaining the original skin sprites and fade alpha.

**Next stage:** PR #13 expands L/R to curved, turning and sequential
chain slides. Handed WIFI, parallel branches, one-flick completion and
Android controller hardware remain separate.

## Unity validation

EditMode: AirshiftChartPlayTests and NoteLoaderTests.
PlayMode: SlideBaseTests, ExtendSlideDropTests, TapDropTests.
Then test a local Editor Game fumen with L1-5[8:1], R3-7[8:1],
and ordinary 1-5[8:1] on separate beats. Check wrong-hand rejection,
color and alpha. Check STANDARD with an ordinary chart. This Draft is
not tested in a Unity Editor yet.
