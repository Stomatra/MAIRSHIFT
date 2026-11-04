using System;
using System.Text;
using UnityEngine;

#nullable enable
namespace MajdataPlay.Scenes.Airshift
{
    /// <summary>
    /// Renders the isolated AIRSHIFT prototype state through a lightweight IMGUI debug view.
    /// </summary>
    internal sealed class AirshiftDebugView : IDisposable
    {
        /// <summary>The requested left-hand cursor color, #34B8FF.</summary>
        private static readonly Color32 LeftColor = new(0x34, 0xB8, 0xFF, 0xFF);
        /// <summary>The requested right-hand cursor color, #FF526F.</summary>
        private static readonly Color32 RightColor = new(0xFF, 0x52, 0x6F, 0xFF);
        /// <summary>The maximum number of recent events shown by the debug view.</summary>
        private const int EventCapacity = 12;

        /// <summary>The recent event ring buffer.</summary>
        private readonly string[] _events = new string[EventCapacity];
        /// <summary>The reusable text builder for the side panel.</summary>
        private readonly StringBuilder _textBuilder = new(1024);
        /// <summary>The generated circular cursor texture.</summary>
        private readonly Texture2D _discTexture;
        /// <summary>The next event slot in the ring buffer.</summary>
        private int _nextEventIndex;
        /// <summary>The number of populated ring-buffer entries.</summary>
        private int _eventCount;
        /// <summary>The lazily initialized title style.</summary>
        private GUIStyle? _titleStyle;
        /// <summary>The lazily initialized body style.</summary>
        private GUIStyle? _bodyStyle;
        /// <summary>The lazily initialized centered area style.</summary>
        private GUIStyle? _areaStyle;

        /// <summary>
        /// Creates the debug view and its transient cursor texture.
        /// </summary>
        internal AirshiftDebugView()
        {
            _discTexture = CreateDiscTexture(64);
        }

        /// <summary>
        /// Draws the complete prototype view.
        /// </summary>
        /// <param name="tracker">The dual-cursor state to present.</param>
        /// <param name="isFocused">Whether the Game view currently owns focus.</param>
        /// <param name="tapChart">The isolated code-defined TAP chart.</param>
        /// <param name="tapTime">The current local TAP timeline time.</param>
        /// <param name="lastTapResult">The latest TAP judgement summary.</param>
        /// <param name="playfield">The STANDARD Main_Display rectangle.</param>
        /// <param name="subDisplay">The STANDARD Sub_Display rectangle.</param>
        /// <param name="showDiagnostics">Whether the F6 debug overlay is visible.</param>
        internal void Draw(
            AirshiftDualCursorTracker tracker,
            bool isFocused,
            AirshiftTestChart tapChart,
            double tapTime,
            string lastTapResult,
            Rect playfield,
            Rect subDisplay,
            bool showDiagnostics)
        {
            EnsureStyles(playfield, subDisplay);
            var previousColor = GUI.color;
            GUI.color = new Color32(8, 16, 30, 255);
            GUI.DrawTexture(playfield, Texture2D.whiteTexture);
            if (!AirshiftStandardVisualResources.DrawSubDisplayBackground(subDisplay))
            {
                GUI.DrawTexture(subDisplay, Texture2D.whiteTexture);
            }
            GUI.color = new Color(0f, 0f, 0f, 0.45f);
            GUI.DrawTexture(subDisplay, Texture2D.whiteTexture);
            GUI.color = Color.white;
            DrawPlayfield(playfield, tracker, tapChart.GetNextPendingNote());
            if (showDiagnostics)
            {
                DrawDiagnostics(subDisplay, tracker, isFocused, tapChart, tapTime, lastTapResult);
            }
            else
            {
                GUI.Label(subDisplay, "AIRSHIFT INPUT TEST\nF6: DEBUG OVERLAY", _titleStyle);
            }
            GUI.color = previousColor;
        }

