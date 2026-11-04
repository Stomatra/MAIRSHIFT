using MajdataPlay.IO;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

#nullable enable
namespace MajdataPlay.Scenes.Airshift
{
    /// <summary>
    /// Displays the AIRSHIFT-only result and returns to its independent song list.
    /// </summary>
    public sealed class AirshiftResultManager : MonoBehaviour
    {
        /// <summary>The original STANDARD result-screen background.</summary>
        [SerializeField]
        private Sprite? _resultBackground;
        /// <summary>The original STANDARD result content panel.</summary>
        [SerializeField]
        private Sprite? _resultPanel;
        /// <summary>Whether this scene currently owns its A4 and A5 subscriptions.</summary>
        private bool _inputsBound;
        /// <summary>Whether one navigation request has already started.</summary>
        private bool _isExiting;
        /// <summary>The message displayed by the result placeholder UI.</summary>
        private string _resultText = string.Empty;
        /// <summary>The cached title label style created during the first IMGUI event.</summary>
        private GUIStyle? _titleStyle;
        /// <summary>The cached body label style created during the first IMGUI event.</summary>
        private GUIStyle? _bodyStyle;
        /// <summary>Reads the displays already positioned by the STANDARD CanvasTemplate.</summary>
        private readonly AirshiftStandardLayoutBridge _layout = new();

        private void Start()
        {
            var session = AirshiftSessionStore.Current;
            if (session?.Kind == AirshiftSessionKind.ChartPlay && session.Score is not null)
            {
                var score = session.Score;
                var record = session.StoredRecord;
                _resultText =
                    $"{session.Song?.Title ?? "UNKNOWN SONG"}  /  {session.Level}\n" +
                    $"SCORE  {score.Score:N0}    ACHIEVEMENT  {score.Achievement:F2}%\n" +
                    $"PERFECT {score.Perfect}   GREAT {score.Great}   GOOD {score.Good}   MISS {score.Miss}\n" +
                    $"MAX COMBO {score.MaxCombo}   FAST {score.Fast}   LATE {score.Late}\n" +
                    (record is null
                        ? "LOCAL RECORD NOT UPDATED\n"
                        : $"PLAYS {record.PlayCount}   BEST {record.BestScore:N0} / {record.BestAchievement:F2}%\n") +
                    (string.IsNullOrWhiteSpace(session.ResultMessage) ? string.Empty : session.ResultMessage + "\n") +
                    "ENTER / ESC / A4 / A5: RETURN TO AIRSHIFT LIST";
            }
            else
            {
                _resultText = session?.IsCompleted == true
                    ? "AIRSHIFT LOCAL TEST COMPLETE\nNO SCORE WAS GENERATED OR SAVED\nENTER / ESC / A4 / A5: RETURN TO LIST"
                    : "NO COMPLETED AIRSHIFT SESSION\nNO SCORE WAS GENERATED OR SAVED\nENTER / ESC / A4 / A5: RETURN TO LIST";
            }
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (!_isExiting && (keyboard?.enterKey.wasPressedThisFrame == true ||
                keyboard?.escapeKey.wasPressedThisFrame == true))
            {
                ReturnToList();
            }
        }

        private void OnGUI()
        {
            if (!_layout.TryGetDisplayRects(out var mainDisplay, out var subDisplay))
            {
                return;
            }
            var titleStyle = _titleStyle ??= CreateStyle(FontStyle.Bold, false);
            var bodyStyle = _bodyStyle ??= CreateStyle(FontStyle.Normal, true);
            var previousColor = GUI.color;
            if (!AirshiftStandardVisualResources.DrawSprite(
                _resultBackground,
                mainDisplay,
                Color.white,
                ScaleMode.StretchToFill))
            {
                GUI.color = new Color32(30, 18, 50, 255);
                GUI.DrawTexture(mainDisplay, Texture2D.whiteTexture);
            }
            if (!AirshiftStandardVisualResources.DrawSubDisplayBackground(subDisplay))
            {
                GUI.DrawTexture(subDisplay, Texture2D.whiteTexture);
            }
            GUI.color = new Color(0f, 0f, 0f, 0.35f);
            GUI.DrawTexture(subDisplay, Texture2D.whiteTexture);
            GUI.color = Color.white;
            titleStyle.fontSize = Mathf.Clamp(Mathf.RoundToInt(subDisplay.height * 0.16f), 28, 72);
            bodyStyle.fontSize = Mathf.Clamp(Mathf.RoundToInt(mainDisplay.height * 0.035f), 18, 42);
            GUI.Label(subDisplay, "AIRSHIFT RESULT", titleStyle);
            AirshiftStandardVisualResources.DrawSprite(
                _resultPanel,
                new Rect(
                    mainDisplay.x + mainDisplay.width * 0.04f,
                    mainDisplay.y + mainDisplay.height * 0.1f,
                    mainDisplay.width * 0.92f,
                    mainDisplay.height * 0.8f),
                Color.white,
                ScaleMode.StretchToFill);
            GUI.Label(
                new Rect(
                    mainDisplay.x + mainDisplay.width * 0.08f,
                    mainDisplay.y + mainDisplay.height * 0.16f,
                    mainDisplay.width * 0.84f,
                    mainDisplay.height * 0.68f),
                _resultText,
                bodyStyle);
            GUI.color = previousColor;
        }

        /// <summary>
        /// Creates one of the result placeholder UI styles during an IMGUI event.
        /// </summary>
        /// <param name="fontStyle">The requested font emphasis.</param>
        /// <param name="wordWrap">Whether the style wraps long lines.</param>
        /// <returns>The configured placeholder label style.</returns>
        private static GUIStyle CreateStyle(FontStyle fontStyle, bool wordWrap)
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = fontStyle,
                wordWrap = wordWrap,
            };
            style.normal.textColor = Color.white;
            return style;
        }

        private void OnEnable()
        {
            BindInputs();
        }

        private void OnDisable()
        {
            UnbindInputs();
        }

        private void OnDestroy()
        {
            UnbindInputs();
            AirshiftSessionStore.Clear();
        }

        /// <summary>
        /// Subscribes the result navigation buttons.
        /// </summary>
        private void BindInputs()
        {
            if (_inputsBound)
            {
                return;
            }
            InputManager.BindButton(OnReturnToList, ButtonZone.A4);
            try
            {
                InputManager.BindButton(OnReturnToList, ButtonZone.A5);
            }
            catch
            {
                InputManager.UnbindButton(OnReturnToList, ButtonZone.A4);
                throw;
            }
            _inputsBound = true;
        }

        /// <summary>
        /// Removes all input subscriptions owned by the result scene.
        /// </summary>
        private void UnbindInputs()
        {
            if (!_inputsBound)
            {
                return;
            }
            InputManager.UnbindButton(OnReturnToList, ButtonZone.A4);
            InputManager.UnbindButton(OnReturnToList, ButtonZone.A5);
            _inputsBound = false;
        }

        /// <summary>
        /// Clears the AIRSHIFT session and returns to the song list.
        /// </summary>
        /// <param name="sender">The input source, which is unused.</param>
        /// <param name="args">The button state transition.</param>
        private void OnReturnToList(object? sender, InputEventArgs args)
        {
            if (!args.IsDown || _isExiting)
            {
                return;
            }
            _isExiting = true;
            ReturnToList();
        }

        /// <summary>Returns to AIRSHIFT selection through one idempotent path.</summary>
        private void ReturnToList()
        {
            _isExiting = true;
            AirshiftSessionStore.Clear();
            UnbindInputs();
            MajInstances.SceneSwitcher.SwitchScene(MajScenes.AirshiftList, false);
        }
    }
}
