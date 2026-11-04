using MajdataPlay.Scenes.Airshift;
using MajdataPlay.Tests;
using MajRadar.Core;
using MajRadar.MajSimaiAdapter;
using MajSimai;
using MajdataPlay.Utils.ChartRadar;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

#nullable enable
namespace MajdataPlay.Tests.Game.Scenes.Airshift
{
    /// <summary>Verifies the Task 006 chart, judgement, score, and persistence isolation.</summary>
    public class AirshiftChartPlayTests
    {
        [Test]
        public void HandedSyntax_StripsOnlySupportedPrefixesAndKeepsOrdinals()
        {
            var parsed = AirshiftHandedSimaiSyntax.Preprocess(
                "(120){4}L1$,R5$,LA1,RB3,1$,A1,E");
            Assert.That(parsed.SimaiText, Is.EqualTo("(120){4}1$,5$,A1,B3,1$,A1,E"));
            Assert.That(parsed.Markers.Count, Is.EqualTo(4));
            Assert.That(parsed.Markers[0].Token, Is.EqualTo("1$"));
            Assert.That(parsed.Markers[0].Key, Is.EqualTo("T:A1"));
            Assert.That(parsed.Markers[0].Occurrence, Is.Zero);
            Assert.That(parsed.SourceCounts["T:A1"], Is.EqualTo(2));
            Assert.That(parsed.SourceCounts["C:A1"], Is.EqualTo(2));
            Assert.That(parsed.Markers[0].RequiredHand, Is.EqualTo(AirshiftRequiredHand.Left));
            Assert.That(parsed.Markers[1].RequiredHand, Is.EqualTo(AirshiftRequiredHand.Right));
            Assert.That(parsed.Markers[2].Token, Is.EqualTo("A1"));
            Assert.That(parsed.Markers[2].RequiredHand, Is.EqualTo(AirshiftRequiredHand.Left));
            Assert.That(parsed.Markers[3].Token, Is.EqualTo("B3"));
            Assert.That(parsed.Markers[3].RequiredHand, Is.EqualTo(AirshiftRequiredHand.Right));
        }

        [Test]
        public void HandedSyntax_RejectsUnsupportedAndAmbiguousPrefixedNotes()
        {
            Assert.Throws<FormatException>(() =>
                AirshiftHandedSimaiSyntax.Preprocess("(120){4}L1-5[8:1],E"));
            Assert.Throws<FormatException>(() =>
                AirshiftHandedSimaiSyntax.Preprocess("(120){4}L1$/R1$,E"));
            Assert.Throws<FormatException>(() =>
                AirshiftHandedSimaiSyntax.Preprocess("(120){4}R1h[4:1],E"));
        }

        [Test]
        public void HandedSyntax_ParsedMajSimaiChartReceivesCorrectHandMetadata()
        {
            var syntax = AirshiftHandedSimaiSyntax.Preprocess(
                "(120){4}L1$,R5$,LA1,RB3,1$,A1,E");
            var simai = SimaiParser.ParseChart(syntax.SimaiText.AsSpan());
            var adaptation = new MajSimaiChartAdapter(
                new PlayExtendedSlideBarCountProvider()).Adapt(simai, CancellationToken.None);
            Assert.That(adaptation.IsSuccess, Is.True,
                string.Join("; ", adaptation.Errors));
            Assert.That(adaptation.Chart, Is.Not.Null);

            var chart = AirshiftChartConverter.Convert(adaptation.Chart!, 0d);
            AirshiftHandedSimaiSyntax.Apply(chart, syntax);
            var leftStar = chart.Notes.Single(note =>
                note.Kind == AirshiftChartNoteKind.Tap &&
                note.Area == AirshiftSensorArea.A1 &&
                note.RequiredHand == AirshiftRequiredHand.Left);
            var rightStar = chart.Notes.Single(note =>
                note.Kind == AirshiftChartNoteKind.Tap &&
                note.Area == AirshiftSensorArea.A5 &&
                note.RequiredHand == AirshiftRequiredHand.Right);
            var leftTouch = chart.Notes.Single(note =>
                note.Kind == AirshiftChartNoteKind.Touch && note.Area == AirshiftSensorArea.A1 &&
                note.RequiredHand == AirshiftRequiredHand.Left);
            var rightTouch = chart.Notes.Single(note =>
                note.Kind == AirshiftChartNoteKind.Touch && note.Area == AirshiftSensorArea.B3 &&
                note.RequiredHand == AirshiftRequiredHand.Right);

            Assert.That(leftStar.IsSlideHead, Is.True);
            Assert.That(rightStar.IsSlideHead, Is.True);
            Assert.That(leftTouch.IsSlideHead, Is.False);
            Assert.That(rightTouch.IsSlideHead, Is.False);
            Assert.That(chart.Notes.Count(n => n.RequiredHand == AirshiftRequiredHand.Any), Is.EqualTo(2));
        }

