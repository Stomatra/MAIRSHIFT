using MajdataPlay.Scenes.Airshift;
using MajdataPlay.Scenes.Game;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

#nullable enable
namespace MajdataPlay.Tests.Game.Scenes.Airshift
{
    /// <summary>
    /// Verifies the pure dual-cursor, geometry, and isolation behavior of the AIRSHIFT input prototype.
    /// </summary>
    public class AirshiftInputPrototypeTests
    {
        /// <summary>The geometry shared by each test.</summary>
        private AirshiftOuterAreaDetector _detector = null!;

        /// <summary>Creates a fresh independent geometry implementation.</summary>
        [SetUp]
        public void SetUp()
        {
            _detector = new AirshiftOuterAreaDetector();
        }

        /// <summary>Checks that moving the active hand does not alter the other hand.</summary>
        [Test]
        public void CursorPositions_AreIndependent()
        {
            var tracker = CreateTracker();
            var originalRight = tracker.RightHand.Position;

            tracker.ApplyActiveDelta(new Vector2(0.1f, 0.05f), 1d);

            AssertVector2(tracker.LeftHand.Position, new Vector2(0.48f, 0.55f));
            AssertVector2(tracker.RightHand.Position, originalRight);
        }

        /// <summary>Checks that switching control changes only the active-hand identifier.</summary>
        [Test]
        public void ToggleHand_DoesNotMoveEitherCursor()
        {
            var tracker = CreateTracker();
            var left = tracker.LeftHand.Position;
            var right = tracker.RightHand.Position;
            var motionCount = 0;
            tracker.MotionSampled += _ => motionCount++;

            var activeHand = tracker.ToggleActiveHand();

            Assert.That(activeHand, Is.EqualTo(HandId.Right));
            AssertVector2(tracker.LeftHand.Position, left);
            AssertVector2(tracker.RightHand.Position, right);
            Assert.That(motionCount, Is.Zero);
        }

        /// <summary>Checks the clockwise A1-A8 orientation inherited from ScreenTouchMapper.</summary>
        [Test]
        public void OuterAreaCenters_MapToCorrectNumbers()
        {
            for (var i = 1; i <= 8; i++)
            {
                var area = (AirshiftArea)i;
                Assert.That(_detector.GetArea(AirshiftOuterAreaDetector.GetAreaCenter(area)), Is.EqualTo(area));
            }
            Assert.That(_detector.GetArea(new Vector2(0.5f, 0.5f)), Is.EqualTo(AirshiftArea.None));
        }

        /// <summary>Checks that movement into and out of an outer region produces ordered transitions.</summary>
        [Test]
        public void AreaTransitions_ReportEnterAndExit()
        {
            var tracker = CreateTracker(new Vector2(0.5f, 0.5f));
            var events = new List<AirshiftAreaTransition>();
            tracker.AreaTransitioned += events.Add;
            var a1Center = AirshiftOuterAreaDetector.GetAreaCenter(AirshiftArea.A1);

            tracker.ApplyActiveDelta(a1Center - tracker.LeftHand.Position, 1d);
            tracker.ApplyActiveDelta(new Vector2(0.5f, 0.5f) - tracker.LeftHand.Position, 2d);

            Assert.That(events, Has.Count.EqualTo(2));
            Assert.That(events[0].Kind, Is.EqualTo(AirshiftAreaTransitionKind.Enter));
            Assert.That(events[0].Area, Is.EqualTo(AirshiftArea.A1));
            Assert.That(events[1].Kind, Is.EqualTo(AirshiftAreaTransitionKind.Exit));
            Assert.That(events[1].Area, Is.EqualTo(AirshiftArea.A1));
            Assert.That(tracker.LeftHand.PreviousArea, Is.EqualTo(AirshiftArea.A1));
        }

        /// <summary>Checks that re-centering suppresses both motion and artificial area transitions.</summary>
        [Test]
        public void Recenter_DoesNotGenerateFalseMotionEvents()
        {
            var a1Center = AirshiftOuterAreaDetector.GetAreaCenter(AirshiftArea.A1);
            var tracker = CreateTracker(a1Center);
            var motionCount = 0;
            var areaCount = 0;
            tracker.MotionSampled += _ => motionCount++;
            tracker.AreaTransitioned += _ => areaCount++;

            tracker.RecenterActiveHand(3d);

            AssertVector2(tracker.LeftHand.Position, new Vector2(0.5f, 0.5f));
            AssertVector2(tracker.LeftHand.Velocity, Vector2.zero);
            Assert.That(tracker.LeftHand.CurrentArea, Is.EqualTo(AirshiftArea.None));
            Assert.That(tracker.LeftHand.PreviousArea, Is.EqualTo(AirshiftArea.A1));
            Assert.That(motionCount, Is.Zero);
            Assert.That(areaCount, Is.Zero);
        }

