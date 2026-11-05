using MajdataPlay.Scenes.Airshift;
using MajdataPlay.Tests;
using MajRadar.Core;
using MajSimai;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#nullable enable
namespace MajdataPlay.Tests.Game.Scenes.Airshift
{
    /// <summary>Verifies the Task 006 chart, judgement, score, and persistence isolation.</summary>
    public class AirshiftChartPlayTests
    {

        /// <summary>
        /// Original untagged '*' branches expand into multiple parsed SLIDEs.
        /// Their contribution must be counted before binding later L/R
        /// markers on the same A-key, not just for an isolated chart.
        /// </summary>
        [Test]
        public void GameSyntax_UnmarkedForkBeforeHandedForkDoesNotShiftBindings()
        {
            const string source =
                "(120){4}L1-4[8:1]*R1-6[8:1]," +
                "1-4-7[4:1]*-6[8:1]," +
                "R1q4[8:1]*L1p4[8:1],E";
            var syntax = AirshiftHandedSimaiSyntax.Preprocess(source);
            Assert.That(syntax.SourceCounts["S:A1"], Is.EqualTo(6));

            var chart = SimaiParser.ParseChart(syntax.SimaiText.AsSpan());
            var slides = FlattenNotes(chart)
                .Where(n => n.Type == SimaiNoteType.Slide).ToArray();
            Assert.That(slides, Has.Length.EqualTo(6));
            var bindings = AirshiftHandedSimaiSyntax.BindGameNotes(chart, syntax);
            Assert.That(bindings.Count, Is.EqualTo(4));
            Assert.That(bindings.ContainsKey(slides[2]), Is.False);
            Assert.That(bindings.ContainsKey(slides[3]), Is.False);
            Assert.That(bindings[slides[4]], Is.EqualTo(AirshiftRequiredHand.Right));
            Assert.That(bindings[slides[5]], Is.EqualTo(AirshiftRequiredHand.Left));
        }

        /// <summary>
        /// Regression for the 86-second PR #15 Master test chart that
        /// previously failed on load: 14 source A1 slide tokens versus
        /// 15 parsed A1 slides because of an ordinary '*' fork at 44s.
        /// </summary>
        [Test]
        public void GameSyntax_ParallelSlideCheckMasterCountsExpandedFork()
        {
            const string source = "(120){4}" +
                ",,,,,,,,L1-4[1:1]*R1-6[1:1]" +
                ",,,,,,,,,,,,,,,,R1q4[1:1]*L1p4[1:1]" +
                ",,,,,,,,,,,,,,,,L3-7[1:1]*R3-5[1:1]" +
                ",,,,,,,,,,,,,,,,L1-4[1:1]*R1-6[2:1]" +
                ",,,,,,,,,,,,,,,,R1-6[1:1]*L1-4[2:1]" +
                ",,,,,,,,,,,,,,,,1-4-7[4:1]*-6[8:1]" +
                ",,,,,,,,,,,,,,,,L1-4[1:1]" +
                ",,,,,,,,,,,,,,,,R1-6[1:1]" +
                ",,,,,,,,,,,,,,,,1-5[1:1]" +
                ",,,,,,,,,,,,,,,,L1q4[1:1]*R1p4[1:1]" +
                ",,,,,,,,,,,,,,,E";

            var syntax = AirshiftHandedSimaiSyntax.Preprocess(source);
            Assert.That(syntax.SourceCounts["S:A1"], Is.EqualTo(15));
            Assert.That(syntax.SourceCounts["S:A3"], Is.EqualTo(2));
            Assert.That(syntax.Markers.Count, Is.EqualTo(14));
            var chart = SimaiParser.ParseChart(syntax.SimaiText.AsSpan());
            var a1 = FlattenNotes(chart).Where(n =>
                n.Type == SimaiNoteType.Slide && n.StartPosition == 1).ToArray();
            var a3 = FlattenNotes(chart).Where(n =>
                n.Type == SimaiNoteType.Slide && n.StartPosition == 3).ToArray();
            Assert.That(a1, Has.Length.EqualTo(15));
            Assert.That(a3, Has.Length.EqualTo(2));

            var bindings = AirshiftHandedSimaiSyntax.BindGameNotes(chart, syntax);
            Assert.That(bindings.Count, Is.EqualTo(14));
            Assert.That(bindings[a1[a1.Length - 2]], Is.EqualTo(AirshiftRequiredHand.Left));
            Assert.That(bindings[a1[a1.Length - 1]], Is.EqualTo(AirshiftRequiredHand.Right));
        }