        [Test]
        public void HandedSyntax_OrdinalsIncludeOrdinaryTapBeforeBlueStar()
        {
            var syntax = AirshiftHandedSimaiSyntax.Preprocess(
                "(120){4}1,L1$,A1,RA1,E");
            Assert.That(syntax.SimaiText, Is.EqualTo("(120){4}1,1$,A1,A1,E"));
            Assert.That(syntax.Markers.Select(m => m.Occurrence), Is.EqualTo(new[] { 1, 1 }));
            Assert.That(syntax.SourceCounts["T:A1"], Is.EqualTo(2));
            Assert.That(syntax.SourceCounts["C:A1"], Is.EqualTo(2));

            // Adapter may normalize raw token strings; semantic area and ordinal
            // must still identify the second TAP and second TOUCH correctly.
            var notes = new[]
            {
                new AirshiftChartNote(1, AirshiftChartNoteKind.Tap, 1d, 1d,
                    AirshiftSensorArea.A1, false, false, false, false, "1"),
                new AirshiftChartNote(2, AirshiftChartNoteKind.Touch, 1d, 1d,
                    AirshiftSensorArea.A1, false, false, false, false, "A1"),
                new AirshiftChartNote(3, AirshiftChartNoteKind.Tap, 2d, 2d,
                    AirshiftSensorArea.A1, false, false, false, false, "normalized-star"),
                new AirshiftChartNote(4, AirshiftChartNoteKind.Touch, 2d, 2d,
                    AirshiftSensorArea.A1, false, false, false, false, "normalized-touch")
            };
            var chart = Chart(notes);
            AirshiftHandedSimaiSyntax.Apply(chart, syntax);
            Assert.That(notes[0].RequiredHand, Is.EqualTo(AirshiftRequiredHand.Any));
            Assert.That(notes[1].RequiredHand, Is.EqualTo(AirshiftRequiredHand.Any));
            Assert.That(notes[2].RequiredHand, Is.EqualTo(AirshiftRequiredHand.Left));
            Assert.That(notes[2].IsSlideHead, Is.True);
            Assert.That(notes[3].RequiredHand, Is.EqualTo(AirshiftRequiredHand.Right));
        }

        [Test]
        public void HandedSyntax_RejectsAdapterCountMismatch()
        {
            var syntax = AirshiftHandedSimaiSyntax.Preprocess("(120){4}L1$,E");
            var chart = Chart(
                Note(1, AirshiftChartNoteKind.Tap, 1d, 1d, AirshiftSensorArea.A1),
                Note(2, AirshiftChartNoteKind.Tap, 2d, 2d, AirshiftSensorArea.A1));
            Assert.Throws<FormatException>(() => AirshiftHandedSimaiSyntax.Apply(chart, syntax));
            Assert.That(chart.Notes.All(note => note.RequiredHand == AirshiftRequiredHand.Any), Is.True);
        }

        [Test]
        public void HandedChartJudge_RejectsWrongHandAndRetainsEachNote()
        {
            var blueStar = Note(1, AirshiftChartNoteKind.Tap, 1d, 1d, AirshiftSensorArea.A1);
            blueStar.RequiredHand = AirshiftRequiredHand.Left;
            blueStar.IsSlideHead = true;
            var redTouch = Note(2, AirshiftChartNoteKind.Touch, 2d, 2d, AirshiftSensorArea.B3);
            redTouch.RequiredHand = AirshiftRequiredHand.Right;
            var judge = new AirshiftChartJudge(Chart(blueStar, redTouch), AirshiftChartJudgeSettings.Prototype);

            Assert.That(judge.ObserveTapGesture(Gesture(1, HandId.Right, AirshiftArea.A1), 1d), Is.False);
            Assert.That(blueStar.State, Is.EqualTo(AirshiftNoteState.Pending));
            Assert.That(judge.ObserveTapGesture(Gesture(2, HandId.Left, AirshiftArea.A1), 1d), Is.True);
            Assert.That(blueStar.State, Is.EqualTo(AirshiftNoteState.Resolved));

            Assert.That(judge.ObserveSensorAction(
                new AirshiftSensorAction(3, HandId.Left, AirshiftSensorArea.B3, 2d, 1f), 2d), Is.False);
            Assert.That(redTouch.State, Is.EqualTo(AirshiftNoteState.Pending));
            Assert.That(judge.ObserveSensorAction(
                new AirshiftSensorAction(4, HandId.Right, AirshiftSensorArea.B3, 2d, 1f), 2d), Is.True);
            Assert.That(redTouch.Grade, Is.EqualTo(AirshiftJudgeGrade.Perfect));
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
