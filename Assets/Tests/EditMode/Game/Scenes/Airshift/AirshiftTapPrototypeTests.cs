using MajdataPlay.Scenes.Airshift;
using MajdataPlay.Scenes.Game;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

#nullable enable
namespace MajdataPlay.Tests.Game.Scenes.Airshift
{
    /// <summary>
    /// Verifies source-independent no-click TAP gesture recognition and isolated local judgement.
    /// </summary>
    public class AirshiftTapPrototypeTests
    {
        /// <summary>Checks that one fast area entry produces one entry-flick action.</summary>
        [Test]
        public void EntryFlick_ProducesOneGesture()
        {
            var detector = CreateDetector();
            var gestures = new List<TapGesture>();
            detector.GestureDetected += gestures.Add;
            detector.ObserveAreaTransition(CreateEnter(HandId.Left, AirshiftArea.A1, 1d));

            detector.ObserveMotionSample(CreateMotion(
                HandId.Left,
                AirshiftArea.A1,
                1d,
                2f,
                Vector2.up));
            detector.ObserveMotionSample(CreateMotion(
                HandId.Left,
                AirshiftArea.A1,
                1d,
                2f,
                Vector2.up));

            Assert.That(gestures, Has.Count.EqualTo(1));
            Assert.That(gestures[0].Area, Is.EqualTo(AirshiftArea.A1));
            Assert.That(gestures[0].HandId, Is.EqualTo(HandId.Left));
            Assert.That(gestures[0].TriggerType, Is.EqualTo(AirshiftTapTriggerType.EntryFlick));
        }

        /// <summary>Checks that a newly armed velocity burst inside an area produces a strike.</summary>
        [Test]
        public void InAreaVelocityBurst_ProducesGesture()
        {
            var detector = CreateDetector();
            var gestures = new List<TapGesture>();
            detector.GestureDetected += gestures.Add;
            detector.ObserveMotionSample(CreateMotion(
                HandId.Right,
                AirshiftArea.A4,
                0.5d,
                0.1f,
                Vector2.right));

            detector.ObserveMotionSample(CreateMotion(
                HandId.Right,
                AirshiftArea.A4,
                1d,
                1.8f,
                Vector2.right));

            Assert.That(gestures, Has.Count.EqualTo(1));
            Assert.That(gestures[0].HandId, Is.EqualTo(HandId.Right));
            Assert.That(gestures[0].Area, Is.EqualTo(AirshiftArea.A4));
            Assert.That(gestures[0].TriggerType, Is.EqualTo(AirshiftTapTriggerType.InAreaStrike));
        }

        /// <summary>Checks that stationary or sustained same-direction speed cannot auto-repeat TAP actions.</summary>
        [Test]
        public void SustainedMotion_DoesNotRepeatUntilRearmed()
        {
            var detector = CreateDetector();
            var gestures = new List<TapGesture>();
            detector.GestureDetected += gestures.Add;

            detector.ObserveMotionSample(CreateMotion(HandId.Left, AirshiftArea.A2, 0.1d, 0f, Vector2.zero));
            detector.ObserveMotionSample(CreateMotion(HandId.Left, AirshiftArea.A2, 0.2d, 1.5f, Vector2.right));
            detector.ObserveMotionSample(CreateMotion(HandId.Left, AirshiftArea.A2, 0.3d, 2.0f, Vector2.right));
            detector.ObserveMotionSample(CreateMotion(HandId.Left, AirshiftArea.A2, 0.4d, 1.7f, Vector2.right));
            detector.ObserveMotionSample(CreateMotion(HandId.Left, AirshiftArea.A2, 0.5d, 0f, Vector2.zero));
            detector.ObserveMotionSample(CreateMotion(HandId.Left, AirshiftArea.A2, 0.6d, 1.5f, Vector2.right));

            Assert.That(gestures, Has.Count.EqualTo(2));
        }

        /// <summary>Checks that a high-speed reversal can represent a new independent in-area action.</summary>
        [Test]
        public void DirectionReversal_ProducesNewIndependentGesture()
        {
            var detector = CreateDetector();
            var gestures = new List<TapGesture>();
            detector.GestureDetected += gestures.Add;

            detector.ObserveMotionSample(CreateMotion(HandId.Left, AirshiftArea.A6, 0.1d, 1.6f, Vector2.right));
            detector.ObserveMotionSample(CreateMotion(HandId.Left, AirshiftArea.A6, 0.2d, 1.6f, Vector2.left));

            Assert.That(gestures, Has.Count.EqualTo(2));
            Assert.That(gestures[0].ActionId, Is.Not.EqualTo(gestures[1].ActionId));
        }

