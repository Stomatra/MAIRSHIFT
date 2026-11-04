using System;
using System.Collections.Generic;

#nullable enable
namespace MajdataPlay.Scenes.Airshift
{
    /// <summary>Identifies an AIRSHIFT chart object's runtime lifecycle.</summary>
    internal enum AirshiftNoteState
    {
        Pending,
        Active,
        Resolved,
    }

    /// <summary>Prototype AIRSHIFT timing and completion grades; these are not official maimai windows.</summary>
    internal enum AirshiftJudgeGrade
    {
        None,
        Perfect,
        Great,
        Good,
        Miss,
    }

    /// <summary>One source-independent action in a physical touch-panel region.</summary>
    internal readonly struct AirshiftSensorAction
    {
        internal long ActionId { get; }
        internal HandId Hand { get; }
        internal AirshiftSensorArea Area { get; }
        internal double Timestamp { get; }
        internal float Speed { get; }

        internal AirshiftSensorAction(long actionId, HandId hand, AirshiftSensorArea area, double timestamp, float speed)
        {
            ActionId = actionId;
            Hand = hand;
            Area = area;
            Timestamp = timestamp;
            Speed = speed;
        }
    }

    /// <summary>Centralized, adjustable AIRSHIFT prototype judgement parameters.</summary>
    internal readonly struct AirshiftChartJudgeSettings
    {
        internal double PerfectWindow { get; }
        internal double GreatWindow { get; }
        internal double GoodWindow { get; }
        internal double HoldLeaveGrace { get; }
        internal double MinimumHoldCoverage { get; }
        internal double SlideCheckpointEarlyWindow { get; }
        internal double SlideCheckpointLateWindow { get; }

        internal static AirshiftChartJudgeSettings Prototype => new(
            0.08d, 0.16d, 0.25d, 0.12d, 0.72d, 0.35d, 0.45d);

        internal AirshiftChartJudgeSettings(
            double perfectWindow,
            double greatWindow,
            double goodWindow,
            double holdLeaveGrace,
            double minimumHoldCoverage,
            double slideCheckpointEarlyWindow,
            double slideCheckpointLateWindow)
        {
            if (perfectWindow < 0d || greatWindow < perfectWindow || goodWindow < greatWindow)
            {
                throw new ArgumentOutOfRangeException(nameof(perfectWindow));
            }
            if (holdLeaveGrace < 0d || minimumHoldCoverage is < 0d or > 1d ||
                slideCheckpointEarlyWindow < 0d || slideCheckpointLateWindow < 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(holdLeaveGrace));
            }
            PerfectWindow = perfectWindow;
            GreatWindow = greatWindow;
            GoodWindow = goodWindow;
            HoldLeaveGrace = holdLeaveGrace;
            MinimumHoldCoverage = minimumHoldCoverage;
            SlideCheckpointEarlyWindow = slideCheckpointEarlyWindow;
            SlideCheckpointLateWindow = slideCheckpointLateWindow;
        }
    }

    /// <summary>Immutable outcome emitted by the independent AIRSHIFT chart judge.</summary>
    internal readonly struct AirshiftChartJudgeResult
    {
        internal AirshiftChartNote Note { get; }
        internal AirshiftJudgeGrade Grade { get; }
        internal double TimingOffset { get; }
        internal bool IsFast => TimingOffset < 0d;

        internal AirshiftChartJudgeResult(AirshiftChartNote note, AirshiftJudgeGrade grade, double timingOffset)
        {
            Note = note;
            Grade = grade;
            TimingOffset = timingOffset;
        }
    }

    /// <summary>
    /// Judges AIRSHIFT chart objects without STANDARD NoteManager, ObjectCounter, or ScoreManager.
    /// </summary>
    internal sealed class AirshiftChartJudge
    {
        private readonly AirshiftChart _chart;
        private readonly AirshiftChartJudgeSettings _settings;
        private readonly HashSet<long> _consumedActions = new();
        private AirshiftSensorArea _leftArea;
        private AirshiftSensorArea _rightArea;

        internal event Action<AirshiftChartJudgeResult>? NoteResolved;

        internal AirshiftChartJudge(AirshiftChart chart, AirshiftChartJudgeSettings settings)
        {
            _chart = chart ?? throw new ArgumentNullException(nameof(chart));
            _settings = settings;
        }

        internal bool ObserveTapGesture(TapGesture gesture, double chartTime)
        {
            if (!_consumedActions.Add(gesture.ActionId))
            {
                return false;
            }
            var area = AirshiftChartConverter.FromOuterArea(gesture.Area);
            var ordinaryMatched = TryStartOrdinary(
                gesture.ActionId,
                gesture.HandId,
                area,
                chartTime,
                includeTouch: false);
            var slideArmed = ArmSlideStart(gesture.HandId, area, chartTime);
            return ordinaryMatched || slideArmed;
        }

        internal bool ObserveSensorAction(AirshiftSensorAction action, double chartTime)
        {
            if (!_consumedActions.Add(action.ActionId))
            {
                return false;
            }
            return TryStartOrdinary(action.ActionId, action.Hand, action.Area, chartTime, includeTouch: true);
        }

        internal void ObserveArea(HandId hand, AirshiftSensorArea area, long transitionId, double chartTime)
        {
            if (hand == HandId.Left)
            {
                _leftArea = area;
            }
            else
            {
                _rightArea = area;
            }
            AdvanceSlide(hand, area, transitionId, chartTime);
        }

        internal void Update(double chartTime, AirshiftSensorArea leftArea, AirshiftSensorArea rightArea)
        {
            _leftArea = leftArea;
            _rightArea = rightArea;
            foreach (var note in _chart.Notes)
            {
                if (note.State == AirshiftNoteState.Active &&
                    note.Kind is AirshiftChartNoteKind.Hold or AirshiftChartNoteKind.TouchHold)
                {
                    UpdateActiveHold(note, chartTime);
                    continue;
                }
                if (note.State != AirshiftNoteState.Pending)
                {
                    continue;
                }
                var missBoundary = note.Kind == AirshiftChartNoteKind.Slide
                    ? note.EndTime + _settings.SlideCheckpointLateWindow
                    : note.StartTime + _settings.GoodWindow;
                if (chartTime > missBoundary)
                {
                    Resolve(note, AirshiftJudgeGrade.Miss, chartTime - note.StartTime);
                }
            }
        }

        internal bool IsComplete
        {
            get
            {
                foreach (var note in _chart.Notes)
                {
                    if (note.State != AirshiftNoteState.Resolved)
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        private bool TryStartOrdinary(
            long actionId,
            HandId hand,
            AirshiftSensorArea area,
            double chartTime,
            bool includeTouch)
        {
            AirshiftChartNote? best = null;
            var bestOffset = double.PositiveInfinity;
            foreach (var note in _chart.Notes)
            {
                if (note.State != AirshiftNoteState.Pending || note.Area != area ||
                    (note.RequiredHand == AirshiftRequiredHand.Left && hand != HandId.Left) ||
                    (note.RequiredHand == AirshiftRequiredHand.Right && hand != HandId.Right))
                {
                    continue;
                }
                var acceptedKind = includeTouch
                    ? note.Kind is AirshiftChartNoteKind.Touch or AirshiftChartNoteKind.TouchHold
                    : note.Kind is AirshiftChartNoteKind.Tap or AirshiftChartNoteKind.Hold;
                if (!acceptedKind)
                {
                    continue;
                }
                var absoluteOffset = Math.Abs(chartTime - note.StartTime);
                if (absoluteOffset > _settings.GoodWindow ||
                    (best is not null && !IsBetter(note, absoluteOffset, best, bestOffset)))
                {
                    continue;
                }
                best = note;
                bestOffset = absoluteOffset;
            }
            if (best is null)
            {
                return false;
            }

            best.ActionId = actionId;
            best.Hand = hand;
            best.TimingOffset = chartTime - best.StartTime;
            var grade = GradeTiming(bestOffset);
            if (best.IsEx && grade != AirshiftJudgeGrade.Miss)
            {
                grade = AirshiftJudgeGrade.Perfect;
            }
            if (best.Kind is AirshiftChartNoteKind.Hold or AirshiftChartNoteKind.TouchHold)
            {
                best.State = AirshiftNoteState.Active;
                best.Grade = grade;
                best.LastCoverageTime = Math.Max(chartTime, best.StartTime);
            }
            else
            {
                Resolve(best, grade, best.TimingOffset);
            }
            return true;
        }

        private void UpdateActiveHold(AirshiftChartNote note, double chartTime)
        {
            var cappedTime = Math.Min(chartTime, note.EndTime);
            var elapsed = Math.Max(0d, cappedTime - note.LastCoverageTime);
            if (CurrentArea(note.Hand) == note.Area)
            {
                note.CoveredDuration += elapsed;
                note.UncoveredDuration = 0d;
            }
            else
            {
                note.UncoveredDuration += elapsed;
                if (note.UncoveredDuration <= _settings.HoldLeaveGrace)
                {
                    note.CoveredDuration += elapsed;
                }
            }
            note.LastCoverageTime = cappedTime;
            if (chartTime < note.EndTime)
            {
                return;
            }
            var duration = Math.Max(0.001d, note.EndTime - note.StartTime);
            var coverage = Math.Clamp(note.CoveredDuration / duration, 0d, 1d);
            var grade = coverage >= 0.94d ? note.Grade :
                coverage >= 0.84d ? Worse(note.Grade, AirshiftJudgeGrade.Great) :
                coverage >= _settings.MinimumHoldCoverage ? Worse(note.Grade, AirshiftJudgeGrade.Good) :
                AirshiftJudgeGrade.Miss;
            Resolve(note, grade, note.TimingOffset);
        }

        private void AdvanceSlide(HandId hand, AirshiftSensorArea area, long transitionId, double chartTime)
        {
            AirshiftChartNote? candidate = null;
            var candidateTime = double.PositiveInfinity;
            foreach (var note in _chart.Notes)
            {
                if (note.Kind != AirshiftChartNoteKind.Slide || note.State == AirshiftNoteState.Resolved ||
                    note.SlideCheckpointIndex >= note.SlideCheckpoints.Count)
                {
                    continue;
                }
                var checkpoint = note.SlideCheckpoints[note.SlideCheckpointIndex];
                if (checkpoint.Area != area || chartTime < checkpoint.TargetTime - _settings.SlideCheckpointEarlyWindow ||
                    chartTime > checkpoint.TargetTime + _settings.SlideCheckpointLateWindow)
                {
                    continue;
                }
                if (checkpoint.TargetTime < candidateTime)
                {
                    candidate = note;
                    candidateTime = checkpoint.TargetTime;
                }
            }
            if (candidate is null || !_consumedActions.Add(transitionId))
            {
                return;
            }
            candidate.Hand ??= hand;
            if (candidate.Hand != hand)
            {
                return;
            }
            candidate.State = AirshiftNoteState.Active;
            candidate.SlideCheckpointIndex++;
            if (candidate.SlideCheckpointIndex < candidate.SlideCheckpoints.Count)
            {
                return;
            }
            var offset = chartTime - candidate.EndTime;
            Resolve(candidate, GradeTiming(Math.Abs(offset)), offset);
        }

        private bool ArmSlideStart(HandId hand, AirshiftSensorArea area, double chartTime)
        {
            foreach (var note in _chart.Notes)
            {
                if (note.Kind != AirshiftChartNoteKind.Slide || note.State != AirshiftNoteState.Pending ||
                    note.SlideCheckpoints.Count == 0 || note.SlideCheckpoints[0].Area != area)
                {
                    continue;
                }
                var checkpoint = note.SlideCheckpoints[0];
                if (chartTime < checkpoint.TargetTime - _settings.SlideCheckpointEarlyWindow ||
                    chartTime > checkpoint.TargetTime + _settings.SlideCheckpointLateWindow)
                {
                    continue;
                }
                note.Hand = hand;
                note.State = AirshiftNoteState.Active;
                note.SlideCheckpointIndex = 1;
                return true;
            }
            return false;
        }

        private void Resolve(AirshiftChartNote note, AirshiftJudgeGrade grade, double timingOffset)
        {
            if (note.State == AirshiftNoteState.Resolved)
            {
                return;
            }
            note.State = AirshiftNoteState.Resolved;
            note.Grade = grade;
            note.TimingOffset = timingOffset;
            NoteResolved?.Invoke(new AirshiftChartJudgeResult(note, grade, timingOffset));
        }

        private AirshiftJudgeGrade GradeTiming(double absoluteOffset)
        {
            return absoluteOffset <= _settings.PerfectWindow ? AirshiftJudgeGrade.Perfect :
                absoluteOffset <= _settings.GreatWindow ? AirshiftJudgeGrade.Great :
                absoluteOffset <= _settings.GoodWindow ? AirshiftJudgeGrade.Good :
                AirshiftJudgeGrade.Miss;
        }

        private AirshiftSensorArea CurrentArea(HandId? hand)
        {
            return hand == HandId.Left ? _leftArea : hand == HandId.Right ? _rightArea : AirshiftSensorArea.None;
        }

        private static AirshiftJudgeGrade Worse(AirshiftJudgeGrade left, AirshiftJudgeGrade right)
        {
            return (AirshiftJudgeGrade)Math.Max((int)left, (int)right);
        }

        private static bool IsBetter(
            AirshiftChartNote candidate,
            double candidateOffset,
            AirshiftChartNote current,
            double currentOffset)
        {
            var offsetComparison = candidateOffset.CompareTo(currentOffset);
            if (offsetComparison != 0)
            {
                return offsetComparison < 0;
            }
            var timeComparison = candidate.StartTime.CompareTo(current.StartTime);
            return timeComparison != 0 ? timeComparison < 0 : candidate.NoteId < current.NoteId;
        }
    }
}
