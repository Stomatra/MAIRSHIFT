using MajdataPlay.Scenes.Airshift;
using NUnit.Framework;
using UnityEngine;

#nullable enable
namespace MajdataPlay.Tests.Game.Scenes.Airshift
{
    /// <summary>Checks the shared camera-input boundary without substituting a camera or SDK.</summary>
    public sealed class AirshiftCameraInputTests
    {
        /// <summary>Preserves the tuning value owned by other fixtures.</summary>
        private float _previousRadius;

        /// <summary>Uses the v0.4 footprint for the camera scenarios.</summary>
        [SetUp]
        public void SetUp()
        {
            _previousRadius = AirshiftControlTuning.CursorRadiusNormalized;
            AirshiftControlTuning.CursorRadiusNormalized = 0.125f;
        }

        /// <summary>Restores shared tuning after every scenario.</summary>
        [TearDown]
        public void TearDown()
        {
            AirshiftControlTuning.CursorRadiusNormalized = _previousRadius;
        }

        /// <summary>Camera samples retain hand/source identity without changing the user's selected hand.</summary>
        [Test]
        public void AbsoluteCameraSample_PreservesOtherHandAndActiveSelection()
        {
            var tracker = new AirshiftDualCursorTracker(new AirshiftOuterAreaDetector(),
                new Vector2(0.38f, 0.5f), new Vector2(0.62f, 0.5f), 0d);
            tracker.ToggleActiveHand();
            var selected = tracker.ActiveHand;
            var rightBefore = tracker.RightHand.Position;
            AirshiftMotionSample? lastSample = null;
            tracker.MotionSampled += sample => lastSample = sample;

            tracker.ApplyHandPosition(HandId.Left, new Vector2(0.6f, 0.6f), 1d,
                AirshiftInputSource.CameraTracking);

            Assert.That(tracker.ActiveHand, Is.EqualTo(selected));
            Assert.That(tracker.RightHand.Position, Is.EqualTo(rightBefore));
            Assert.That(tracker.LeftHand.Position, Is.EqualTo(new Vector2(0.6f, 0.6f)));
            Assert.That(lastSample.HasValue, Is.True);
            Assert.That(lastSample!.Value.HandId, Is.EqualTo(HandId.Left));
            Assert.That(lastSample.Value.InputSource, Is.EqualTo(AirshiftInputSource.CameraTracking));
        }

        /// <summary>Reacquisition and loss reset motion without publishing phantom movement samples.</summary>
        [Test]
        public void SnapAndIdle_DoNotEmitMotionOrRetainVelocity()
        {
            var tracker = new AirshiftDualCursorTracker(new AirshiftOuterAreaDetector(),
                new Vector2(0.38f, 0.5f), new Vector2(0.62f, 0.5f), 0d);
            tracker.ApplyHandPosition(HandId.Left, new Vector2(0.6f, 0.6f), 1d,
                AirshiftInputSource.CameraTracking);
            var samples = 0;
            tracker.MotionSampled += _ => samples++;

            tracker.MarkHandIdle(HandId.Left, 2d);
            tracker.SnapHandPosition(HandId.Left, Vector2.one, 3d);

            Assert.That(samples, Is.Zero);
            Assert.That(tracker.LeftHand.Velocity, Is.EqualTo(Vector2.zero));
            Assert.That(tracker.LeftHand.Timestamp, Is.EqualTo(3d));
            Assert.That(Vector2.Distance(tracker.LeftHand.Position, Vector2.one * 0.5f),
                Is.LessThanOrEqualTo(0.37501f));
        }

        /// <summary>The enlarged footprint contacts the outer ring while the other hand stays independent.</summary>
        [Test]
        public void PerHandDiscContact_DoesNotBorrowTheOtherHandContact()
        {
            var detector = new AirshiftOuterAreaDetector();
            var angle = 67.5f * Mathf.Deg2Rad;
            var position = Vector2.one * 0.5f +
                new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 0.375f;
            var tracker = new AirshiftDiscContactTracker(position, Vector2.one * 0.5f, 0.125f);
            var mask = detector.GetContactMask(position, 0.125f);
            Assert.That(mask, Is.Not.Zero);

            for (var area = 1; area <= 8; area++)
            {
                Assert.That(tracker.IsAreaContacted(area, HandId.Left),
                    Is.EqualTo((mask & (1 << (area - 1))) != 0));
                Assert.That(tracker.IsAreaContacted(area, HandId.Right), Is.False);
            }
            Assert.That(tracker.IsAreaContacted(0, HandId.Left), Is.False);
            Assert.That(tracker.IsAreaContacted(9, HandId.Right), Is.False);
        }
    }
}
