using System;
using System.Collections.Generic;
using System.Text;
using MajdataPlay.Scenes.Game.Notes.Skins;
using UnityEngine;

#nullable enable
namespace MajdataPlay.Scenes.Airshift
{
    /// <summary>Lightweight AIRSHIFT chart renderer kept separate from judgement and STANDARD note prefabs.</summary>
    internal sealed class AirshiftChartView : IDisposable
    {
        private const double ApproachSeconds = 2d;
        private readonly Texture2D _disc;
        private readonly Sprite? _gameBackground;
        private readonly Queue<string> _feedback = new();
        private readonly StringBuilder _hud = new(512);
        private GUIStyle? _titleStyle;
        private GUIStyle? _labelStyle;
        private GUIStyle? _smallStyle;

        internal AirshiftChartView(Sprite? gameBackground)
        {
            _gameBackground = gameBackground;
            _disc = CreateDiscTexture(64);
        }

        internal void AddResult(AirshiftChartJudgeResult result)
        {
            _feedback.Enqueue($"#{result.Note.NoteId} {result.Note.Kind} {result.Grade} {result.TimingOffset:+0.000;-0.000;0.000}s");
            while (_feedback.Count > 7)
            {
                _feedback.Dequeue();
            }
        }

        internal void Draw(
            AirshiftDualCursorTracker tracker,
            AirshiftChartPlayer player,
            bool focused,
            string songTitle,
            string level,
            Rect playfield,
            Rect subDisplay,
            bool showDebugOverlay,
            bool useStandardPresentation)
        {
            EnsureStyles(playfield, subDisplay);
            var oldColor = GUI.color;
            if (!useStandardPresentation)
            {
                if (!AirshiftStandardVisualResources.DrawSprite(
                    _gameBackground,
                    playfield,
                    Color.white,
                    ScaleMode.StretchToFill))
                {
                    GUI.color = new Color32(6, 12, 24, 255);
                    GUI.DrawTexture(playfield, Texture2D.whiteTexture);
                }
                if (!AirshiftStandardVisualResources.DrawSubDisplayBackground(subDisplay))
                {
                    GUI.DrawTexture(subDisplay, Texture2D.whiteTexture);
                }
                GUI.color = new Color(0f, 0f, 0f, 0.38f);
                GUI.DrawTexture(subDisplay, Texture2D.whiteTexture);
            }
            if (showDebugOverlay)
            {
                DrawAreaGuide(playfield);
            }
            if (player.Chart is not null)
            {
                DrawNotes(playfield, player.Chart, player.CurrentTime);
            }
            DrawCursors(playfield, tracker);
            if (!useStandardPresentation || showDebugOverlay)
            {
                DrawHud(subDisplay, player, focused, songTitle, level, showDebugOverlay);
            }
            GUI.color = oldColor;
        }

        public void Dispose()
        {
            UnityEngine.Object.Destroy(_disc);
        }

        private void DrawAreaGuide(Rect rect)
        {
            for (var index = 0; index < 8; index++)
            {
                var area = AirshiftSensorArea.A1 + index;
                var center = ToScreen(rect, AreaCenter(area));
                GUI.color = new Color32(70, 104, 145, 255);
                GUI.DrawTexture(Centered(center, rect.width * 0.085f), _disc);
                GUI.color = Color.white;
                GUI.Label(Centered(center, rect.width * 0.07f), $"A{index + 1}", _smallStyle);
            }
        }

