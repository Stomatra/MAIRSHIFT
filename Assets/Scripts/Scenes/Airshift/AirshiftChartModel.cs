using MajRadar.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

#nullable enable
namespace MajdataPlay.Scenes.Airshift
{
    /// <summary>Identifies the playable AIRSHIFT note families.</summary>
    internal enum AirshiftChartNoteKind
    {
        Tap,
        Hold,
        Touch,
        TouchHold,
        Slide,
    }

    /// <summary>Identifies one of the physical maimai touch-panel regions.</summary>
    internal enum AirshiftSensorArea
    {
        None = -1,
        A1 = 0, A2, A3, A4, A5, A6, A7, A8,
        B1, B2, B3, B4, B5, B6, B7, B8,
        C,
        D1, D2, D3, D4, D5, D6, D7, D8,
        E1, E2, E3, E4, E5, E6, E7, E8,
    }

    /// <summary>Represents one ordered region of an AIRSHIFT slide path.</summary>
    internal readonly struct AirshiftSlideCheckpoint
    {
        internal AirshiftSensorArea Area { get; }
        internal double TargetTime { get; }

        internal AirshiftSlideCheckpoint(AirshiftSensorArea area, double targetTime)
        {
            Area = area;
            TargetTime = targetTime;
        }
    }

    /// <summary>Mutable runtime state of one independently judged chart object.</summary>
    internal sealed class AirshiftChartNote
    {
        internal int NoteId { get; }
        internal AirshiftChartNoteKind Kind { get; }
        internal double StartTime { get; }
        internal double EndTime { get; }
        internal AirshiftSensorArea Area { get; }
        internal bool IsSlideHead { get; set; }
        internal AirshiftRequiredHand RequiredHand { get; set; } = AirshiftRequiredHand.Any;
        internal bool IsBreak { get; }
        internal bool IsEx { get; }
        internal bool IsMine { get; }
        internal string RawToken { get; }
        internal IReadOnlyList<AirshiftSlideCheckpoint> SlideCheckpoints { get; }
        internal AirshiftNoteState State { get; set; }
        internal AirshiftJudgeGrade Grade { get; set; }
        internal double TimingOffset { get; set; }
        internal HandId? Hand { get; set; }
        internal long? ActionId { get; set; }
        internal double CoveredDuration { get; set; }
        internal double UncoveredDuration { get; set; }
        internal double LastCoverageTime { get; set; }
        internal int SlideCheckpointIndex { get; set; }

        internal AirshiftChartNote(
            int noteId,
            AirshiftChartNoteKind kind,
            double startTime,
            double endTime,
            AirshiftSensorArea area,
            bool isSlideHead,
            bool isBreak,
            bool isEx,
            bool isMine,
            string rawToken,
            IReadOnlyList<AirshiftSlideCheckpoint>? slideCheckpoints = null)
        {
            NoteId = noteId;
            Kind = kind;
            StartTime = startTime;
            EndTime = Math.Max(startTime, endTime);
            Area = area;
            IsSlideHead = isSlideHead;
            IsBreak = isBreak;
            IsEx = isEx;
            IsMine = isMine;
            RawToken = rawToken;
            SlideCheckpoints = slideCheckpoints ?? Array.Empty<AirshiftSlideCheckpoint>();
        }
    }

    /// <summary>Contains a stable AIRSHIFT copy of one parsed Simai chart.</summary>
    internal sealed class AirshiftChart
    {
        internal IReadOnlyList<AirshiftChartNote> Notes { get; }
        internal double EndTime { get; }

        internal AirshiftChart(IReadOnlyList<AirshiftChartNote> notes, double endTime)
        {
            Notes = notes;
            EndTime = endTime;
        }
    }

    /// <summary>Converts MajRadar's stable Simai event representation without using STANDARD note objects.</summary>
    internal static class AirshiftChartConverter
    {
        internal static AirshiftChart Convert(RadarChartInput source, double audioOffsetSeconds)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            var notes = source.Events
                .Where(item => item.Kind != RadarEventKind.Timing)
                .Select(item => ConvertEvent(item, audioOffsetSeconds))
                .OrderBy(item => item.StartTime)
                .ThenBy(item => item.NoteId)
                .ToArray();
            return new AirshiftChart(
                notes,
                Math.Max(audioOffsetSeconds + source.ChartEndTimeSeconds,
                    notes.Length == 0 ? 0d : notes.Max(item => item.EndTime)));
        }