        /// <summary>Checks that presenting the same action twice cannot consume two notes.</summary>
        [Test]
        public void SameAction_CannotConsumeTwice()
        {
            var chart = CreateChart(
                new AirshiftTestTapNote("A", 1d, AirshiftArea.A1, AirshiftRequiredHand.Any),
                new AirshiftTestTapNote("B", 1.1d, AirshiftArea.A1, AirshiftRequiredHand.Any));
            var judge = CreateJudge(chart);
            var gesture = CreateGesture(10, HandId.Left, AirshiftArea.A1, 1d);

            Assert.That(judge.TryJudge(gesture, 1d, out _), Is.True);
            Assert.That(judge.TryJudge(gesture, 1.1d, out _), Is.False);
            Assert.That(chart.Notes[0].State, Is.Not.EqualTo(AirshiftTapNoteState.Pending));
            Assert.That(chart.Notes[1].State, Is.EqualTo(AirshiftTapNoteState.Pending));
        }

        /// <summary>Checks Left, Right, and Any hand constraints without fallback consumption.</summary>
        [Test]
        public void RequiredHand_IsEnforced()
        {
            var rightChart = CreateChart(
                new AirshiftTestTapNote("RIGHT", 1d, AirshiftArea.A3, AirshiftRequiredHand.Right));
            var rightJudge = CreateJudge(rightChart);

            Assert.That(rightJudge.TryJudge(CreateGesture(1, HandId.Left, AirshiftArea.A3, 1d), 1d, out _), Is.False);
            Assert.That(rightJudge.TryJudge(CreateGesture(2, HandId.Right, AirshiftArea.A3, 1d), 1d, out _), Is.True);

            var anyChart = CreateChart(
                new AirshiftTestTapNote("ANY", 1d, AirshiftArea.A4, AirshiftRequiredHand.Any));
            var anyJudge = CreateJudge(anyChart);
            Assert.That(anyJudge.TryJudge(CreateGesture(3, HandId.Left, AirshiftArea.A4, 1d), 1d, out _), Is.True);
        }

        /// <summary>Checks early rejection, all prototype hit bands, and late MISS resolution.</summary>
        [Test]
        public void TimingWindows_RejectEarlyAndResolveLate()
        {
            var chart = CreateChart(
                new AirshiftTestTapNote("P", 1d, AirshiftArea.A1, AirshiftRequiredHand.Any),
                new AirshiftTestTapNote("G", 2d, AirshiftArea.A2, AirshiftRequiredHand.Any),
                new AirshiftTestTapNote("O", 3d, AirshiftArea.A3, AirshiftRequiredHand.Any),
                new AirshiftTestTapNote("M", 4d, AirshiftArea.A4, AirshiftRequiredHand.Any));
            var judge = CreateJudge(chart);

            Assert.That(judge.TryJudge(CreateGesture(1, HandId.Left, AirshiftArea.A1, 0.6d), 0.6d, out _), Is.False);
            Assert.That(judge.TryJudge(CreateGesture(2, HandId.Left, AirshiftArea.A1, 1.04d), 1.04d, out _), Is.True);
            Assert.That(judge.TryJudge(CreateGesture(3, HandId.Left, AirshiftArea.A2, 2.12d), 2.12d, out _), Is.True);
            Assert.That(judge.TryJudge(CreateGesture(4, HandId.Left, AirshiftArea.A3, 3.22d), 3.22d, out _), Is.True);
            judge.UpdateMisses(4.26d);

            Assert.That(chart.Notes[0].State, Is.EqualTo(AirshiftTapNoteState.Perfect));
            Assert.That(chart.Notes[1].State, Is.EqualTo(AirshiftTapNoteState.Great));
            Assert.That(chart.Notes[2].State, Is.EqualTo(AirshiftTapNoteState.Good));
            Assert.That(chart.Notes[3].State, Is.EqualTo(AirshiftTapNoteState.Miss));
        }

