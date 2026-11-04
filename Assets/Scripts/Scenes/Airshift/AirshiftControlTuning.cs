using UnityEngine;

namespace MajdataPlay.Scenes.Airshift
{
    /// <summary>
    /// Runtime tuning values for AIRSHIFT input.
    /// Will be connected to player settings later.
    /// </summary>
    internal static class AirshiftControlTuning
    {
        // Radius relative to the main playfield width.
        // 0.05 = 5% radius, 10% diameter.
        internal static float CursorRadiusNormalized { get; set; }
            = 0.05f;

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
