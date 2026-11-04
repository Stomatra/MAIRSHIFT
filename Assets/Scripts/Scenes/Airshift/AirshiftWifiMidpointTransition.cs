
namespace MajdataPlay.Scenes.Airshift
{
    internal readonly struct AirshiftWifiMidpointTransition
    {
        internal AirshiftSensorArea Area { get; }
        internal bool Entered { get; }
        internal double Timestamp { get; }
        internal long Sequence { get; }

        internal AirshiftWifiMidpointTransition(
            AirshiftSensorArea area,
            bool entered,
            double timestamp,
            long sequence)
        {
            Area = area;
            Entered = entered;
            Timestamp = timestamp;
            Sequence = sequence;
        }
    }
}
