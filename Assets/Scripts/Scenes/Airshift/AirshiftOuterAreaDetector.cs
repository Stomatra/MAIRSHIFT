using System;
using UnityEngine;

#nullable enable
namespace MajdataPlay.Scenes.Airshift
{
    internal readonly struct AirshiftDiscContactTracePoint
    {
        internal Vector2 Position { get; }
        internal int ContactMask { get; }

        internal AirshiftDiscContactTracePoint(
            Vector2 position,
            int contactMask)
        {
            Position = position;
            ContactMask = contactMask;
        }
    }

    /// <summary>
    /// Detects the A1-A8 outer ring using the orientation and numbering from <c>ScreenTouchMapper</c>.
    /// </summary>
    internal sealed class AirshiftOuterAreaDetector : IAirshiftAreaDetector
    {
        /// <summary>The normalized inner radius of the prototype outer ring.</summary>
        internal const float InnerRadius = 0.72f;
        /// <summary>The normalized outer radius of the prototype outer ring.</summary>
        internal const float OuterRadius = 1f;
        /// <summary>The maximum normalized distance between continuous trace samples.</summary>
        private const float TraceStep = 0.01f;
        /// <summary>The maximum number of samples evaluated for one motion segment.</summary>
        private const int MaxTraceSteps = 256;

        /// <inheritdoc />
        public AirshiftArea GetArea(Vector2 position)
        {
            var point = (position - new Vector2(0.5f, 0.5f)) * 2f;
            var radius = point.magnitude;
            if (radius < InnerRadius || radius > OuterRadius)
            {
                return AirshiftArea.None;
            }

            var degree = (-Mathf.Atan2(point.y, point.x) * Mathf.Rad2Deg) + 180f;
            if (degree >= 360f)
            {
                degree -= 360f;
            }
            var sector = Mathf.FloorToInt(degree / 22.5f);
            var degreeDifference = degree - (22.5f * sector);
            var center = sector;
            var left = (sector + 15) & 15;
            var right = (sector + 1) & 15;
            var isSensorCenter = (sector & 1) == 0;
            var edge = isSensorCenter ? 8f : 14.5f;
            var target = Mathf.Abs(degreeDifference) <= edge
                ? center
                : degreeDifference > 0f ? right : left;
            if ((target & 1) == 0)
            {
                return AirshiftArea.None;
            }
            var index = (((target >> 1) + 6) & 7) + 1;
            return (AirshiftArea)index;
        }

        /// <inheritdoc />
        public int TraceSegment(Vector2 from, Vector2 to, Span<AirshiftAreaTracePoint> tracePoints)
        {
            if (tracePoints.IsEmpty)
            {
                return 0;
            }
            var distance = Vector2.Distance(from, to);
            var stepCount = Mathf.Clamp(Mathf.CeilToInt(distance / TraceStep), 1, MaxTraceSteps);
            var previousArea = GetArea(from);
            var written = 0;
            for (var i = 1; i <= stepCount; i++)
            {
                var position = Vector2.Lerp(from, to, i / (float)stepCount);
                var area = GetArea(position);
                if (area == previousArea)
                {
                    continue;
                }
                var tracePoint = new AirshiftAreaTracePoint(area, position);
                if (written < tracePoints.Length)
                {
                    tracePoints[written++] = tracePoint;
                }
                else
                {
                    tracePoints[^1] = tracePoint;
                }
                previousArea = area;
            }
            return written;
        }
        
