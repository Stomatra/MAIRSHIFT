using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;
using UnityEngine;

#nullable enable

namespace MajdataPlay.Scenes.Airshift
{
    /// <summary>
    /// Collects AIRSHIFT input and publishes recognized TAP gestures.
    /// Does not perform note judgement or scoring.
    /// </summary>
    internal sealed class AirshiftInputController : IDisposable
    {
        private readonly AirshiftDualCursorTracker _cursorTracker;
        private readonly AirshiftMouseSimulationSource _mouseSource;
        private readonly AirshiftTapGestureDetector _tapGestureDetector;
        private readonly AirshiftDiscContactTracker _discContactTracker;
        private readonly AirshiftSensorTracker _sensorTracker;
        private readonly HashSet<AirshiftSensorArea> _pendingTouches = new();
        private readonly List<AirshiftSensorTransition> _slideTransitions = new();
        private readonly List<AirshiftWifiMidpointTransition>
            _wifiTransitions = new();

        private bool _hasFocus;
        private bool _disposed;
        private bool _slideTransitionsSorted = true;
        private bool _wifiTransitionsSorted = true;
        private long _nextWifiTransitionId;
        private Vector2 _wifiMidpointPosition;
        private AirshiftSensorArea _wifiMidpointArea;

        /// <summary>
        /// Raised whenever a new TAP gesture is recognized.
        /// </summary>
        internal event Action<TapGesture>? GestureDetected;
        internal AirshiftSensorArea SlideFrameLeftStart { get; private set; }
        internal AirshiftSensorArea SlideFrameRightStart { get; private set; }

        internal AirshiftSensorArea WifiFrameStart { get; private set; }

        internal AirshiftInputController(double timestamp)
        {
            var areaDetector = new AirshiftOuterAreaDetector();

            _cursorTracker = new AirshiftDualCursorTracker(
                areaDetector,
                new Vector2(0.38f, 0.5f),
                new Vector2(0.62f, 0.5f),
                timestamp
            );

            _mouseSource = new AirshiftMouseSimulationSource();

            _tapGestureDetector = new AirshiftTapGestureDetector(
                AirshiftTapGestureSettings.Prototype
            );

            _discContactTracker = new AirshiftDiscContactTracker(
                _cursorTracker.LeftHand.Position,
                _cursorTracker.RightHand.Position,
                AirshiftControlTuning.CursorRadiusNormalized
            );

            _sensorTracker = new AirshiftSensorTracker(
                _cursorTracker.LeftHand.Position,
                _cursorTracker.RightHand.Position
            );

            _sensorTracker.Transitioned += OnSensorTransitioned;

            _wifiMidpointPosition =
                (_cursorTracker.LeftHand.Position +
                _cursorTracker.RightHand.Position) * 0.5f;

            _wifiMidpointArea = _sensorTracker.GetArea(_wifiMidpointPosition);
            WifiFrameStart = _wifiMidpointArea;

            _cursorTracker.MotionSampled += OnMotionSampled;

            // Use disc contact transitions instead of point-center transitions.
            _discContactTracker.AreaTransitioned += OnAreaTransitioned;

            _tapGestureDetector.GestureDetected += OnTapGestureDetected;

            _mouseSource.SuppressNextDelta(_cursorTracker, timestamp);
        }

