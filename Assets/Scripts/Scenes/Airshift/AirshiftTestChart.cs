using System;
using System.Collections.Generic;

#nullable enable
namespace MajdataPlay.Scenes.Airshift
{
    /// <summary>
    /// Represents one code-defined TAP note in the isolated AIRSHIFT test chart.
    /// </summary>
    internal sealed class AirshiftTestTapNote
    {
        /// <summary>Gets the stable local-test note identifier.</summary>
        internal string NoteId { get; }
        /// <summary>Gets the target time on the local timeline.</summary>
        internal double TargetTime { get; }
        /// <summary>Gets the required A1-A8 target.</summary>
        internal AirshiftArea Area { get; }
        /// <summary>Gets the accepted hand constraint.</summary>
        internal AirshiftRequiredHand RequiredHand { get; }
        /// <summary>Gets the current note state.</summary>
        internal AirshiftTapNoteState State { get; private set; }
        /// <summary>Gets the consuming gesture identifier, if the note was hit.</summary>
        internal long? ActionId { get; private set; }
        /// <summary>Gets the resolved timing offset, if available.</summary>
        internal double? TimingOffset { get; private set; }

        /// <summary>
        /// Creates one local-test TAP note.
        /// </summary>
        /// <param name="noteId">The stable test identifier.</param>
        /// <param name="targetTime">The target local timeline time.</param>
        /// <param name="area">The target outer area.</param>
        /// <param name="requiredHand">The accepted hand constraint.</param>
        /// <exception cref="ArgumentException">The identifier is empty.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The time or area is invalid.</exception>
        internal AirshiftTestTapNote(
            string noteId,
            double targetTime,
            AirshiftArea area,
            AirshiftRequiredHand requiredHand)
        {
            if (string.IsNullOrWhiteSpace(noteId))
            {
                throw new ArgumentException("A test note requires an identifier.", nameof(noteId));
            }
            if (targetTime < 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(targetTime));
            }
            var areaValue = (int)area;
            if (areaValue < (int)AirshiftArea.A1 || areaValue > (int)AirshiftArea.A8)
            {
                throw new ArgumentOutOfRangeException(nameof(area));
            }
            NoteId = noteId;
            TargetTime = targetTime;
            Area = area;
            RequiredHand = requiredHand;
        }

        /// <summary>
        /// Resolves this note exactly once.
        /// </summary>
        /// <param name="state">The final judgement state.</param>
        /// <param name="actionId">The consuming action identifier, if hit.</param>
        /// <param name="timingOffset">The local timing offset.</param>
        /// <exception cref="InvalidOperationException">The note has already been resolved.</exception>
        internal void Resolve(AirshiftTapNoteState state, long? actionId, double timingOffset)
        {
            if (State != AirshiftTapNoteState.Pending)
            {
                throw new InvalidOperationException($"Note {NoteId} has already been resolved.");
            }
            State = state;
            ActionId = actionId;
            TimingOffset = timingOffset;
        }

