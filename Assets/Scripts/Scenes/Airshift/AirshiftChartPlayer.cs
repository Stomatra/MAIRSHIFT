using Cysharp.Threading.Tasks;
using MajdataPlay.IO;
using MajRadar.MajSimaiAdapter;
using MajSimai;
using MajdataPlay.Utils.ChartRadar;
using System;
using System.Linq;
using System.Threading;

#nullable enable
namespace MajdataPlay.Scenes.Airshift
{
    /// <summary>Owns loading, audio-clock playback, judgement, and independent result creation for one chart.</summary>
    internal sealed class AirshiftChartPlayer : IDisposable
    {
        private readonly AirshiftSession _session;
        private AudioSampleWrap? _audio;
        private AirshiftChartJudge? _judge;
        private AirshiftScoreCounter? _score;
        private bool _completed;

        internal event Action<AirshiftChartJudgeResult>? NoteResolved;
        internal event Action? Completed;

        internal AirshiftChart? Chart { get; private set; }
        internal string Status { get; private set; } = "LOADING LOCAL CHART...";
        internal bool IsReady => Chart is not null && _audio is not null && _judge is not null;
        internal double CurrentTime => _audio?.CurrentSec ?? 0d;
        internal double AudioLength => _audio?.Length.TotalSeconds ?? 0d;
        /// <summary>Gets a read-only live AIRSHIFT score snapshot for visual adapters.</summary>
        internal AirshiftScoreSnapshot? CurrentScore =>
            _score is null || Chart is null ? null : _score.CreateSnapshot(Chart.Notes.Count);
        /// <summary>Gets the current AIRSHIFT combo without exposing the score counter.</summary>
        internal int CurrentCombo => _score?.CurrentCombo ?? 0;