        /// <summary>Two opposite hands trace two distinct same-origin SLIDEs.</summary>
        [TestCase("(120){4}L1-4[8:1]*R1-6[8:1],E",
                  "(120){4}1-4[8:1]/1-6[8:1],E")]
        [TestCase("(120){4}R1q4[8:1]*L1p4[8:1],E",
                  "(120){4}1q4[8:1]/1p4[8:1],E")]
        public void GameSyntax_ParallelLrSlidesBindTwoSeparatePaths(
            string source, string stripped)
        {
            var syntax = AirshiftHandedSimaiSyntax.Preprocess(source);
            Assert.That(syntax.SimaiText, Is.EqualTo(stripped));
            Assert.That(syntax.Markers.Count, Is.EqualTo(2));

            var chart = SimaiParser.ParseChart(syntax.SimaiText.AsSpan());
            var slides = FlattenNotes(chart).Where(n => n.Type == SimaiNoteType.Slide).ToArray();
            Assert.That(slides, Has.Length.EqualTo(2));
            Assert.That(slides[0], Is.Not.SameAs(slides[1]));
            Assert.That(slides[0].StartPosition, Is.EqualTo(slides[1].StartPosition));
            Assert.That(slides[0].RawContent, Is.Not.EqualTo(slides[1].RawContent));

            var bindings = AirshiftHandedSimaiSyntax.BindGameNotes(chart, syntax);
            Assert.That(bindings.Count, Is.EqualTo(2));
            Assert.That(bindings[slides[0]], Is.EqualTo(
                source[8] == 'L' ? AirshiftRequiredHand.Left : AirshiftRequiredHand.Right));
            Assert.That(bindings.Values, Is.EquivalentTo(new[]
            {
                AirshiftRequiredHand.Left,
                AirshiftRequiredHand.Right
            }));
        }

        /// <summary>Ordinary unmarked Simai fork notation stays intact.</summary>
        [Test]
        public void GameSyntax_UnmarkedParallelSlideRemainsOriginal()
        {
            const string source = "(120){4}1-4[8:1]*-6[8:1],E";
            var syntax = AirshiftHandedSimaiSyntax.Preprocess(source);
            Assert.That(syntax.SimaiText, Is.EqualTo(source));
            Assert.That(syntax.Markers.Count, Is.Zero);
        }

        /// <summary>One-hand, identical and ambiguous fork labels are rejected.</summary>
        [TestCase("(120){4}L1-4[8:1]*L1-6[8:1],E")]
        [TestCase("(120){4}L1-4[8:1]*R2-6[8:1],E")]
        [TestCase("(120){4}L1-4[8:1]*R1-4[8:1],E")]
        [TestCase("(120){4}L1-4[8:1]*R1w5[8:1],E")]
        [TestCase("(120){4}L1-4[8:1]/R1-6[8:1],E")]
        [TestCase("(120){4}L1-4[8:1]*R1-6[8:1]*L1-7[8:1],E")]
        public void GameSyntax_RejectsAmbiguousParallelHandPairs(string source)
        {
            Assert.Throws<FormatException>(() => AirshiftHandedSimaiSyntax.Preprocess(source));
        }

        [Test]
        public void GameSyntax_RecognizesHandedStarsAndTouchesOnlyInAirshift()
        {
            var source = "(120){4}L1$,R5$,LA1,RB3,1$,A1,E";
            var syntax = AirshiftHandedSimaiSyntax.Preprocess(source);

            Assert.That(syntax.SimaiText, Is.EqualTo("(120){4}1$,5$,A1,B3,1$,A1,E"));
            Assert.That(syntax.Markers.Count, Is.EqualTo(4));
            Assert.That(syntax.Markers.Select(x => x.RequiredHand), Is.EqualTo(new[]
            {
                AirshiftRequiredHand.Left,
                AirshiftRequiredHand.Right,
                AirshiftRequiredHand.Left,
                AirshiftRequiredHand.Right,
            }));
            Assert.That(syntax.SourceCounts["T:A1"], Is.EqualTo(2));
            Assert.That(syntax.SourceCounts["C:A1"], Is.EqualTo(2));
            // STANDARD must never invoke Preprocess, so it receives unmodified fumen.
            Assert.That(source, Does.StartWith("(120){4}L1$"));
        }

        [Test]
        public void GameSyntax_RejectsUnsupportedHandedSlidesAndIdenticalDoubles()
        {
            Assert.Throws<FormatException>(() =>
                AirshiftHandedSimaiSyntax.Preprocess("(120){4}L1w5[8:1],E"));
            Assert.Throws<FormatException>(() =>
                AirshiftHandedSimaiSyntax.Preprocess("(120){4}R1h[4:1],E"));
            Assert.Throws<FormatException>(() =>
                AirshiftHandedSimaiSyntax.Preprocess("(120){4}L1$/L1$,E"));
        }