        /// <summary>Checks that Tab, re-center, and focus baseline operations do not emit a gesture.</summary>
        [Test]
        public void NonMotionResets_DoNotProduceGesture()
        {
            var areaDetector = new AirshiftOuterAreaDetector();
            var tracker = new AirshiftDualCursorTracker(
                areaDetector,
                AirshiftOuterAreaDetector.GetAreaCenter(AirshiftArea.A1),
                new Vector2(0.62f, 0.5f),
                0d);
            var detector = CreateDetector();
            var gestureCount = 0;
            detector.GestureDetected += _ => gestureCount++;
            tracker.AreaTransitioned += detector.ObserveAreaTransition;
            tracker.MotionSampled += detector.ObserveMotionSample;

            tracker.ToggleActiveHand();
            tracker.RecenterActiveHand(1d);
            tracker.ResetMotionBaselines(2d);
            detector.ObserveAreaTransition(CreateEnter(HandId.Left, AirshiftArea.A1, 3d));
            detector.Reset();
            detector.ObserveMotionSample(CreateMotion(
                HandId.Left,
                AirshiftArea.None,
                3d,
                2f,
                Vector2.up));

            Assert.That(gestureCount, Is.Zero);
        }

        /// <summary>Checks that segmented high-speed traversal can recognize an intermediate entered area.</summary>
        [Test]
        public void FastTraversal_RecognizesIntermediateArea()
        {
            var areaDetector = new AirshiftOuterAreaDetector();
            var tracker = new AirshiftDualCursorTracker(
                areaDetector,
                new Vector2(0.05f, 0.85f),
                new Vector2(0.62f, 0.5f),
                0d);
            var detector = CreateDetector();
            var gestures = new List<TapGesture>();
            detector.GestureDetected += gestures.Add;
            tracker.AreaTransitioned += detector.ObserveAreaTransition;
            tracker.MotionSampled += detector.ObserveMotionSample;

            tracker.ApplyActiveDelta(new Vector2(0.9f, 0f), 0.1d);

            Assert.That(gestures, Has.Count.EqualTo(1));
            Assert.That(gestures[0].TriggerType, Is.EqualTo(AirshiftTapTriggerType.EntryFlick));
            Assert.That(gestures[0].Area, Is.Not.EqualTo(AirshiftArea.None));
            Assert.That(tracker.LeftHand.CurrentArea, Is.EqualTo(AirshiftArea.None));
        }

        /// <summary>Checks that same-area consecutive notes require different action identifiers.</summary>
        [Test]
        public void ConsecutiveSameAreaNotes_RequireIndependentActions()
        {
            var chart = CreateChart(
                new AirshiftTestTapNote("FIRST", 1d, AirshiftArea.A5, AirshiftRequiredHand.Any),
                new AirshiftTestTapNote("SECOND", 1.5d, AirshiftArea.A5, AirshiftRequiredHand.Any));
            var judge = CreateJudge(chart);
            var first = CreateGesture(1, HandId.Left, AirshiftArea.A5, 1d);

            Assert.That(judge.TryJudge(first, 1d, out _), Is.True);
            Assert.That(judge.TryJudge(first, 1.5d, out _), Is.False);
            Assert.That(judge.TryJudge(CreateGesture(2, HandId.Left, AirshiftArea.A5, 1.5d), 1.5d, out _), Is.True);
            Assert.That(chart.Notes[0].ActionId, Is.EqualTo(1L));
            Assert.That(chart.Notes[1].ActionId, Is.EqualTo(2L));
        }

        /// <summary>Checks that the code-defined chart covers all required hands and a repeated area.</summary>
        [Test]
        public void PrototypeChart_HasRequiredCoverage()
        {
            var chart = AirshiftTestChart.CreatePrototype();
            var hasAny = false;
            var hasLeft = false;
            var hasRight = false;
            var a3Count = 0;
            for (var i = 0; i < chart.Notes.Count; i++)
            {
                var note = chart.Notes[i];
                hasAny |= note.RequiredHand == AirshiftRequiredHand.Any;
                hasLeft |= note.RequiredHand == AirshiftRequiredHand.Left;
                hasRight |= note.RequiredHand == AirshiftRequiredHand.Right;
                if (note.Area == AirshiftArea.A3)
                {
                    a3Count++;
                }
            }

            Assert.That(chart.Notes.Count, Is.EqualTo(5));
            Assert.That(hasAny, Is.True);
            Assert.That(hasLeft, Is.True);
            Assert.That(hasRight, Is.True);
            Assert.That(a3Count, Is.EqualTo(2));
        }

        /// <summary>Checks that the controllable local clock pauses and remains monotonic.</summary>
        [Test]
        public void LocalClock_IsControllableAndMonotonic()
        {
            IAirshiftTimelineClock clock = new AirshiftLocalTestClock(10d);

            Assert.That(clock.GetTime(11d), Is.EqualTo(1d));
            clock.Pause(11.5d);
            Assert.That(clock.GetTime(20d), Is.EqualTo(1.5d));
            clock.Resume(21d);
            Assert.That(clock.GetTime(22d), Is.EqualTo(2.5d));
            Assert.That(clock.GetTime(21.5d), Is.EqualTo(2.5d));
            clock.Restart(30d);
            Assert.That(clock.GetTime(30d), Is.Zero);
        }

