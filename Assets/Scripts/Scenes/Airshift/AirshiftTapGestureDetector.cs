using System;
using UnityEngine;

#nullable enable
namespace MajdataPlay.Scenes.Airshift
{
    /// <summary>
    /// Converts source-independent AIRSHIFT motion and area transitions into no-click TAP gestures.
    /// </summary>
    internal sealed class AirshiftTapGestureDetector
    {
        /// <summary>The maximum number of entry transitions produced by one tracked segment.</summary>
        private const int PendingEntryCapacity = 32;
        /// <summary>The adjustable normalized motion thresholds.</summary>
        private readonly AirshiftTapGestureSettings _settings;
        /// <summary>The per-hand hysteresis and direction state.</summary>
        private readonly HandGestureState[] _handStates =
        {
            new HandGestureState(),
            new HandGestureState(),
        };
        /// <summary>The entry transitions waiting for their same-sample velocity.</summary>
        private readonly AirshiftAreaTransition[] _pendingEntries = new AirshiftAreaTransition[PendingEntryCapacity];
        
        // Each outer area may emit at most one TAP per Unity frame.
        private readonly int[] _lastTapFrameByArea =
        {
            -1, -1, -1, -1, -1, -1, -1, -1
        };

        // EditMode tests do not advance Unity's game frame counter.
        // Use motion timestamps as logical frame identifiers there.
        private readonly double[] _lastTapTimeByArea =
        {
            double.NegativeInfinity,
            double.NegativeInfinity,
            double.NegativeInfinity,
            double.NegativeInfinity,
            double.NegativeInfinity,
            double.NegativeInfinity,
            double.NegativeInfinity,
            double.NegativeInfinity
        };

        /// <summary>The number of pending entry transitions.</summary>
        private int _pendingEntryCount;
        /// <summary>The next process-local action identifier.</summary>
        private long _nextActionId = 1;

        /// <summary>Raised once for every independent recognized TAP action.</summary>
        internal event Action<TapGesture>? GestureDetected;

        /// <summary>
        /// Creates a gesture detector with adjustable normalized thresholds.
        /// </summary>
        /// <param name="settings">The prototype motion thresholds.</param>
        internal AirshiftTapGestureDetector(AirshiftTapGestureSettings settings)
        {
            _settings = settings;
        }

        /// <summary>
        /// Caches an entered area until the tracker publishes its same-timestamp velocity sample.
        /// </summary>
        /// <param name="transition">The continuous area transition.</param>
        internal void ObserveAreaTransition(AirshiftAreaTransition transition)
        {
            if (transition.Kind != AirshiftAreaTransitionKind.Enter || !IsOuterArea(transition.Area))
            {
                return;
            }
            if (_pendingEntryCount < _pendingEntries.Length)
            {
                _pendingEntries[_pendingEntryCount++] = transition;
            }
        }

        /// <summary>
        /// Evaluates one real motion sample for entry and armed in-area gestures.
        /// </summary>
        /// <param name="sample">The source-independent hand motion sample.</param>
        internal void ObserveMotionSample(AirshiftMotionSample sample)
        {
            var state = _handStates[(int)sample.HandId];
            var emitted = TryEmitEntryGesture(sample, state);
            DiscardPendingEntriesForHand(sample.HandId);

            if (!emitted)
            {
                TryEmitInAreaGesture(sample, state);
            }
            if (sample.Speed > state.PeakSpeed)
            {
                state.PeakSpeed = sample.Speed;
            }
        }

        /// <summary>
        /// Clears pending and hysteresis state after a non-motion discontinuity.
        /// </summary>
        internal void Reset()
        {
            _pendingEntryCount = 0;
            ResetHandState(_handStates[(int)HandId.Left]);
            ResetHandState(_handStates[(int)HandId.Right]);
        }

        /// <summary>
        /// Discards pending entries belonging only to the sampled hand.
        /// Entries for the other hand must remain available.
        /// </summary>
        private void DiscardPendingEntriesForHand(HandId handId)
        {
            var remainingCount = 0;

            for (var i = 0; i < _pendingEntryCount; i++)
            {
                var entry = _pendingEntries[i];

                if (entry.HandId == handId)
                {
                    continue;
                }

                _pendingEntries[remainingCount] = entry;
                remainingCount++;
            }

            _pendingEntryCount = remainingCount;
        }

        /// <summary>
        /// Emits at most one entry gesture for the current source motion.
        /// </summary>
        /// <param name="sample">The velocity sample corresponding to pending entries.</param>
        /// <param name="state">The originating hand state.</param>
        /// <returns>Whether an entry gesture was emitted.</returns>
        private bool TryEmitEntryGesture(
            AirshiftMotionSample sample,
            HandGestureState state)
        {
            if (sample.Speed < _settings.EntrySpeed)
            {
                return false;
            }

            bool emittedAny = false;

            // Process every entered region, not only the first.
            for (int i = 0; i < _pendingEntryCount; i++)
            {
                var entry = _pendingEntries[i];

                if (entry.HandId != sample.HandId ||
                    Math.Abs(entry.Timestamp - sample.Timestamp) > 0.000001d)
                {
                    continue;
                }

                if (TryEmitGesture(
                    sample,
                    entry.Area,
                    AirshiftTapTriggerType.EntryFlick,
                    state))
                {
                    emittedAny = true;
                }
            }

            return emittedAny;
        }