        /// <summary>
        /// Restores the note to its initial local-test state.
        /// </summary>
        internal void Reset()
        {
            State = AirshiftTapNoteState.Pending;
            ActionId = null;
            TimingOffset = null;
        }
    }

    /// <summary>
    /// Owns the small code-defined TAP sequence used only by the AIRSHIFT input test scene.
    /// </summary>
    internal sealed class AirshiftTestChart
    {
        /// <summary>The deterministically ordered local-test notes.</summary>
        private readonly AirshiftTestTapNote[] _notes;

        /// <summary>Gets the read-only ordered note collection.</summary>
        internal IReadOnlyList<AirshiftTestTapNote> Notes => _notes;

        /// <summary>
        /// Creates a chart from isolated local-test notes.
        /// </summary>
        /// <param name="notes">The notes to copy and order.</param>
        /// <exception cref="ArgumentException">The chart is empty or contains duplicate identifiers.</exception>
        internal AirshiftTestChart(IEnumerable<AirshiftTestTapNote> notes)
        {
            _notes = new List<AirshiftTestTapNote>(notes ?? throw new ArgumentNullException(nameof(notes))).ToArray();
            if (_notes.Length == 0)
            {
                throw new ArgumentException("A test chart requires at least one note.", nameof(notes));
            }
            var noteIds = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < _notes.Length; i++)
            {
                if (!noteIds.Add(_notes[i].NoteId))
                {
                    throw new ArgumentException($"Duplicate test note identifier: {_notes[i].NoteId}", nameof(notes));
                }
            }
            Array.Sort(_notes, CompareNotes);
        }

        /// <summary>
        /// Creates the five-note AIRSHIFT TAP prototype without reading a Simai chart.
        /// </summary>
        /// <returns>The fresh code-defined chart.</returns>
        internal static AirshiftTestChart CreatePrototype()
        {
            return new AirshiftTestChart(new[]
            {
                new AirshiftTestTapNote("TAP-01", 1.50d, AirshiftArea.A1, AirshiftRequiredHand.Any),
                new AirshiftTestTapNote("TAP-02", 3.00d, AirshiftArea.A3, AirshiftRequiredHand.Left),
                new AirshiftTestTapNote("TAP-03", 4.50d, AirshiftArea.A5, AirshiftRequiredHand.Right),
                new AirshiftTestTapNote("TAP-04", 6.00d, AirshiftArea.A3, AirshiftRequiredHand.Any),
                new AirshiftTestTapNote("TAP-05", 7.50d, AirshiftArea.A7, AirshiftRequiredHand.Left),
            });
        }

        /// <summary>
        /// Gets the earliest unresolved note.
        /// </summary>
        /// <returns>The earliest pending note, or <see langword="null"/> when complete.</returns>
        internal AirshiftTestTapNote? GetNextPendingNote()
        {
            for (var i = 0; i < _notes.Length; i++)
            {
                if (_notes[i].State == AirshiftTapNoteState.Pending)
                {
                    return _notes[i];
                }
            }
            return null;
        }

        /// <summary>
        /// Restores every test note to pending.
        /// </summary>
        internal void Reset()
        {
            for (var i = 0; i < _notes.Length; i++)
            {
                _notes[i].Reset();
            }
        }

        /// <summary>
        /// Orders notes by target time and then stable identifier.
        /// </summary>
        /// <param name="left">The left note.</param>
        /// <param name="right">The right note.</param>
        /// <returns>The deterministic comparison result.</returns>
        private static int CompareNotes(AirshiftTestTapNote left, AirshiftTestTapNote right)
        {
            var timeComparison = left.TargetTime.CompareTo(right.TargetTime);
            return timeComparison != 0
                ? timeComparison
                : string.Compare(left.NoteId, right.NoteId, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// Defines the replaceable timeline contract used by the isolated AIRSHIFT test judge.
    /// </summary>
    internal interface IAirshiftTimelineClock
    {
        /// <summary>Gets whether the timeline is paused.</summary>
        bool IsPaused { get; }

        /// <summary>
        /// Restarts the timeline from zero.
        /// </summary>
        /// <param name="timestamp">The monotonic restart timestamp.</param>
        void Restart(double timestamp);

        /// <summary>
        /// Pauses the timeline.
        /// </summary>
        /// <param name="timestamp">The monotonic pause timestamp.</param>
        void Pause(double timestamp);

        /// <summary>
        /// Resumes the timeline.
        /// </summary>
        /// <param name="timestamp">The monotonic resume timestamp.</param>
        void Resume(double timestamp);

        /// <summary>
        /// Converts a monotonic source timestamp to timeline time.
        /// </summary>
        /// <param name="timestamp">The source monotonic timestamp.</param>
        /// <returns>The non-decreasing timeline time.</returns>
        double GetTime(double timestamp);
    }

    /// <summary>
    /// Provides a controllable monotonic local timeline that can later be replaced by audio time.
    /// </summary>
    internal sealed class AirshiftLocalTestClock : IAirshiftTimelineClock
    {
        /// <summary>The monotonic timestamp at the latest restart.</summary>
        private double _originTimestamp;
        /// <summary>The monotonic timestamp at which the current pause started.</summary>
        private double _pauseTimestamp;
        /// <summary>The accumulated paused duration.</summary>
        private double _pausedDuration;
        /// <summary>The last returned timeline time.</summary>
        private double _lastTime;

        /// <summary>Gets whether the clock is currently paused.</summary>
        public bool IsPaused { get; private set; }

        /// <summary>
        /// Creates and starts a local timeline.
        /// </summary>
        /// <param name="timestamp">The initial monotonic timestamp.</param>
        internal AirshiftLocalTestClock(double timestamp)
        {
            Restart(timestamp);
        }

        /// <summary>
        /// Restarts the timeline from zero.
        /// </summary>
        /// <param name="timestamp">The monotonic restart timestamp.</param>
        public void Restart(double timestamp)
        {
            _originTimestamp = timestamp;
            _pauseTimestamp = timestamp;
            _pausedDuration = 0d;
            _lastTime = 0d;
            IsPaused = false;
        }

        /// <summary>
        /// Pauses the local timeline without allowing it to move backwards.
        /// </summary>
        /// <param name="timestamp">The monotonic pause timestamp.</param>
        public void Pause(double timestamp)
        {
            if (IsPaused)
            {
                return;
            }
            _lastTime = GetTime(timestamp);
            _pauseTimestamp = Math.Max(timestamp, _originTimestamp);
            IsPaused = true;
        }

        /// <summary>
        /// Resumes the local timeline after a focus pause.
        /// </summary>
        /// <param name="timestamp">The monotonic resume timestamp.</param>
        public void Resume(double timestamp)
        {
            if (!IsPaused)
            {
                return;
            }
            _pausedDuration += Math.Max(0d, timestamp - _pauseTimestamp);
            IsPaused = false;
        }

        /// <summary>
        /// Converts a monotonic source timestamp to the current local timeline.
        /// </summary>
        /// <param name="timestamp">The source monotonic timestamp.</param>
        /// <returns>The non-decreasing local timeline time.</returns>
        public double GetTime(double timestamp)
        {
            var effectiveTimestamp = IsPaused ? _pauseTimestamp : timestamp;
            var elapsed = Math.Max(0d, effectiveTimestamp - _originTimestamp - _pausedDuration);
            _lastTime = Math.Max(_lastTime, elapsed);
            return _lastTime;
        }
    }
}