        /// <summary>
        /// Curves, pivots and multi-segment simai SLIDEs bind the original
        /// chart object and leave the original Game scene judge queues intact.
        /// </summary>
        [TestCase("(120){4}L1p4[8:1],E", "(120){4}1p4[8:1],E", "Left")]
        [TestCase("(120){4}R1q4[8:1],E", "(120){4}1q4[8:1],E", "Right")]
        [TestCase("(120){4}L1V35[8:1],E", "(120){4}1V35[8:1],E", "Left")]
        [TestCase("(120){4}R1v3[8:1],E", "(120){4}1v3[8:1],E", "Right")]
        [TestCase("(120){4}L1-4[8:1]q7[8:1],E", "(120){4}1-4[8:1]q7[8:1],E", "Left")]
        [TestCase("(120){4}R1-3[8:1]-5[8:1],E", "(120){4}1-3[8:1]-5[8:1],E", "Right")]
        [TestCase("(120){4}L1pp6[8:1],E", "(120){4}1pp6[8:1],E", "Left")]
        public void GameSyntax_ComplexSlideKeepsHandOnOriginalParsedNote(
            string source, string stripped, string handName)
        {
            var syntax = AirshiftHandedSimaiSyntax.Preprocess(source);
            Assert.That(syntax.SimaiText, Is.EqualTo(stripped));
            Assert.That(syntax.Markers.Count, Is.EqualTo(1));
            var chart = SimaiParser.ParseChart(syntax.SimaiText.AsSpan());
            var notes = FlattenNotes(chart).Where(n => n.Type == SimaiNoteType.Slide).ToArray();
            Assert.That(notes, Has.Length.EqualTo(1));
            var hands = AirshiftHandedSimaiSyntax.BindGameNotes(chart, syntax);
            Assert.That(hands.Count, Is.EqualTo(1));
            Assert.That(hands[notes[0]], Is.EqualTo(
                Enum.Parse<AirshiftRequiredHand>(handName)));
        }

        /// <summary>
        /// A preceding ordinary slide at the same source key must not be
        /// accidentally assigned the hand of a later curved slide.
        /// </summary>
        [Test]
        public void GameSyntax_HandedCurveAfterOrdinarySlideKeepsOccurrenceOrder()
        {
            var syntax = AirshiftHandedSimaiSyntax.Preprocess(
                "(120){4}1p4[8:1],R1V35[8:1],E");
            var chart = SimaiParser.ParseChart(syntax.SimaiText.AsSpan());
            var slides = FlattenNotes(chart).Where(n => n.Type == SimaiNoteType.Slide).ToArray();
            var hands = AirshiftHandedSimaiSyntax.BindGameNotes(chart, syntax);
            Assert.That(slides, Has.Length.EqualTo(2));
            Assert.That(hands.ContainsKey(slides[0]), Is.False);
            Assert.That(hands[slides[1]], Is.EqualTo(AirshiftRequiredHand.Right));
        }

        /// <summary>
        /// The single-cursor rule is not silently applied to WIFI, multiple
        /// same-beat branches, or malformed tagged slide tokens.
        /// </summary>
        [TestCase("(120){4}L1w5[8:1],E")]
        [TestCase("(120){4}R3w7[8:1],E")]
        [TestCase("(120){4}L1-4[8:1]*-6[8:1],E")]
        [TestCase("(120){4}L1-5,E")]
        [TestCase("(120){4}R1-5[8:1]b,E")]
        public void GameSyntax_ComplexSlideUnsupportedInputRejectsExplicitly(string input)
        {
            Assert.Throws<FormatException>(() => AirshiftHandedSimaiSyntax.Preprocess(input));
        }

        /// <summary>
        /// High-density charts may contain two separate WIFI slides on
        /// the same beat; neither needs an L/R source prefix or exclusive
        /// ownership of both hands. Both must survive preprocessing
        /// and parsing as distinct full-path notes.
        /// </summary>
        [TestCase("(120){4}1w5[8:1]/5w1[8:1],E")]
        [TestCase("(120){4}1w5[8:1]/3w7[8:1],E")]
        public void GameSyntax_TwoConcurrentWifisPreserveBothFullPaths(string input)
        {
            var syntax = AirshiftHandedSimaiSyntax.Preprocess(input);
            Assert.That(syntax.SimaiText, Is.EqualTo(input));
            Assert.That(syntax.Markers, Is.Empty);

            var chart = SimaiParser.ParseChart(syntax.SimaiText.AsSpan());
            var slides = FlattenNotes(chart)
                .Where(n => n.Type == SimaiNoteType.Slide).ToArray();
            Assert.That(slides, Has.Length.EqualTo(2));
            Assert.That(slides[0], Is.Not.SameAs(slides[1]));
            Assert.That(slides[0].StartPosition,
                Is.Not.EqualTo(slides[1].StartPosition));
            Assert.That(slides.All(n => n.RawContent.Contains("w")), Is.True);
            Assert.That(AirshiftHandedSimaiSyntax.BindGameNotes(chart, syntax),
                Is.Empty);
        }

