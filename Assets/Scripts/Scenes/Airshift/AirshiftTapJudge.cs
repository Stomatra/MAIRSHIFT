using System;
using System.Collections.Generic;

#nullable enable
namespace MajdataPlay.Scenes.Airshift
{
    /// <summary>
    /// Describes one resolved local-test TAP note.
    /// </summary>
    internal readonly struct AirshiftTapJudgeResult
    {
        /// <summary>Gets the resolved note.</summary>
        internal AirshiftTestTapNote Note { get; }
        /// <summary>Gets the resulting prototype grade.</summary>
        internal AirshiftTapNoteState State { get; }
        /// <summary>Gets the event-minus-target timing offset.</summary>
        internal double TimingOffset { get; }
        /// <summary>Gets the consuming action identifier, if this was a hit.</summary>
        internal long? ActionId { get; }

        /// <summary>
        /// Creates one immutable judgement result.
        /// </summary>
        /// <param name="note">The resolved note.</param>
        /// <param name="state">The prototype grade.</param>
        /// <param name="timingOffset">The event-minus-target timing offset.</param>
        /// <param name="actionId">The consuming action identifier, if any.</param>
        internal AirshiftTapJudgeResult(
            AirshiftTestTapNote note,
            AirshiftTapNoteState state,
            double timingOffset,
            long? actionId)
        {
            Note = note;
            State = state;
            TimingOffset = timingOffset;
            ActionId = actionId;
        }
    }

    /// <summary>
    /// Judges isolated single-note TAP gestures without touching STANDARD score or note systems.
    /// </summary>
    internal sealed class AirshiftTapJudge
    {
        /// <summary>The isolated code-defined test chart.</summary>
        private readonly AirshiftTestChart _chart;
        /// <summary>The adjustable non-official timing windows.</summary>
        private readonly AirshiftTapJudgeSettings _settings;
        /// <summary>Every action already presented to this judge, whether it hit or not.</summary>
        private readonly HashSet<long> _processedActions = new();

        /// <summary>Raised whenever a note becomes hit or missed.</summary>
        internal event Action<AirshiftTapJudgeResult>? NoteResolved;

        /// <summary>
        /// Creates an isolated TAP judge.
        /// </summary>
        /// <param name="chart">The local-test chart.</param>
        /// <param name="settings">The prototype timing windows.</param>
        internal AirshiftTapJudge(AirshiftTestChart chart, AirshiftTapJudgeSettings settings)
        {
            _chart = chart ?? throw new ArgumentNullException(nameof(chart));
            _settings = settings;
        }

        /// <summary>
        /// Attempts to consume one gesture against at most one eligible note.
        /// </summary>
        /// <param name="gesture">The unique no-click action.</param>
        /// <param name="timelineTime">The gesture time on the local chart timeline.</param>
        /// <param name="result">The resulting hit when successful.</param>
        /// <returns>Whether one note was consumed.</returns>
        internal bool TryJudge(
            TapGesture gesture,
            double timelineTime,
            out AirshiftTapJudgeResult result)
        {
            result = default;
            if (!_processedActions.Add(gesture.ActionId))
            {
                return false;
            }
            UpdateMisses(timelineTime);

            AirshiftTestTapNote? bestNote = null;
            var bestAbsoluteOffset = double.PositiveInfinity;
            for (var i = 0; i < _chart.Notes.Count; i++)
            {
                var note = _chart.Notes[i];
                if (note.State != AirshiftTapNoteState.Pending ||
                    note.Area != gesture.Area ||
                    !AcceptsHand(note.RequiredHand, gesture.HandId))
                {
                    continue;
                }
                var absoluteOffset = Math.Abs(timelineTime - note.TargetTime);
                if (absoluteOffset > _settings.GoodWindow)
                {
                    continue;
                }
                if (bestNote is null || IsBetterCandidate(note, absoluteOffset, bestNote, bestAbsoluteOffset))
                {
                    bestNote = note;
                    bestAbsoluteOffset = absoluteOffset;
                }
            }
            if (bestNote is null)
            {
                return false;
            }

            var timingOffset = timelineTime - bestNote.TargetTime;
            var state = GetHitState(bestAbsoluteOffset);
            bestNote.Resolve(state, gesture.ActionId, timingOffset);
            result = new AirshiftTapJudgeResult(bestNote, state, timingOffset, gesture.ActionId);
            NoteResolved?.Invoke(result);
            return true;
        }

        /// <summary>
        /// Marks every note whose late boundary has passed as missed.
        /// </summary>
        /// <param name="timelineTime">The current local chart time.</param>
        internal void UpdateMisses(double timelineTime)
        {
            for (var i = 0; i < _chart.Notes.Count; i++)
            {
                var note = _chart.Notes[i];
                if (note.State != AirshiftTapNoteState.Pending || timelineTime <= note.TargetTime + _settings.GoodWindow)
                {
                    continue;
                }
                var timingOffset = timelineTime - note.TargetTime;
                note.Resolve(AirshiftTapNoteState.Miss, null, timingOffset);
                NoteResolved?.Invoke(new AirshiftTapJudgeResult(
                    note,
                    AirshiftTapNoteState.Miss,
                    timingOffset,
                    null));
            }
        }

        /// <summary>
        /// Clears consumed actions and restores every test note.
        /// </summary>
        internal void Restart()
        {
            _processedActions.Clear();
            _chart.Reset();
        }

        /// <summary>
        /// Maps one absolute offset to a prototype grade.
        /// </summary>
        /// <param name="absoluteOffset">The absolute timing error.</param>
        /// <returns>The non-official prototype judgement.</returns>
        private AirshiftTapNoteState GetHitState(double absoluteOffset)
        {
            if (absoluteOffset <= _settings.PerfectWindow)
            {
                return AirshiftTapNoteState.Perfect;
            }
            if (absoluteOffset <= _settings.GreatWindow)
            {
                return AirshiftTapNoteState.Great;
            }
            return AirshiftTapNoteState.Good;
        }

        /// <summary>
        /// Checks whether a gesture hand satisfies one note constraint.
        /// </summary>
        /// <param name="requiredHand">The note constraint.</param>
        /// <param name="gestureHand">The gesture hand.</param>
        /// <returns>Whether the gesture hand is accepted.</returns>
        private static bool AcceptsHand(AirshiftRequiredHand requiredHand, HandId gestureHand)
        {
            return requiredHand switch
            {
                AirshiftRequiredHand.Any => true,
                AirshiftRequiredHand.Left => gestureHand == HandId.Left,
                AirshiftRequiredHand.Right => gestureHand == HandId.Right,
                _ => false,
            };
        }

        /// <summary>
        /// Applies deterministic closest-time, earlier-time, and identifier tie breakers.
        /// </summary>
        /// <param name="candidate">The candidate note.</param>
        /// <param name="candidateOffset">The candidate absolute offset.</param>
        /// <param name="current">The current best note.</param>
        /// <param name="currentOffset">The current best absolute offset.</param>
        /// <returns>Whether the candidate should replace the current note.</returns>
        private static bool IsBetterCandidate(
            AirshiftTestTapNote candidate,
            double candidateOffset,
            AirshiftTestTapNote current,
            double currentOffset)
        {
            var offsetComparison = candidateOffset.CompareTo(currentOffset);
            if (offsetComparison != 0)
            {
                return offsetComparison < 0;
            }
            var timeComparison = candidate.TargetTime.CompareTo(current.TargetTime);
            return timeComparison != 0
                ? timeComparison < 0
                : string.Compare(candidate.NoteId, current.NoteId, StringComparison.Ordinal) < 0;
        }
    }
}
