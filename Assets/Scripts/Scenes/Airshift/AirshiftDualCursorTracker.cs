using System;
using UnityEngine;

#nullable enable
namespace MajdataPlay.Scenes.Airshift
{
    /// <summary>
    /// Holds one hand's independent cursor and motion state.
    /// </summary>
    internal sealed class AirshiftHandState
    {
        /// <summary>Gets the represented hand.</summary>
        internal HandId HandId { get; }
        /// <summary>Gets the normalized cursor position.</summary>
        internal Vector2 Position { get; set; }
        /// <summary>Gets the normalized cursor velocity.</summary>
        internal Vector2 Velocity { get; set; }
        /// <summary>Gets the current speed.</summary>
        internal float Speed => Velocity.magnitude;
        /// <summary>Gets the current outer area.</summary>
        internal AirshiftArea CurrentArea { get; set; }
        /// <summary>Gets the last valid outer area.</summary>
        internal AirshiftArea PreviousArea { get; set; }
        /// <summary>Gets the last monotonic update timestamp.</summary>
        internal double Timestamp { get; set; }

        /// <summary>
        /// Creates one hand state.
        /// </summary>
        /// <param name="handId">The represented hand.</param>
        /// <param name="position">The initial normalized position.</param>
        /// <param name="timestamp">The initial monotonic timestamp.</param>
        /// <param name="areaDetector">The shared area detector.</param>
        internal AirshiftHandState(
            HandId handId,
            Vector2 position,
            double timestamp,
            IAirshiftAreaDetector areaDetector)
        {
            HandId = handId;
            Position = position;
            Timestamp = timestamp;
            CurrentArea = areaDetector.GetArea(position);
            PreviousArea = CurrentArea == AirshiftArea.None ? AirshiftArea.None : CurrentArea;
        }
    }

    /// <summary>
    /// Tracks two independent AIRSHIFT cursors and converts source deltas into motion and area events.
    /// </summary>
    internal sealed class AirshiftDualCursorTracker
    {
        /// <summary>The maximum number of area changes retained for one sampled segment.</summary>
        private const int TraceBufferSize = 32;
        /// <summary>The shared geometry implementation.</summary>
        private readonly IAirshiftAreaDetector _areaDetector;

        /// <summary>Raised after a real source movement updates one hand.</summary>
        internal event Action<AirshiftMotionSample>? MotionSampled;
        /// <summary>Raised for every entered or exited area found along a continuous segment.</summary>
        internal event Action<AirshiftAreaTransition>? AreaTransitioned;

        /// <summary>Gets the left-hand cursor state.</summary>
        internal AirshiftHandState LeftHand { get; }
        /// <summary>Gets the right-hand cursor state.</summary>
        internal AirshiftHandState RightHand { get; }
        /// <summary>Gets the hand currently controlled by the simulation source.</summary>
        internal HandId ActiveHand { get; private set; } = HandId.Left;

        /// <summary>
        /// Creates an independent dual-cursor tracker.
        /// </summary>
        /// <param name="areaDetector">The geometry implementation shared by all input sources.</param>
        /// <param name="leftPosition">The initial left-hand normalized position.</param>
        /// <param name="rightPosition">The initial right-hand normalized position.</param>
        /// <param name="timestamp">The initial monotonic timestamp.</param>
        internal AirshiftDualCursorTracker(
            IAirshiftAreaDetector areaDetector,
            Vector2 leftPosition,
            Vector2 rightPosition,
            double timestamp)
        {
            _areaDetector = areaDetector ?? throw new ArgumentNullException(nameof(areaDetector));
            LeftHand = new AirshiftHandState(HandId.Left, leftPosition, timestamp, areaDetector);
            RightHand = new AirshiftHandState(HandId.Right, rightPosition, timestamp, areaDetector);
        }

        /// <summary>
        /// Gets one hand's mutable runtime state.
        /// </summary>
        /// <param name="handId">The requested hand.</param>
        /// <returns>The hand state.</returns>
        internal AirshiftHandState GetHand(HandId handId)
        {
            return handId == HandId.Left ? LeftHand : RightHand;
        }

        /// <summary>
        /// Switches the controlled hand without changing either cursor or emitting motion events.
        /// </summary>
        /// <returns>The newly active hand.</returns>
        internal HandId ToggleActiveHand()
        {
            ActiveHand = ActiveHand == HandId.Left ? HandId.Right : HandId.Left;
            return ActiveHand;
        }