        /// <summary>Ordinary unprefixed WIFI charts still reach MajSimai untouched.</summary>
        [Test]
        public void GameSyntax_OrdinaryWifiRemainsUnchanged()
        {
            const string input = "(120){4}1w5[8:1],E";
            var syntax = AirshiftHandedSimaiSyntax.Preprocess(input);
            Assert.That(syntax.SimaiText, Is.EqualTo(input));
            Assert.That(syntax.Markers.Count, Is.Zero);
        }

        /// <summary>One blue and one red identical STAR may share one beat and key.</summary>
        [TestCase("(120){4}L1$/R1$,E", "(120){4}1$/1$,E")]
        [TestCase("(120){4}R1$/L1$,E", "(120){4}1$/1$,E")]
        [TestCase("(120){4}L1$$/R1$$,E", "(120){4}1$$/1$$,E")]
        [TestCase("(120){4}R8$$/L8$$,E", "(120){4}8$$/8$$,E")]
        public void GameSyntax_SamePositionOppositeHandStars_MapBothOriginalNotes(string source, string stripped)
        {
            var syntax = AirshiftHandedSimaiSyntax.Preprocess(source);
            Assert.That(syntax.SimaiText, Is.EqualTo(stripped));
            Assert.That(syntax.Markers.Count, Is.EqualTo(2));

            var parsed = SimaiParser.ParseChart(syntax.SimaiText.AsSpan());
            var bindings = AirshiftHandedSimaiSyntax.BindGameNotes(parsed, syntax);
            var stars = FlattenNotes(parsed).Where(n => n.Type == SimaiNoteType.Tap).ToArray();

            Assert.That(stars, Has.Length.EqualTo(2),
                "MajSimai must preserve both coincident STAR objects for independent judging.");
            Assert.That(stars[0], Is.Not.SameAs(stars[1]));
            Assert.That(stars.All(n => n.IsForceStar), Is.True);
            Assert.That(stars[0].StartPosition, Is.EqualTo(stars[1].StartPosition));
            Assert.That(bindings.Count, Is.EqualTo(2));
            Assert.That(bindings.Values, Is.EquivalentTo(new[]
            {
                AirshiftRequiredHand.Left,
                AirshiftRequiredHand.Right,
            }));
        }

        /// <summary>Ambiguous same-beat duplicates still fail before parser binding.</summary>
        [TestCase("(120){4}L1$/L1$,E")]
        [TestCase("(120){4}R1$/R1$,E")]
        [TestCase("(120){4}L1$/R1$$,E")]
        [TestCase("(120){4}L1$$/R1$,E")]
        [TestCase("(120){4}L1$/1$,E")]
        [TestCase("(120){4}L1$/R1$/1$,E")]
        public void GameSyntax_RejectsAmbiguousSameBeatSameAreaNotes(string source)
        {
            Assert.Throws<FormatException>(() => AirshiftHandedSimaiSyntax.Preprocess(source));
        }


        /// <summary>Opposite-handed plain TOUCHes are allowed at one area and time.</summary>
        [TestCase("(120){4}LA1/RA1,E", "(120){4}A1/A1,E")]
        [TestCase("(120){4}RB3/LB3,E", "(120){4}B3/B3,E")]
        [TestCase("(120){4}LC/RC,E", "(120){4}C/C,E")]
        public void GameSyntax_SamePositionOppositeHandTouchesRemainSeparate(string source, string stripped)
        {
            var syntax = AirshiftHandedSimaiSyntax.Preprocess(source);
            Assert.That(syntax.SimaiText, Is.EqualTo(stripped));
            var chart = SimaiParser.ParseChart(syntax.SimaiText.AsSpan());
            var bindings = AirshiftHandedSimaiSyntax.BindGameNotes(chart, syntax);
            var notes = FlattenNotes(chart).Where(n => n.Type == SimaiNoteType.Touch).ToArray();

            Assert.That(notes, Has.Length.EqualTo(2));
            Assert.That(notes[0], Is.Not.SameAs(notes[1]));
            Assert.That(bindings.Count, Is.EqualTo(2));
            Assert.That(bindings[notes[0]], Is.EqualTo(
                source.Contains("LA1") || source.Contains("LC") ?
                    AirshiftRequiredHand.Left : AirshiftRequiredHand.Right));
            Assert.That(bindings.Values, Is.EquivalentTo(new[]
            {
                AirshiftRequiredHand.Left,
                AirshiftRequiredHand.Right
            }));
        }

        /// <summary>Ambiguous same-sensor TOUCH twins remain invalid.</summary>
        [TestCase("(120){4}LA1/LA1,E")]
        [TestCase("(120){4}RA1/RA1,E")]
        [TestCase("(120){4}LA1/A1,E")]
        [TestCase("(120){4}LA1/RA1/LA1,E")]
        [TestCase("(120){4}LC/RC/C,E")]
        [TestCase("(120){4}LA1f/RA1f,E")]
        public void GameSyntax_RejectsAmbiguousSameAreaTouches(string source)
        {
            Assert.Throws<FormatException>(() => AirshiftHandedSimaiSyntax.Preprocess(source));
        }

