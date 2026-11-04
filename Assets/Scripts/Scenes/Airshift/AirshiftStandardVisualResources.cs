using MajdataPlay.Scenes.Game.Notes.Skins;
using UnityEngine;

#nullable enable
namespace MajdataPlay.Scenes.Airshift
{
    /// <summary>
    /// Borrows already-loaded STANDARD skin sprites for AIRSHIFT presentation without starting STANDARD note logic.
    /// </summary>
    internal static class AirshiftStandardVisualResources
    {
        /// <summary>Gets the currently selected shared skin when initialization has completed.</summary>
        internal static CustomSkin? CurrentSkin
        {
            get
            {
                var skinManager = Majdata<SkinManager>.Instance;
                return skinManager?.IsInited == true ? skinManager.SelectedSkin : null;
            }
        }

        /// <summary>Draws the selected skin's original Sub Monitor background.</summary>
        internal static bool DrawSubDisplayBackground(Rect rect)
        {
            return DrawSprite(CurrentSkin?.SubDisplay, rect, Color.white, ScaleMode.StretchToFill);
        }

        /// <summary>Draws one sprite including its atlas texture rectangle.</summary>
        internal static bool DrawSprite(
            Sprite? sprite,
            Rect rect,
            Color color,
            ScaleMode scaleMode = ScaleMode.ScaleToFit)
        {
            if (sprite == null || sprite.texture == null)
            {
                return false;
            }

            var oldColor = GUI.color;
            GUI.color = color;
            var texture = sprite.texture;
            var textureRect = sprite.textureRect;
            var uv = new Rect(
                textureRect.x / texture.width,
                textureRect.y / texture.height,
                textureRect.width / texture.width,
                textureRect.height / texture.height);
            var drawRect = scaleMode == ScaleMode.ScaleToFit
                ? Fit(rect, sprite.rect.size)
                : rect;
            GUI.DrawTextureWithTexCoords(drawRect, texture, uv, true);
            GUI.color = oldColor;
            return true;
        }

        /// <summary>Fits a source aspect ratio inside a target without inventing display-layout rules.</summary>
        private static Rect Fit(Rect target, Vector2 sourceSize)
        {
            if (sourceSize.x <= 0f || sourceSize.y <= 0f)
            {
                return target;
            }
            var scale = Mathf.Min(target.width / sourceSize.x, target.height / sourceSize.y);
            var size = sourceSize * scale;
            return new Rect(
                target.center.x - size.x * 0.5f,
                target.center.y - size.y * 0.5f,
                size.x,
                size.y);
        }
    }
}
