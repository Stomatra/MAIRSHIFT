using UnityEngine;

#nullable enable
namespace MajdataPlay.Scenes.Airshift
{
    /// <summary>
    /// Reads the display rectangles already positioned by the STANDARD CanvasTemplate layout system.
    /// </summary>
    /// <remarks>
    /// This class performs no aspect-ratio or display-setting calculation. It only converts the actual
    /// <c>Main_Display</c> and <c>Sub_Display</c> RectTransforms to IMGUI coordinates.
    /// </remarks>
    internal sealed class AirshiftStandardLayoutBridge
    {
        private const string CanvasTemplateName = "CanvasTemplate";
        private const string MainDisplayName = "Main_Display";
        private const string SubDisplayName = "Sub_Display";

        private RectTransform? _mainDisplay;
        private RectTransform? _subDisplay;

        /// <summary>
        /// Gets the current STANDARD main and sub display rectangles in top-left IMGUI coordinates.
        /// </summary>
        /// <param name="mainDisplay">The square main-display rectangle.</param>
        /// <param name="subDisplay">The Sub Monitor rectangle.</param>
        /// <returns>Whether both live display transforms are available and laid out.</returns>
        internal bool TryGetDisplayRects(out Rect mainDisplay, out Rect subDisplay)
        {
            if (!ResolveTransforms())
            {
                mainDisplay = default;
                subDisplay = default;
                return false;
            }

            mainDisplay = ToGuiRect(_mainDisplay!);
            subDisplay = ToGuiRect(_subDisplay!);
            return mainDisplay.width > 1f && mainDisplay.height > 1f &&
                subDisplay.width > 1f && subDisplay.height > 1f;
        }

        /// <summary>Resolves the one scene-local CanvasTemplate instance without creating another canvas.</summary>
        private bool ResolveTransforms()
        {
            if (_mainDisplay != null && _subDisplay != null)
            {
                return true;
            }

            var root = GameObject.Find(CanvasTemplateName);
            if (root == null)
            {
                return false;
            }
            _mainDisplay = root.transform.Find(MainDisplayName) as RectTransform;
            _subDisplay = root.transform.Find(SubDisplayName) as RectTransform;
            return _mainDisplay != null && _subDisplay != null;
        }

        /// <summary>Converts one laid-out RectTransform into a top-left-origin IMGUI rectangle.</summary>
        private static Rect ToGuiRect(RectTransform display)
        {
            var canvas = display.GetComponentInParent<Canvas>();
            var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;
            var corners = new Vector3[4];
            display.GetWorldCorners(corners);
            var bottomLeft = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
            var topRight = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
            return FromScreenCorners(bottomLeft, topRight, Screen.height);
        }

        /// <summary>Pure coordinate conversion exposed for targeted EditMode coverage.</summary>
        internal static Rect FromScreenCorners(Vector2 bottomLeft, Vector2 topRight, float screenHeight)
        {
            return new Rect(
                bottomLeft.x,
                screenHeight - topRight.y,
                topRight.x - bottomLeft.x,
                topRight.y - bottomLeft.y);
        }
    }
}
