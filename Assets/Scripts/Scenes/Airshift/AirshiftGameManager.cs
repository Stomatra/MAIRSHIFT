using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

#nullable enable
namespace MajdataPlay.Scenes.Airshift
{
    /// <summary>
    /// Controls the AIRSHIFT local-test scene without using the STANDARD gameplay pipeline.
    /// </summary>
    public sealed class AirshiftGameManager : MonoBehaviour
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        private AirshiftAndroidCameraSource? _androidCameraSource;
#endif
        // Opt-in: enable on AirshiftGame scene component, not on STANDARD scenes.
        [SerializeField] private bool _enableCameraInput = false;
        private AirshiftCameraUdpSource? _cameraSource;

        /// <summary>The original STANDARD main-game background sprite.</summary>
        [SerializeField]
        private Sprite? _gameBackground;
        /// <summary>Whether one navigation request has already started.</summary>
        private bool _isExiting;
        /// <summary>Whether the session must survive destruction for the result scene.</summary>
        private bool _preserveSessionForResult;
        /// <summary>Whether the Game view currently owns mouse and keyboard focus.</summary>
        private bool _isFocused;
        /// <summary>The independent A1-A8 geometry implementation.</summary>
        private AirshiftOuterAreaDetector _areaDetector = null!;
        /// <summary>The independent two-hand cursor and motion state.</summary>
        private AirshiftDualCursorTracker _cursorTracker = null!;
        /// <summary>The replaceable relative-mouse simulation source.</summary>
        private AirshiftMouseSimulationSource _mouseSource = null!;
        /// <summary>The isolated main-row keyboard debug source.</summary>
        private AirshiftKeyboardDebugSource _keyboardSource = null!;
        /// <summary>The lightweight debug-only presentation layer.</summary>
        private AirshiftDebugView _debugView = null!;
        /// <summary>The source-independent no-click TAP gesture recognizer.</summary>
        private AirshiftTapGestureDetector _tapGestureDetector = null!;
        /// <summary>The isolated code-defined TAP test chart.</summary>
        private AirshiftTestChart _tapTestChart = null!;
        /// <summary>The isolated single-note TAP judge.</summary>
        private AirshiftTapJudge _tapJudge = null!;
        /// <summary>The controllable local test timeline.</summary>
        private IAirshiftTimelineClock _tapClock = null!;
        /// <summary>The local timeline time cached for debug rendering.</summary>
        private double _tapTestTime;
        /// <summary>The latest local TAP judgement summary.</summary>
        private string _lastTapResult = "WAITING";
        /// <summary>Tracks the original touch-panel collider geometry for real-chart TOUCH and SLIDE input.</summary>
        private AirshiftSensorTracker _sensorTracker = null!;
        /// <summary>Renders actual chart notes without STANDARD note prefabs.</summary>
        private AirshiftChartView _chartView = null!;
        /// <summary>Owns the actual Simai/audio AIRSHIFT vertical slice.</summary>
        private AirshiftChartPlayer? _chartPlayer;
        /// <summary>Cancels asynchronous chart and audio loading when the scene leaves.</summary>
        private readonly CancellationTokenSource _loadCancellation = new();
        /// <summary>Whether this scene is running a selected local chart rather than the Task 005 test.</summary>
        private bool _isChartSession;
        /// <summary>Selected song label cached for IMGUI.</summary>
        private string _chartSongTitle = string.Empty;
        /// <summary>Selected difficulty label cached for IMGUI.</summary>
        private string _chartLevel = string.Empty;
        /// <summary>Reads the Main/Sub display rectangles positioned by the STANDARD CanvasTemplate.</summary>
        private AirshiftStandardLayoutBridge _layout = null!;
        /// <summary>Drives the copied STANDARD background and HUD with AIRSHIFT data.</summary>
        private AirshiftStandardGameVisualAdapter _standardVisuals = null!;
        /// <summary>Whether the scene-local diagnostic overlay is visible.</summary>
        private bool _isDebugOverlayVisible;