        /// <summary>
        /// Adds an area transition to the recent event log.
        /// </summary>
        /// <param name="transition">The transition to display.</param>
        internal void AddEvent(AirshiftAreaTransition transition)
        {
            AddEvent($"{transition.Timestamp,8:F3}  {transition.HandId,-5}  {transition.Kind,-5} {transition.Area}");
        }

        /// <summary>
        /// Adds a keyboard debug transition to the recent event log.
        /// </summary>
        /// <param name="buttonEvent">The keyboard event to display.</param>
        internal void AddEvent(DebugButtonEvent buttonEvent)
        {
            AddEvent($"{buttonEvent.Timestamp,8:F3}  KEY    {buttonEvent.Area} {(buttonEvent.IsPressed ? "DOWN" : "UP")}");
        }

        /// <summary>
        /// Adds one recognized no-click TAP action to the recent event log.
        /// </summary>
        /// <param name="gesture">The recognized gesture.</param>
        internal void AddEvent(TapGesture gesture)
        {
            AddEvent(
                $"{gesture.Timestamp,8:F3}  TAP#{gesture.ActionId,-3} {gesture.HandId,-5} " +
                $"{gesture.Area} {gesture.TriggerType} v={gesture.Speed:F2}");
        }

        /// <summary>
        /// Adds one isolated test-note judgement to the recent event log.
        /// </summary>
        /// <param name="result">The resolved note result.</param>
        internal void AddEvent(AirshiftTapJudgeResult result)
        {
            AddEvent(
                $"{result.Note.NoteId,-6} {result.State,-7} " +
                $"{result.TimingOffset:+0.000;-0.000;0.000}s");
        }

        /// <summary>
        /// Adds a scene-control message to the recent event log.
        /// </summary>
        /// <param name="timestamp">The monotonic event timestamp.</param>
        /// <param name="message">The message to display.</param>
        internal void AddSystemEvent(double timestamp, string message)
        {
            AddEvent($"{timestamp,8:F3}  SYSTEM {message}");
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_discTexture != null)
            {
                UnityEngine.Object.Destroy(_discTexture);
            }
        }

        /// <summary>
        /// Adds one preformatted entry to the fixed-size ring buffer.
        /// </summary>
        /// <param name="message">The event message.</param>
        private void AddEvent(string message)
        {
            _events[_nextEventIndex] = message;
            _nextEventIndex = (_nextEventIndex + 1) % EventCapacity;
            _eventCount = Mathf.Min(_eventCount + 1, EventCapacity);
        }

        /// <summary>
        /// Draws the circular A1-A8 layout and both cursor states.
        /// </summary>
        /// <param name="rect">The square playfield rectangle.</param>
        /// <param name="tracker">The cursor tracker.</param>
        /// <param name="nextTap">The next pending test note, if any.</param>
        private void DrawPlayfield(
            Rect rect,
            AirshiftDualCursorTracker tracker,
            AirshiftTestTapNote? nextTap)
        {
            var center = rect.center;
            var radius = rect.width * 0.5f;
            GUI.color = new Color32(17, 34, 57, 255);
            GUI.DrawTexture(rect, _discTexture);
            GUI.color = new Color32(110, 150, 190, 255);
            DrawCircle(center, radius, 2f, 96);
            DrawCircle(center, radius * AirshiftOuterAreaDetector.InnerRadius, 2f, 96);
            for (var i = 0; i < 8; i++)
            {
                var centerAngle = 67.5f - (i * 45f);
                DrawRadialBoundary(center, radius, centerAngle - 14.5f);
                DrawRadialBoundary(center, radius, centerAngle + 14.5f);
            }
            for (var i = 1; i <= 8; i++)
            {
                var area = (AirshiftArea)i;
                var labelPosition = ToScreen(rect, AirshiftOuterAreaDetector.GetAreaCenter(area));
                var isActive = tracker.LeftHand.CurrentArea == area || tracker.RightHand.CurrentArea == area;
                var isTapTarget = nextTap?.Area == area;
                GUI.color = isActive
                    ? new Color32(255, 230, 90, 255)
                    : isTapTarget ? new Color32(80, 255, 150, 255) : Color.white;
                GUI.Label(new Rect(labelPosition.x - 28f, labelPosition.y - 16f, 56f, 32f), area.ToString(), _areaStyle!);
            }
            DrawCursor(rect, tracker.LeftHand, tracker.ActiveHand == HandId.Left, LeftColor);
            DrawCursor(rect, tracker.RightHand, tracker.ActiveHand == HandId.Right, RightColor);
            GUI.color = Color.white;
        }

