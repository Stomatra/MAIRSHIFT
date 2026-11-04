using System;
using UnityEngine;

#nullable enable
namespace MajdataPlay.Scenes.Airshift
{
    /// <summary>One continuous transition through the real maimai sensor collider layout.</summary>
    internal readonly struct AirshiftSensorTransition
    {
        internal long TransitionId { get; }
        internal HandId Hand { get; }
        internal AirshiftSensorArea Area { get; }
        internal bool Entered { get; }
        internal double Timestamp { get; }

        internal AirshiftSensorTransition(long transitionId, HandId hand, AirshiftSensorArea area, bool entered, double timestamp)
        {
            TransitionId = transitionId;
            Hand = hand;
            Area = area;
            Entered = entered;
            Timestamp = timestamp;
        }
    }

    /// <summary>
    /// Samples the persistent original touch-panel colliders without writing to STANDARD input state.
    /// </summary>
    internal sealed class AirshiftSensorTracker
    {
        private const float WorldDiameter = 10.8f;
        private const float TraceStep = 0.01f;
        private const float SensorActionSpeed = 0.65f;
        private readonly RaycastHit[] _hits = new RaycastHit[16];
        private Vector2 _leftPosition;
        private Vector2 _rightPosition;
        private AirshiftSensorArea _leftArea;
        private AirshiftSensorArea _rightArea;
        private long _nextEventId = 1_000_000;

        internal event Action<AirshiftSensorTransition>? Transitioned;
        internal event Action<AirshiftSensorAction>? ActionDetected;

        internal AirshiftSensorArea LeftArea => _leftArea;
        internal AirshiftSensorArea RightArea => _rightArea;

        internal AirshiftSensorTracker(Vector2 leftPosition, Vector2 rightPosition)
        {
            _leftPosition = leftPosition;
            _rightPosition = rightPosition;
            _leftArea = GetArea(leftPosition);
            _rightArea = GetArea(rightPosition);
        }

        internal bool IsAreaContacted(int sensorArea)
        {
            var area = (AirshiftSensorArea)sensorArea;

            return area != AirshiftSensorArea.None &&
                (_leftArea == area || _rightArea == area);
        }

        internal void Observe(AirshiftMotionSample sample)
        {
            var from = sample.HandId == HandId.Left ? _leftPosition : _rightPosition;
            var previousArea = sample.HandId == HandId.Left ? _leftArea : _rightArea;
            var distance = Vector2.Distance(from, sample.Position);
            var steps = Mathf.Clamp(Mathf.CeilToInt(distance / TraceStep), 1, 256);
            for (var index = 1; index <= steps; index++)
            {
                var fraction = index / (float)steps;
                var point = Vector2.Lerp(from, sample.Position, fraction);
                var area = GetArea(point);
                if (area == previousArea)
                {
                    continue;
                }
                var transitionTime = sample.Timestamp - ((1f - fraction) * distance / Math.Max(sample.Speed, 0.001f));
                if (previousArea != AirshiftSensorArea.None)
                {
                    Transitioned?.Invoke(new AirshiftSensorTransition(
                        _nextEventId++, sample.HandId, previousArea, false, transitionTime));
                }
                if (area != AirshiftSensorArea.None)
                {
                    var id = _nextEventId++;
                    Transitioned?.Invoke(new AirshiftSensorTransition(id, sample.HandId, area, true, transitionTime));
                    if (area is >= AirshiftSensorArea.B1 and <= AirshiftSensorArea.E8 && sample.Speed >= SensorActionSpeed)
                    {
                        ActionDetected?.Invoke(new AirshiftSensorAction(id, sample.HandId, area, transitionTime, sample.Speed));
                    }
                }
                previousArea = area;
            }
            if (sample.HandId == HandId.Left)
            {
                _leftPosition = sample.Position;
                _leftArea = previousArea;
            }
            else
            {
                _rightPosition = sample.Position;
                _rightArea = previousArea;
            }
        }

        internal void Reset(Vector2 leftPosition, Vector2 rightPosition)
        {
            _leftPosition = leftPosition;
            _rightPosition = rightPosition;
            _leftArea = GetArea(leftPosition);
            _rightArea = GetArea(rightPosition);
        }

        internal AirshiftSensorArea GetArea(Vector2 normalizedPosition)
        {
            var mapping = Majdata<DummyTouchPanelRenderer>.Instance?.InstanceID2SensorIndexMappingTable;
            if (mapping is null || mapping.Count == 0)
            {
                return AirshiftSensorArea.None;
            }
            var world = new Vector3(
                (normalizedPosition.x - 0.5f) * WorldDiameter,
                (normalizedPosition.y - 0.5f) * WorldDiameter,
                -10f);
            var count = Physics.RaycastNonAlloc(new Ray(world, Vector3.forward), _hits, 25f);
            for (var index = 0; index < count; index++)
            {
                if (!mapping.ContainsKey(_hits[index].colliderInstanceID))
                {
                    continue;
                }
                try
                {
                    return (AirshiftSensorArea)(int)IO.InputManager.GetSensorAreaFromInstanceID(
                        _hits[index].colliderInstanceID);
                }
                catch (InvalidOperationException)
                {
                    return AirshiftSensorArea.None;
                }
                catch (ArgumentOutOfRangeException)
                {
                    return AirshiftSensorArea.None;
                }
            }
            return AirshiftSensorArea.None;
        }
    }
}
