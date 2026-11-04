
using System.Collections.Generic;

#nullable enable

namespace MajdataPlay.Scenes.Airshift
{
    /// <summary>
    /// Buffers AIRSHIFT TAP gestures and allows notes
    /// to consume matching actions once per frame.
    /// </summary>
    internal sealed class AirshiftInputBridge
    {
        private readonly List<TapGesture> _pendingGestures = new();

        // Action IDs are generated monotonically by
        // AirshiftTapGestureDetector.
        private long _lastReceivedActionId;

        /// <summary>
        /// Number of unconsumed actions in the current frame.
        /// </summary>
        internal int PendingCount => _pendingGestures.Count;

        /// <summary>
        /// Starts a new input frame.
        /// Old unconsumed actions must not hit later notes.
        /// </summary>
        internal void BeginFrame()
        {
            _pendingGestures.Clear();
        }

        /// <summary>
        /// Receives a recognized gesture.
        /// Duplicate or outdated actions are rejected.
        /// </summary>
        internal bool Enqueue(TapGesture gesture)
        {
            if (gesture.ActionId <= _lastReceivedActionId)
            {
                return false;
            }

            _lastReceivedActionId = gesture.ActionId;

            if (gesture.Area < AirshiftArea.A1 ||
                gesture.Area > AirshiftArea.A8)
            {
                return false;
            }

            _pendingGestures.Add(gesture);
            return true;
        }

        /// <summary>
        /// Consumes one matching TAP action.
        /// requiredHand = null accepts either hand.
        /// </summary>
        internal bool TryConsume(
            AirshiftArea area,
            HandId? requiredHand,
            out TapGesture gesture)
        {
            for (int i = 0; i < _pendingGestures.Count; i++)
            {
                var candidate = _pendingGestures[i];

                if (candidate.Area != area)
                {
                    continue;
                }

                if (requiredHand.HasValue &&
                    candidate.HandId != requiredHand.Value)
                {
                    continue;
                }

                gesture = candidate;

                // Remove the action immediately so
                // another note cannot consume it again.
                _pendingGestures.RemoveAt(i);

                return true;
            }

            gesture = default;
            return false;
        }

        /// <summary>
        /// Clears unconsumed actions without resetting
        /// the detector's action ID history.
        /// </summary>
        internal void Clear()
        {
            _pendingGestures.Clear();
        }
    }

    /// <summary>
    /// Per-frame TOUCH entry buffer that retains each originating hand.
    /// Repeated entries from the same hand into the same area are coalesced;
    /// entries from different hands remain independently consumable.
    /// </summary>
    internal sealed class AirshiftTouchInputBridge
    {
        private readonly List<AirshiftSensorTransition> _pendingEntries = new();
        private readonly HashSet<(AirshiftSensorArea Area, HandId Hand)> _seenThisFrame = new();

        internal int PendingCount => _pendingEntries.Count;

        internal void BeginFrame()
        {
            _pendingEntries.Clear();
            _seenThisFrame.Clear();
        }

        internal bool Enqueue(AirshiftSensorTransition transition)
        {
            if (!transition.Entered || transition.Area == AirshiftSensorArea.None)
            {
                return false;
            }

            if (!_seenThisFrame.Add((transition.Area, transition.Hand)))
            {
                return false;
            }

            _pendingEntries.Add(transition);
            return true;
        }

        internal bool TryConsume(
            AirshiftSensorArea area,
            AirshiftRequiredHand requiredHand,
            out AirshiftSensorTransition transition)
        {
            for (int i = 0; i < _pendingEntries.Count; i++)
            {
                var candidate = _pendingEntries[i];
                if (candidate.Area != area)
                {
                    continue;
                }

                bool wrongHand = requiredHand switch
                {
                    AirshiftRequiredHand.Any => false,
                    AirshiftRequiredHand.Left => candidate.Hand != HandId.Left,
                    AirshiftRequiredHand.Right => candidate.Hand != HandId.Right,
                    _ => true
                };
                if (wrongHand)
                {
                    continue;
                }

                _pendingEntries.RemoveAt(i);
                transition = candidate;
                return true;
            }

            transition = default;
            return false;
        }

        internal void Clear() => BeginFrame();
    }
}