        private void Awake()
        {
            var timestamp = Time.realtimeSinceStartupAsDouble;
            _areaDetector = new AirshiftOuterAreaDetector();
            _layout = new AirshiftStandardLayoutBridge();
            _standardVisuals = gameObject.GetComponent<AirshiftStandardGameVisualAdapter>() ??
                gameObject.AddComponent<AirshiftStandardGameVisualAdapter>();
            _cursorTracker = new AirshiftDualCursorTracker(
                _areaDetector,
                new Vector2(0.38f, 0.5f),
                new Vector2(0.62f, 0.5f),
                timestamp);
            _mouseSource = new AirshiftMouseSimulationSource();
#if UNITY_ANDROID && !UNITY_EDITOR
            _androidCameraSource = new AirshiftAndroidCameraSource();
            _enableCameraInput = false; // Android reads its own camera, not editor UDP.
#endif
            if (_enableCameraInput)
            {
                Application.runInBackground = true;
                try { _cameraSource = new AirshiftCameraUdpSource(19476); }
                catch (Exception ex)
                {
                    Debug.LogError("[AIRSHIFT CAMERA] Could not open UDP 19476: " + ex.Message);
                    _enableCameraInput = false;
                }
            }
            _keyboardSource = new AirshiftKeyboardDebugSource();
            _debugView = new AirshiftDebugView();
            _chartView = new AirshiftChartView(_gameBackground);
            _sensorTracker = new AirshiftSensorTracker(
                _cursorTracker.LeftHand.Position,
                _cursorTracker.RightHand.Position);
            _tapGestureDetector = new AirshiftTapGestureDetector(AirshiftTapGestureSettings.Prototype);
            _tapTestChart = AirshiftTestChart.CreatePrototype();
            _tapJudge = new AirshiftTapJudge(_tapTestChart, AirshiftTapJudgeSettings.Prototype);
            _tapClock = new AirshiftLocalTestClock(timestamp);
            _cursorTracker.MotionSampled += OnMotionSampled;
            _cursorTracker.AreaTransitioned += OnAreaTransitioned;
            _keyboardSource.DebugButtonChanged += OnDebugButtonChanged;
            _tapGestureDetector.GestureDetected += OnTapGestureDetected;
            _tapJudge.NoteResolved += OnTapNoteResolved;
            _sensorTracker.Transitioned += OnSensorTransitioned;
            _sensorTracker.ActionDetected += OnSensorActionDetected;
            _isFocused = Application.isFocused || _enableCameraInput;
            _mouseSource.SuppressNextDelta(_cursorTracker, timestamp);
        }

        private void Start()
        {
            var session = AirshiftSessionStore.Current ?? AirshiftSessionStore.StartLocalTest();
            InitializeStandardVisualsAsync(session, _loadCancellation.Token).Forget();
            _isChartSession = session.Kind == AirshiftSessionKind.ChartPlay && session.Song is not null;
            if (_isChartSession)
            {
                _chartSongTitle = session.Song!.Title;
                var levelIndex = (int)session.Level;
                _chartLevel = levelIndex >= 0 && levelIndex < session.Song.Levels.Length
                    ? session.Song.Levels[levelIndex]
                    : session.Level.ToString();
                LoadChartAsync(session, _loadCancellation.Token).Forget();
                return;
            }
            _debugView.AddSystemEvent(
                Time.realtimeSinceStartupAsDouble,
                session.Kind == AirshiftSessionKind.LocalTest
                    ? "NO-CLICK TAP TEST READY - CODE CHART"
                    : "UNSUPPORTED SESSION");
        }

        private void OnEnable()
        {
            _isFocused = Application.isFocused || _enableCameraInput;
            var timestamp = Time.realtimeSinceStartupAsDouble;
            _mouseSource.SuppressNextDelta(_cursorTracker, timestamp);
            if (_isFocused)
            {
                _tapClock.Resume(timestamp);
            }
            else
            {
                _tapClock.Pause(timestamp);
            }
        }

        private void OnDisable()
        {
            var timestamp = Time.realtimeSinceStartupAsDouble;
            _keyboardSource.ReleaseAll(timestamp);
            _mouseSource.SuppressNextDelta(_cursorTracker, timestamp);
            _tapGestureDetector.Reset();
            _tapClock.Pause(timestamp);
            _isFocused = false;
        }

