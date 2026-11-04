using Cysharp.Threading.Tasks;
using MajdataPlay.IO;
using MajdataPlay.Scenes.Setting;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;

#nullable enable
namespace MajdataPlay.Scenes.Airshift
{
    /// <summary>
    /// Provides a lightweight local-only AIRSHIFT song and difficulty selector.
    /// </summary>
    public sealed class AirshiftListManager : MonoBehaviour
    {
        /// <summary>The original STANDARD large-cover frame sprite.</summary>
        [SerializeField]
        private Sprite? _coverBackground;
        /// <summary>The local songs visible to AIRSHIFT.</summary>
        private readonly List<ISongDetail> _songs = new();
        /// <summary>Cancels cover loading when this scene leaves.</summary>
        private readonly CancellationTokenSource _cancellationSource = new();
        /// <summary>The current local-song index.</summary>
        private int _songIndex;
        /// <summary>The current difficulty index.</summary>
        private int _levelIndex;
        /// <summary>The generation of the latest requested cover.</summary>
        private int _coverGeneration;
        /// <summary>The current cover sprite borrowed from the song cache.</summary>
        private Sprite? _cover;
        /// <summary>Whether one navigation request is already in progress.</summary>
        private bool _isExiting;
        /// <summary>The current user-visible status.</summary>
        private string _status = "LOADING LOCAL SONGS";
        /// <summary>The cached title style.</summary>
        private GUIStyle? _titleStyle;
        /// <summary>The cached body style.</summary>
        private GUIStyle? _bodyStyle;
        /// <summary>The cached centered style.</summary>
        private GUIStyle? _centerStyle;
        /// <summary>Reads the displays already positioned by the STANDARD CanvasTemplate.</summary>
        private readonly AirshiftStandardLayoutBridge _layout = new();

        private void Start()
        {
            BuildLocalSongList();
        }

        private void Update()
        {
            if (!_isExiting && IsSettingPressed())
            {
                OpenSharedSetting();
                return;
            }
            if (_isExiting || _songs.Count == 0)
            {
                if (!_isExiting && IsBackPressed())
                {
                    ReturnToStandardList();
                }
                return;
            }

            var keyboard = Keyboard.current;
            var songDirection = 0;
            if (keyboard?.leftArrowKey.wasPressedThisFrame == true ||
                InputManager.GetButtonState(ButtonZone.A3).ClickCompletedThisFrame)
            {
                songDirection = -1;
            }
            else if (keyboard?.rightArrowKey.wasPressedThisFrame == true ||
                InputManager.GetButtonState(ButtonZone.A6).ClickCompletedThisFrame)
            {
                songDirection = 1;
            }
            if (songDirection != 0)
            {
                SelectSong(songDirection);
                return;
            }

            var levelDirection = 0;
            if (keyboard?.downArrowKey.wasPressedThisFrame == true ||
                InputManager.GetButtonState(ButtonZone.A8).ClickCompletedThisFrame)
            {
                levelDirection = -1;
            }
            else if (keyboard?.upArrowKey.wasPressedThisFrame == true ||
                InputManager.GetButtonState(ButtonZone.A1).ClickCompletedThisFrame)
            {
                levelDirection = 1;
            }
            if (levelDirection != 0)
            {
                SelectDifficulty(levelDirection);
                return;
            }

            if (keyboard?.enterKey.wasPressedThisFrame == true ||
                InputManager.GetButtonState(ButtonZone.A4).ClickCompletedThisFrame)
            {
                StartSelectedChart();
                return;
            }
            if (keyboard?.f7Key.wasPressedThisFrame == true)
            {
                StartLocalInputTest();
                return;
            }
            if (IsBackPressed())
            {
                ReturnToStandardList();
            }
        }