        /// <summary>Checks that the isolated TAP prototype does not replace STANDARD game state.</summary>
        [Test]
        public void TapPrototype_DoesNotTouchStandardGameInfo()
        {
            var previous = Majdata<GameInfo>.Instance;
            var standardInfo = new GameInfo(GameMode.Normal, null!, null!);
            try
            {
                Majdata<GameInfo>.Instance = standardInfo;
                var chart = AirshiftTestChart.CreatePrototype();
                var judge = CreateJudge(chart);

                judge.TryJudge(CreateGesture(1, HandId.Left, AirshiftArea.A1, 1.5d), 1.5d, out _);

                Assert.That(Majdata<GameInfo>.Instance, Is.SameAs(standardInfo));
            }
            finally
            {
                Majdata<GameInfo>.Instance = previous;
            }
        }

        /// <summary>
        /// Creates a detector with the runtime prototype settings.
        /// </summary>
        /// <returns>A fresh gesture detector.</returns>
        private static AirshiftTapGestureDetector CreateDetector()
        {
            return new AirshiftTapGestureDetector(AirshiftTapGestureSettings.Prototype);
        }

        /// <summary>
        /// Creates an isolated chart from test notes.
        /// </summary>
        /// <param name="notes">The notes to include.</param>
        /// <returns>A fresh ordered test chart.</returns>
        private static AirshiftTestChart CreateChart(params AirshiftTestTapNote[] notes)
        {
            return new AirshiftTestChart(notes);
        }

        /// <summary>
        /// Creates an isolated judge with the runtime prototype timing windows.
        /// </summary>
        /// <param name="chart">The chart to judge.</param>
        /// <returns>A fresh TAP judge.</returns>
        private static AirshiftTapJudge CreateJudge(AirshiftTestChart chart)
        {
            return new AirshiftTapJudge(chart, AirshiftTapJudgeSettings.Prototype);
        }

        /// <summary>
        /// Creates one synthetic area-entry transition.
        /// </summary>
        /// <param name="handId">The synthetic hand.</param>
        /// <param name="area">The entered area.</param>
        /// <param name="timestamp">The monotonic timestamp.</param>
        /// <returns>The synthetic transition.</returns>
        private static AirshiftAreaTransition CreateEnter(
            HandId handId,
            AirshiftArea area,
            double timestamp)
        {
            return new AirshiftAreaTransition(
                handId,
                area,
                AirshiftAreaTransitionKind.Enter,
                AirshiftOuterAreaDetector.GetAreaCenter(area),
                timestamp,
                AirshiftInputSource.MouseSimulation);
        }

        /// <summary>
        /// Creates one synthetic normalized motion sample.
        /// </summary>
        /// <param name="handId">The synthetic hand.</param>
        /// <param name="area">The active area.</param>
        /// <param name="timestamp">The monotonic timestamp.</param>
        /// <param name="speed">The normalized speed.</param>
        /// <param name="direction">The motion direction.</param>
        /// <returns>The synthetic sample.</returns>
        private static AirshiftMotionSample CreateMotion(
            HandId handId,
            AirshiftArea area,
            double timestamp,
            float speed,
            Vector2 direction)
        {
            var normalizedDirection = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.zero;
            var position = area == AirshiftArea.None
                ? new Vector2(0.5f, 0.5f)
                : AirshiftOuterAreaDetector.GetAreaCenter(area);
            return new AirshiftMotionSample(
                handId,
                position,
                timestamp,
                normalizedDirection * speed,
                area,
                AirshiftArea.None,
                AirshiftInputSource.MouseSimulation);
        }

        /// <summary>
        /// Creates one synthetic unique TAP action.
        /// </summary>
        /// <param name="actionId">The action identifier.</param>
        /// <param name="handId">The action hand.</param>
        /// <param name="area">The target area.</param>
        /// <param name="timestamp">The source timestamp.</param>
        /// <returns>The synthetic gesture.</returns>
        private static TapGesture CreateGesture(
            long actionId,
            HandId handId,
            AirshiftArea area,
            double timestamp)
        {
            return new TapGesture(
                actionId,
                handId,
                area,
                timestamp,
                AirshiftTapTriggerType.EntryFlick,
                2f,
                Vector2.up,
                AirshiftInputSource.MouseSimulation);
        }
    }
}