        private void Update()
        {
            if (_isExiting)
            {
                return;
            }
            if (_isFocused != (Application.isFocused || _enableCameraInput))
            {
                HandleFocusChange(Application.isFocused);
            }
            var timestamp = Time.realtimeSinceStartupAsDouble;
            var commands = _keyboardSource.Poll(timestamp, _isFocused);
            if (commands.ToggleDebugOverlay)
            {
                _isDebugOverlayVisible = !_isDebugOverlayVisible;
                _debugView.AddSystemEvent(
                    timestamp,
                    _isDebugOverlayVisible ? "DEBUG OVERLAY SHOWN" : "DEBUG OVERLAY HIDDEN");
            }
            if (commands.Finish)
            {
                FinishTest();
                return;
            }
            if (commands.Cancel)
            {
                CancelTest();
                return;
            }
            if (commands.RestartTapTest)
            {
                if (!_isChartSession)
                {
                    RestartTapTest(timestamp);
                }
            }
            if (commands.ToggleHand)
            {
                var activeHand = _cursorTracker.ToggleActiveHand();
                _mouseSource.SuppressNextDelta(_cursorTracker, timestamp);
                _tapGestureDetector.Reset();
                _debugView.AddSystemEvent(timestamp, $"CONTROL -> {activeHand}");
            }
            if (commands.Recenter)
            {
                _cursorTracker.RecenterActiveHand(timestamp);
                _mouseSource.SuppressNextDelta(_cursorTracker, timestamp);
                _tapGestureDetector.Reset();
                _debugView.AddSystemEvent(timestamp, $"RECENTER {_cursorTracker.ActiveHand}");
            }
            if (!_isChartSession)
            {
                _tapTestTime = _tapClock.GetTime(timestamp);
                _tapJudge.UpdateMisses(_tapTestTime);
            }
#if UNITY_ANDROID && !UNITY_EDITOR
            _androidCameraSource?.Poll(_cursorTracker, timestamp, () =>
            {
                _tapGestureDetector.Reset();
                _sensorTracker.Reset(_cursorTracker.LeftHand.Position, _cursorTracker.RightHand.Position);
            });
#endif
            if (!_layout.TryGetDisplayRects(out var playfield, out _))
            {
                _mouseSource.SuppressNextDelta(_cursorTracker, timestamp);
                return;
            }
#if !UNITY_ANDROID || UNITY_EDITOR
            if (_enableCameraInput && _cameraSource != null)
            {
                // Edge can have focus while Unity plays in the background.
                _cameraSource.Poll(_cursorTracker, timestamp, () =>
                {
                    _tapGestureDetector.Reset();
                    _sensorTracker.Reset(_cursorTracker.LeftHand.Position, _cursorTracker.RightHand.Position);
                });
            }
            else if (_mouseSource.Poll(_cursorTracker, playfield.width, timestamp, _isFocused))
            {
                _tapGestureDetector.Reset();
                _sensorTracker.Reset(_cursorTracker.LeftHand.Position, _cursorTracker.RightHand.Position);
            }
#endif
            _chartPlayer?.Update(_sensorTracker.LeftArea, _sensorTracker.RightArea);
            if (_chartPlayer is not null)
            {
                _standardVisuals.UpdateScore(_chartPlayer);
            }
        }

        private void OnGUI()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            _androidCameraSource?.DrawOverlay();
#endif
            if (!_layout.TryGetDisplayRects(out var playfield, out var subDisplay))
            {
                return;
            }
            if (_isChartSession && _chartPlayer is not null)
            {
                _chartView.Draw(
                    _cursorTracker,
                    _chartPlayer,
                    _isFocused,
                    _chartSongTitle,
                    _chartLevel,
                    playfield,
                    subDisplay,
                    _isDebugOverlayVisible,
                    _standardVisuals.IsAvailable);
                return;
            }
            _debugView.Draw(
                _cursorTracker,
                _isFocused,
                _tapTestChart,
                _tapTestTime,
                _lastTapResult,
                playfield,
                subDisplay,
                _isDebugOverlayVisible);
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private void OnApplicationPause(bool paused)
        {
            _androidCameraSource?.OnPause(paused);
        }
#endif

        private void OnApplicationFocus(bool hasFocus)
        {
            HandleFocusChange(hasFocus);
        }

