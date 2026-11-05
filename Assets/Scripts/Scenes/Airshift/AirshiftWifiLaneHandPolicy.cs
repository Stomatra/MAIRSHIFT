namespace MajdataPlay.Scenes.Airshift
{
    /// <summary>
    /// WIFI's two outer tracks require two separate real hands. Either
    /// physical hand may claim either outer track by entering its first
    /// exclusive (non-start) sensor. The common start stays unclaimed.
    /// </summary>
    internal sealed class AirshiftWifiLaneHandPolicy
    {
        private HandId? _lane0;
        private HandId? _lane2;

        internal HandId? Lane0Owner => _lane0;
        internal HandId? Lane2Owner => _lane2;

        internal void Reset()
        {
            _lane0 = null;
            _lane2 = null;
        }

        internal bool IsOuterLanePressed(
            int lane, AirshiftSensorArea target, AirshiftSensorArea start,
            AirshiftSensorArea leftArea, AirshiftSensorArea rightArea)
        {
            if (lane != 0 && lane != 2)
                throw new System.ArgumentOutOfRangeException(nameof(lane));
            if (target == AirshiftSensorArea.None)
                return false;

            bool leftMatches = leftArea == target;
            bool rightMatches = rightArea == target;

            // All lanes share the first sensor. Do not claim at the start.
            if (target == start)
                return leftMatches || rightMatches;

            var owner = lane == 0 ? _lane0 : _lane2;
            var otherOwner = lane == 0 ? _lane2 : _lane0;

            if (owner.HasValue)
                return owner.Value == HandId.Left ? leftMatches : rightMatches;

            // Both hands in the same sensor are not an unambiguous claim.
            if (leftMatches == rightMatches)
                return false;

            var candidate = leftMatches ? HandId.Left : HandId.Right;
            if (otherOwner.HasValue && otherOwner.Value == candidate)
                return false;

            if (lane == 0) _lane0 = candidate;
            else _lane2 = candidate;
            return true;
        }
    }
}
