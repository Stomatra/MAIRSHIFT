
using MajdataPlay.Scenes.Airshift;
using NUnit.Framework;
using UnityEngine;

namespace MajdataPlay.Tests.EditMode.Game.Scenes.Airshift
{
    [TestFixture]
    public class AirshiftOuterDiscContactTests
    {
        private static int Bit(AirshiftArea area)
        {
            return 1 << ((int)area - 1);
        }

        [Test]
        public void ZeroRadius_MatchesPointDetectionAtAreaCenters()
        {
            var detector = new AirshiftOuterAreaDetector();

            for (int i = 1; i <= 8; i++)
            {
                var area = (AirshiftArea)i;
                var position =
                    AirshiftOuterAreaDetector.GetAreaCenter(area);

                Assert.That(detector.GetArea(position),
                    Is.EqualTo(area));

                Assert.That(detector.GetContactMask(position, 0f),
                    Is.EqualTo(Bit(area)));
            }
        }

        [Test]
        public void DiscEdge_ContactsBeforeCenterEntersOuterRing()
        {
            var detector = new AirshiftOuterAreaDetector();

            // Normalized radial position 0.63
            // corresponds to an actual distance of 0.315.
            // The ring begins at 0.36.
            var position =
                AirshiftOuterAreaDetector.GetAreaCenter(
                    AirshiftArea.A1, 0.63f);

            Assert.That(detector.GetArea(position),
                Is.EqualTo(AirshiftArea.None));

            Assert.That(
                detector.GetContactMask(position, 0.05f)
                & Bit(AirshiftArea.A1),
                Is.Not.EqualTo(0));
        }

        [Test]
        public void AngularGap_RespectsDiscRadius()
        {
            var detector = new AirshiftOuterAreaDetector();

            // Halfway between A1 (67.5 degrees)
            // and A2 (22.5 degrees).
            float angle = 45f * Mathf.Deg2Rad;
            float distance = 0.43f;

            var position = new Vector2(
                0.5f + Mathf.Cos(angle) * distance,
                0.5f + Mathf.Sin(angle) * distance);

            Assert.That(detector.GetArea(position),
                Is.EqualTo(AirshiftArea.None));

            // Small disc fits inside the angular gap.
            Assert.That(detector.GetContactMask(position, 0.05f),
                Is.EqualTo(0));

            // Larger disc touches both neighbouring areas.
            int mask = detector.GetContactMask(position, 0.09f);

            Assert.That(mask & Bit(AirshiftArea.A1),
                Is.Not.EqualTo(0));

            Assert.That(mask & Bit(AirshiftArea.A2),
                Is.Not.EqualTo(0));
        }

        [Test]
        public void DiscAtCenter_DoesNotTouchOuterAreas()
        {
            var detector = new AirshiftOuterAreaDetector();

            Assert.That(
                detector.GetContactMask(
                    new Vector2(0.5f, 0.5f),
                    0.05f),
                Is.EqualTo(0));
        }

        [Test]
        public void FastPassThrough_DetectsIntermediateContact()
        {
            var detector = new AirshiftOuterAreaDetector();

            Vector2 MakePosition(float degrees)
            {
                float angle = degrees * Mathf.Deg2Rad;

                return new Vector2(
                    0.5f + Mathf.Cos(angle) * 0.45f,
                    0.5f + Mathf.Sin(angle) * 0.45f
                );
            }

            var from = MakePosition(87.5f);
            var to = MakePosition(47.5f);

            const float radius = 0.015f;

            Assert.That(
                detector.GetContactMask(from, radius),
                Is.Zero
            );

            Assert.That(
                detector.GetContactMask(to, radius),
                Is.Zero
            );

            var points =
                new AirshiftDiscContactTracePoint[32];

            int count = detector.TraceDiscContacts(
                from, to, radius, points
            );

            Assert.That(count, Is.EqualTo(2));

            // First transition: enters A1.
            Assert.That(
                points[0].ContactMask & 1,
                Is.Not.EqualTo(0)
            );

            // Second transition: exits A1.
            Assert.That(
                points[1].ContactMask & 1,
                Is.Zero
            );
        }

        [Test]
        public void StationaryCursor_ProducesNoTransitions()
        {
            var detector = new AirshiftOuterAreaDetector();

            var position =
                AirshiftOuterAreaDetector.GetAreaCenter(
                    AirshiftArea.A3
                );

            var points =
                new AirshiftDiscContactTracePoint[32];

            int count = detector.TraceDiscContacts(
                position,
                position,
                0.05f,
                points
            );

            Assert.That(count, Is.Zero);
        }

        [Test]
        public void Trace_PreservesSimultaneousContactMask()
        {
            var detector = new AirshiftOuterAreaDetector();

            float angle = 45f * Mathf.Deg2Rad;

            var from = new Vector2(0.5f, 0.5f);

            var to = new Vector2(
                0.5f + Mathf.Cos(angle) * 0.43f,
                0.5f + Mathf.Sin(angle) * 0.43f
            );

            const float radius = 0.09f;

            var points =
                new AirshiftDiscContactTracePoint[32];

            int count = detector.TraceDiscContacts(
                from, to, radius, points
            );

            Assert.That(count, Is.GreaterThan(0));

            int finalMask = points[count - 1].ContactMask;

            // A1 and A2 must both be contacted.
            Assert.That(finalMask & 1, Is.Not.EqualTo(0));
            Assert.That(finalMask & 2, Is.Not.EqualTo(0));
        }
    }
}
