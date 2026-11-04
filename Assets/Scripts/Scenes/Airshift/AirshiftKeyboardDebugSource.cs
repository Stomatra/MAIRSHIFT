using System;
using UnityEngine.InputSystem;

#nullable enable
namespace MajdataPlay.Scenes.Airshift
{
    /// <summary>
    /// Represents scene-local AIRSHIFT development commands from one keyboard poll.
    /// </summary>
    internal readonly struct AirshiftDebugCommands
    {
        /// <summary>Gets whether F9 requested the placeholder result.</summary>
        internal bool Finish { get; }
        /// <summary>Gets whether F10 requested the song list.</summary>
        internal bool Cancel { get; }
        /// <summary>Gets whether Tab requested a hand switch.</summary>
        internal bool ToggleHand { get; }
        /// <summary>Gets whether R requested cursor re-centering.</summary>
        internal bool Recenter { get; }
        /// <summary>Gets whether F8 requested a local TAP chart restart.</summary>
        internal bool RestartTapTest { get; }
        /// <summary>Gets whether F6 requested a debug-overlay visibility toggle.</summary>
        internal bool ToggleDebugOverlay { get; }

        /// <summary>
        /// Creates one command snapshot.
        /// </summary>
        /// <param name="finish">Whether F9 was pressed.</param>
        /// <param name="cancel">Whether F10 was pressed.</param>
        /// <param name="toggleHand">Whether Tab was pressed.</param>
        /// <param name="recenter">Whether R was pressed.</param>
        /// <param name="restartTapTest">Whether F8 was pressed.</param>
        /// <param name="toggleDebugOverlay">Whether F6 was pressed.</param>
        internal AirshiftDebugCommands(
            bool finish,
            bool cancel,
            bool toggleHand,
            bool recenter,
            bool restartTapTest,
            bool toggleDebugOverlay)
        {
            Finish = finish;
            Cancel = cancel;
            ToggleHand = toggleHand;
            Recenter = recenter;
            RestartTapTest = restartTapTest;
            ToggleDebugOverlay = toggleDebugOverlay;
        }
    }

    /// <summary>
    /// Polls AIRSHIFT-only development keys without injecting events into STANDARD <c>InputManager</c>.
    /// </summary>
    internal sealed class AirshiftKeyboardDebugSource
    {
        /// <summary>Main-row number keys in A1-A8 order.</summary>
        private static readonly Key[] s_areaKeys =
        {
            Key.Digit1,
            Key.Digit2,
            Key.Digit3,
            Key.Digit4,
            Key.Digit5,
            Key.Digit6,
            Key.Digit7,
            Key.Digit8,
        };

        /// <summary>The last reported state of every debug area key.</summary>
        private readonly bool[] _pressedStates = new bool[8];

        /// <summary>Raised for independent main-row A1-A8 press and release transitions.</summary>
        internal event Action<DebugButtonEvent>? DebugButtonChanged;

        /// <summary>
        /// Polls development commands and emits changed debug button states.
        /// </summary>
        /// <param name="timestamp">The monotonic poll timestamp.</param>
        /// <param name="isFocused">Whether the Game view currently owns keyboard focus.</param>
        /// <returns>The scene navigation and cursor commands pressed this frame.</returns>
        internal AirshiftDebugCommands Poll(double timestamp, bool isFocused)
        {
            var keyboard = Keyboard.current;
            if (!isFocused || keyboard is null)
            {
                ReleaseAll(timestamp);
                return default;
            }
            for (var i = 0; i < s_areaKeys.Length; i++)
            {
                var isPressed = keyboard[s_areaKeys[i]].isPressed;
                UpdateAreaKeyState(i, isPressed, timestamp);
            }
            return new AirshiftDebugCommands(
                keyboard.f9Key.wasPressedThisFrame,
                keyboard.f10Key.wasPressedThisFrame,
                keyboard.tabKey.wasPressedThisFrame,
                keyboard.rKey.wasPressedThisFrame,
                keyboard.f8Key.wasPressedThisFrame,
                keyboard.f6Key.wasPressedThisFrame);
        }

        /// <summary>
        /// Releases any remembered debug keys when focus or scene ownership ends.
        /// </summary>
        /// <param name="timestamp">The monotonic release timestamp.</param>
        internal void ReleaseAll(double timestamp)
        {
            for (var i = 0; i < _pressedStates.Length; i++)
            {
                UpdateAreaKeyState(i, false, timestamp);
            }
        }

        /// <summary>
        /// Applies one observed key state and emits an event only when that state changes.
        /// </summary>
        /// <param name="keyIndex">The zero-based A1-A8 key index.</param>
        /// <param name="isPressed">Whether the key is currently pressed.</param>
        /// <param name="timestamp">The monotonic event timestamp.</param>
        /// <exception cref="ArgumentOutOfRangeException">The key index is outside A1-A8.</exception>
        internal void UpdateAreaKeyState(int keyIndex, bool isPressed, double timestamp)
        {
            if ((uint)keyIndex >= _pressedStates.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(keyIndex));
            }
            if (isPressed == _pressedStates[keyIndex])
            {
                return;
            }
            _pressedStates[keyIndex] = isPressed;
            DebugButtonChanged?.Invoke(new DebugButtonEvent(
                (AirshiftArea)(keyIndex + 1),
                isPressed,
                timestamp));
        }

        /// <summary>
        /// Maps a Unity key to an AIRSHIFT debug area for tests and future remapping UI.
        /// </summary>
        /// <param name="key">The Unity Input System key.</param>
        /// <returns>A1-A8 for main-row digits, otherwise <see cref="AirshiftArea.None"/>.</returns>
        internal static AirshiftArea GetAreaForKey(Key key)
        {
            for (var i = 0; i < s_areaKeys.Length; i++)
            {
                if (s_areaKeys[i] == key)
                {
                    return (AirshiftArea)(i + 1);
                }
            }
            return AirshiftArea.None;
        }
    }
}