        private void DrawNotes(Rect playfield, AirshiftChart chart, double time)
        {
            var skin = AirshiftStandardVisualResources.CurrentSkin;
            foreach (var note in chart.Notes)
            {
                var until = note.StartTime - time;
                var tail = Math.Max(note.EndTime, note.StartTime) - time;
                if (until > ApproachSeconds || tail < -0.45d)
                {
                    continue;
                }
                if (note.Kind == AirshiftChartNoteKind.Slide)
                {
                    DrawSlide(playfield, note, time, skin);
                    continue;
                }
                var target = AreaCenter(note.Area);
                var progress = Mathf.Clamp01((float)(1d - Math.Max(0d, until) / ApproachSeconds));
                var displayed = Vector2.Lerp(new Vector2(0.5f, 0.5f), target, progress);
                var screen = ToScreen(playfield, displayed);
                var size = playfield.width * (note.Kind is AirshiftChartNoteKind.Touch or AirshiftChartNoteKind.TouchHold ? 0.085f : 0.065f);
                GUI.color = NoteColor(note);
                var noteRect = Centered(screen, size);
                if (!AirshiftStandardVisualResources.DrawSprite(ResolveNoteSprite(skin, note), noteRect, Color.white))
                {
                    GUI.DrawTexture(noteRect, _disc);
                }
                if (note.Kind is AirshiftChartNoteKind.Hold or AirshiftChartNoteKind.TouchHold)
                {
                    DrawLine(screen, ToScreen(playfield, target), NoteColor(note), Mathf.Max(3f, playfield.width * 0.008f));
                    GUI.Label(Centered(screen, size * 0.8f), "H", _smallStyle);
                }
                else
                {
                    GUI.Label(Centered(screen, size * 0.8f), note.IsSlideHead ? "★" : note.Kind == AirshiftChartNoteKind.Touch ? "T" : "", _smallStyle);
                }
                if (note.State == AirshiftNoteState.Resolved)
                {
                    GUI.color = note.Grade == AirshiftJudgeGrade.Miss ? Color.red : Color.white;
                    GUI.Label(new Rect(screen.x - size, screen.y + size * 0.45f, size * 2f, size), note.Grade.ToString().ToUpperInvariant(), _smallStyle);
                }
            }
        }

        private void DrawSlide(Rect playfield, AirshiftChartNote note, double time, CustomSkin? skin)
        {
            if (note.SlideCheckpoints.Count == 0)
            {
                return;
            }
            for (var index = 1; index < note.SlideCheckpoints.Count; index++)
            {
                var from = ToScreen(playfield, AreaCenter(note.SlideCheckpoints[index - 1].Area));
                var to = ToScreen(playfield, AreaCenter(note.SlideCheckpoints[index].Area));
                var passed = index < note.SlideCheckpointIndex;
                DrawLine(from, to, passed ? new Color32(80, 255, 150, 255) : NoteColor(note), playfield.width * 0.018f);
            }
            for (var index = 0; index < note.SlideCheckpoints.Count; index++)
            {
                var point = ToScreen(playfield, AreaCenter(note.SlideCheckpoints[index].Area));
                GUI.color = index < note.SlideCheckpointIndex ? new Color32(80, 255, 150, 255) : NoteColor(note);
                var arrowRect = Centered(point, playfield.width * 0.055f);
                if (!AirshiftStandardVisualResources.DrawSprite(ResolveSlideSprite(skin, note), arrowRect, GUI.color))
                {
                    GUI.DrawTexture(arrowRect, _disc);
                }
            }
            var first = ToScreen(playfield, AreaCenter(note.SlideCheckpoints[0].Area));
            GUI.color = Color.white;
            var starRect = Centered(first, playfield.width * 0.075f);
            if (!AirshiftStandardVisualResources.DrawSprite(ResolveStarSprite(skin, note), starRect, Color.white))
            {
                GUI.Label(Centered(first, playfield.width * 0.05f), "★", _smallStyle);
            }
        }

        /// <summary>Maps an AIRSHIFT note to the equivalent sprite from the shared STANDARD skin.</summary>
        private static Sprite? ResolveNoteSprite(CustomSkin? skin, AirshiftChartNote note)
        {
            if (skin == null)
            {
                return null;
            }
            if (note.IsSlideHead)
            {
                return ResolveStarSprite(skin, note);
            }
            return note.Kind switch
            {
                AirshiftChartNoteKind.Tap when note.IsBreak => skin.Tap_Break,
                AirshiftChartNoteKind.Tap when note.IsEx => skin.Tap_Ex,
                AirshiftChartNoteKind.Tap => skin.Tap,
                AirshiftChartNoteKind.Hold when note.IsBreak => skin.Hold_Break,
                AirshiftChartNoteKind.Hold when note.IsEx => skin.Hold_Ex,
                AirshiftChartNoteKind.Hold => skin.Hold,
                AirshiftChartNoteKind.Touch when note.IsBreak => skin.Touch_Break,
                AirshiftChartNoteKind.Touch => skin.Touch,
                AirshiftChartNoteKind.TouchHold when note.IsBreak => skin.TouchHold_Break[4],
                AirshiftChartNoteKind.TouchHold => skin.TouchHold[4],
                _ => null,
            };
        }