        /// <summary>
        /// Returns an 8-bit mask of outer areas touched by a
        /// circular cursor. Bit 0 = A1, bit 7 = A8.
        /// </summary>
        internal int GetContactMask(
            Vector2 position,
            float cursorRadius)
        {
            var center = new Vector2(0.5f, 0.5f);
            var offset = position - center;

            // Convert the detector's doubled coordinates
            // back into [0,1] playfield coordinates.
            const float half = 0.5f;
            float inner = InnerRadius * half;
            float outer = OuterRadius * half;

            const float halfAngle = 14.5f;

            float distance = offset.magnitude;
            float angle = Mathf.Atan2(
                offset.y, offset.x) * Mathf.Rad2Deg;

            float radius = Mathf.Max(0f, cursorRadius);
            float radiusSquared = radius * radius;

            int mask = 0;

            for (int i = 0; i < 8; i++)
            {
                // Match GetAreaCenter() and the existing
                // A1-A8 numbering/orientation.
                float centerAngle = 67.5f - i * 45f;

                float difference = Mathf.Abs(
                    Mathf.DeltaAngle(angle, centerAngle));

                float distanceSquared;

                if (difference <= halfAngle)
                {
                    // The cursor center is angularly inside
                    // the sector: find the nearest ring radius.
                    float nearestRadius = Mathf.Clamp(
                        distance, inner, outer);

                    float radialGap = distance - nearestRadius;
                    distanceSquared = radialGap * radialGap;
                }
                else
                {
                    // Outside the angular span: the closest
                    // point lies on one of the radial edges.
                    float d1 = DistanceSquaredToRadialEdge(
                        offset,
                        centerAngle - halfAngle,
                        inner,
                        outer);

                    float d2 = DistanceSquaredToRadialEdge(
                        offset,
                        centerAngle + halfAngle,
                        inner,
                        outer);

                    distanceSquared = Mathf.Min(d1, d2);
                }

                if (distanceSquared <= radiusSquared + 1e-7f)
                {
                    mask |= 1 << i;
                }
            }

            return mask;
        }

        private static float DistanceSquaredToRadialEdge(
            Vector2 offset,
            float degrees,
            float inner,
            float outer)
        {
            float radians = degrees * Mathf.Deg2Rad;

            var direction = new Vector2(
                Mathf.Cos(radians),
                Mathf.Sin(radians));

            float projectedRadius = Mathf.Clamp(
                Vector2.Dot(offset, direction),
                inner,
                outer);

            var closest = direction * projectedRadius;

            return (offset - closest).sqrMagnitude;
        }

        /// <summary>
        /// Traces circular cursor contacts across a movement segment.
        /// Returns each changed contact mask in movement order.
        /// Returns -1 when the output buffer is insufficient.
        /// </summary>
        internal int TraceDiscContacts(
            Vector2 from,
            Vector2 to,
            float cursorRadius,
            Span<AirshiftDiscContactTracePoint> tracePoints)
        {
            if (tracePoints.IsEmpty)
            {
                return -1;
            }

            const float contactTraceStep = 0.005f;

            float distance = Vector2.Distance(from, to);

            int stepCount = Mathf.Clamp(
                Mathf.CeilToInt(distance / contactTraceStep),
                1,
                MaxTraceSteps
            );

            int previousMask = GetContactMask(
                from,
                cursorRadius
            );

            int written = 0;

            for (int i = 1; i <= stepCount; i++)
            {
                float t = i / (float)stepCount;

                Vector2 position = Vector2.Lerp(
                    from,
                    to,
                    t
                );

                int currentMask = GetContactMask(
                    position,
                    cursorRadius
                );

                if (currentMask == previousMask)
                {
                    continue;
                }

                if (written >= tracePoints.Length)
                {
                    return -1;
                }

                tracePoints[written++] =
                    new AirshiftDiscContactTracePoint(
                        position,
                        currentMask
                    );

                previousMask = currentMask;
            }

            return written;
        }

        /// <summary>
        /// Gets the normalized visual center of an A1-A8 region.
        /// </summary>
        /// <param name="area">The requested outer area.</param>
        /// <param name="radius">The normalized radial position inside the outer ring.</param>
        /// <returns>The normalized visual center.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The area is not A1-A8.</exception>
        internal static Vector2 GetAreaCenter(AirshiftArea area, float radius = 0.86f)
        {
            var index = (int)area - 1;
            if (index is < 0 or > 7)
            {
                throw new ArgumentOutOfRangeException(nameof(area), area, null);
            }
            var angle = (67.5f - (index * 45f)) * Mathf.Deg2Rad;
            var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            return new Vector2(0.5f, 0.5f) + (direction * radius * 0.5f);
        }
    }
}
