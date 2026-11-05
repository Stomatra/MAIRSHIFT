using System;
using MajdataPlay.Scenes.Airshift;
using NUnit.Framework;

namespace MajdataPlay.Tests.Game.Scenes.Airshift
{
    /// <summary>
    /// WIFI's outer paths accept contact from either physical hand,
    /// even when two independent WIFI notes appear on the same beat.
    /// Completion remains the responsibility of the original three
    /// ordered SLIDE queues, not this stateless contact filter.
    /// </summary>
    public class AirshiftWifiLaneHandPolicyTests
    {
        private static bool Press(int lane, AirshiftSensorArea target,
            AirshiftSensorArea left, AirshiftSensorArea right)
        {
            return AirshiftWifiLaneHandPolicy.IsOuterLanePressed(
                lane, target, left, right);
        }

        [Test]
        public void SharedStartAcceptsEitherHand()
        {
            Assert.That(Press(0, AirshiftSensorArea.A1,
                AirshiftSensorArea.A1, AirshiftSensorArea.None), Is.True);
            Assert.That(Press(2, AirshiftSensorArea.A1,
                AirshiftSensorArea.None, AirshiftSensorArea.A1), Is.True);
            Assert.That(Press(0, AirshiftSensorArea.A1,
                AirshiftSensorArea.A1, AirshiftSensorArea.A1), Is.True);
        }

        [TestCase(0)]
        [TestCase(2)]
        public void EachOuterLaneAcceptsEitherHand(int lane)
        {
            Assert.That(Press(lane, AirshiftSensorArea.B8,
                AirshiftSensorArea.B8, AirshiftSensorArea.None), Is.True);
            Assert.That(Press(lane, AirshiftSensorArea.B8,
                AirshiftSensorArea.None, AirshiftSensorArea.B8), Is.True);
        }

        [Test]
        public void OneHandCanVisitBothOuterLanesSequentially()
        {
            Assert.That(Press(0, AirshiftSensorArea.B8,
                AirshiftSensorArea.B8, AirshiftSensorArea.None), Is.True);
            Assert.That(Press(0, AirshiftSensorArea.B7,
                AirshiftSensorArea.B7, AirshiftSensorArea.None), Is.True);
            Assert.That(Press(2, AirshiftSensorArea.B2,
                AirshiftSensorArea.B2, AirshiftSensorArea.None), Is.True);
            Assert.That(Press(2, AirshiftSensorArea.B3,
                AirshiftSensorArea.B3, AirshiftSensorArea.None), Is.True);
        }

        [Test]
        public void EitherHandCanTakeOverAnOuterLaneMidPath()
        {
            Assert.That(Press(2, AirshiftSensorArea.B2,
                AirshiftSensorArea.B2, AirshiftSensorArea.None), Is.True);
            Assert.That(Press(2, AirshiftSensorArea.B3,
                AirshiftSensorArea.None, AirshiftSensorArea.B3), Is.True);
            Assert.That(Press(0, AirshiftSensorArea.B8,
                AirshiftSensorArea.None, AirshiftSensorArea.B8), Is.True);
        }

        [Test]
        public void TwoSimultaneousWifiNotesHaveNoGlobalHandClaims()
        {
            // WIFI at A1: hand L traces left outer branch; hand R
            // remains available for a second WIFI at A5.
            Assert.That(Press(0, AirshiftSensorArea.B8,
                AirshiftSensorArea.B8, AirshiftSensorArea.None), Is.True);
            Assert.That(Press(2, AirshiftSensorArea.B6,
                AirshiftSensorArea.B8, AirshiftSensorArea.B6), Is.True);
            // Reverse lanes or hands without releasing any ownership.
            Assert.That(Press(2, AirshiftSensorArea.B2,
                AirshiftSensorArea.B2, AirshiftSensorArea.B6), Is.True);
            Assert.That(Press(0, AirshiftSensorArea.B4,
                AirshiftSensorArea.B2, AirshiftSensorArea.B4), Is.True);
        }

        [Test]
        public void AnUnoccupiedSensorDoesNotAdvanceEitherLane()
        {
            Assert.That(Press(0, AirshiftSensorArea.B8,
                AirshiftSensorArea.B7, AirshiftSensorArea.B3), Is.False);
            Assert.That(Press(2, AirshiftSensorArea.B2,
                AirshiftSensorArea.None, AirshiftSensorArea.None), Is.False);
            Assert.That(Press(0, AirshiftSensorArea.None,
                AirshiftSensorArea.None, AirshiftSensorArea.None), Is.False);
        }

        [Test]
        public void BothHandsInSameAreaAreValidContact()
        {
            Assert.That(Press(0, AirshiftSensorArea.B8,
                AirshiftSensorArea.B8, AirshiftSensorArea.B8), Is.True);
            Assert.That(Press(2, AirshiftSensorArea.B2,
                AirshiftSensorArea.B2, AirshiftSensorArea.B2), Is.True);
        }

        [Test]
        public void OnlyOuterLaneIndicesAreAccepted()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                Press(1, AirshiftSensorArea.C,
                    AirshiftSensorArea.C, AirshiftSensorArea.None));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                Press(-1, AirshiftSensorArea.B8,
                    AirshiftSensorArea.B8, AirshiftSensorArea.None));
        }
    }
}
