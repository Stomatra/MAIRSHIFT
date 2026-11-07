
using System;
using UnityEngine;

#nullable enable
namespace MajdataPlay.Scenes.Airshift
{
    /// <summary>
    /// Tracks independent circular contacts for both hands.
    /// Converts contact-mask changes into area transitions.
    /// </summary>
    internal sealed class AirshiftDiscContactTracker
    {
        private const int TraceCapacity = 64;

        private readonly AirshiftOuterAreaDetector _detector = new();
        private readonly AirshiftDiscContactTracePoint[] _trace =
            new AirshiftDiscContactTracePoint[TraceCapacity];

        private readonly Vector2[] _positions = new Vector2[2];
        private readonly int[] _masks = new int[2];

        private float _radius;

        internal event Action<AirshiftAreaTransition>? AreaTransitioned;

        internal AirshiftDiscContactTracker(
            Vector2 leftPosition,
            Vector2 rightPosition,
            float radius)
        {
            Reset(leftPosition, rightPosition, radius);
        }

        /// <summary>
        /// Updates contact radius without generating fake entries.
        /// </summary>
        internal void SetRadius(float radius)
        {
            radius = Mathf.Clamp(radius, 0.01f, 0.15f);

            if (Mathf.Approximately(_radius, radius))
                return;

            _radius = radius;

            for (int i = 0; i < 2; i++)
            {
                _masks[i] = _detector.GetContactMask(
                    _positions[i], _radius);
            }
        }

        /// <summary>
        /// Resynchronizes both hands after focus or source changes.
        /// No transitions are emitted.
        /// </summary>
        internal void Reset(
            Vector2 leftPosition,
            Vector2 rightPosition,
            float radius)
        {
            _positions[0] = leftPosition;
            _positions[1] = rightPosition;

            _radius = -1f;
            SetRadius(radius);
        }

        /// <summary>
        /// Processes a real cursor movement.
        /// Returns false if its contact trace overflowed.
        /// </summary>
        internal bool Observe(AirshiftMotionSample sample)
        {
            int index = (int)sample.HandId;
            Vector2 from = _positions[index];

            int count = _detector.TraceDiscContacts(
                from,
                sample.Position,
                _radius,
                _trace);

            // Fail closed: never publish a partial trace.
            if (count < 0)
            {
                _positions[index] = sample.Position;
                _masks[index] = _detector.GetContactMask(
                    sample.Position, _radius);
                return false;
            }

            int previousMask = _masks[index];

            for (int i = 0; i < count; i++)
            {
                int nextMask = _trace[i].ContactMask;

                EmitMaskChanges(
                    sample,
                    _trace[i].Position,
                    previousMask,
                    nextMask);

                previousMask = nextMask;
            }

            _positions[index] = sample.Position;
            _masks[index] = _detector.GetContactMask(
                sample.Position, _radius);

            return true;
        }
        
        internal bool IsAreaContacted(int startPos)
        {
            if (startPos < 1 || startPos > 8)
            {
                return false;
            }

            int bit = 1 << (startPos - 1);

            // Either hand can sustain a HOLD.
            return ((_masks[0] | _masks[1]) & bit) != 0;
        }

        // AIRSHIFT_ANDROID_FULL_V03: the Android camera can lose one hand while the other remains visible.
        internal bool IsAreaContacted(int startPos, HandId hand)
        {
            if (startPos < 1 || startPos > 8)
                return false;

            int bit = 1 << (startPos - 1);
            return (_masks[(int)hand] & bit) != 0;
        }

        private void EmitMaskChanges(
            AirshiftMotionSample sample,
            Vector2 position,
            int previousMask,
            int nextMask)
        {
            // Exits first, then entries.
            int exited = previousMask & ~nextMask;
            int entered = nextMask & ~previousMask;

            for (int i = 0; i < 8; i++)
            {
                if ((exited & (1 << i)) == 0)
                    continue;

                EmitTransition(
                    sample,
                    position,
                    (AirshiftArea)(i + 1),
                    AirshiftAreaTransitionKind.Exit);
            }

            for (int i = 0; i < 8; i++)
            {
                if ((entered & (1 << i)) == 0)
                    continue;

                EmitTransition(
                    sample,
                    position,
                    (AirshiftArea)(i + 1),
                    AirshiftAreaTransitionKind.Enter);
            }
        }

        private void EmitTransition(
            AirshiftMotionSample sample,
            Vector2 position,
            AirshiftArea area,
            AirshiftAreaTransitionKind kind)
        {
            AreaTransitioned?.Invoke(
                new AirshiftAreaTransition(
                    sample.HandId,
                    area,
                    kind,
                    position,
                    sample.Timestamp,
                    sample.InputSource));
        }
    }
}
