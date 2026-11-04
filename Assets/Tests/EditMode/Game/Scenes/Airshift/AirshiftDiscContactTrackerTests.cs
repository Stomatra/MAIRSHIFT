
using System.Collections.Generic;
using MajdataPlay.Scenes.Airshift;
using NUnit.Framework;
using UnityEngine;

namespace MajdataPlay.Tests.EditMode.Game.Scenes.Airshift
{
    [TestFixture]
    public class AirshiftDiscContactTrackerTests
    {
        private static Vector2 At(AirshiftArea area, float radius)
        {
            return AirshiftOuterAreaDetector.GetAreaCenter(
                area, radius);
        }

        private static AirshiftMotionSample Sample(
            HandId hand,
            Vector2 position,
            double time)
        {
            return new AirshiftMotionSample(
                hand,
                position,
                time,
                Vector2.up * 5f,
                AirshiftArea.None,
                AirshiftArea.None,
                AirshiftInputSource.MouseSimulation);
        }

        [Test]
        public void EdgeContact_ProducesEnterBeforeCenterEnters()
        {
            var start = At(AirshiftArea.A1, 0.40f);
            var end = At(AirshiftArea.A1, 0.68f);

            var tracker = new AirshiftDiscContactTracker(
                start, Vector2.one * 0.5f, 0.05f);

            var events = new List<AirshiftAreaTransition>();
            tracker.AreaTransitioned += events.Add;

            Assert.That(
                new AirshiftOuterAreaDetector().GetArea(end),
                Is.EqualTo(AirshiftArea.None));

            Assert.That(tracker.Observe(
                Sample(HandId.Left, end, 1.0)), Is.True);

            Assert.That(events, Has.Count.EqualTo(1));
            Assert.That(events[0].Area,
                Is.EqualTo(AirshiftArea.A1));
            Assert.That(events[0].Kind,
                Is.EqualTo(AirshiftAreaTransitionKind.Enter));
        }

        [Test]
        public void StationaryContact_DoesNotRepeatEnter()
        {
            var start = At(AirshiftArea.A1, 0.40f);
            var end = At(AirshiftArea.A1, 0.68f);

            var tracker = new AirshiftDiscContactTracker(
                start, Vector2.one * 0.5f, 0.05f);

            int entries = 0;

            tracker.AreaTransitioned += e =>
            {
                if (e.Kind == AirshiftAreaTransitionKind.Enter)
                    entries++;
            };

            tracker.Observe(Sample(HandId.Left, end, 1.0));
            tracker.Observe(Sample(HandId.Left, end, 2.0));

            Assert.That(entries, Is.EqualTo(1));
        }

        [Test]
        public void TwoHands_KeepIndependentContactStates()
        {
            var tracker = new AirshiftDiscContactTracker(
                At(AirshiftArea.A1, 0.40f),
                At(AirshiftArea.A5, 0.40f),
                0.05f);

            var events = new List<AirshiftAreaTransition>();
            tracker.AreaTransitioned += events.Add;

            tracker.Observe(Sample(
                HandId.Left, At(AirshiftArea.A1, 0.68f), 1.0));

            tracker.Observe(Sample(
                HandId.Right, At(AirshiftArea.A5, 0.68f), 1.0));

            Assert.That(events, Has.Count.EqualTo(2));
            Assert.That(events[0].HandId,
                Is.EqualTo(HandId.Left));
            Assert.That(events[0].Area,
                Is.EqualTo(AirshiftArea.A1));
            Assert.That(events[1].HandId,
                Is.EqualTo(HandId.Right));
            Assert.That(events[1].Area,
                Is.EqualTo(AirshiftArea.A5));
        }

        [Test]
        public void Reset_DoesNotGenerateFakeContact()
        {
            var tracker = new AirshiftDiscContactTracker(
                Vector2.one * 0.5f,
                Vector2.one * 0.5f,
                0.05f);

            int eventCount = 0;
            tracker.AreaTransitioned += _ => eventCount++;

            var inside = At(AirshiftArea.A1, 0.86f);

            tracker.Reset(
                inside,
                Vector2.one * 0.5f,
                0.05f);

            tracker.Observe(
                Sample(HandId.Left, inside, 1.0));

            Assert.That(eventCount, Is.Zero);
        }
    }
}