        /// <summary>
        /// Draws one cursor, its active highlight, velocity direction, and current area.
        /// </summary>
        /// <param name="playfield">The square playfield rectangle.</param>
        /// <param name="hand">The hand state.</param>
        /// <param name="isActive">Whether this cursor currently receives mouse deltas.</param>
        /// <param name="color">The requested cursor color.</param>
        private void DrawCursor(Rect playfield, AirshiftHandState hand, bool isActive, Color color)
        {
            var position = ToScreen(playfield, hand.Position);
            if (hand.Speed > 0.0001f)
            {
                var direction = new Vector2(hand.Velocity.x, -hand.Velocity.y).normalized;
                GUI.color = new Color(color.r, color.g, color.b, 0.65f);
                DrawLine(position, position + (direction * Mathf.Min(70f, 18f + (hand.Speed * 12f))), 3f);
            }
            if (isActive)
            {
                GUI.color = Color.white;
                GUI.DrawTexture(new Rect(position.x - 20f, position.y - 20f, 40f, 40f), _discTexture);
            }
            GUI.color = color;
            var size = isActive ? 30f : 24f;
            GUI.DrawTexture(new Rect(position.x - (size * 0.5f), position.y - (size * 0.5f), size, size), _discTexture);
            if (hand.CurrentArea != AirshiftArea.None)
            {
                GUI.color = Color.white;
                GUI.Label(new Rect(position.x - 35f, position.y + 18f, 70f, 26f), hand.CurrentArea.ToString(), _areaStyle!);
            }
        }

        /// <summary>
        /// Draws active-hand state, both motion samples, recent events, and all shortcuts.
        /// </summary>
        /// <param name="rect">The diagnostics panel rectangle.</param>
        /// <param name="tracker">The cursor tracker.</param>
        /// <param name="isFocused">Whether the Game view currently owns focus.</param>
        /// <param name="tapChart">The isolated code-defined TAP chart.</param>
        /// <param name="tapTime">The current local TAP timeline time.</param>
        /// <param name="lastTapResult">The latest TAP result.</param>
        private void DrawDiagnostics(
            Rect rect,
            AirshiftDualCursorTracker tracker,
            bool isFocused,
            AirshiftTestChart tapChart,
            double tapTime,
            string lastTapResult)
        {
            GUI.color = new Color(0f, 0f, 0f, 0.45f);
            GUI.Box(rect, GUIContent.none);
            GUI.color = Color.white;
            var padding = Mathf.Max(12f, rect.width * 0.035f);
            var content = new Rect(rect.x + padding, rect.y + padding, rect.width - (padding * 2f), rect.height - (padding * 2f));
            GUI.Label(new Rect(content.x, content.y, content.width, 50f), "AIRSHIFT NO-CLICK TAP PROTOTYPE", _titleStyle);
            _textBuilder.Clear();
            _textBuilder.Append("Focus: ").Append(isFocused ? "ACTIVE" : "PAUSED").Append('\n');
            _textBuilder.Append("Controlled hand: ").Append(tracker.ActiveHand).Append("  (highlighted)\n\n");
            AppendHandState(_textBuilder, tracker.LeftHand);
            AppendHandState(_textBuilder, tracker.RightHand);
            AppendTapTest(_textBuilder, tapChart, tapTime, lastTapResult);
            _textBuilder.Append("\nRecent input events:\n");
            for (var i = 0; i < _eventCount; i++)
            {
                var index = (_nextEventIndex - _eventCount + i + EventCapacity) % EventCapacity;
                _textBuilder.Append(_events[index]).Append('\n');
            }
            _textBuilder.Append("\nF6: hide debug    F8: restart TAP test    F9: result    F10: cancel\n");
            _textBuilder.Append("Tab: switch hand    R: re-center active hand\n");
            _textBuilder.Append("Main keyboard 1-8: DebugButtonEvent A1-A8\n");
            _textBuilder.Append("Move mouse: no-click TAP motion\n");
            _textBuilder.Append("Timing windows are prototype values, not official maimai parameters.");
            GUI.Label(new Rect(content.x, content.y + 52f, content.width, content.height - 52f), _textBuilder.ToString(), _bodyStyle);
        }