        /// <summary>Global ordinal mapping remains stable across an earlier ordinary STAR.</summary>
        [Test]
        public void GameSyntax_OverlappingStarPairAfterEarlierUnmarkedStar()
        {
            var syntax = AirshiftHandedSimaiSyntax.Preprocess("(120){4}1$,L1$/R1$,E");
            var chart = SimaiParser.ParseChart(syntax.SimaiText.AsSpan());
            var bindings = AirshiftHandedSimaiSyntax.BindGameNotes(chart, syntax);
            var notes = FlattenNotes(chart).Where(n => n.Type == SimaiNoteType.Tap).ToArray();

            Assert.That(notes, Has.Length.EqualTo(3));
            Assert.That(bindings.ContainsKey(notes[0]), Is.False);
            Assert.That(bindings[notes[1]], Is.EqualTo(AirshiftRequiredHand.Left));
            Assert.That(bindings[notes[2]], Is.EqualTo(AirshiftRequiredHand.Right));
        }


        /// <summary>Handed single straight SLIDE maps to the original parsed Slide note.</summary>
        [TestCase("(120){4}L1-5[8:1],E", "(120){4}1-5[8:1],E", "Left")]
        [TestCase("(120){4}R3-7[8:1],E", "(120){4}3-7[8:1],E", "Right")]
        public void GameSyntax_StraightSlideBindsHand(string source, string output, string expectedHand)
        {
            var syntax = AirshiftHandedSimaiSyntax.Preprocess(source);
            Assert.That(syntax.SimaiText, Is.EqualTo(output));
            Assert.That(syntax.Markers.Count, Is.EqualTo(1));
            var chart = SimaiParser.ParseChart(syntax.SimaiText.AsSpan());
            var bindings = AirshiftHandedSimaiSyntax.BindGameNotes(chart, syntax);
            var slides = FlattenNotes(chart).Where(note => note.Type == SimaiNoteType.Slide).ToArray();
            Assert.That(slides, Has.Length.EqualTo(1));
            Assert.That(bindings.Count, Is.EqualTo(1));
            Assert.That(bindings[slides[0]], Is.EqualTo(Enum.Parse<AirshiftRequiredHand>(expectedHand)));
        }

        /// <summary>Source occurrences preserve the right marker even after an ordinary slide.</summary>
        [Test]
        public void GameSyntax_StraightSlideAfterOrdinarySlideKeepsMarkerIdentity()
        {
            var syntax = AirshiftHandedSimaiSyntax.Preprocess("(120){4}1-5[8:1],L1-5[8:1],E");
            var chart = SimaiParser.ParseChart(syntax.SimaiText.AsSpan());
            var slides = FlattenNotes(chart).Where(note => note.Type == SimaiNoteType.Slide).ToArray();
            var bindings = AirshiftHandedSimaiSyntax.BindGameNotes(chart, syntax);
            Assert.That(slides, Has.Length.EqualTo(2));
            Assert.That(bindings.ContainsKey(slides[0]), Is.False);
            Assert.That(bindings[slides[1]], Is.EqualTo(AirshiftRequiredHand.Left));
        }

        /// <summary>Same-beat, same-origin slide duplication is not a supported v1 syntax.</summary>
        [TestCase("(120){4}L1-5[8:1]/R1-5[8:1],E")]
        [TestCase("(120){4}L1-5[8:1]/1-5[8:1],E")]
        public void GameSyntax_RejectsAmbiguousStraightSlideTwins(string source)
        {
            Assert.Throws<FormatException>(() => AirshiftHandedSimaiSyntax.Preprocess(source));
        }

        [Test]
        public void GameSyntax_ParsedMajSimaiNotesHaveHandsForOriginalGameLoader()
        {
            var syntax = AirshiftHandedSimaiSyntax.Preprocess(
                "(120){4}L1$,R5$,LA1,RB3,1$,A1,E");
            var chart = SimaiParser.ParseChart(syntax.SimaiText.AsSpan());
            var bindings = AirshiftHandedSimaiSyntax.BindGameNotes(chart, syntax);

            Assert.That(bindings.Count, Is.EqualTo(4));
            var marked = FlattenNotes(chart)
                .Where(bindings.ContainsKey).ToArray();
            Assert.That(marked.Count(n => n.Type == SimaiNoteType.Tap && n.IsForceStar), Is.EqualTo(2));
            Assert.That(marked.Count(n => n.Type == SimaiNoteType.Touch), Is.EqualTo(2));
            Assert.That(marked.Single(n => n.Type == SimaiNoteType.Tap && n.StartPosition == 1)
                .IsForceStar, Is.True);
            Assert.That(bindings[marked.Single(n => n.Type == SimaiNoteType.Tap && n.StartPosition == 1)],
                Is.EqualTo(AirshiftRequiredHand.Left));
            Assert.That(bindings[marked.Single(n => n.Type == SimaiNoteType.Tap && n.StartPosition == 5)],
                Is.EqualTo(AirshiftRequiredHand.Right));
            Assert.That(bindings[marked.Single(n => n.Type == SimaiNoteType.Touch && n.TouchArea == 'A')],
                Is.EqualTo(AirshiftRequiredHand.Left));
            Assert.That(bindings[marked.Single(n => n.Type == SimaiNoteType.Touch && n.TouchArea == 'B')],
                Is.EqualTo(AirshiftRequiredHand.Right));
            Assert.That(FlattenNotes(chart).Count() - bindings.Count,
                Is.EqualTo(2), "Unprefixed notes remain unbound and accept either hand.");
        }