        internal static bool TryParseArea(string? value, out AirshiftSensorArea area)
        {
            area = AirshiftSensorArea.None;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }
            if (string.Equals(value, "C", StringComparison.OrdinalIgnoreCase))
            {
                area = AirshiftSensorArea.C;
                return true;
            }
            if (value.Length == 1 && value[0] is >= '1' and <= '8')
            {
                area = AirshiftSensorArea.A1 + (value[0] - '1');
                return true;
            }
            if (value.Length == 2 && value[1] is >= '1' and <= '8')
            {
                var group = char.ToUpperInvariant(value[0]);
                var index = value[1] - '1';
                area = group switch
                {
                    'A' => AirshiftSensorArea.A1 + index,
                    'B' => AirshiftSensorArea.B1 + index,
                    'D' => AirshiftSensorArea.D1 + index,
                    'E' => AirshiftSensorArea.E1 + index,
                    _ => AirshiftSensorArea.None,
                };
                return area != AirshiftSensorArea.None;
            }
            return false;
        }

        internal static AirshiftSensorArea FromOuterArea(AirshiftArea area)
        {
            return area is >= AirshiftArea.A1 and <= AirshiftArea.A8
                ? AirshiftSensorArea.A1 + ((int)area - (int)AirshiftArea.A1)
                : AirshiftSensorArea.None;
        }

        private static AirshiftChartNote ConvertEvent(RadarEvent source, double offset)
        {
            if (!TryParseArea(source.Position, out var area))
            {
                throw new FormatException(string.Format(
                    CultureInfo.InvariantCulture,
                    "AIRSHIFT cannot map chart event {0} position '{1}'.",
                    source.EventId,
                    source.Position));
            }
            var kind = source.Kind switch
            {
                RadarEventKind.Tap => AirshiftChartNoteKind.Tap,
                RadarEventKind.Hold => AirshiftChartNoteKind.Hold,
                RadarEventKind.Touch => AirshiftChartNoteKind.Touch,
                RadarEventKind.TouchHold => AirshiftChartNoteKind.TouchHold,
                RadarEventKind.Slide => AirshiftChartNoteKind.Slide,
                _ => throw new ArgumentOutOfRangeException(nameof(source.Kind)),
            };
            var checkpoints = kind == AirshiftChartNoteKind.Slide
                ? ConvertSlidePath(source, offset)
                : Array.Empty<AirshiftSlideCheckpoint>();
            return new AirshiftChartNote(
                source.EventId,
                kind,
                source.StartTimeSeconds + offset,
                source.EndTimeSeconds + offset,
                area,
                source.IsSlideHead == true,
                source.IsBreak == true,
                source.IsEx == true,
                source.IsMine == true,
                source.RawToken,
                checkpoints);
        }

        private static AirshiftSlideCheckpoint[] ConvertSlidePath(RadarEvent source, double offset)
        {
            var output = new List<AirshiftSlideCheckpoint>();
            foreach (var segment in source.SlidePath ?? Array.Empty<SlidePathSegment>())
            {
                AddCheckpoint(output, segment.StartPosition, segment.StartTimeSeconds + offset);
                if (segment.ViaPosition is int via)
                {
                    AddCheckpoint(output, via, ((segment.StartTimeSeconds + segment.EndTimeSeconds) * 0.5d) + offset);
                }
                AddCheckpoint(output, segment.EndPosition, segment.EndTimeSeconds + offset);
            }
            if (output.Count == 0 && TryParseArea(source.Position, out var start))
            {
                output.Add(new AirshiftSlideCheckpoint(start, source.StartTimeSeconds + offset));
            }
            return output.ToArray();
        }

        private static void AddCheckpoint(List<AirshiftSlideCheckpoint> output, int position, double time)
        {
            var area = AirshiftSensorArea.A1 + (position - 1);
            if (output.Count == 0 || output[^1].Area != area)
            {
                output.Add(new AirshiftSlideCheckpoint(area, time));
            }
        }
    }
}