        /// <summary>Gets the shared STANDARD slide-arrow sprite for a chart note.</summary>
        private static Sprite? ResolveSlideSprite(CustomSkin? skin, AirshiftChartNote note)
        {
            if (skin == null)
            {
                return null;
            }
            return note.IsBreak ? skin.Slide_Break : skin.Slide;
        }

        /// <summary>Gets the shared STANDARD star sprite for a slide head.</summary>
        private static Sprite? ResolveStarSprite(CustomSkin? skin, AirshiftChartNote note)
        {
            if (skin == null)
            {
                return null;
            }
            if (note.IsBreak)
            {
                return skin.Star_Break;
            }
            if (note.IsEx)
            {
                return skin.Star_Ex;
            }
            return skin.Star;
        }

        private void DrawCursors(Rect playfield, AirshiftDualCursorTracker tracker)
        {
            DrawCursor(playfield, tracker.LeftHand, tracker.ActiveHand == HandId.Left, new Color32(0x34, 0xB8, 0xFF, 0xFF));
            DrawCursor(playfield, tracker.RightHand, tracker.ActiveHand == HandId.Right, new Color32(0xFF, 0x52, 0x6F, 0xFF));
        }

        private void DrawCursor(Rect playfield, AirshiftHandState hand, bool active, Color color)
        {
            var center = ToScreen(playfield, hand.Position);
            GUI.color = color;
            GUI.DrawTexture(Centered(center, playfield.width * (active ? 0.075f : 0.055f)), _disc);
            GUI.color = Color.white;
            GUI.Label(Centered(center, playfield.width * 0.05f), hand.HandId == HandId.Left ? "L" : "R", _smallStyle);
        }

        private void DrawHud(
            Rect subDisplay,
            AirshiftChartPlayer player,
            bool focused,
            string songTitle,
            string level,
            bool showDebugOverlay)
        {
            var padding = Mathf.Max(8f, subDisplay.width * 0.02f);
            var panelX = subDisplay.x + padding;
            var panelWidth = subDisplay.width - padding * 2f;
            GUI.color = Color.white;
            GUI.Label(new Rect(panelX, subDisplay.y + padding, panelWidth, subDisplay.height * 0.24f), "AIRSHIFT", _titleStyle);
            _hud.Clear();
            _hud.AppendLine(songTitle);
            _hud.Append("Difficulty: ").AppendLine(level);
            _hud.Append("Time: ").Append(player.CurrentTime.ToString("F2")).Append(" / ").AppendLine(player.AudioLength.ToString("F2"));
            _hud.Append("Status: ").AppendLine(player.Status);
            _hud.Append("Focus: ").AppendLine(focused ? "ACTIVE" : "PAUSED");
            _hud.AppendLine("F6: DEBUG OVERLAY");
            if (showDebugOverlay)
            {
                _hud.AppendLine();
                _hud.AppendLine("Tab switch hand  R recenter");
                _hud.AppendLine("F9 finish  F10 cancel");
                _hud.AppendLine("1-8 debug only (never judges)");
                foreach (var item in _feedback)
                {
                    _hud.AppendLine(item);
                }
            }
            GUI.Label(
                new Rect(
                    panelX,
                    subDisplay.y + subDisplay.height * 0.25f,
                    panelWidth,
                    subDisplay.height * 0.72f),
                _hud.ToString(),
                _labelStyle);
        }