        /// <summary>
        /// Applies one normalized relative movement to the active hand.
        /// </summary>
        /// <param name="delta">The normalized relative movement.</param>
        /// <param name="timestamp">The monotonic sample timestamp.</param>
        /// <param name="inputSource">The source producing the movement.</param>
        /// <returns>The resulting motion sample.</returns>
        internal AirshiftMotionSample ApplyActiveDelta(
            Vector2 delta,
            double timestamp,
            AirshiftInputSource inputSource = AirshiftInputSource.MouseSimulation)
        {
            var hand = GetHand(ActiveHand);
            var monotonicTimestamp = Math.Max(timestamp, hand.Timestamp);
            
            var oldPosition = hand.Position;

            // Apply the source delta, then constrain the
            // actual cursor position to the circular playfield.
            var proposedPosition = oldPosition + delta;

            var newPosition =
                AirshiftControlTuning.ClampCursorPosition(
                    proposedPosition
                );

            ProcessAreaTransitions(hand, oldPosition, newPosition, monotonicTimestamp, inputSource);
            var elapsed = monotonicTimestamp - hand.Timestamp;
            hand.Velocity = elapsed > 0.000001d
                ? (newPosition - oldPosition) / (float)elapsed
                : Vector2.zero;
            hand.Position = newPosition;
            hand.Timestamp = monotonicTimestamp;
            var sample = new AirshiftMotionSample(
                hand.HandId,
                hand.Position,
                hand.Timestamp,
                hand.Velocity,
                hand.CurrentArea,
                hand.PreviousArea,
                inputSource);
            MotionSampled?.Invoke(sample);
            return sample;
        }

        /// <summary>
        /// Marks the active hand stationary without emitting a motion or area event.
        /// </summary>
        /// <param name="timestamp">The monotonic update timestamp.</param>
        internal void MarkActiveHandIdle(double timestamp)
        {
            var hand = GetHand(ActiveHand);
            hand.Timestamp = Math.Max(timestamp, hand.Timestamp);
            hand.Velocity = Vector2.zero;
        }

        /// <summary>
        /// Re-centers the active cursor without interpreting the discontinuity as motion.
        /// </summary>
        /// <param name="timestamp">The monotonic reset timestamp.</param>
        internal void RecenterActiveHand(double timestamp)
        {
            ResetHandWithoutEvents(GetHand(ActiveHand), new Vector2(0.5f, 0.5f), timestamp);
        }

        /// <summary>
        /// Resets velocity baselines after focus, window, device, or source discontinuities.
        /// </summary>
        /// <param name="timestamp">The monotonic reset timestamp.</param>
        internal void ResetMotionBaselines(double timestamp)
        {
            ResetVelocity(LeftHand, timestamp);
            ResetVelocity(RightHand, timestamp);
        }

        /// <summary>
        /// Processes all area changes sampled along a continuous segment.
        /// </summary>
        /// <param name="hand">The hand being moved.</param>
        /// <param name="from">The segment start.</param>
        /// <param name="to">The segment end.</param>
        /// <param name="timestamp">The monotonic sample timestamp.</param>
        /// <param name="inputSource">The originating input source.</param>
        private void ProcessAreaTransitions(
            AirshiftHandState hand,
            Vector2 from,
            Vector2 to,
            double timestamp,
            AirshiftInputSource inputSource)
        {
            Span<AirshiftAreaTracePoint> tracePoints = stackalloc AirshiftAreaTracePoint[TraceBufferSize];
            var count = _areaDetector.TraceSegment(from, to, tracePoints);
            var currentArea = hand.CurrentArea;
            for (var i = 0; i < count; i++)
            {
                var tracePoint = tracePoints[i];
                var nextArea = tracePoint.Area;
                if (currentArea == nextArea)
                {
                    continue;
                }
                if (currentArea != AirshiftArea.None)
                {
                    hand.PreviousArea = currentArea;
                    AreaTransitioned?.Invoke(new AirshiftAreaTransition(
                        hand.HandId,
                        currentArea,
                        AirshiftAreaTransitionKind.Exit,
                        tracePoint.Position,
                        timestamp,
                        inputSource));
                }
                if (nextArea != AirshiftArea.None)
                {
                    AreaTransitioned?.Invoke(new AirshiftAreaTransition(
                        hand.HandId,
                        nextArea,
                        AirshiftAreaTransitionKind.Enter,
                        tracePoint.Position,
                        timestamp,
                        inputSource));
                }
                currentArea = nextArea;
            }
            hand.CurrentArea = currentArea;
        }

        /// <summary>
        /// Resets one hand to a position without creating motion or area transitions.
        /// </summary>
        /// <param name="hand">The hand to reset.</param>
        /// <param name="position">The new normalized position.</param>
        /// <param name="timestamp">The monotonic reset timestamp.</param>
        private void ResetHandWithoutEvents(AirshiftHandState hand, Vector2 position, double timestamp)
        {
            if (hand.CurrentArea != AirshiftArea.None)
            {
                hand.PreviousArea = hand.CurrentArea;
            }
            hand.Position = position;
            hand.CurrentArea = _areaDetector.GetArea(position);
            ResetVelocity(hand, timestamp);
        }

        /// <summary>
        /// Clears one hand's velocity while preserving its position and area history.
        /// </summary>
        /// <param name="hand">The hand to reset.</param>
        /// <param name="timestamp">The monotonic reset timestamp.</param>
        private static void ResetVelocity(AirshiftHandState hand, double timestamp)
        {
            hand.Velocity = Vector2.zero;
            hand.Timestamp = Math.Max(timestamp, hand.Timestamp);
        }
    }
}