        private void OnGUI()
        {
            if (!_layout.TryGetDisplayRects(out var mainDisplay, out var subDisplay))
            {
                return;
            }
            EnsureStyles(mainDisplay, subDisplay);
            var previousColor = GUI.color;
            GUI.color = new Color32(8, 20, 36, 255);
            GUI.DrawTexture(mainDisplay, Texture2D.whiteTexture);
            if (!AirshiftStandardVisualResources.DrawSubDisplayBackground(subDisplay))
            {
                GUI.DrawTexture(subDisplay, Texture2D.whiteTexture);
            }
            GUI.color = new Color(0f, 0f, 0f, 0.35f);
            GUI.DrawTexture(subDisplay, Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUI.Label(
                subDisplay,
                "AIRSHIFT SONG SELECT",
                _titleStyle);

            if (_songs.Count == 0)
            {
                GUI.Label(
                    new Rect(
                        mainDisplay.x + mainDisplay.width * 0.1f,
                        mainDisplay.y + mainDisplay.height * 0.25f,
                        mainDisplay.width * 0.8f,
                        mainDisplay.height * 0.5f),
                    $"{_status}\n\nEsc / A5: RETURN TO STANDARD LIST",
                    _centerStyle);
                GUI.color = previousColor;
                return;
            }

            var song = _songs[_songIndex];
            var coverSize = Mathf.Min(mainDisplay.height * 0.46f, mainDisplay.width * 0.4f);
            var coverRect = new Rect(
                mainDisplay.x + mainDisplay.width * 0.07f,
                mainDisplay.y + mainDisplay.height * 0.18f,
                coverSize,
                coverSize);
            var coverBackgroundRect = new Rect(
                coverRect.x - coverRect.width * 0.04f,
                coverRect.y - coverRect.height * 0.04f,
                coverRect.width * 1.08f,
                coverRect.height * 1.08f);
            if (!AirshiftStandardVisualResources.DrawSprite(
                _coverBackground,
                coverBackgroundRect,
                Color.white,
                ScaleMode.StretchToFill))
            {
                GUI.color = new Color32(24, 45, 70, 255);
                GUI.Box(coverRect, GUIContent.none);
            }
            GUI.color = Color.white;
            if (_cover != null)
            {
                GUI.DrawTexture(coverRect, _cover.texture, ScaleMode.ScaleToFit, true);
            }

            var infoRect = new Rect(
                coverRect.xMax + mainDisplay.width * 0.04f,
                coverRect.y,
                mainDisplay.xMax - coverRect.xMax - mainDisplay.width * 0.08f,
                coverRect.height);
            var levels = song.Levels;
            var levelText = _levelIndex < levels.Length ? levels[_levelIndex] : string.Empty;
            var info =
                $"{song.Title}\n" +
                $"{song.Artist}\n\n" +
                $"DIFFICULTY: {(ChartLevel)_levelIndex}  {levelText}\n" +
                $"SONG {_songIndex + 1} / {_songs.Count}\n" +
                $"LOCAL CHART\n\n" +
                $"{_status}";
            GUI.Label(infoRect, info, _bodyStyle);

            GUI.Label(
                new Rect(
                    mainDisplay.x + mainDisplay.width * 0.05f,
                    mainDisplay.y + mainDisplay.height * 0.78f,
                    mainDisplay.width * 0.9f,
                    mainDisplay.height * 0.17f),
                "←/→ or A3/A6: SONG    ↑/↓ or A1/A8: DIFFICULTY    ENTER/A4: PLAY\n" +
                "F1/A7: SETTINGS    F7: TASK 005 INPUT TEST    ESC/A5: STANDARD LIST",
                _centerStyle);
            GUI.color = previousColor;
        }

        private void OnDestroy()
        {
            _cancellationSource.Cancel();
            _cancellationSource.Dispose();
        }

        /// <summary>
        /// Builds a deduplicated snapshot of local songs from the shared song library.
        /// </summary>
        private void BuildLocalSongList()
        {
            _songs.Clear();
            var hashes = new HashSet<string>(StringComparer.Ordinal);
            var collections = SongStorage.Collections;
            for (var collectionIndex = 0; collectionIndex < collections.Length; collectionIndex++)
            {
                var collection = collections[collectionIndex];
                if (collection.IsOnline)
                {
                    continue;
                }
                for (var songIndex = 0; songIndex < collection.Count; songIndex++)
                {
                    var song = collection[songIndex];
                    if (song.IsOnline || !hashes.Add(song.Hash))
                    {
                        continue;
                    }
                    _songs.Add(song);
                }
            }

            if (_songs.Count == 0)
            {
                _status = "NO LOCAL SONGS AVAILABLE";
                return;
            }
            _songIndex = AirshiftSelectionStore.ResolveSongIndex(_songs);
            _levelIndex = AirshiftSelectionStore.SelectedLevel;
            NormalizeDifficulty(1);
            UpdateSelection();
        }

        /// <summary>
        /// Moves the selected song while preserving a valid local difficulty.
        /// </summary>
        /// <param name="direction">The signed selection direction.</param>
        private void SelectSong(int direction)
        {
            _songIndex = Wrap(_songIndex + direction, _songs.Count);
            NormalizeDifficulty(direction);
            UpdateSelection();
        }

        /// <summary>
        /// Moves to the next available difficulty for the current song.
        /// </summary>
        /// <param name="direction">The signed selection direction.</param>
        private void SelectDifficulty(int direction)
        {
            var song = _songs[_songIndex];
            for (var i = 0; i < Enum.GetValues(typeof(ChartLevel)).Length; i++)
            {
                _levelIndex = Wrap(_levelIndex + direction, Enum.GetValues(typeof(ChartLevel)).Length);
                if (HasChart(song, _levelIndex))
                {
                    UpdateSelection();
                    return;
                }
            }
            _status = "NO PLAYABLE DIFFICULTY";
        }

        /// <summary>
        /// Ensures the selected difficulty exists for the current song.
        /// </summary>
        /// <param name="direction">The preferred search direction.</param>
        private void NormalizeDifficulty(int direction)
        {
            var song = _songs[_songIndex];
            _levelIndex = Mathf.Clamp(_levelIndex, 0, Enum.GetValues(typeof(ChartLevel)).Length - 1);
            if (HasChart(song, _levelIndex))
            {
                return;
            }
            for (var i = 0; i < Enum.GetValues(typeof(ChartLevel)).Length; i++)
            {
                _levelIndex = Wrap(_levelIndex + direction, Enum.GetValues(typeof(ChartLevel)).Length);
                if (HasChart(song, _levelIndex))
                {
                    return;
                }
            }
        }

        /// <summary>
        /// Stores the current selection and asynchronously requests its cover.
        /// </summary>
        private void UpdateSelection()
        {
            var song = _songs[_songIndex];
            AirshiftSelectionStore.Remember(song, (ChartLevel)_levelIndex);
            _status = HasChart(song, _levelIndex) ? "READY" : "NO CHART AT THIS DIFFICULTY";
            LoadCoverAsync(song, ++_coverGeneration).Forget();
        }

        /// <summary>
        /// Loads one cached cover without allowing stale requests to replace the active selection.
        /// </summary>
        /// <param name="song">The song whose cover is requested.</param>
        /// <param name="generation">The selection generation owning the request.</param>
        /// <returns>An asynchronous cover-loading operation.</returns>
        private async UniTask LoadCoverAsync(ISongDetail song, int generation)
        {
            try
            {
                var cover = await song.GetCoverAsync(true, token: _cancellationSource.Token);
                if (generation == _coverGeneration)
                {
                    _cover = cover;
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                if (generation == _coverGeneration)
                {
                    _cover = null;
                    _status = $"COVER LOAD FAILED: {exception.Message}";
                }
            }
        }

        /// <summary>
        /// Starts a playable local-chart AIRSHIFT session.
        /// </summary>
        private void StartSelectedChart()
        {
            var song = _songs[_songIndex];
            if (!HasChart(song, _levelIndex))
            {
                _status = "SELECT A PLAYABLE DIFFICULTY";
                return;
            }
            _isExiting = true;
            AirshiftSessionStore.StartChart(song, (ChartLevel)_levelIndex);
            MajInstances.SceneSwitcher.SwitchScene(MajScenes.AirshiftGame, false);
        }

        /// <summary>
        /// Retains the explicit Task 005 input-test entry without treating it as a scored chart.
        /// </summary>
        private void StartLocalInputTest()
        {
            _isExiting = true;
            AirshiftSessionStore.StartLocalTest();
            MajInstances.SceneSwitcher.SwitchScene(MajScenes.AirshiftGame, false);
        }

        /// <summary>
        /// Opens the original Setting scene while preserving the AIRSHIFT selector as its return target.
        /// </summary>
        private void OpenSharedSetting()
        {
            _isExiting = true;
            SettingReturnContext.Begin(MajScenes.AirshiftList);
            MajInstances.SceneSwitcher.SwitchScene(MajScenes.Setting, false);
        }

        /// <summary>
        /// Leaves the independent selector and restores the existing STANDARD list.
        /// </summary>
        private void ReturnToStandardList()
        {
            _isExiting = true;
            AirshiftSessionStore.Clear();
            MajInstances.SceneSwitcher.SwitchScene(MajScenes.List, false);
        }

        /// <summary>
        /// Checks the keyboard and cabinet back controls.
        /// </summary>
        /// <returns>Whether a back action completed this frame.</returns>
        private static bool IsBackPressed()
        {
            return Keyboard.current?.escapeKey.wasPressedThisFrame == true ||
                InputManager.GetButtonState(ButtonZone.A5).ClickCompletedThisFrame;
        }

        /// <summary>
        /// Checks the original selector settings button and a keyboard-only development shortcut.
        /// </summary>
        /// <returns>Whether the shared settings scene was requested this frame.</returns>
        private static bool IsSettingPressed()
        {
            return Keyboard.current?.f1Key.wasPressedThisFrame == true ||
                InputManager.GetButtonState(ButtonZone.A7).ClickCompletedThisFrame;
        }

        /// <summary>
        /// Checks whether one song exposes a non-empty chart at an index.
        /// </summary>
        /// <param name="song">The selected song.</param>
        /// <param name="levelIndex">The difficulty index.</param>
        /// <returns>Whether the chart is available.</returns>
        private static bool HasChart(ISongDetail song, int levelIndex)
        {
            var levels = song.Levels;
            return levelIndex >= 0 &&
                levelIndex < levels.Length &&
                !string.IsNullOrWhiteSpace(levels[levelIndex]);
        }

        /// <summary>
        /// Wraps an integer into a positive selection range.
        /// </summary>
        /// <param name="value">The value to wrap.</param>
        /// <param name="count">The positive range size.</param>
        /// <returns>The wrapped index.</returns>
        private static int Wrap(int value, int count)
        {
            return ((value % count) + count) % count;
        }

        /// <summary>
        /// Creates debug UI styles inside a valid IMGUI event.
        /// </summary>
        private void EnsureStyles(Rect mainDisplay, Rect subDisplay)
        {
            _titleStyle ??= CreateStyle(FontStyle.Bold, TextAnchor.MiddleCenter, false, 30);
            _bodyStyle ??= CreateStyle(FontStyle.Bold, TextAnchor.UpperLeft, true, 20);
            _centerStyle ??= CreateStyle(FontStyle.Normal, TextAnchor.MiddleCenter, true, 18);
            _titleStyle.fontSize = Mathf.Clamp(Mathf.RoundToInt(subDisplay.height * 0.14f), 24, 64);
            _bodyStyle.fontSize = Mathf.Clamp(Mathf.RoundToInt(mainDisplay.height * 0.032f), 16, 38);
            _centerStyle.fontSize = Mathf.Clamp(Mathf.RoundToInt(mainDisplay.height * 0.028f), 15, 34);
        }

        /// <summary>
        /// Creates one white IMGUI text style.
        /// </summary>
        /// <param name="fontStyle">The requested font weight.</param>
        /// <param name="alignment">The requested alignment.</param>
        /// <param name="wordWrap">Whether text may wrap.</param>
        /// <param name="fontSize">The font size.</param>
        /// <returns>The configured style.</returns>
        private static GUIStyle CreateStyle(
            FontStyle fontStyle,
            TextAnchor alignment,
            bool wordWrap,
            int fontSize)
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                alignment = alignment,
                fontStyle = fontStyle,
                wordWrap = wordWrap,
                fontSize = fontSize,
            };
            style.normal.textColor = Color.white;
            return style;
        }
    }