        [Test]
        public void GameSyntax_UsesOrdinalAfterEarlierOrdinaryNote()
        {
            var syntax = AirshiftHandedSimaiSyntax.Preprocess("(120){4}1,L1$,A1,RA1,E");
            var chart = SimaiParser.ParseChart(syntax.SimaiText.AsSpan());
            var bindings = AirshiftHandedSimaiSyntax.BindGameNotes(chart, syntax);

            var taps = FlattenNotes(chart)
                .Where(n => n.Type == SimaiNoteType.Tap && n.StartPosition == 1).ToArray();
            var touches = FlattenNotes(chart)
                .Where(n => n.Type == SimaiNoteType.Touch && n.TouchArea == 'A').ToArray();
            Assert.That(taps.Length, Is.EqualTo(2));
            Assert.That(touches.Length, Is.EqualTo(2));
            Assert.That(bindings.ContainsKey(taps[0]), Is.False);
            Assert.That(bindings[taps[1]], Is.EqualTo(AirshiftRequiredHand.Left));
            Assert.That(bindings.ContainsKey(touches[0]), Is.False);
            Assert.That(bindings[touches[1]], Is.EqualTo(AirshiftRequiredHand.Right));
        }

        [Test]
        public void GameSyntax_RejectsParserCountMismatchInsteadOfWrongHandAssignment()
        {
            var syntax = AirshiftHandedSimaiSyntax.Preprocess("(120){4}L1$,E");
            var wrongChart = SimaiParser.ParseChart("(120){4}1,1$,E".AsSpan());
            Assert.Throws<FormatException>(() =>
                AirshiftHandedSimaiSyntax.BindGameNotes(wrongChart, syntax));
        }

        [Test]
        public void Converter_PreservesStableIdsDuplicatesAndModifiers()
        {
            var source = new RadarChartInput
            {
                ChartEndTimeSeconds = 4d,
                Events = new[]
                {
                    Event(11, RadarEventKind.Tap, "1", 1d, 1d, isBreak: true),
                    Event(12, RadarEventKind.Tap, "1", 1d, 1d, isEx: true),
                    Event(13, RadarEventKind.TouchHold, "C", 2d, 3d),
                },
            };

            var chart = AirshiftChartConverter.Convert(source, 0.25d);

            Assert.That(chart.Notes.Select(note => note.NoteId), Is.EqualTo(new[] { 11, 12, 13 }));
            Assert.That(chart.Notes[0].StartTime, Is.EqualTo(1.25d));
            Assert.That(chart.Notes[0].IsBreak, Is.True);
            Assert.That(chart.Notes[1].IsEx, Is.True);
            Assert.That(chart.Notes[2].Area, Is.EqualTo(AirshiftSensorArea.C));
        }

        [Test]
        public void TapAction_ConsumesOnlyOneOverlappingNote()
        {
            var chart = Chart(
                Note(1, AirshiftChartNoteKind.Tap, 1d, 1d, AirshiftSensorArea.A1),
                Note(2, AirshiftChartNoteKind.Tap, 1d, 1d, AirshiftSensorArea.A1));
            var judge = new AirshiftChartJudge(chart, AirshiftChartJudgeSettings.Prototype);
            var gesture = Gesture(100, HandId.Left, AirshiftArea.A1);

            Assert.That(judge.ObserveTapGesture(gesture, 1d), Is.True);
            Assert.That(judge.ObserveTapGesture(gesture, 1d), Is.False);
            Assert.That(chart.Notes.Count(note => note.State == AirshiftNoteState.Resolved), Is.EqualTo(1));
        }

        [Test]
        public void Hold_RequiresSustainedCoverage()
        {
            var hold = Note(1, AirshiftChartNoteKind.Hold, 1d, 2d, AirshiftSensorArea.A2);
            var chart = Chart(hold);
            var judge = new AirshiftChartJudge(chart, AirshiftChartJudgeSettings.Prototype);

            Assert.That(judge.ObserveTapGesture(Gesture(1, HandId.Left, AirshiftArea.A2), 1d), Is.True);
            judge.Update(1.5d, AirshiftSensorArea.A2, AirshiftSensorArea.None);
            judge.Update(2d, AirshiftSensorArea.A2, AirshiftSensorArea.None);

            Assert.That(hold.State, Is.EqualTo(AirshiftNoteState.Resolved));
            Assert.That(hold.Grade, Is.EqualTo(AirshiftJudgeGrade.Perfect));
        }