        /// <summary>
        /// Appends the current local TAP target, countdown, result, and compact chart state.
        /// </summary>
        /// <param name="builder">The reusable diagnostics builder.</param>
        /// <param name="chart">The isolated test chart.</param>
        /// <param name="tapTime">The local chart time.</param>
        /// <param name="lastTapResult">The latest judgement summary.</param>
        private static void AppendTapTest(
            StringBuilder builder,
            AirshiftTestChart chart,
            double tapTime,
            string lastTapResult)
        {
            builder.Append("\nTAP test time: ").AppendFormat("{0:F3}s", tapTime).Append('\n');
            var next = chart.GetNextPendingNote();
            if (next is null)
            {
                builder.Append("Next: COMPLETE\n");
            }
            else
            {
                builder.Append("Next: ").Append(next.NoteId)
                    .Append(' ').Append(next.Area)
                    .Append(' ').Append(next.RequiredHand)
                    .Append("  in ").AppendFormat("{0:+0.000;-0.000;0.000}s", next.TargetTime - tapTime)
                    .Append('\n');
            }
            builder.Append("Last: ").Append(lastTapResult).Append('\n');
            builder.Append("Chart: ");
            for (var i = 0; i < chart.Notes.Count; i++)
            {
                var note = chart.Notes[i];
                if (i > 0)
                {
                    builder.Append(" | ");
                }
                builder.Append(note.NoteId).Append(':').Append(note.State);
            }
            builder.Append('\n');
        }

        /// <summary>
        /// Appends one hand's position, velocity, speed, direction, and area state.
        /// </summary>
        /// <param name="builder">The reusable diagnostics builder.</param>
        /// <param name="hand">The hand state to append.</param>
        private static void AppendHandState(StringBuilder builder, AirshiftHandState hand)
        {
            var direction = hand.Speed > 0.0001f ? hand.Velocity / hand.Speed : Vector2.zero;
            builder.Append(hand.HandId).Append(": pos ")
                .AppendFormat("({0:F3}, {1:F3})", hand.Position.x, hand.Position.y)
                .Append("  velocity ")
                .AppendFormat("({0:F2}, {1:F2})", hand.Velocity.x, hand.Velocity.y)
                .AppendFormat("  speed {0:F2}\n", hand.Speed)
                .Append("  direction ")
                .AppendFormat("({0:F2}, {1:F2})", direction.x, direction.y)
                .Append("  area ").Append(hand.CurrentArea)
                .Append("  previous ").Append(hand.PreviousArea).Append("\n");
        }

        /// <summary>
        /// Converts normalized playfield coordinates to IMGUI screen coordinates.
        /// </summary>
        /// <param name="rect">The square playfield rectangle.</param>
        /// <param name="position">The normalized AIRSHIFT position.</param>
        /// <returns>The IMGUI screen position.</returns>
        private static Vector2 ToScreen(Rect rect, Vector2 position)
        {
            return new Vector2(
                rect.x + (position.x * rect.width),
                rect.y + ((1f - position.y) * rect.height));
        }