        /// <summary>
        /// Applies peak, hysteresis, and direction-reversal gates to an in-area velocity burst.
        /// </summary>
        /// <param name="sample">The current motion sample.</param>
        /// <param name="state">The originating hand state.</param>
        private void TryEmitInAreaGesture(AirshiftMotionSample sample, HandGestureState state)
        {
            if (sample.Speed <= _settings.RearmSpeed)
            {
                state.IsArmed = true;
                state.PeakSpeed = 0f;
                return;
            }
            if (!IsOuterArea(sample.CurrentArea) || sample.Speed < _settings.StrikeSpeed || !CanEmit(sample.Timestamp, state))
            {
                return;
            }

            var isRisingPeak = state.IsArmed && sample.Speed > state.PeakSpeed;
            var isReversal = !state.IsArmed &&
                state.LastGestureDirection.sqrMagnitude > 0.0001f &&
                sample.Direction.sqrMagnitude > 0.0001f &&
                Vector2.Dot(state.LastGestureDirection, sample.Direction) <= _settings.ReverseDirectionDot;
            if (!isRisingPeak && !isReversal)
            {
                return;
            }
            TryEmitGesture(sample, sample.CurrentArea, AirshiftTapTriggerType.InAreaStrike, state);
        }

        /// <summary>
        /// Publishes one unique action and disarms the current velocity burst.
        /// </summary>
        /// <param name="sample">The source motion sample.</param>
        /// <param name="area">The gesture target area.</param>
        /// <param name="triggerType">The gesture recognition path.</param>
        /// <param name="state">The originating hand state.</param>
        private bool TryEmitGesture(
            AirshiftMotionSample sample,
            AirshiftArea area,
            AirshiftTapTriggerType triggerType,
            HandGestureState state)
        {
            int index = (int)area - 1;

            if (index < 0 || index >= 8)
            {
                return false;
            }

            if (Application.isPlaying)
            {
                // Runtime: strict per-area, per-frame limit.
                int frame = Time.frameCount;

                if (_lastTapFrameByArea[index] == frame)
                {
                    return false;
                }

                _lastTapFrameByArea[index] = frame;
            }
            else
            {
                // EditMode test fallback: a shared sample timestamp
                // represents one simulated frame.
                if (Math.Abs(
                    _lastTapTimeByArea[index] -
                    sample.Timestamp) <= 0.000001d)
                {
                    return false;
                }
            }

            _lastTapTimeByArea[index] = sample.Timestamp;

            var gesture = new TapGesture(
                _nextActionId++,
                sample.HandId,
                area,
                sample.Timestamp,
                triggerType,
                sample.Speed,
                sample.Direction,
                sample.InputSource
            );

            state.IsArmed = false;
            state.PeakSpeed = sample.Speed;
            state.LastGestureTimestamp = sample.Timestamp;
            state.LastGestureDirection = sample.Direction;

            GestureDetected?.Invoke(gesture);
            return true;
        }


        /// <summary>
        /// Checks the per-hand cooldown gate.
        /// </summary>
        /// <param name="timestamp">The candidate timestamp.</param>
        /// <param name="state">The originating hand state.</param>
        /// <returns>Whether a new gesture may be emitted.</returns>
        private bool CanEmit(double timestamp, HandGestureState state)
        {
            return timestamp - state.LastGestureTimestamp >= _settings.MinimumGestureInterval;
        }

        /// <summary>
        /// Checks whether an area is one of A1-A8.
        /// </summary>
        /// <param name="area">The candidate area.</param>
        /// <returns>Whether the area is an outer TAP target.</returns>
        private static bool IsOuterArea(AirshiftArea area)
        {
            var areaValue = (int)area;
            return areaValue >= (int)AirshiftArea.A1 && areaValue <= (int)AirshiftArea.A8;
        }

        /// <summary>
        /// Rearms one hand without creating an input action.
        /// </summary>
        /// <param name="state">The hand state to clear.</param>
        private static void ResetHandState(HandGestureState state)
        {
            state.IsArmed = true;
            state.PeakSpeed = 0f;
            state.LastGestureTimestamp = double.NegativeInfinity;
            state.LastGestureDirection = Vector2.zero;
        }

        /// <summary>
        /// Stores peak and rearm state for one hand independently.
        /// </summary>
        private sealed class HandGestureState
        {
            /// <summary>Whether the next rising velocity burst may trigger.</summary>
            internal bool IsArmed { get; set; } = true;
            /// <summary>The maximum speed observed in the current burst.</summary>
            internal float PeakSpeed { get; set; }
            /// <summary>The last emitted gesture timestamp.</summary>
            internal double LastGestureTimestamp { get; set; } = double.NegativeInfinity;
            /// <summary>The direction of the last emitted gesture.</summary>
            internal Vector2 LastGestureDirection { get; set; }
        }
    }
}