        [Test]
        public void Hold_InstantStrikeWithoutCoverageMisses()
        {
            var hold = Note(1, AirshiftChartNoteKind.Hold, 1d, 2d, AirshiftSensorArea.A2);
            var chart = Chart(hold);
            var judge = new AirshiftChartJudge(chart, AirshiftChartJudgeSettings.Prototype);

            judge.ObserveTapGesture(Gesture(1, HandId.Left, AirshiftArea.A2), 1d);
            judge.Update(1.2d, AirshiftSensorArea.None, AirshiftSensorArea.None);
            judge.Update(2d, AirshiftSensorArea.None, AirshiftSensorArea.None);

            Assert.That(hold.Grade, Is.EqualTo(AirshiftJudgeGrade.Miss));
        }

        [Test]
        public void TouchAndTouchHold_UseSensorActionsAndCoverage()
        {
            var touch = Note(1, AirshiftChartNoteKind.Touch, 1d, 1d, AirshiftSensorArea.B3);
            var touchHold = Note(2, AirshiftChartNoteKind.TouchHold, 2d, 3d, AirshiftSensorArea.C);
            var chart = Chart(touch, touchHold);
            var judge = new AirshiftChartJudge(chart, AirshiftChartJudgeSettings.Prototype);

            Assert.That(judge.ObserveSensorAction(new AirshiftSensorAction(1, HandId.Right, AirshiftSensorArea.B3, 1d, 1f), 1d), Is.True);
            Assert.That(judge.ObserveSensorAction(new AirshiftSensorAction(2, HandId.Left, AirshiftSensorArea.C, 2d, 1f), 2d), Is.True);
            judge.Update(2.5d, AirshiftSensorArea.C, AirshiftSensorArea.None);
            judge.Update(3d, AirshiftSensorArea.C, AirshiftSensorArea.None);

            Assert.That(touch.Grade, Is.EqualTo(AirshiftJudgeGrade.Perfect));
            Assert.That(touchHold.Grade, Is.EqualTo(AirshiftJudgeGrade.Perfect));
        }

        [Test]
        public void Slide_RequiresOrderedCheckpoints()
        {
            var slide = new AirshiftChartNote(
                1,
                AirshiftChartNoteKind.Slide,
                1d,
                2d,
                AirshiftSensorArea.A1,
                false,
                false,
                false,
                false,
                "1-3[4:1]",
                new[]
                {
                    new AirshiftSlideCheckpoint(AirshiftSensorArea.A1, 1d),
                    new AirshiftSlideCheckpoint(AirshiftSensorArea.A2, 1.5d),
                    new AirshiftSlideCheckpoint(AirshiftSensorArea.A3, 2d),
                });
            var chart = Chart(slide);
            var judge = new AirshiftChartJudge(chart, AirshiftChartJudgeSettings.Prototype);

            judge.ObserveArea(HandId.Left, AirshiftSensorArea.A3, 1, 1d);
            Assert.That(slide.SlideCheckpointIndex, Is.Zero);
            judge.ObserveArea(HandId.Left, AirshiftSensorArea.A1, 2, 1d);
            judge.ObserveArea(HandId.Left, AirshiftSensorArea.A2, 3, 1.5d);
            judge.ObserveArea(HandId.Left, AirshiftSensorArea.A3, 4, 2d);

            Assert.That(slide.State, Is.EqualTo(AirshiftNoteState.Resolved));
            Assert.That(slide.Grade, Is.EqualTo(AirshiftJudgeGrade.Perfect));
        }

        [Test]
        public void SlideHeadGesture_ArmsPathButDoesNotAutoCompleteIt()
        {
            var head = Note(1, AirshiftChartNoteKind.Tap, 1d, 1d, AirshiftSensorArea.A1);
            var slide = new AirshiftChartNote(
                2,
                AirshiftChartNoteKind.Slide,
                1d,
                2d,
                AirshiftSensorArea.A1,
                false,
                false,
                false,
                false,
                "1-2[4:1]",
                new[]
                {
                    new AirshiftSlideCheckpoint(AirshiftSensorArea.A1, 1d),
                    new AirshiftSlideCheckpoint(AirshiftSensorArea.A2, 2d),
                });
            var chart = Chart(head, slide);
            var judge = new AirshiftChartJudge(chart, AirshiftChartJudgeSettings.Prototype);

            Assert.That(judge.ObserveTapGesture(Gesture(20, HandId.Left, AirshiftArea.A1), 1d), Is.True);

            Assert.That(head.State, Is.EqualTo(AirshiftNoteState.Resolved));
            Assert.That(slide.State, Is.EqualTo(AirshiftNoteState.Active));
            Assert.That(slide.SlideCheckpointIndex, Is.EqualTo(1));
            Assert.That(slide.Grade, Is.EqualTo(AirshiftJudgeGrade.None));
        }