        /// <summary>
        /// Polls the mouse simulation source once per gameplay frame.
        /// Must be called before note judgement.
        /// </summary>
        internal void Poll(
            float playfieldWidth,
            double timestamp,
            bool hasFocus)
        {
            if (_disposed)
            {
                return;
            }

            if (_hasFocus != hasFocus)
            {
                Reset(timestamp);
                _hasFocus = hasFocus;
            }

            if (!hasFocus || playfieldWidth <= 0f)
            {
                return;
            }

            _wifiTransitions.Clear();
            _wifiTransitionsSorted = true;
            WifiFrameStart = _wifiMidpointArea;

            _slideTransitions.Clear();
            _slideTransitionsSorted = true;

            SlideFrameLeftStart = _sensorTracker.LeftArea;
            SlideFrameRightStart = _sensorTracker.RightArea;

            _pendingTouches.Clear();

            bool discontinuity = _mouseSource.Poll(
                _cursorTracker,
                playfieldWidth,
                timestamp,
                hasFocus
            );

#if UNITY_EDITOR
            // Switch which virtual hand receives mouse movement.
            // Only available in the Unity Editor debug environment.
            var keyboard = Keyboard.current;

            if (keyboard != null &&
                keyboard.tabKey.wasPressedThisFrame)
            {
                _cursorTracker.ToggleActiveHand();

                // Avoid treating the hand switch as a fast movement.
                Reset(timestamp);

                // Do not sample mouse movement on the switch frame.
                return;
            }
#endif

            if (discontinuity)
            {
                _tapGestureDetector.Reset();
            }
        }

        internal Vector2 GetCursorPosition(HandId hand)
        {
            return hand == HandId.Left
                ? _cursorTracker.LeftHand.Position
                : _cursorTracker.RightHand.Position;
        }

        /// <summary>
        /// Resets motion state after focus changes or interruptions.
        /// </summary>
        internal void Reset(double timestamp)
        {
            _mouseSource.SuppressNextDelta(_cursorTracker, timestamp);
            _cursorTracker.ResetMotionBaselines(timestamp);
            _tapGestureDetector.Reset();
            _discContactTracker.Reset(
                _cursorTracker.LeftHand.Position,
                _cursorTracker.RightHand.Position,
                AirshiftControlTuning.CursorRadiusNormalized
            );

            _sensorTracker.Reset(
                _cursorTracker.LeftHand.Position,
                _cursorTracker.RightHand.Position
            );

            _wifiMidpointPosition =
                (_cursorTracker.LeftHand.Position +
                _cursorTracker.RightHand.Position) * 0.5f;

            _wifiMidpointArea = _sensorTracker.GetArea(_wifiMidpointPosition);

            _wifiTransitions.Clear();
            _wifiTransitionsSorted = true;
            WifiFrameStart = _wifiMidpointArea;

            _slideTransitions.Clear();
            _slideTransitionsSorted = true;

            SlideFrameLeftStart = _sensorTracker.LeftArea;
            SlideFrameRightStart = _sensorTracker.RightArea;

            _pendingTouches.Clear();
        }

        internal IReadOnlyList<AirshiftWifiMidpointTransition>
            GetOrderedWifiMidpointTransitions()
        {
            if (!_wifiTransitionsSorted)
            {
                _wifiTransitions.Sort((a, b) =>
                {
                    int comparison = a.Timestamp.CompareTo(b.Timestamp);
                    return comparison != 0
                        ? comparison
                        : a.Sequence.CompareTo(b.Sequence);
                });

                _wifiTransitionsSorted = true;
            }

            return _wifiTransitions;
        }

        internal bool IsAreaContacted(int startPos)
        {
            if (_disposed || !_hasFocus)
            {
                return false;
            }

            _discContactTracker.SetRadius(
                AirshiftControlTuning.CursorRadiusNormalized
            );

            return _discContactTracker.IsAreaContacted(startPos);
        }

        internal bool TryConsumeTouch(int sensorArea)
        {
            if (_disposed || !_hasFocus)
            {
                return false;
            }

            return _pendingTouches.Remove(
                (AirshiftSensorArea)sensorArea
            );
        }

        internal bool IsTouchContacted(int sensorArea)
        {
            return !_disposed
                && _hasFocus
                && _sensorTracker.IsAreaContacted(sensorArea);
        }
   
