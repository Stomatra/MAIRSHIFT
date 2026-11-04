
using MajdataPlay.Scenes.Airshift;
using NUnit.Framework;
using UnityEngine;

namespace MajdataPlay.Tests.EditMode.Game.Scenes.Airshift
{
    [TestFixture]
    public class AirshiftInputBridgeTests
    {
        private static TapGesture CreateGesture(
            long actionId,
            HandId hand,
            AirshiftArea area)
        {
            return new TapGesture(
                actionId,
                hand,
                area,
                1.0d,
                AirshiftTapTriggerType.EntryFlick,
                2.0f,
                Vector2.right,
                AirshiftInputSource.MouseSimulation
            );
        }

        [Test]
        public void BothHands_CanBeConsumedIndependently()
        {
            var bridge = new AirshiftInputBridge();

            bridge.BeginFrame();

            Assert.That(bridge.Enqueue(
                CreateGesture(1, HandId.Left, AirshiftArea.A3)),
                Is.True);

            Assert.That(bridge.Enqueue(
                CreateGesture(2, HandId.Right, AirshiftArea.A3)),
                Is.True);

            Assert.That(bridge.PendingCount, Is.EqualTo(2));

            // Consume the right hand first.
            Assert.That(bridge.TryConsume(
                AirshiftArea.A3,
                HandId.Right,
                out var right), Is.True);

            Assert.That(right.ActionId, Is.EqualTo(2L));
            Assert.That(bridge.PendingCount, Is.EqualTo(1));

            // The left-hand action must still exist.
            Assert.That(bridge.TryConsume(
                AirshiftArea.A3,
                HandId.Left,
                out var left), Is.True);

            Assert.That(left.ActionId, Is.EqualTo(1L));
            Assert.That(bridge.PendingCount, Is.Zero);
        }

        [Test]
        public void OneGesture_CannotBeConsumedTwice()
        {
            var bridge = new AirshiftInputBridge();

            bridge.BeginFrame();
            bridge.Enqueue(CreateGesture(
                1, HandId.Left, AirshiftArea.A1));

            Assert.That(bridge.TryConsume(
                AirshiftArea.A1, null, out _), Is.True);

            Assert.That(bridge.TryConsume(
                AirshiftArea.A1, null, out _), Is.False);
        }

        [Test]
        public void WrongHandOrArea_DoesNotConsumeGesture()
        {
            var bridge = new AirshiftInputBridge();

            bridge.BeginFrame();
            bridge.Enqueue(CreateGesture(
                1, HandId.Left, AirshiftArea.A5));

            Assert.That(bridge.TryConsume(
                AirshiftArea.A5,
                HandId.Right,
                out _), Is.False);

            Assert.That(bridge.TryConsume(
                AirshiftArea.A6,
                HandId.Left,
                out _), Is.False);

            Assert.That(bridge.PendingCount, Is.EqualTo(1));

            Assert.That(bridge.TryConsume(
                AirshiftArea.A5,
                HandId.Left,
                out _), Is.True);
        }

        [Test]
        public void BeginFrame_DiscardsPreviousFrameActions()
        {
            var bridge = new AirshiftInputBridge();

            bridge.BeginFrame();
            bridge.Enqueue(CreateGesture(
                1, HandId.Left, AirshiftArea.A2));

            Assert.That(bridge.PendingCount, Is.EqualTo(1));

            bridge.BeginFrame();

            Assert.That(bridge.PendingCount, Is.Zero);

            Assert.That(bridge.TryConsume(
                AirshiftArea.A2, null, out _), Is.False);
        }

        [Test]
        public void DuplicateAndOutOfOrderActions_AreRejected()
        {
            var bridge = new AirshiftInputBridge();

            bridge.BeginFrame();

            Assert.That(bridge.Enqueue(
                CreateGesture(10, HandId.Left, AirshiftArea.A1)),
                Is.True);

            Assert.That(bridge.Enqueue(
                CreateGesture(10, HandId.Left, AirshiftArea.A1)),
                Is.False);

            Assert.That(bridge.Enqueue(
                CreateGesture(9, HandId.Right, AirshiftArea.A2)),
                Is.False);

            Assert.That(bridge.Enqueue(
                CreateGesture(11, HandId.Right, AirshiftArea.A2)),
                Is.True);

            Assert.That(bridge.PendingCount, Is.EqualTo(2));
        }

        [Test]
        public void InvalidArea_IsRejected()
        {
            var bridge = new AirshiftInputBridge();

            bridge.BeginFrame();

            Assert.That(bridge.Enqueue(
                CreateGesture(1, HandId.Left, AirshiftArea.None)),
                Is.False);

            Assert.That(bridge.PendingCount, Is.Zero);

            Assert.That(bridge.Enqueue(
                CreateGesture(2, HandId.Left, AirshiftArea.A8)),
                Is.True);
        }

        [Test]
        public void Clear_DropsActionsButPreservesIdHistory()
        {
            var bridge = new AirshiftInputBridge();

            bridge.BeginFrame();
            bridge.Enqueue(CreateGesture(
                10, HandId.Left, AirshiftArea.A1));

            bridge.Clear();

            Assert.That(bridge.PendingCount, Is.Zero);

            Assert.That(bridge.Enqueue(
                CreateGesture(10, HandId.Left, AirshiftArea.A1)),
                Is.False);

            Assert.That(bridge.Enqueue(
                CreateGesture(11, HandId.Left, AirshiftArea.A1)),
                Is.True);
        }
    }
}
