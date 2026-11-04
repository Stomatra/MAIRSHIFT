using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace MajdataPlay.Scenes.Airshift
{
    /// <summary>
    /// AIRSHIFT-only L/R prefix layer. All prefixes are removed before invoking
    /// unmodified MajSimai; the sidecar markers are attached to AIRSHIFT notes.
    /// </summary>
    internal static class AirshiftHandedSimaiSyntax
    {
        // Include unprefixed simple TAPs and TOUCHes in the source ordinals:
        // 1 before L1$ must not cause L1$ to target that earlier 1.
        private static readonly Regex NoteToken = new(
            @"(?<![A-Za-z0-9_])(?<hand>[LR]?)(?<token>(?<star>[1-8]\$\$?)|(?<tap>[1-8][bxm]?)|(?<touch>(?:[ABDE][1-8]|C)[fbxm]?))(?=[,/\x60\s]|$)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        // Catch unsupported prefixes such as L1h, R1-5, LA1h, etc.
        private static readonly Regex PossibleHandPrefix = new(
            @"(?<![A-Za-z0-9_])[LR](?=[1-8]|[ABDE][1-8]|C)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        internal readonly struct Marker
        {
            internal string Token { get; }
            internal string Key { get; }
            internal int Occurrence { get; }
            internal AirshiftRequiredHand RequiredHand { get; }
            internal bool IsStar { get; }

            internal Marker(string token, string key, int occurrence, AirshiftRequiredHand requiredHand, bool isStar)
            {
                Token = token;
                Key = key;
                Occurrence = occurrence;
                RequiredHand = requiredHand;
                IsStar = isStar;
            }
        }

        internal sealed class Preprocessed
        {
            internal string SimaiText { get; }
            internal IReadOnlyList<Marker> Markers { get; }
            internal IReadOnlyDictionary<string, int> SourceCounts { get; }

            internal Preprocessed(
                string simaiText,
                IReadOnlyList<Marker> markers,
                IReadOnlyDictionary<string, int> sourceCounts)
            {
                SimaiText = simaiText;
                Markers = markers;
                SourceCounts = sourceCounts;
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
            var sourceCounts = new Dictionary<string, int>(StringComparer.Ordinal);
            var sameBeatCounts = new Dictionary<string, int>(StringComparer.Ordinal);
            var markers = new List<Marker>();
            var output = new StringBuilder(fumen.Length);
            var previous = 0;

            // Count every simple source TAP/TOUCH, including unprefixed notes.
            foreach (Match match in matches)
            {
                var key = GetSourceKey(match);
                sourceCounts.TryGetValue(key, out var index);
                sourceCounts[key] = index + 1;

                var beatKey = fumen.LastIndexOf(',', match.Index) + ":" + key;
                sameBeatCounts.TryGetValue(beatKey, out var beatCount);
                sameBeatCounts[beatKey] = beatCount + 1;
            }

            var ordinals = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (Match match in matches)
            {
                var key = GetSourceKey(match);
                ordinals.TryGetValue(key, out var index);
                ordinals[key] = index + 1;

                var prefix = match.Groups["hand"].Value;
                if (prefix.Length == 0)
                {
                    continue;
                }

                var token = match.Groups["token"].Value;
                var isStar = match.Groups["star"].Success;
                var isTouch = match.Groups["touch"].Success;
                if (!isStar && (!isTouch || token.EndsWith("b", StringComparison.Ordinal) ||
                    token.EndsWith("x", StringComparison.Ordinal) ||
                    token.EndsWith("m", StringComparison.Ordinal)))
                {
                    throw new FormatException(
                        "AIRSHIFT hand prefix only supports standalone stars and ordinary TOUCH: " + prefix + token);
                }

                var beatKey = fumen.LastIndexOf(',', match.Index) + ":" + key;
                if (sameBeatCounts[beatKey] != 1)
                {
                    throw new FormatException(
                        "AIRSHIFT cannot safely match multiple notes at the same area in one beat: " + key);
                }

                validPrefixes.Add(match.Index);
                markers.Add(new Marker(
                    token,
                    key,
                    index,
                    prefix == "L" ? AirshiftRequiredHand.Left : AirshiftRequiredHand.Right,
                    isStar));
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
                        ". First version supports L/R + 1$/1$$ or ordinary TOUCH (A1/B3/C).");
                }
            }

            if (markers.Count == 0)
            {
                return new Preprocessed(fumen, markers, sourceCounts);
            }

            output.Append(fumen, previous, fumen.Length - previous);
            return new Preprocessed(output.ToString(), markers, sourceCounts);
        }

        private static string GetSourceKey(Match match)
        {
            var token = match.Groups["token"].Value;
            if (match.Groups["tap"].Success || match.Groups["star"].Success)
            {
                return "T:A" + token[0];
            }

            var area = token[0] == 'C' ? "C" : token.Substring(0, 2);
            return "C:" + area;
        }

        /// <summary>
        /// Match independent AIRSHIFT notes by semantic family, sensor and
        /// chronological ordinal, NOT by adapter RawToken (which is normalized).
        /// Reject ambiguous counts rather than assigning a hand to wrong notes.
        /// </summary>
        internal static void Apply(AirshiftChart chart, Preprocessed syntax)
        {
            if (chart is null) throw new ArgumentNullException(nameof(chart));
            if (syntax is null) throw new ArgumentNullException(nameof(syntax));
            if (syntax.Markers.Count == 0) return;

            var neededKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var marker in syntax.Markers) neededKeys.Add(marker.Key);

            var buckets = new Dictionary<string, List<AirshiftChartNote>>(StringComparer.Ordinal);
            foreach (var note in chart.Notes)
            {
                var prefix = note.Kind switch
                {
                    AirshiftChartNoteKind.Tap => "T:",
                    AirshiftChartNoteKind.Touch => "C:",
                    _ => null,
                };
                if (prefix is null) continue;
                var key = prefix + note.Area;
                if (!neededKeys.Contains(key)) continue;
                if (!buckets.TryGetValue(key, out var list))
                {
                    list = new List<AirshiftChartNote>();
                    buckets.Add(key, list);
                }
                list.Add(note);
            }

            foreach (var key in neededKeys)
            {
                buckets.TryGetValue(key, out var list);
                var parsedCount = list?.Count ?? 0;
                var sourceCount = syntax.SourceCounts[key];
                if (parsedCount != sourceCount)
                {
                    throw new FormatException(
                        "AIRSHIFT note mapping is ambiguous for " + key +
                        ": source has " + sourceCount + " simple notes but the adapter produced " +
                        parsedCount + " candidate chart events. Avoid mixed slide heads or unsupported " +
                        "modifiers at this position.");
                }
            }

            foreach (var marker in syntax.Markers)
            {
                var note = buckets[marker.Key][marker.Occurrence];
                if ((marker.IsStar && note.Kind != AirshiftChartNoteKind.Tap) ||
                    (!marker.IsStar && note.Kind != AirshiftChartNoteKind.Touch))
                {
                    throw new FormatException("AIRSHIFT marker matched an unexpected note kind: " + marker.Token);
                }

                note.RequiredHand = marker.RequiredHand;
                if (marker.IsStar) note.IsSlideHead = true;
            }
        }
    }
}