        private void OnDestroy()
        {
            _loadCancellation.Cancel();
            _loadCancellation.Dispose();
            var timestamp = Time.realtimeSinceStartupAsDouble;
            _keyboardSource.ReleaseAll(timestamp);
            _keyboardSource.DebugButtonChanged -= OnDebugButtonChanged;
            _cursorTracker.MotionSampled -= OnMotionSampled;
            _cursorTracker.AreaTransitioned -= OnAreaTransitioned;
            _tapGestureDetector.GestureDetected -= OnTapGestureDetected;
            _tapJudge.NoteResolved -= OnTapNoteResolved;
            _sensorTracker.Transitioned -= OnSensorTransitioned;
            _sensorTracker.ActionDetected -= OnSensorActionDetected;
            if (_chartPlayer is not null)
            {
                _chartPlayer.NoteResolved -= OnChartNoteResolved;
                _chartPlayer.Completed -= OnChartCompleted;
                _chartPlayer.Dispose();
            }
            _cameraSource?.Dispose();
#if UNITY_ANDROID && !UNITY_EDITOR
            _androidCameraSource?.Dispose();
#endif
            _debugView.Dispose();
            _chartView.Dispose();
            if (!_preserveSessionForResult)
            {
                AirshiftSessionStore.Clear();
            }
        }

        /// <summary>
        /// Handles focus discontinuities without turning them into motion samples or area crossings.
        /// </summary>
        /// <param name="hasFocus">Whether the Game view now owns focus.</param>
        private void HandleFocusChange(bool hasFocus)
        {
            _isFocused = hasFocus || _enableCameraInput;
            var timestamp = Time.realtimeSinceStartupAsDouble;
            _mouseSource.SuppressNextDelta(_cursorTracker, timestamp);
            _cursorTracker.ResetMotionBaselines(timestamp);
            _tapGestureDetector.Reset();
            if (!_isFocused)
            {
                _keyboardSource.ReleaseAll(timestamp);
                _tapClock.Pause(timestamp);
                _chartPlayer?.Pause();
            }
            else
            {
                _tapClock.Resume(timestamp);
                _chartPlayer?.Resume();
            }
            _sensorTracker.Reset(_cursorTracker.LeftHand.Position, _cursorTracker.RightHand.Position);
            _debugView.AddSystemEvent(timestamp, _isFocused ? "FOCUS RESTORED" : "FOCUS LOST");
        }

        /// <summary>
        /// Feeds real source movement into the independent TAP gesture detector.
        /// </summary>
        /// <param name="sample">The normalized AIRSHIFT motion sample.</param>
        private void OnMotionSampled(AirshiftMotionSample sample)
        {
            _tapGestureDetector.ObserveMotionSample(sample);
            _sensorTracker.Observe(sample);
        }

        /// <summary>
        /// Records one continuous geometry transition in the debug event list.
        /// </summary>
        /// <param name="transition">The entered or exited area.</param>
        private void OnAreaTransitioned(AirshiftAreaTransition transition)
        {
            _tapGestureDetector.ObserveAreaTransition(transition);
            _debugView.AddEvent(transition);
        }

        /// <summary>
        /// Records one isolated main-row keyboard debug transition.
        /// </summary>
        /// <param name="buttonEvent">The changed debug button.</param>
        private void OnDebugButtonChanged(DebugButtonEvent buttonEvent)
        {
            _debugView.AddEvent(buttonEvent);
        }

        /// <summary>
        /// Presents one recognized motion action to the isolated test judge.
        /// </summary>
        /// <param name="gesture">The unique no-click TAP action.</param>
        private void OnTapGestureDetected(TapGesture gesture)
        {
            _debugView.AddEvent(gesture);
            if (_isChartSession)
            {
                _chartPlayer?.ObserveTapGesture(gesture);
                return;
            }
            var timelineTime = _tapClock.GetTime(gesture.Timestamp);
            if (!_tapJudge.TryJudge(gesture, timelineTime, out _))
            {
                _lastTapResult = $"NO MATCH  #{gesture.ActionId} {gesture.HandId} {gesture.Area}";
            }
        }

        /// <summary>
        /// Records one local TAP hit or miss for the debug presentation.
        /// </summary>
        /// <param name="result">The resolved local-test note.</param>
        private void OnTapNoteResolved(AirshiftTapJudgeResult result)
        {
            _lastTapResult = result.State == AirshiftTapNoteState.Miss
                ? $"{result.Note.NoteId}  MISS"
                : $"{result.Note.NoteId}  {result.State.ToString().ToUpperInvariant()}  {result.TimingOffset:+0.000;-0.000;0.000}s";
            _debugView.AddEvent(result);
        }