    /// <summary>
    /// Remembers AIRSHIFT selection independently from STANDARD list configuration.
    /// </summary>
    internal static class AirshiftSelectionStore
    {
        /// <summary>The last selected local song hash.</summary>
        private static string? s_songHash;
        /// <summary>Gets the last selected AIRSHIFT difficulty.</summary>
        internal static int SelectedLevel { get; private set; } = (int)ChartLevel.Master;

        /// <summary>
        /// Stores one local song and difficulty selection for the current process.
        /// </summary>
        /// <param name="song">The selected song.</param>
        /// <param name="level">The selected difficulty.</param>
        internal static void Remember(ISongDetail song, ChartLevel level)
        {
            s_songHash = song.Hash;
            SelectedLevel = (int)level;
        }

        /// <summary>
        /// Finds the remembered song in a fresh local snapshot.
        /// </summary>
        /// <param name="songs">The current local songs.</param>
        /// <returns>The remembered song index, or zero.</returns>
        internal static int ResolveSongIndex(IReadOnlyList<ISongDetail> songs)
        {
            if (s_songHash is null)
            {
                return 0;
            }
            for (var i = 0; i < songs.Count; i++)
            {
                if (string.Equals(songs[i].Hash, s_songHash, StringComparison.Ordinal))
                {
                    return i;
                }
            }
            return 0;
        }
    }
}