        /// <summary>
        /// Draws one true A-region edge, leaving the intervening D-sensor gap visible.
        /// </summary>
        /// <param name="center">The playfield center.</param>
        /// <param name="radius">The playfield radius.</param>
        /// <param name="angleDegrees">The AIRSHIFT-space edge angle in degrees.</param>
        private static void DrawRadialBoundary(Vector2 center, float radius, float angleDegrees)
        {
            var angle = angleDegrees * Mathf.Deg2Rad;
            var direction = new Vector2(Mathf.Cos(angle), -Mathf.Sin(angle));
            DrawLine(
                center + (direction * radius * AirshiftOuterAreaDetector.InnerRadius),
                center + (direction * radius),
                1.5f);
        }

        /// <summary>
        /// Draws a thin IMGUI line between two screen points.
        /// </summary>
        /// <param name="from">The line start.</param>
        /// <param name="to">The line end.</param>
        /// <param name="thickness">The line thickness.</param>
        private static void DrawLine(Vector2 from, Vector2 to, float thickness)
        {
            var direction = to - from;
            var length = direction.magnitude;
            if (length <= 0.001f)
            {
                return;
            }
            var previousMatrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg, from);
            GUI.DrawTexture(new Rect(from.x, from.y - (thickness * 0.5f), length, thickness), Texture2D.whiteTexture);
            GUI.matrix = previousMatrix;
        }

        /// <summary>
        /// Draws a segmented circle in IMGUI screen coordinates.
        /// </summary>
        /// <param name="center">The circle center.</param>
        /// <param name="radius">The circle radius.</param>
        /// <param name="thickness">The segment thickness.</param>
        /// <param name="segments">The number of segments.</param>
        private static void DrawCircle(Vector2 center, float radius, float thickness, int segments)
        {
            var previous = center + new Vector2(radius, 0f);
            for (var i = 1; i <= segments; i++)
            {
                var angle = (i / (float)segments) * Mathf.PI * 2f;
                var next = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                DrawLine(previous, next, thickness);
                previous = next;
            }
        }

        /// <summary>
        /// Creates a transient antialiased circular texture for cursors and the playfield fill.
        /// </summary>
        /// <param name="size">The square texture size.</param>
        /// <returns>The generated texture.</returns>
        private static Texture2D CreateDiscTexture(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "AIRSHIFT Debug Disc",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
            };
            var pixels = new Color32[size * size];
            var center = (size - 1) * 0.5f;
            var radius = center;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    var alpha = (byte)Mathf.RoundToInt(Mathf.Clamp01(radius - distance + 1f) * 255f);
                    pixels[(y * size) + x] = new Color32(255, 255, 255, alpha);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        /// <summary>
        /// Creates the IMGUI styles during a valid GUI event.
        /// </summary>
        private void EnsureStyles(Rect playfield, Rect subDisplay)
        {
            _titleStyle ??= CreateStyle(FontStyle.Bold, TextAnchor.UpperLeft, false);
            _bodyStyle ??= CreateStyle(FontStyle.Normal, TextAnchor.UpperLeft, true);
            _areaStyle ??= CreateStyle(FontStyle.Bold, TextAnchor.MiddleCenter, false);
            _titleStyle.fontSize = Mathf.Clamp(Mathf.RoundToInt(subDisplay.height * 0.12f), 22, 42);
            _bodyStyle.fontSize = Mathf.Clamp(Mathf.RoundToInt(subDisplay.height * 0.05f), 13, 24);
            _areaStyle.fontSize = Mathf.Clamp(Mathf.RoundToInt(playfield.height * 0.024f), 12, 26);
        }

        /// <summary>
        /// Creates one debug label style.
        /// </summary>
        /// <param name="fontStyle">The requested font emphasis.</param>
        /// <param name="alignment">The requested text alignment.</param>
        /// <param name="wordWrap">Whether the label wraps text.</param>
        /// <returns>The configured label style.</returns>
        private static GUIStyle CreateStyle(FontStyle fontStyle, TextAnchor alignment, bool wordWrap)
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                alignment = alignment,
                fontStyle = fontStyle,
                wordWrap = wordWrap,
                clipping = TextClipping.Clip,
            };
            style.normal.textColor = Color.white;
            return style;
        }
    }
}
