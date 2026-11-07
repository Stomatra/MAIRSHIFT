using UnityEngine;

namespace MajdataPlay.Scenes.Airshift
{
    /// <summary>
    /// Runtime tuning values for AIRSHIFT input.
    /// Will be connected to player settings later.
    /// </summary>
    internal static class AirshiftControlTuning
    {
        // AIRSHIFT_COMFORT_V04: cursor radius 2.5x larger than the tested v0.3 baseline.
        // Normalized radius: 0.05f * 2.5f = 0.125f of the main playfield width.
        // This shared parameter drives visual circle, A-ring disc contact and cursor clamping.
        internal static float CursorRadiusNormalized { get; set; }
            = 0.125f;

        // Relative mouse movement multiplier.
        internal static float MouseSensitivity { get; set; }
            = 1.8f;

        /// <summary>
        /// Constrains the entire circular cursor to the
        /// normalized circular playfield.
        /// </summary>
        internal static Vector2 ClampCursorPosition(Vector2 position)
        {
            var center = new Vector2(0.5f, 0.5f);

            float cursorRadius = Mathf.Clamp(
                CursorRadiusNormalized,
                0.01f,
                0.15f
            );

            const float playfieldRadius = 0.5f;

            float maxCenterDistance = Mathf.Max(
                0f,
                playfieldRadius - cursorRadius
            );

            var offset = position - center;

            return center + Vector2.ClampMagnitude(
                offset,
                maxCenterDistance
            );
        }

    }
}
