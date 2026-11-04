using UnityEngine;
using UnityEngine.InputSystem;

#nullable enable
namespace MajdataPlay.Scenes.Airshift
{
    /// <summary>
    /// Converts relative mouse movement into normalized AIRSHIFT cursor samples.
    /// </summary>
    internal sealed class AirshiftMouseSimulationSource
    {
        /// <summary>The playfield-relative distance treated as a device or window warp.</summary>
        private const float JumpThresholdRatio = 0.45f;
        /// <summary>The last observed Game view dimensions.</summary>
        private Vector2Int _lastWindowSize = new(-1, -1);
        /// <summary>Whether the Game view was focused during the previous poll.</summary>
        private bool _wasFocused;
        /// <summary>Whether the next device delta must be discarded.</summary>
        private bool _suppressNextDelta = true;

        /// <summary>
        /// Polls the mouse and updates the active cursor when the delta is continuous and trustworthy.
        /// </summary>
        /// <param name="tracker">The independent cursor tracker.</param>
        /// <param name="playfieldSize">The square playfield size in screen pixels.</param>
        /// <param name="timestamp">The monotonic sample timestamp.</param>
        /// <param name="isFocused">Whether the Game view currently owns input focus.</param>
        /// <returns>Whether a source discontinuity reset motion baselines during this poll.</returns>
        internal bool Poll(
            AirshiftDualCursorTracker tracker,
            float playfieldSize,
            double timestamp,
            bool isFocused)
        {
            var windowSize = new Vector2Int(Screen.width, Screen.height);
            if (windowSize != _lastWindowSize)
            {
                _lastWindowSize = windowSize;
                SuppressNextDelta(tracker, timestamp);
            }
            if (!isFocused)
            {
                var resetForFocusLoss = false;
                if (_wasFocused)
                {
                    SuppressNextDelta(tracker, timestamp);
                    resetForFocusLoss = true;
                }
                _wasFocused = false;
                return resetForFocusLoss;
            }
            if (!_wasFocused)
            {
                _wasFocused = true;
                SuppressNextDelta(tracker, timestamp);
            }

            var mouse = Mouse.current;
            if (mouse is null)
            {
                tracker.MarkActiveHandIdle(timestamp);
                return false;
            }
            var pixelDelta = mouse.delta.ReadValue();
            if (_suppressNextDelta)
            {
                _suppressNextDelta = false;
                tracker.ResetMotionBaselines(timestamp);
                return true;
            }
            var safePlayfieldSize = Mathf.Max(playfieldSize, 1f);
            if (pixelDelta.magnitude > safePlayfieldSize * JumpThresholdRatio)
            {
                SuppressNextDelta(tracker, timestamp);
                return true;
            }
            if (pixelDelta.sqrMagnitude <= 0.0001f)
            {
                tracker.MarkActiveHandIdle(timestamp);
                return false;
            }
            
            var sensitivity = Mathf.Clamp(
                AirshiftControlTuning.MouseSensitivity,
                0.5f,
                3f
            );

            tracker.ApplyActiveDelta(
                pixelDelta / safePlayfieldSize * sensitivity,
                timestamp
            );

            return false;
        }

        /// <summary>
        /// Discards the next mouse delta and clears velocity baselines without emitting motion events.
        /// </summary>
        /// <param name="tracker">The independent cursor tracker.</param>
        /// <param name="timestamp">The monotonic discontinuity timestamp.</param>
        internal void SuppressNextDelta(AirshiftDualCursorTracker tracker, double timestamp)
        {
            _suppressNextDelta = true;
            tracker.ResetMotionBaselines(timestamp);
        }
    }
}
