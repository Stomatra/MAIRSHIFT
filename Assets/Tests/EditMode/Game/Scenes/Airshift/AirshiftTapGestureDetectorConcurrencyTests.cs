
using System.Collections.Generic;
using MajdataPlay.Scenes.Airshift;
using NUnit.Framework;
using UnityEngine;

namespace MajdataPlay.Tests.EditMode.Game.Scenes.Airshift
{
    [TestFixture]
    public class AirshiftTapGestureDetectorConcurrencyTests
    {
        private static AirshiftAreaTransition Enter(
            HandId hand,
            AirshiftArea area,
            double time)
        {
            return new AirshiftAreaTransition(
                hand,
                area,
                AirshiftAreaTransitionKind.Enter,
                Vector2.zero,
                time,
                AirshiftInputSource.MouseSimulation
            );
        }

        private static AirshiftMotionSample Motion(
            HandId hand,
            AirshiftArea area,
            double time)
        {
            return new AirshiftMotionSample(
                hand,
                Vector2.zero,
                time,
                new Vector2(1.25f, 0f),
                area,
                AirshiftArea.None,
                AirshiftInputSource.MouseSimulation
            );
        }

        [Test]
        public void SimultaneousEntries_BothHandsProduceEntryFlicks()
        {
            var detector = new AirshiftTapGestureDetector(
                AirshiftTapGestureSettings.Prototype);

            var gestures = new List<TapGesture>();
            detector.GestureDetected += gestures.Add;

            const double time = 10.0;

            detector.ObserveAreaTransition(
                Enter(HandId.Left, AirshiftArea.A2, time));

            detector.ObserveAreaTransition(
                Enter(HandId.Right, AirshiftArea.A5, time));

            detector.ObserveMotionSample(
                Motion(HandId.Left, AirshiftArea.A2, time));

            detector.ObserveMotionSample(
                Motion(HandId.Right, AirshiftArea.A5, time));

            Assert.That(gestures.Count, Is.EqualTo(2));

            Assert.That(gestures[0].HandId, Is.EqualTo(HandId.Left));
            Assert.That(gestures[0].Area, Is.EqualTo(AirshiftArea.A2));

            Assert.That(gestures[1].HandId, Is.EqualTo(HandId.Right));
            Assert.That(gestures[1].Area, Is.EqualTo(AirshiftArea.A5));

            Assert.That(
                gestures[0].TriggerType,
                Is.EqualTo(AirshiftTapTriggerType.EntryFlick));

            Assert.That(
                gestures[1].TriggerType,
                Is.EqualTo(AirshiftTapTriggerType.EntryFlick));
        }

        [Test]
        public void SamplingOtherHand_DoesNotDiscardPendingEntry()
        {
            var detector = new AirshiftTapGestureDetector(
                AirshiftTapGestureSettings.Prototype);

            var gestures = new List<TapGesture>();
            detector.GestureDetected += gestures.Add;

            const double time = 20.0;

            detector.ObserveAreaTransition(
                Enter(HandId.Left, AirshiftArea.A3, time));

            // The right hand has no entry transition.
            detector.ObserveMotionSample(
                Motion(HandId.Right, AirshiftArea.None, time));

            // The left-hand entry must still be pending.
            detector.ObserveMotionSample(
                Motion(HandId.Left, AirshiftArea.A3, time));

            Assert.That(gestures.Count, Is.EqualTo(1));
            Assert.That(gestures[0].HandId, Is.EqualTo(HandId.Left));
            Assert.That(gestures[0].Area, Is.EqualTo(AirshiftArea.A3));
            Assert.That(
                gestures[0].TriggerType,
                Is.EqualTo(AirshiftTapTriggerType.EntryFlick));
        }

        [Test]
        public void FastSweep_EmitsAllDistinctAreas()
        {
            var detector = new AirshiftTapGestureDetector(
                AirshiftTapGestureSettings.Prototype);

            var results = new List<TapGesture>();
            detector.GestureDetected += results.Add;

            const double timestamp = 1.0;

            foreach (var area in new[]
            {
                AirshiftArea.A1,
                AirshiftArea.A2,
                AirshiftArea.A3,
                AirshiftArea.A4
            })
            {
                detector.ObserveAreaTransition(
                    new AirshiftAreaTransition(
                        HandId.Left,
                        area,
                        AirshiftAreaTransitionKind.Enter,
                        Vector2.zero,
                        timestamp,
                        AirshiftInputSource.MouseSimulation));
            }

            detector.ObserveMotionSample(
                new AirshiftMotionSample(
                    HandId.Left,
                    Vector2.zero,
                    timestamp,
                    Vector2.right * 5f,
                    AirshiftArea.None,
                    AirshiftArea.None,
                    AirshiftInputSource.MouseSimulation));

            Assert.That(results.Count, Is.EqualTo(4));

            for (int i = 0; i < 4; i++)
            {
                Assert.That(
                    results[i].Area,
                    Is.EqualTo((AirshiftArea)(i + 1)));
            }
        }

        [Test]
        public void SameAreaRepeatedInFrame_EmitsOnlyOnce()
        {
            var detector = new AirshiftTapGestureDetector(
                AirshiftTapGestureSettings.Prototype);

            var results = new List<TapGesture>();
            detector.GestureDetected += results.Add;

            const double timestamp = 1.0;

            for (int i = 0; i < 3; i++)
            {
                detector.ObserveAreaTransition(
                    new AirshiftAreaTransition(
                        HandId.Left,
                        AirshiftArea.A3,
                        AirshiftAreaTransitionKind.Enter,
                        Vector2.zero,
                        timestamp,
                        AirshiftInputSource.MouseSimulation));
            }

            detector.ObserveMotionSample(
                new AirshiftMotionSample(
                    HandId.Left,
                    Vector2.zero,
                    timestamp,
                    Vector2.right * 5f,
                    AirshiftArea.None,
                    AirshiftArea.None,
                    AirshiftInputSource.MouseSimulation));

            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].Area,
                Is.EqualTo(AirshiftArea.A3));
        }
    }
}
