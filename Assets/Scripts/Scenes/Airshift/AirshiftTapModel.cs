using System;
using UnityEngine;

#nullable enable
namespace MajdataPlay.Scenes.Airshift
{
    /// <summary>
    /// Identifies how one AIRSHIFT TAP gesture was recognized.
    /// </summary>
    internal enum AirshiftTapTriggerType
    {
        /// <summary>A sufficiently fast trajectory entered an outer area.</summary>
        EntryFlick = 0,
        /// <summary>A newly armed velocity burst occurred while already inside an outer area.</summary>
        InAreaStrike = 1,
    }

    /// <summary>
    /// Specifies the hand accepted by one local-test TAP note.
    /// </summary>
    internal enum AirshiftRequiredHand
    {
        /// <summary>Either hand is accepted.</summary>
        Any = 0,
        /// <summary>Only the left hand is accepted.</summary>
        Left = 1,
        /// <summary>Only the right hand is accepted.</summary>
        Right = 2,
    }

    /// <summary>
    /// Represents the lifecycle and judgement of one local-test TAP note.
    /// </summary>
    internal enum AirshiftTapNoteState
    {
        /// <summary>The note is waiting for an eligible gesture.</summary>
        Pending = 0,
        /// <summary>The note was hit inside the prototype PERFECT window.</summary>
        Perfect = 1,
        /// <summary>The note was hit inside the prototype GREAT window.</summary>
        Great = 2,
        /// <summary>The note was hit inside the prototype GOOD window.</summary>
        Good = 3,
        /// <summary>The note passed its prototype hit window.</summary>
        Miss = 4,
    }

    /// <summary>
    /// Describes one independent no-click TAP action recognized from AIRSHIFT motion.
    /// </summary>
    internal readonly struct TapGesture
    {
        /// <summary>Gets the process-local unique action identifier.</summary>
        internal long ActionId { get; }
        /// <summary>Gets the hand that produced the gesture.</summary>
        internal HandId HandId { get; }
        /// <summary>Gets the outer area targeted by the gesture.</summary>
        internal AirshiftArea Area { get; }
        /// <summary>Gets the monotonic source timestamp.</summary>
        internal double Timestamp { get; }
        /// <summary>Gets how the gesture was recognized.</summary>
        internal AirshiftTapTriggerType TriggerType { get; }
        /// <summary>Gets the normalized playfield speed at recognition.</summary>
        internal float Speed { get; }
        /// <summary>Gets the normalized movement direction at recognition.</summary>
        internal Vector2 Direction { get; }
        /// <summary>Gets the motion source that produced the gesture.</summary>
        internal AirshiftInputSource InputSource { get; }

        /// <summary>
        /// Creates one immutable TAP gesture.
        /// </summary>
        /// <param name="actionId">The unique action identifier.</param>
        /// <param name="handId">The originating hand.</param>
        /// <param name="area">The targeted outer area.</param>
        /// <param name="timestamp">The monotonic source timestamp.</param>
        /// <param name="triggerType">How the gesture was recognized.</param>
        /// <param name="speed">The normalized playfield speed.</param>
        /// <param name="direction">The normalized motion direction.</param>
        /// <param name="inputSource">The originating motion source.</param>
        internal TapGesture(
            long actionId,
            HandId handId,
            AirshiftArea area,
            double timestamp,
            AirshiftTapTriggerType triggerType,
            float speed,
            Vector2 direction,
            AirshiftInputSource inputSource)
        {
            ActionId = actionId;
            HandId = handId;
            Area = area;
            Timestamp = timestamp;
            TriggerType = triggerType;
            Speed = speed;
            Direction = direction;
            InputSource = inputSource;
        }
    }