        internal IReadOnlyList<AirshiftSensorTransition>
            GetOrderedSlideTransitions()
        {
            if (!_slideTransitionsSorted)
            {
                _slideTransitions.Sort((a, b) =>
                {
                    int comparison = a.Timestamp.CompareTo(b.Timestamp);

                    return comparison != 0
                        ? comparison
                        : a.TransitionId.CompareTo(b.TransitionId);
                });

                _slideTransitionsSorted = true;
            }

            return _slideTransitions;
        }
        
        private void ObserveWifiMidpoint(AirshiftMotionSample sample)
        {
            var target =
                (_cursorTracker.LeftHand.Position +
                _cursorTracker.RightHand.Position) * 0.5f;

            var distance = Vector2.Distance(
                _wifiMidpointPosition, target);

            var steps = Mathf.Clamp(
                Mathf.CeilToInt(distance / 0.01f), 1, 256);

            // One moving hand shifts the midpoint by half the distance.
            var midpointSpeed = Mathf.Max(sample.Speed * 0.5f, 0.001f);

            for (var i = 1; i <= steps; i++)
            {
                float fraction = i / (float)steps;

                var point = Vector2.Lerp(
                    _wifiMidpointPosition, target, fraction);

                var area = _sensorTracker.GetArea(point);

                if (area == _wifiMidpointArea)
                {
                    continue;
                }

                double transitionTime = sample.Timestamp -
                    (1f - fraction) * distance / midpointSpeed;

                // Record exit before entry.
                if (_wifiMidpointArea != AirshiftSensorArea.None)
                {
                    _wifiTransitions.Add(
                        new AirshiftWifiMidpointTransition(
                            _wifiMidpointArea,
                            false,
                            transitionTime,
                            _nextWifiTransitionId++));
                }

                if (area != AirshiftSensorArea.None)
                {
                    _wifiTransitions.Add(
                        new AirshiftWifiMidpointTransition(
                            area,
                            true,
                            transitionTime,
                            _nextWifiTransitionId++));
                }

                _wifiTransitionsSorted = false;

        #if UNITY_EDITOR
                Debug.Log(
                    $"[AIRSHIFT WIFI] " +
                    $"Virtual Thumb: {_wifiMidpointArea} -> {area}");
        #endif

                _wifiMidpointArea = area;
            }

            _wifiMidpointPosition = target;
        }

        private void OnMotionSampled(AirshiftMotionSample sample)
        {
            _sensorTracker.Observe(sample);
            ObserveWifiMidpoint(sample);

            _discContactTracker.SetRadius(
                AirshiftControlTuning.CursorRadiusNormalized
            );

            // Area entries must arrive before the velocity sample.
            bool traceValid = _discContactTracker.Observe(sample);

            if (!traceValid)
            {
                // Avoid judging an incomplete movement trace.
                _tapGestureDetector.Reset();
                return;
            }

            _tapGestureDetector.ObserveMotionSample(sample);
        }

        private void OnAreaTransitioned(AirshiftAreaTransition transition)
        {
            _tapGestureDetector.ObserveAreaTransition(transition);
        }

        private void OnTapGestureDetected(TapGesture gesture)
        {
            GestureDetected?.Invoke(gesture);
        }
        
        private void OnSensorTransitioned(
            AirshiftSensorTransition transition)
        {
            _slideTransitions.Add(transition);
            _slideTransitionsSorted = false;

            if (!transition.Entered)
            {
                return;
            }

            // The same area produces at most one TOUCH per frame.
            _pendingTouches.Add(transition.Area);

#if UNITY_EDITOR
            Debug.Log(
                $"[AIRSHIFT TOUCH] " +
                $"{transition.Hand} ENTER {transition.Area}"
            );
#endif
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            _cursorTracker.MotionSampled -= OnMotionSampled;
            _discContactTracker.AreaTransitioned -= OnAreaTransitioned;
            _tapGestureDetector.GestureDetected -= OnTapGestureDetected;
            _sensorTracker.Transitioned -= OnSensorTransitioned;

            GestureDetected = null;
        }
    }
}
