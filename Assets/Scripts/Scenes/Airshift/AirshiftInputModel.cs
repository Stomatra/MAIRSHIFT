using System;
using UnityEngine;

#nullable enable
namespace MajdataPlay.Scenes.Airshift
{
    /// <summary>
    /// Identifies the hand that produced an AIRSHIFT input sample.
    /// </summary>
    internal enum HandId
    {
        /// <summary>The blue left-hand cursor.</summary>
        Left = 0,
        /// <summary>The red right-hand cursor.</summary>
        Right = 1,
    }

    /// <summary>
    /// Identifies an AIRSHIFT test area without reusing STANDARD input enums.
    /// </summary>
    internal enum AirshiftArea
    {
        /// <summary>No active region.</summary>
        None = 0,
        /// <summary>Outer region A1.</summary>
        A1 = 1,
        /// <summary>Outer region A2.</summary>
        A2 = 2,
        /// <summary>Outer region A3.</summary>
        A3 = 3,
        /// <summary>Outer region A4.</summary>
        A4 = 4,
        /// <summary>Outer region A5.</summary>
        A5 = 5,
        /// <summary>Outer region A6.</summary>
        A6 = 6,
        /// <summary>Outer region A7.</summary>
        A7 = 7,
        /// <summary>Outer region A8.</summary>
        A8 = 8,
        /// <summary>Reserved identifier range for the future inner TOUCH detector.</summary>
        TouchReserved = 100,
    }

    /// <summary>
    /// Identifies the isolated source that produced AIRSHIFT prototype data.
    /// </summary>
    internal enum AirshiftInputSource
    {
        /// <summary>Relative mouse motion used for local simulation.</summary>
        MouseSimulation = 0,
        /// <summary>Main-row keyboard buttons used for local debugging.</summary>
        KeyboardDebug = 1,
    }

    /// <summary>
    /// Identifies whether a cursor entered or exited a detected area.
    /// </summary>
    internal enum AirshiftAreaTransitionKind
    {
        /// <summary>The cursor entered an area.</summary>
        Enter = 0,
        /// <summary>The cursor exited an area.</summary>
        Exit = 1,
    }

    /// <summary>
    /// Represents one motion sample in normalized AIRSHIFT playfield coordinates.
    /// </summary>
    internal readonly struct AirshiftMotionSample
    {
        /// <summary>Gets the sampled hand.</summary>
        internal HandId HandId { get; }
        /// <summary>Gets the normalized playfield position.</summary>
        internal Vector2 Position { get; }
        /// <summary>Gets the monotonic sample timestamp in seconds.</summary>
        internal double Timestamp { get; }
        /// <summary>Gets the normalized velocity in playfield units per second.</summary>
        internal Vector2 Velocity { get; }
        /// <summary>Gets the velocity magnitude.</summary>
        internal float Speed { get; }
        /// <summary>Gets the normalized motion direction, or zero while stationary.</summary>
        internal Vector2 Direction { get; }
        /// <summary>Gets the currently active area.</summary>
        internal AirshiftArea CurrentArea { get; }
        /// <summary>Gets the last valid non-empty area.</summary>
        internal AirshiftArea PreviousArea { get; }
        /// <summary>Gets the source of the sample.</summary>
        internal AirshiftInputSource InputSource { get; }

        /// <summary>
        /// Creates an immutable AIRSHIFT motion sample.
        /// </summary>
        /// <param name="handId">The sampled hand.</param>
        /// <param name="position">The normalized playfield position.</param>
        /// <param name="timestamp">The monotonic sample timestamp.</param>
        /// <param name="velocity">The normalized velocity.</param>
        /// <param name="currentArea">The active area.</param>
        /// <param name="previousArea">The last valid area.</param>
        /// <param name="inputSource">The source of the sample.</param>
        internal AirshiftMotionSample(
            HandId handId,
            Vector2 position,
            double timestamp,
            Vector2 velocity,
            AirshiftArea currentArea,
            AirshiftArea previousArea,
            AirshiftInputSource inputSource)
        {
            HandId = handId;
            Position = position;
            Timestamp = timestamp;
            Velocity = velocity;
            Speed = velocity.magnitude;
            Direction = Speed > 0.0001f ? velocity / Speed : Vector2.zero;
            CurrentArea = currentArea;
            PreviousArea = previousArea;
            InputSource = inputSource;
        }
    }

