using UnityEngine;

namespace MajdataPlay.Scenes.Airshift
{
    /// <summary>
    /// AIRSHIFT-only hand colors for notes. These are per-renderer tints;
    /// no imported skin texture, sprite, or shared material is modified.
    /// </summary>
    internal static class AirshiftNoteVisualPalette
    {
        // Blue = left; red = right. Any-hand and STANDARD notes keep their skin colors.
        internal static readonly Color Left = new(0.27f, 0.59f, 1f, 1f);
        internal static readonly Color Right = new(0.98f, 0.32f, 0.43f, 1f);

        internal static Color GetTint(AirshiftRequiredHand requiredHand, bool airshiftEnabled)
        {
            if (!airshiftEnabled)
            {
                return Color.white;
            }

            return requiredHand switch
            {
                AirshiftRequiredHand.Left => Left,
                AirshiftRequiredHand.Right => Right,
                _ => Color.white,
            };
        }

        /// <summary>Multiplies RGB while preserving the base animation alpha.</summary>
        internal static Color Multiply(Color baseColor, Color tint) => new(
            baseColor.r * tint.r,
            baseColor.g * tint.g,
            baseColor.b * tint.b,
            baseColor.a * tint.a);
    }
}