        [Test]
        public void Score_UsesIndependentPrototypeWeights()
        {
            var normal = Note(1, AirshiftChartNoteKind.Tap, 1d, 1d, AirshiftSensorArea.A1);
            var breakNote = new AirshiftChartNote(2, AirshiftChartNoteKind.Tap, 2d, 2d, AirshiftSensorArea.A2, false, true, false, false, "2b");
            var counter = new AirshiftScoreCounter(new[] { normal, breakNote });
            counter.Add(new AirshiftChartJudgeResult(normal, AirshiftJudgeGrade.Perfect, -0.01d));
            counter.Add(new AirshiftChartJudgeResult(breakNote, AirshiftJudgeGrade.Great, 0.02d));

            var score = counter.CreateSnapshot(2);

            Assert.That(score.Perfect, Is.EqualTo(1));
            Assert.That(score.Great, Is.EqualTo(1));
            Assert.That(score.Fast, Is.EqualTo(1));
            Assert.That(score.Late, Is.EqualTo(1));
            Assert.That(score.Achievement, Is.EqualTo(83.333d).Within(0.01d));
            Assert.That(counter.CurrentCombo, Is.EqualTo(2));
        }

        [Test]
        public void RecordStore_IsVersionedAndDoesNotUseStandardDatabase()
        {
            using var temporary = new TemporaryDirectory();
            var directory = temporary.Directory.CreateDirectory("Runtime");
            var score = new AirshiftScoreSnapshot { Score = 987654, Achievement = 98.7654d, MaxCombo = 42 };
            var key = AirshiftRecordStore.CreateChartKey("song-hash", ChartLevel.Master);

            var first = AirshiftRecordStore.Save(directory, key, score, debugInput: false);
            var second = AirshiftRecordStore.Save(directory, key, score, debugInput: false);
            var loaded = AirshiftRecordStore.Load(directory);

            Assert.That(first.PlayCount, Is.EqualTo(1));
            Assert.That(second.PlayCount, Is.EqualTo(2));
            Assert.That(loaded.Version, Is.EqualTo(1));
            Assert.That(loaded.Records.Single().ChartKey, Does.Contain(AirshiftRecordStore.PlayVersion));
            Assert.That(directory.FindFile(AirshiftRecordStore.FileName), Is.Not.Null);
            Assert.That(directory.FindFile("runtime.db"), Is.Null);
        }

        [Test]
        public void DebugResults_AreRejectedBeforeWriting()
        {
            using var temporary = new TemporaryDirectory();
            var directory = temporary.Directory.CreateDirectory("Runtime");
            Assert.Throws<InvalidOperationException>(() => AirshiftRecordStore.Save(
                directory,
                "debug",
                new AirshiftScoreSnapshot(),
                debugInput: true));
            Assert.That(directory.FindFile(AirshiftRecordStore.FileName), Is.Null);
        }

        /// <summary>
        /// Materializes the parser's ReadOnlySpan-backed timing/note collections
        /// before using LINQ. LINQ's SelectMany cannot enumerate ReadOnlySpan.
        /// </summary>
        private static SimaiNote[] FlattenNotes(SimaiChart chart)
        {
            var notes = new List<SimaiNote>();
            foreach (var timing in chart.NoteTimings)
            {
                foreach (var note in timing.Notes)
                {
                    notes.Add(note);
                }
            }
            return notes.ToArray();
        }

        private static AirshiftChart Chart(params AirshiftChartNote[] notes)
        {
            return new AirshiftChart(notes, notes.Length == 0 ? 0d : notes.Max(note => note.EndTime));
        }

        private static AirshiftChartNote Note(
            int id,
            AirshiftChartNoteKind kind,
            double start,
            double end,
            AirshiftSensorArea area)
        {
            return new AirshiftChartNote(id, kind, start, end, area, false, false, false, false, string.Empty);
        }

        private static TapGesture Gesture(long id, HandId hand, AirshiftArea area)
        {
            return new TapGesture(id, hand, area, 0d, AirshiftTapTriggerType.EntryFlick, 2f, Vector2.up, AirshiftInputSource.MouseSimulation);
        }

        private static RadarEvent Event(
            int id,
            RadarEventKind kind,
            string position,
            double start,
            double end,
            bool isBreak = false,
            bool isEx = false)
        {
            return new RadarEvent
            {
                EventId = id,
                Kind = kind,
                Position = position,
                StartTimeSeconds = start,
                EndTimeSeconds = end,
                IsBreak = isBreak,
                IsEx = isEx,
            };
        }
    }
}
