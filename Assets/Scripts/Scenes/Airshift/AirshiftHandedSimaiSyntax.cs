using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace MajdataPlay.Scenes.Airshift
{
    /// <summary>
    /// AIRSHIFT-only prefix layer over the unmodified MajSimai grammar.
    /// Phase one supports L/R on standalone star TAPs (1$, 1$$) and TOUCH (A1, B3, C).
    /// It removes the prefix before passing text to MajSimai, then maps the
    /// resulting stable note tokens back to independently judged AIRSHIFT notes.
    /// </summary>
    internal static class AirshiftHandedSimaiSyntax
    {
        // The boundary is intentionally restrictive; unsupported hand-marked
        // slides, holds, or mixed modifiers fail instead of silently losing color.
        private static readonly Regex NoteToken = new(
            @"(?<![A-Za-z0-9_])(?<hand>[LR]?)(?<token>(?:[1-8]\$\$?|(?:[ABDE][1-8]|C)f?))(?=[,/\x60\s]|$)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex PossibleHandPrefix = new(
            @"(?<![A-Za-z0-9_])[LR](?=[1-8]|[ABDE][1-8]|C)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        internal readonly struct Marker
        {
            internal string Token { get; }
            internal int Occurrence { get; }
            internal AirshiftRequiredHand RequiredHand { get; }
            internal bool IsStar { get; }

            internal Marker(string token, int occurrence, AirshiftRequiredHand requiredHand, bool isStar)
            {
                Token = token;
                Occurrence = occurrence;
                RequiredHand = requiredHand;
                IsStar = isStar;
            }
        }

        internal sealed class Preprocessed
        {
            internal string SimaiText { get; }
            internal IReadOnlyList<Marker> Markers { get; }

            internal Preprocessed(string simaiText, IReadOnlyList<Marker> markers)
            {
                SimaiText = simaiText;
                Markers = markers;
            }
        }

        internal static Preprocessed Preprocess(string fumen)
        {
            if (fumen is null)
            {
                throw new ArgumentNullException(nameof(fumen));
            }

            var matches = NoteToken.Matches(fumen);
            var validPrefixes = new HashSet<int>();
            var counts = new Dictionary<string, int>(StringComparer.Ordinal);
            var sameBeatCounts = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (Match candidate in matches)
            {
                // All source candidates are counted, even ones without L/R.
                // Same-raw-token doubles in the same beat need event-ID based
                // matching; reject them in v1 rather than swapping hands.
                var beat = fumen.LastIndexOf(',', candidate.Index);
                var beatKey = beat + ":" + candidate.Groups["token"].Value;
                sameBeatCounts.TryGetValue(beatKey, out var count);
                sameBeatCounts[beatKey] = count + 1;
            }

            var markers = new List<Marker>();
            var output = new StringBuilder(fumen.Length);
            var previous = 0;

            foreach (Match match in matches)
            {
                var token = match.Groups["token"].Value;
                var prefix = match.Groups["hand"].Value;
                counts.TryGetValue(token, out var index);
                counts[token] = index + 1;

                if (prefix.Length == 0)
                {
                    continue;
                }

                var beatKey = fumen.LastIndexOf(',', match.Index) + ":" + token;
                if (sameBeatCounts[beatKey] != 1)
                {
                    throw new FormatException(
                        "AIRSHIFT cannot distinguish hand-marked same-token notes in one beat: " +
                        token + ". Split them across beats or use distinct positions.");
                }

                validPrefixes.Add(match.Index);
                var hand = prefix == "L" ? AirshiftRequiredHand.Left : AirshiftRequiredHand.Right;
                markers.Add(new Marker(token, index, hand, token[0] is >= '1' and <= '8'));
                output.Append(fumen, previous, match.Index - previous);
                output.Append(token);
                previous = match.Index + match.Length;
            }

            foreach (Match possible in PossibleHandPrefix.Matches(fumen))
            {
                if (!validPrefixes.Contains(possible.Index))
                {
                    throw new FormatException(
                        "Unsupported AIRSHIFT hand prefix at chart character " + possible.Index +
                        ". First version supports only L/R + standalone star (1$) or TOUCH (A1/B3/C).");
                }
            }

            if (markers.Count == 0)
            {
                return new Preprocessed(fumen, markers);
            }

            output.Append(fumen, previous, fumen.Length - previous);
            return new Preprocessed(output.ToString(), markers);
        }

        /// <summary>
        /// Applies tags using normalized raw token plus occurrence order.
        /// Fails closed if MajSimai/MajRadar normalized a token unexpectedly.
        /// </summary>
        internal static void Apply(AirshiftChart chart, Preprocessed syntax)
        {
            var buckets = new Dictionary<string, List<AirshiftChartNote>>(StringComparer.Ordinal);
            foreach (var note in chart.Notes)
            {
                var eligible = note.Kind == AirshiftChartNoteKind.Touch ||
                    (note.Kind == AirshiftChartNoteKind.Tap &&
                     note.RawToken.Contains("$", StringComparison.Ordinal));
                if (!eligible) continue;

                if (!buckets.TryGetValue(note.RawToken, out var list))
                {
                    list = new List<AirshiftChartNote>();
                    buckets.Add(note.RawToken, list);
                }
                list.Add(note);
            }

            foreach (var marker in syntax.Markers)
            {
                if (!buckets.TryGetValue(marker.Token, out var matching) ||
                    marker.Occurrence >= matching.Count)
                {
                    throw new FormatException(
                        "AIRSHIFT prefix could not be matched to parsed note '" +
                        marker.Token + "' occurrence #" + (marker.Occurrence + 1) + ".");
                }

                var note = matching[marker.Occurrence];
                if ((marker.IsStar && note.Kind != AirshiftChartNoteKind.Tap) ||
                    (!marker.IsStar && note.Kind != AirshiftChartNoteKind.Touch))
                {
                    throw new FormatException("AIRSHIFT prefix matched an unexpected note type: " + marker.Token);
                }

                note.RequiredHand = marker.RequiredHand;
                if (marker.IsStar) note.IsSlideHead = true;
            }
        }
    }
}