    /// <summary>
    /// Centralizes normalized motion thresholds for the no-click TAP prototype.
    /// </summary>
    internal readonly struct AirshiftTapGestureSettings
    {
        /// <summary>Gets the minimum speed accepted for an entry flick.</summary>
        internal float EntrySpeed { get; }
        /// <summary>Gets the speed that triggers an armed in-area strike.</summary>
        internal float StrikeSpeed { get; }
        /// <summary>Gets the lower hysteresis threshold that rearms an in-area strike.</summary>
        internal float RearmSpeed { get; }
        /// <summary>Gets the maximum direction dot product considered a reversal.</summary>
        internal float ReverseDirectionDot { get; }
        /// <summary>Gets the minimum duration between gestures from one hand.</summary>
        internal double MinimumGestureInterval { get; }

        /// <summary>Gets the non-official prototype motion settings.</summary>
        internal static AirshiftTapGestureSettings Prototype => new(
            entrySpeed: 1.2f,
            strikeSpeed: 1.35f,
            rearmSpeed: 0.45f,
            reverseDirectionDot: -0.35f,
            minimumGestureInterval: 0.03d);

        /// <summary>
        /// Creates adjustable normalized gesture thresholds.
        /// </summary>
        /// <param name="entrySpeed">The minimum entry-flick speed.</param>
        /// <param name="strikeSpeed">The in-area strike trigger speed.</param>
        /// <param name="rearmSpeed">The lower rearm threshold.</param>
        /// <param name="reverseDirectionDot">The reversal direction dot threshold.</param>
        /// <param name="minimumGestureInterval">The minimum interval between gestures.</param>
        /// <exception cref="ArgumentOutOfRangeException">A threshold is inconsistent or outside its valid range.</exception>
        internal AirshiftTapGestureSettings(
            float entrySpeed,
            float strikeSpeed,
            float rearmSpeed,
            float reverseDirectionDot,
            double minimumGestureInterval)
        {
            if (entrySpeed <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(entrySpeed));
            }
            if (strikeSpeed <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(strikeSpeed));
            }
            if (rearmSpeed < 0f || rearmSpeed >= strikeSpeed)
            {
                throw new ArgumentOutOfRangeException(nameof(rearmSpeed));
            }
            if (reverseDirectionDot is < -1f or > 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(reverseDirectionDot));
            }
            if (minimumGestureInterval < 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(minimumGestureInterval));
            }
            EntrySpeed = entrySpeed;
            StrikeSpeed = strikeSpeed;
            RearmSpeed = rearmSpeed;
            ReverseDirectionDot = reverseDirectionDot;
            MinimumGestureInterval = minimumGestureInterval;
        }
    }

    /// <summary>
    /// Centralizes the non-official timing windows used by the local TAP prototype.
    /// </summary>
    internal readonly struct AirshiftTapJudgeSettings
    {
        /// <summary>Gets the symmetric prototype PERFECT window in seconds.</summary>
        internal double PerfectWindow { get; }
        /// <summary>Gets the symmetric prototype GREAT window in seconds.</summary>
        internal double GreatWindow { get; }
        /// <summary>Gets the symmetric prototype GOOD and MISS boundary in seconds.</summary>
        internal double GoodWindow { get; }

        /// <summary>Gets the non-official prototype timing windows.</summary>
        internal static AirshiftTapJudgeSettings Prototype => new(
            perfectWindow: 0.08d,
            greatWindow: 0.16d,
            goodWindow: 0.25d);

        /// <summary>
        /// Creates adjustable local-test timing windows.
        /// </summary>
        /// <param name="perfectWindow">The PERFECT boundary in seconds.</param>
        /// <param name="greatWindow">The GREAT boundary in seconds.</param>
        /// <param name="goodWindow">The GOOD and MISS boundary in seconds.</param>
        /// <exception cref="ArgumentOutOfRangeException">The windows are negative or not ordered.</exception>
        internal AirshiftTapJudgeSettings(double perfectWindow, double greatWindow, double goodWindow)
        {
            if (perfectWindow < 0d || greatWindow < perfectWindow || goodWindow < greatWindow)
            {
                throw new ArgumentOutOfRangeException(nameof(perfectWindow));
            }
            PerfectWindow = perfectWindow;
            GreatWindow = greatWindow;
            GoodWindow = goodWindow;
        }
    }
}
