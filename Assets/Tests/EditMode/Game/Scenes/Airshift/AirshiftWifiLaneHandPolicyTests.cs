using System;
using MajdataPlay.Scenes.Airshift;
using NUnit.Framework;

namespace MajdataPlay.Tests.Game.Scenes.Airshift
{
    /// <summary>AIRSHIFT WIFI outer lane ownership test suite.</summary>
    public class AirshiftWifiLaneHandPolicyTests
    {
        private AirshiftWifiLaneHandPolicy _policy = null!;

        [SetUp]
        public void SetUp() => _policy = new AirshiftWifiLaneHandPolicy();

        private bool Press(int lane, AirshiftSensorArea target,
            AirshiftSensorArea left, AirshiftSensorArea right)
        {
            return _policy.IsOuterLanePressed(
                lane, target, AirshiftSensorArea.A1, left, right);
        }

        [Test]
        public void SharedStartDoesNotClaimEitherOuterTrack()
        {
            Assert.That(Press(0, AirshiftSensorArea.A1, AirshiftSensorArea.A1, AirshiftSensorArea.A1), Is.True);
            Assert.That(Press(2, AirshiftSensorArea.A1, AirshiftSensorArea.A1, AirshiftSensorArea.A1), Is.True);
            Assert.That(_policy.Lane0Owner, Is.Null);
            Assert.That(_policy.Lane2Owner, Is.Null);
        }

        [Test]
        public void EachHandCanAdvanceItsOwnedTrack()
        {
            Assert.That(Press(0, AirshiftSensorArea.B8, AirshiftSensorArea.B8, AirshiftSensorArea.None), Is.True);
            Assert.That(Press(2, AirshiftSensorArea.B2, AirshiftSensorArea.B8, AirshiftSensorArea.B2), Is.True);
            Assert.That(_policy.Lane0Owner, Is.EqualTo(HandId.Left));
            Assert.That(_policy.Lane2Owner, Is.EqualTo(HandId.Right));
            Assert.That(Press(0, AirshiftSensorArea.B7, AirshiftSensorArea.B7, AirshiftSensorArea.B3), Is.True);
            Assert.That(Press(2, AirshiftSensorArea.B3, AirshiftSensorArea.B7, AirshiftSensorArea.B3), Is.True);
        }

        [Test]
        public void OneHandCannotClaimBothOuterTracks()
        {
            Assert.That(Press(0, AirshiftSensorArea.B8, AirshiftSensorArea.B8, AirshiftSensorArea.None), Is.True);
            Assert.That(Press(2, AirshiftSensorArea.B2, AirshiftSensorArea.B2, AirshiftSensorArea.None), Is.False);
            Assert.That(_policy.Lane2Owner, Is.Null);
            Assert.That(Press(2, AirshiftSensorArea.B2, AirshiftSensorArea.None, AirshiftSensorArea.B2), Is.True);
            Assert.That(Press(0, AirshiftSensorArea.B7, AirshiftSensorArea.None, AirshiftSensorArea.B7), Is.False);
        }

        [Test]
        public void SwappedPhysicalHandOrientationWorks()
        {
            Assert.That(Press(2, AirshiftSensorArea.B2, AirshiftSensorArea.B2, AirshiftSensorArea.None), Is.True);
            Assert.That(Press(0, AirshiftSensorArea.B8, AirshiftSensorArea.None, AirshiftSensorArea.B8), Is.True);
            Assert.That(_policy.Lane0Owner, Is.EqualTo(HandId.Right));
            Assert.That(_policy.Lane2Owner, Is.EqualTo(HandId.Left));
        }

        [Test]
        public void SimultaneousIdenticalSensorDoesNotClaim()
        {
            Assert.That(Press(0, AirshiftSensorArea.B8, AirshiftSensorArea.B8, AirshiftSensorArea.B8), Is.False);
            Assert.That(_policy.Lane0Owner, Is.Null);
            Assert.That(Press(0, AirshiftSensorArea.B8, AirshiftSensorArea.B8, AirshiftSensorArea.None), Is.True);
        }

        [Test]
        public void ResetLetsTheNextWifiChooseAgain()
        {
            Assert.That(Press(0, AirshiftSensorArea.B8, AirshiftSensorArea.B8, AirshiftSensorArea.None), Is.True);
            _policy.Reset();
            Assert.That(_policy.Lane0Owner, Is.Null);
            Assert.That(Press(0, AirshiftSensorArea.B8, AirshiftSensorArea.None, AirshiftSensorArea.B8), Is.True);
            Assert.That(_policy.Lane0Owner, Is.EqualTo(HandId.Right));
        }

        [Test]
        public void RejectInvalidOuterLaneIndex()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                Press(1, AirshiftSensorArea.C, AirshiftSensorArea.C, AirshiftSensorArea.None));
        }
    }
}