        /// <summary>
        /// Restarts only the isolated code-defined TAP test and its local clock.
        /// </summary>
        /// <param name="timestamp">The monotonic restart timestamp.</param>
        private void RestartTapTest(double timestamp)
        {
            _tapJudge.Restart();
            _tapClock.Restart(timestamp);
            if (!_isFocused)
            {
                _tapClock.Pause(timestamp);
            }
            _tapGestureDetector.Reset();
            _tapTestTime = 0d;
            _lastTapResult = "RESTARTED";
            _debugView.AddSystemEvent(timestamp, "TAP TEST RESTARTED");
        }

        /// <summary>
        /// Completes the local test and opens its independent placeholder result.
        /// </summary>
        private void FinishTest()
        {
            if (_isChartSession)
            {
                _chartPlayer?.FinishEarly();
                return;
            }
            _isExiting = true;
            AirshiftSessionStore.Current?.Complete();
            _preserveSessionForResult = true;
            _keyboardSource.ReleaseAll(Time.realtimeSinceStartupAsDouble);
            MajInstances.SceneSwitcher.SwitchScene(MajScenes.AirshiftResult, false);
        }

        /// <summary>
        /// Cancels the local test and returns directly to the song list.
        /// </summary>
        private void CancelTest()
        {
            _isExiting = true;
            AirshiftSessionStore.Clear();
            _keyboardSource.ReleaseAll(Time.realtimeSinceStartupAsDouble);
            MajInstances.SceneSwitcher.SwitchScene(MajScenes.AirshiftList, false);
        }

        /// <summary>Loads the selected local chart and starts its real audio clock.</summary>
        private async UniTaskVoid LoadChartAsync(AirshiftSession session, CancellationToken cancellationToken)
        {
            _chartPlayer = new AirshiftChartPlayer(session);
            _chartPlayer.NoteResolved += OnChartNoteResolved;
            _chartPlayer.Completed += OnChartCompleted;
            try
            {
                await _chartPlayer.InitializeAsync(cancellationToken);
                if (!_isFocused)
                {
                    _chartPlayer.Pause();
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                _debugView.AddSystemEvent(
                    Time.realtimeSinceStartupAsDouble,
                    "CHART LOAD FAILED: " + exception.Message);
            }
        }

        /// <summary>Initializes copied STANDARD visual objects without affecting AIRSHIFT gameplay state.</summary>
        private async UniTaskVoid InitializeStandardVisualsAsync(
            AirshiftSession session,
            CancellationToken cancellationToken)
        {
            try
            {
                await _standardVisuals.InitializeAsync(session, cancellationToken);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                _debugView.AddSystemEvent(
                    Time.realtimeSinceStartupAsDouble,
                    "STANDARD VISUAL LOAD FAILED: " + exception.Message);
            }
        }

        /// <summary>Forwards real-collider transitions to ordered SLIDE and hold tracking.</summary>
        private void OnSensorTransitioned(AirshiftSensorTransition transition)
        {
            _chartPlayer?.ObserveSensorTransition(transition);
        }

        /// <summary>Forwards inner-region no-click actions to TOUCH and TOUCH HOLD judgement.</summary>
        private void OnSensorActionDetected(AirshiftSensorAction action)
        {
            _chartPlayer?.ObserveSensorAction(action);
        }

        /// <summary>Displays one real-chart judgement without touching STANDARD feedback objects.</summary>
        private void OnChartNoteResolved(AirshiftChartJudgeResult result)
        {
            _chartView.AddResult(result);
        }

        /// <summary>Moves a completed chart to the independent AIRSHIFT result scene.</summary>
        private void OnChartCompleted()
        {
            if (_isExiting)
            {
                return;
            }
            _isExiting = true;
            _preserveSessionForResult = true;
            _keyboardSource.ReleaseAll(Time.realtimeSinceStartupAsDouble);
            MajInstances.SceneSwitcher.SwitchScene(MajScenes.AirshiftResult, false);
        }
    }
}