        private static Color NoteColor(AirshiftChartNote note)
        {
            if (note.IsBreak)
            {
                return new Color32(255, 105, 35, 255);
            }
            if (note.IsEx)
            {
                return new Color32(255, 245, 150, 255);
            }
            return note.Kind switch
            {
                AirshiftChartNoteKind.Tap => new Color32(255, 90, 105, 255),
                AirshiftChartNoteKind.Hold => new Color32(255, 220, 70, 255),
                AirshiftChartNoteKind.Touch => new Color32(80, 220, 255, 255),
                AirshiftChartNoteKind.TouchHold => new Color32(80, 255, 175, 255),
                AirshiftChartNoteKind.Slide => new Color32(210, 120, 255, 255),
                _ => Color.white,
            };
        }

        internal static Vector2 AreaCenter(AirshiftSensorArea area)
        {
            if (area == AirshiftSensorArea.C)
            {
                return new Vector2(0.5f, 0.5f);
            }
            var value = (int)area;
            var groupStart = value switch
            {
                >= (int)AirshiftSensorArea.A1 and <= (int)AirshiftSensorArea.A8 => (int)AirshiftSensorArea.A1,
                >= (int)AirshiftSensorArea.B1 and <= (int)AirshiftSensorArea.B8 => (int)AirshiftSensorArea.B1,
                >= (int)AirshiftSensorArea.D1 and <= (int)AirshiftSensorArea.D8 => (int)AirshiftSensorArea.D1,
                >= (int)AirshiftSensorArea.E1 and <= (int)AirshiftSensorArea.E8 => (int)AirshiftSensorArea.E1,
                _ => -1,
            };
            if (groupStart < 0)
            {
                return new Vector2(0.5f, 0.5f);
            }
            var index = value - groupStart;
            var radius = groupStart switch
            {
                (int)AirshiftSensorArea.A1 => 0.86f,
                (int)AirshiftSensorArea.B1 => 0.38f,
                (int)AirshiftSensorArea.D1 => 0.96f,
                _ => 0.62f,
            };
            var angle = (67.5f - index * 45f) * Mathf.Deg2Rad;
            return new Vector2(0.5f, 0.5f) + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius * 0.5f;
        }

        private void EnsureStyles(Rect playfield, Rect subDisplay)
        {
            _titleStyle ??= CreateStyle(FontStyle.Bold, TextAnchor.MiddleCenter, 28);
            _labelStyle ??= CreateStyle(FontStyle.Normal, TextAnchor.UpperLeft, 17);
            _smallStyle ??= CreateStyle(FontStyle.Bold, TextAnchor.MiddleCenter, 12);
            _titleStyle.fontSize = Mathf.Clamp(Mathf.RoundToInt(subDisplay.height * 0.14f), 24, 64);
            _labelStyle.fontSize = Mathf.Clamp(Mathf.RoundToInt(subDisplay.height * 0.06f), 14, 30);
            _smallStyle.fontSize = Mathf.Clamp(Mathf.RoundToInt(playfield.height * 0.025f), 12, 28);
        }

        private static GUIStyle CreateStyle(FontStyle fontStyle, TextAnchor anchor, int fontSize)
        {
            var style = new GUIStyle(GUI.skin.label) { fontStyle = fontStyle, alignment = anchor, fontSize = fontSize, wordWrap = true };
            style.normal.textColor = Color.white;
            return style;
        }

        private static Vector2 ToScreen(Rect rect, Vector2 normalized)
        {
            return new Vector2(rect.x + normalized.x * rect.width, rect.y + (1f - normalized.y) * rect.height);
        }

        private static Rect Centered(Vector2 center, float size)
        {
            return new Rect(center.x - size * 0.5f, center.y - size * 0.5f, size, size);
        }

        private static void DrawLine(Vector2 from, Vector2 to, Color color, float width)
        {
            var oldMatrix = GUI.matrix;
            var delta = to - from;
            GUI.color = color;
            GUIUtility.RotateAroundPivot(Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg, from);
            GUI.DrawTexture(new Rect(from.x, from.y - width * 0.5f, delta.magnitude, width), Texture2D.whiteTexture);
            GUI.matrix = oldMatrix;
        }

        private static Texture2D CreateDiscTexture(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "AirshiftChartDisc" };
            var pixels = new Color32[size * size];
            var center = (size - 1) * 0.5f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var inside = new Vector2(x - center, y - center).sqrMagnitude <= center * center;
                    pixels[y * size + x] = inside ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }
    }
}