    /// <summary>
    /// Describes one area boundary crossing produced by continuous trajectory sampling.
    /// </summary>
    internal readonly struct AirshiftAreaTransition
    {
        /// <summary>Gets the hand that crossed the boundary.</summary>
        internal HandId HandId { get; }
        /// <summary>Gets the crossed area.</summary>
        internal AirshiftArea Area { get; }
        /// <summary>Gets whether the area was entered or exited.</summary>
        internal AirshiftAreaTransitionKind Kind { get; }
        /// <summary>Gets the approximate normalized crossing position.</summary>
        internal Vector2 Position { get; }
        /// <summary>Gets the monotonic crossing timestamp.</summary>
        internal double Timestamp { get; }
        /// <summary>Gets the source that produced the crossing.</summary>
        internal AirshiftInputSource InputSource { get; }

        /// <summary>
        /// Creates an immutable area transition.
        /// </summary>
        /// <param name="handId">The hand that crossed the boundary.</param>
        /// <param name="area">The crossed area.</param>
        /// <param name="kind">The transition kind.</param>
        /// <param name="position">The approximate crossing position.</param>
        /// <param name="timestamp">The monotonic timestamp.</param>
        /// <param name="inputSource">The originating input source.</param>
        internal AirshiftAreaTransition(
            HandId handId,
            AirshiftArea area,
            AirshiftAreaTransitionKind kind,
            Vector2 position,
            double timestamp,
            AirshiftInputSource inputSource)
        {
            HandId = handId;
            Area = area;
            Kind = kind;
            Position = position;
            Timestamp = timestamp;
            InputSource = inputSource;
        }
    }

    /// <summary>
    /// Represents an isolated main-row keyboard button transition.
    /// </summary>
    internal readonly struct DebugButtonEvent
    {
        /// <summary>Gets the simulated outer area.</summary>
        internal AirshiftArea Area { get; }
        /// <summary>Gets whether the key is now pressed.</summary>
        internal bool IsPressed { get; }
        /// <summary>Gets the monotonic event timestamp.</summary>
        internal double Timestamp { get; }
        /// <summary>Gets the event source.</summary>
        internal AirshiftInputSource InputSource { get; }

        /// <summary>
        /// Creates an immutable debug button event.
        /// </summary>
        /// <param name="area">The simulated outer area.</param>
        /// <param name="isPressed">Whether the key is now pressed.</param>
        /// <param name="timestamp">The monotonic event timestamp.</param>
        internal DebugButtonEvent(AirshiftArea area, bool isPressed, double timestamp)
        {
            Area = area;
            IsPressed = isPressed;
            Timestamp = timestamp;
            InputSource = AirshiftInputSource.KeyboardDebug;
        }
    }

    /// <summary>
    /// Represents one area change found while tracing a continuous cursor segment.
    /// </summary>
    internal readonly struct AirshiftAreaTracePoint
    {
        /// <summary>Gets the area reached at this trace point.</summary>
        internal AirshiftArea Area { get; }
        /// <summary>Gets the normalized point where the sampled area changed.</summary>
        internal Vector2 Position { get; }

        /// <summary>
        /// Creates an immutable trace point.
        /// </summary>
        /// <param name="area">The newly reached area.</param>
        /// <param name="position">The sampled normalized position.</param>
        internal AirshiftAreaTracePoint(AirshiftArea area, Vector2 position)
        {
            Area = area;
            Position = position;
        }
    }

    /// <summary>
    /// Detects AIRSHIFT regions independently from a physical or simulated motion source.
    /// </summary>
    /// <remarks>
    /// A future composite implementation can add inner TOUCH regions without changing mouse or MPU6050 sources.
    /// </remarks>
    internal interface IAirshiftAreaDetector
    {
        /// <summary>
        /// Gets the area containing one normalized position.
        /// </summary>
        /// <param name="position">The normalized playfield position.</param>
        /// <returns>The detected area, or <see cref="AirshiftArea.None"/>.</returns>
        AirshiftArea GetArea(Vector2 position);

        /// <summary>
        /// Traces a continuous motion segment and records every sampled area change.
        /// </summary>
        /// <param name="from">The normalized segment start.</param>
        /// <param name="to">The normalized segment end.</param>
        /// <param name="tracePoints">The caller-provided transition buffer.</param>
        /// <returns>The number of trace points written.</returns>
        int TraceSegment(Vector2 from, Vector2 to, Span<AirshiftAreaTracePoint> tracePoints);
    }
}