        /// <summary>Checks that focus or window baseline resets do not move either hand or emit input.</summary>
        [Test]
        public void BaselineReset_DoesNotGenerateEventsOrMoveCursors()
        {
            var tracker = CreateTracker();
            var left = tracker.LeftHand.Position;
            var right = tracker.RightHand.Position;
            var motionCount = 0;
            var areaCount = 0;
            tracker.MotionSampled += _ => motionCount++;
            tracker.AreaTransitioned += _ => areaCount++;

            tracker.ResetMotionBaselines(5d);

            AssertVector2(tracker.LeftHand.Position, left);
            AssertVector2(tracker.RightHand.Position, right);
            AssertVector2(tracker.LeftHand.Velocity, Vector2.zero);
            AssertVector2(tracker.RightHand.Velocity, Vector2.zero);
            Assert.That(motionCount, Is.Zero);
            Assert.That(areaCount, Is.Zero);
        }

        /// <summary>Checks that segmented tracing observes intermediate boundaries during fast movement.</summary>
        [Test]
        public void FastMovement_UsesSegmentedAreaTracing()
        {
            var start = new Vector2(0.05f, 0.85f);
            var tracker = CreateTracker(start);
            var events = new List<AirshiftAreaTransition>();
            tracker.AreaTransitioned += events.Add;

            tracker.ApplyActiveDelta(new Vector2(0.9f, 0f), 0.1d);

            Assert.That(events.Count, Is.GreaterThanOrEqualTo(2));
            Assert.That(events.Any(value => value.Kind == AirshiftAreaTransitionKind.Exit), Is.True);
            Assert.That(events.Any(value => value.Kind == AirshiftAreaTransitionKind.Enter), Is.True);
        }

        /// <summary>Checks that only main-row digits map to A1-A8 and Numpad3 remains untouched.</summary>
        [Test]
        public void KeyboardMapping_UsesMainRowDigitsOnly()
        {
            Assert.That(AirshiftKeyboardDebugSource.GetAreaForKey(Key.Digit1), Is.EqualTo(AirshiftArea.A1));
            Assert.That(AirshiftKeyboardDebugSource.GetAreaForKey(Key.Digit8), Is.EqualTo(AirshiftArea.A8));
            Assert.That(AirshiftKeyboardDebugSource.GetAreaForKey(Key.Numpad3), Is.EqualTo(AirshiftArea.None));
        }

        /// <summary>Checks that a main-row debug key produces one press and one release transition.</summary>
        [Test]
        public void KeyboardState_ReportsPressAndReleaseOnce()
        {
            var source = new AirshiftKeyboardDebugSource();
            var events = new List<DebugButtonEvent>();
            source.DebugButtonChanged += events.Add;

            source.UpdateAreaKeyState(2, true, 1d);
            source.UpdateAreaKeyState(2, true, 1.5d);
            source.UpdateAreaKeyState(2, false, 2d);

            Assert.That(events, Has.Count.EqualTo(2));
            Assert.That(events[0].Area, Is.EqualTo(AirshiftArea.A3));
            Assert.That(events[0].IsPressed, Is.True);
            Assert.That(events[0].InputSource, Is.EqualTo(AirshiftInputSource.KeyboardDebug));
            Assert.That(events[1].Area, Is.EqualTo(AirshiftArea.A3));
            Assert.That(events[1].IsPressed, Is.False);
            Assert.That(events[1].InputSource, Is.EqualTo(AirshiftInputSource.KeyboardDebug));
        }

        /// <summary>Checks that AIRSHIFT motion cannot replace the STANDARD game session.</summary>
        [Test]
        public void CursorMotion_DoesNotTouchStandardGameInfo()
        {
            var previous = Majdata<GameInfo>.Instance;
            var standardInfo = new GameInfo(GameMode.Normal, null!, null!);
            try
            {
                Majdata<GameInfo>.Instance = standardInfo;
                var tracker = CreateTracker();

                tracker.ApplyActiveDelta(new Vector2(0.2f, 0.1f), 1d);

                Assert.That(Majdata<GameInfo>.Instance, Is.SameAs(standardInfo));
            }
            finally
            {
                Majdata<GameInfo>.Instance = previous;
            }
        }

        /// <summary>
        /// Creates a tracker with test-friendly initial positions.
        /// </summary>
        /// <param name="leftPosition">The optional left-hand initial position.</param>
        /// <returns>A fresh tracker.</returns>
        private AirshiftDualCursorTracker CreateTracker(Vector2? leftPosition = null)
        {
            return new AirshiftDualCursorTracker(
                _detector,
                leftPosition ?? new Vector2(0.38f, 0.5f),
                new Vector2(0.62f, 0.5f),
                0d);
        }

        /// <summary>
        /// Compares two vectors without depending on optional Unity Test Framework comparer helpers.
        /// </summary>
        /// <param name="actual">The observed vector.</param>
        /// <param name="expected">The expected vector.</param>
        private static void AssertVector2(Vector2 actual, Vector2 expected)
        {
            Assert.That(Vector2.Distance(actual, expected), Is.LessThan(0.00001f));
        }
    }
}