        internal AirshiftChartPlayer(AirshiftSession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        internal async UniTask InitializeAsync(CancellationToken cancellationToken)
        {
            if (_session.Song is null)
            {
                throw new InvalidOperationException("AIRSHIFT chart session has no song.");
            }
            try
            {
                var song = _session.Song;
                var levelIndex = (int)_session.Level;
                var simaiFile = await song.GetMaidataAsync(true, token: cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                if (levelIndex < 0 || levelIndex >= simaiFile.Charts.Length)
                {
                    throw new InvalidOperationException("Selected AIRSHIFT difficulty is outside the maidata chart range.");
                }
                var fumen = simaiFile.Charts[levelIndex].Fumen;
                if (string.IsNullOrWhiteSpace(fumen))
                {
                    throw new InvalidOperationException("Selected AIRSHIFT chart is empty.");
                }
                var level = levelIndex < song.Levels.Length ? song.Levels[levelIndex] : string.Empty;
                var designer = levelIndex < song.Designers.Length ? song.Designers[levelIndex] : string.Empty;
                // Only AIRSHIFT understands L/R note prefixes. STANDARD still
                // passes the original fumen to MajSimai without preprocessing.
                var handedSyntax = AirshiftHandedSimaiSyntax.Preprocess(fumen);
                var simaiChart = await SimaiParser.ParseChartAsync(level, designer, handedSyntax.SimaiText);
                cancellationToken.ThrowIfCancellationRequested();
                var adaptation = new MajSimaiChartAdapter(new PlayExtendedSlideBarCountProvider())
                    .Adapt(simaiChart, cancellationToken);
                if (!adaptation.IsSuccess || adaptation.Chart is null)
                {
                    throw new InvalidOperationException(
                        "AIRSHIFT chart conversion failed: " + string.Join("; ", adaptation.Errors));
                }
                Chart = AirshiftChartConverter.Convert(adaptation.Chart, simaiFile.Offset);
                AirshiftHandedSimaiSyntax.Apply(Chart, handedSyntax);
                if (Chart.Notes.Count == 0)
                {
                    throw new InvalidOperationException("Selected AIRSHIFT chart contains no playable notes.");
                }
                if (Chart.Notes.Any(note => note.IsMine))
                {
                    throw new NotSupportedException(
                        "This chart contains MINE notes, which Task 006 deliberately does not judge as normal AIRSHIFT notes.");
                }
                _judge = new AirshiftChartJudge(Chart, AirshiftChartJudgeSettings.Prototype);
                _judge.NoteResolved += OnNoteResolved;
                _score = new AirshiftScoreCounter(Chart.Notes);

                Status = "LOADING AUDIO...";
                _audio = await song.GetAudioTrackAsync(token: cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                if (_audio.IsEmpty)
                {
                    throw new InvalidOperationException("Selected song returned an empty audio track.");
                }
                _audio.Stop();
                _audio.CurrentSec = 0d;
                _audio.IsLoop = false;
                _audio.Volume = MajEnv.Settings.Audio.Volume.BGM;
                _audio.Play();
                Status = "PLAYING";
            }
            catch (OperationCanceledException)
            {
                Status = "LOAD CANCELLED";
                throw;
            }
            catch (Exception exception)
            {
                Status = "LOAD FAILED: " + exception.Message;
                throw;
            }
        }

        internal void Update(AirshiftSensorArea leftArea, AirshiftSensorArea rightArea)
        {
            if (!IsReady || _completed)
            {
                return;
            }
            _judge!.Update(CurrentTime, leftArea, rightArea);
            var chartFinished = _judge.IsComplete && CurrentTime > Chart!.EndTime + 0.35d;
            var audioFinished = AudioLength > 0d && CurrentTime >= AudioLength - 0.03d;
            if (chartFinished || audioFinished)
            {
                Complete();
            }
        }

        internal void ObserveTapGesture(TapGesture gesture)
        {
            _judge?.ObserveTapGesture(gesture, CurrentTime);
        }

        internal void ObserveSensorAction(AirshiftSensorAction action)
        {
            _judge?.ObserveSensorAction(action, CurrentTime);
        }

        internal void ObserveSensorTransition(AirshiftSensorTransition transition)
        {
            _judge?.ObserveArea(
                transition.Hand,
                transition.Entered ? transition.Area : AirshiftSensorArea.None,
                transition.TransitionId,
                CurrentTime);
        }

        internal void FinishEarly()
        {
            if (!IsReady || _completed)
            {
                return;
            }
            _judge!.Update(double.MaxValue / 4d, AirshiftSensorArea.None, AirshiftSensorArea.None);
            Complete();
        }

        internal void Pause()
        {
            if (_audio?.IsPlaying == true)
            {
                _audio.Pause();
            }
        }

        internal void Resume()
        {
            if (IsReady && !_completed && _audio?.IsPlaying == false)
            {
                _audio.Play();
            }
        }

        public void Dispose()
        {
            if (_judge is not null)
            {
                _judge.NoteResolved -= OnNoteResolved;
            }
            if (_audio is not null)
            {
                _audio.Stop();
            }
        }

        private void OnNoteResolved(AirshiftChartJudgeResult result)
        {
            _score!.Add(result);
            NoteResolved?.Invoke(result);
        }

        private void Complete()
        {
            if (_completed || Chart is null || _score is null || _session.Song is null)
            {
                return;
            }
            _completed = true;
            _audio?.Stop();
            var snapshot = _score.CreateSnapshot(Chart.Notes.Count);
            AirshiftStoredRecord? storedRecord = null;
            string? message = null;
            try
            {
                storedRecord = AirshiftRecordStore.Save(
                    MajEnv.RuntimeDirectory,
                    AirshiftRecordStore.CreateChartKey(_session.Song.Hash, _session.Level),
                    snapshot,
                    debugInput: false);
            }
            catch (Exception exception)
            {
                message = "AIRSHIFT score could not be saved: " + exception.Message;
            }
            _session.CompleteChart(snapshot, storedRecord, usedDebugInput: false, message);
            Status = "COMPLETE";
            Completed?.Invoke();
        }
    }
}
