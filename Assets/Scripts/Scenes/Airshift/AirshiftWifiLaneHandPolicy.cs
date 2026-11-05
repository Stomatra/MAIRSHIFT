using System;

namespace MajdataPlay.Scenes.Airshift
{
    /// <summary>
    /// Evaluates physical sensor contact for either outer WIFI lane.
    /// Unlike the old two-hand ownership rule, a single hand may trace
    /// both outer lanes. Each WIFI still owns three independent ordered
    /// judge queues in SlideBase; this only decides whether a sensor is
    /// currently touched, not whether its path is complete.
    /// </summary>
    internal static class AirshiftWifiLaneHandPolicy
    {
        internal static bool IsOuterLanePressed(
            int lane, AirshiftSensorArea target,
            AirshiftSensorArea leftArea, AirshiftSensorArea rightArea)
        {
            if (lane != 0 && lane != 2)
            {
                throw new ArgumentOutOfRangeException(nameof(lane));
            }

            if (target == AirshiftSensorArea.None)
            {
                return false;
            }

            // Keep the same area matching logic used by untagged
            // AIRSHIFT slides, without claiming a lane or a hand.
            // This also permits two simultaneous WIFI notes to be
            // swept independently by just two physical hands.
            return leftArea == target || rightArea == target;
        }
    }
}
