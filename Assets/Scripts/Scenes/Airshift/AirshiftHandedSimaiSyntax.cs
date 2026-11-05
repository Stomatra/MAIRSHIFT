using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using MajSimai;

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
            @"(?<![A-Za-z0-9_])(?<hand>[LR]?)(?<token>(?<slide>[1-8](?=[\-^<>pqszvVw])[^,/\x60\s]+)|(?<star>[1-8]\$\$?)|(?<tap>[1-8][bxm]?)|(?<touch>(?:[ABDE][1-8]|C)[fbxm]?))(?=[,/\x60\s]|$)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        // A pair of complete AIRSHIFT-hand-tagged paths sharing the
        // start key is an L/R dual-hand fork. Unmarked '*' stays intact.
        private static readonly Regex HandedParallelPair = new(
            @"(?<![A-Za-z0-9_])(?<first>[LR](?<start>[1-8])[-^<>pqszvV][^,/*\x60\s]+)\*(?<second>[LR](?<endstart>[1-8])[-^<>pqszvV][^,/*\x60\s]+)(?=[,/\x60\s]|$)",
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
            internal bool IsSlide { get; }

            internal Marker(string token, string key, int occurrence, AirshiftRequiredHand requiredHand, bool isStar, bool isSlide)
            {
                Token = token;
                Key = key;
                Occurrence = occurrence;
                RequiredHand = requiredHand;
                IsStar = isStar;
                IsSlide = isSlide;
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

            // One character only is replaced, preserving every source
            // index used by the existing per-key ordinal binding.
            var normalized = fumen.ToCharArray();
            var parallelKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (Match pair in HandedParallelPair.Matches(fumen))
            {
                var first = pair.Groups["first"].Value;
                var second = pair.Groups["second"].Value;
                var start = pair.Groups["start"].Value;
                if (first[0] == second[0] ||
                    start != pair.Groups["endstart"].Value ||
                    first.Substring(1) == second.Substring(1) ||
                    first.IndexOf('w') >= 0 || second.IndexOf('w') >= 0 ||
                    first.IndexOf('[') < 0 || second.IndexOf('[') < 0 ||
                    !first.EndsWith("]", StringComparison.Ordinal) ||
                    !second.EndsWith("]", StringComparison.Ordinal))
                {
                    throw new FormatException(
                        "AIRSHIFT two-hand parallel SLIDE requires distinct full " +
                        "paths from the same key, one L and one R: " + pair.Value);
                }
                var beat = fumen.LastIndexOf(',', pair.Index) + ":S:A" + start;
                if (!parallelKeys.Add(beat))
                {
                    throw new FormatException("Only one parallel SLIDE pair per key and beat is supported.");
                }
                normalized[pair.Groups["first"].Index + pair.Groups["first"].Length] = '/';
            }
            var normalizedFumen = new string(normalized);
            var matches = NoteToken.Matches(normalizedFumen);
            var validPrefixes = new HashSet<int>();
            var sourceCounts = new Dictionary<string, int>(StringComparer.Ordinal);
            var sameBeatNotes = new Dictionary<string, List<Match>>(StringComparer.Ordinal);
            var markers = new List<Marker>();
            var output = new StringBuilder(normalizedFumen.Length);
            var previous = 0;

            // Count every simple source TAP/TOUCH, including unprefixed notes.
            foreach (Match match in matches)
            {
                var key = GetSourceKey(match);
                sourceCounts.TryGetValue(key, out var index);
                sourceCounts[key] = index + 1;

                var beatKey = normalizedFumen.LastIndexOf(',', match.Index) + ":" + key;
                if (!sameBeatNotes.TryGetValue(beatKey, out var beatNotes))
                {
                    beatNotes = new List<Match>(2);
                    sameBeatNotes.Add(beatKey, beatNotes);
                }
                beatNotes.Add(match);
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
                var isSlide = match.Groups["slide"].Success;
                // A tagged WIFI inherently requires two real hands and a
                // virtual midpoint. Its per-hand policy is separate from
                // the single-cursor curve/chain feature.
                if (isSlide && (token.IndexOf('w') >= 0 ||
                                token.IndexOf('*') >= 0 ||
                                token.IndexOf('[') < 0 ||
                                !token.EndsWith("]", StringComparison.Ordinal)))
                {
                    throw new FormatException(
                        "AIRSHIFT hand-prefixed WIFI / parallel / malformed SLIDE " +
                        "is not supported by the curved/chain hand policy: " + prefix + token);
                }
                if (!isSlide && !isStar && (!isTouch || token.EndsWith("b", StringComparison.Ordinal) ||
                    token.EndsWith("x", StringComparison.Ordinal) ||
                    token.EndsWith("m", StringComparison.Ordinal)))
                {
                    throw new FormatException(
                        "AIRSHIFT hand prefix supports standalone STAR, plain TOUCH or curved, turning or chained SLIDE: " + prefix + token);
                }

                var beatKey = normalizedFumen.LastIndexOf(',', match.Index) + ":" + key;
                var beatNotes = sameBeatNotes[beatKey];
                // A strictly identical pair is the only same-beat/same-area
                // duplicate that can be bound without ambiguous source identity.
                if (beatNotes.Count > 1 &&
                    !IsOppositeHandStarPair(beatNotes) &&
                    !IsOppositeHandTouchPair(beatNotes) &&
                    !(parallelKeys.Contains(beatKey) &&
                      IsOppositeHandParallelSlidePair(beatNotes)))
                {
                    throw new FormatException(
                        "AIRSHIFT same-beat overlapping notes require exactly one L and one R " +
                        "of an explicit STAR, TOUCH or two-hand parallel SLIDE pair: " + key);
                }

                validPrefixes.Add(match.Index);
                markers.Add(new Marker(
                    token,
                    key,
                    index,
                    prefix == "L" ? AirshiftRequiredHand.Left : AirshiftRequiredHand.Right,
                    isStar,
                    isSlide));
                output.Append(normalizedFumen, previous, match.Index - previous);
                output.Append(token);
                previous = match.Index + match.Length;
            }

            foreach (Match possible in PossibleHandPrefix.Matches(normalizedFumen))
            {
                if (!validPrefixes.Contains(possible.Index))
                {
                    throw new FormatException(
                        "Unsupported AIRSHIFT hand prefix at chart character " + possible.Index +
                        ". Supported: L/R + 1$/1$, ordinary TOUCH and non-WIFI SLIDE with duration; use L1-4[8:1]*R1-6[8:1] for a two-hand parallel pair.");
                }
            }

            if (markers.Count == 0)
            {
                return new Preprocessed(normalizedFumen, markers, sourceCounts);
            }

            output.Append(normalizedFumen, previous, normalizedFumen.Length - previous);
            return new Preprocessed(output.ToString(), markers, sourceCounts);
        }

        /// <summary>
        /// Only two identical STAR tokens with opposite hand prefixes may share
        /// an exact key and beat. The parser can then assign either identical
        /// STAR identity to the L/R side without altering musical semantics.
        /// </summary>
        /// <param name="notes">All simple notes at one source key within one beat.</param>
        /// <returns>True only for an unambiguous left/right standalone STAR pair.</returns>
        private static bool IsOppositeHandStarPair(IReadOnlyList<Match> notes)
        {
            if (notes.Count != 2 ||
                !notes[0].Groups["star"].Success ||
                !notes[1].Groups["star"].Success ||
                notes[0].Groups["token"].Value != notes[1].Groups["token"].Value)
            {
                return false;
            }

            var first = notes[0].Groups["hand"].Value;
            var second = notes[1].Groups["hand"].Value;
            return first == "L" && second == "R" ||
                   first == "R" && second == "L";
        }

        /// <summary>
        /// Accept only plain, unmodified TOUCH tokens with one left and one
        /// right prefix. Firework and special variants are deliberately excluded.
        /// </summary>
        private static bool IsOppositeHandTouchPair(IReadOnlyList<Match> notes)
        {
            if (notes.Count != 2 ||
                !notes[0].Groups["touch"].Success ||
                !notes[1].Groups["touch"].Success)
            {
                return false;
            }

            var firstToken = notes[0].Groups["token"].Value;
            var secondToken = notes[1].Groups["token"].Value;
            if (firstToken != secondToken ||
                firstToken.EndsWith("f", StringComparison.Ordinal) ||
                firstToken.EndsWith("b", StringComparison.Ordinal) ||
                firstToken.EndsWith("x", StringComparison.Ordinal) ||
                firstToken.EndsWith("m", StringComparison.Ordinal))
            {
                return false;
            }

            var firstHand = notes[0].Groups["hand"].Value;
            var secondHand = notes[1].Groups["hand"].Value;
            return firstHand == "L" && secondHand == "R" ||
                   firstHand == "R" && secondHand == "L";
        }

        /// <summary>Only exactly two different complete opposite-hand branches.</summary>
        private static bool IsOppositeHandParallelSlidePair(IReadOnlyList<Match> notes)
        {
            if (notes.Count != 2 ||
                !notes[0].Groups["slide"].Success ||
                !notes[1].Groups["slide"].Success)
            {
                return false;
            }
            var first = notes[0].Groups["hand"].Value;
            var second = notes[1].Groups["hand"].Value;
            return (first == "L" && second == "R" ||
                    first == "R" && second == "L") &&
                   notes[0].Groups["token"].Value != notes[1].Groups["token"].Value;
        }

        private static string GetSourceKey(Match match)
        {
            var token = match.Groups["token"].Value;
            if (match.Groups["slide"].Success) return "S:A" + token[0];
            if (match.Groups["tap"].Success || match.Groups["star"].Success)
            {
                return "T:A" + token[0];
            }

            var area = token[0] == 'C' ? "C" : token.Substring(0, 2);
            return "C:" + area;
        }

        /// <summary>
        /// Resolves hand-prefixed notes against the Simai objects used by the
        /// existing Game scene. The original non-slide objects survive NoteFolding,
        /// so the NoteLoader can transfer these bindings to its pooled notes.
        /// No additional gameplay scene or duplicate judgment engine is needed.
        /// </summary>
        internal static IReadOnlyDictionary<SimaiNote, AirshiftRequiredHand> BindGameNotes(
            SimaiChart chart,
            Preprocessed syntax)
        {
            if (chart is null) throw new ArgumentNullException(nameof(chart));
            if (syntax is null) throw new ArgumentNullException(nameof(syntax));
            var bindings = new Dictionary<SimaiNote, AirshiftRequiredHand>();
            if (syntax.Markers.Count == 0) return bindings;

            var neededKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var marker in syntax.Markers)
            {
                neededKeys.Add(marker.Key);
            }

            var buckets = new Dictionary<string, List<SimaiNote>>(StringComparer.Ordinal);
            foreach (var timing in chart.NoteTimings)
            {
                foreach (var note in timing.Notes)
                {
                    var key = GetGameKey(note);
                    if (key is null || !neededKeys.Contains(key)) continue;

                    if (!buckets.TryGetValue(key, out var notes))
                    {
                        notes = new List<SimaiNote>();
                        buckets.Add(key, notes);
                    }
                    notes.Add(note);
                }
            }

            // Validate before mutating any binding: mixed slide heads or other
            // unusual parser output must not silently retag unrelated notes.
            foreach (var key in neededKeys)
            {
                buckets.TryGetValue(key, out var notes);
                var parsedCount = notes?.Count ?? 0;
                var sourceCount = syntax.SourceCounts[key];
                if (parsedCount != sourceCount)
                {
                    throw new FormatException(
                        "AIRSHIFT Game chart mapping is ambiguous for " + key +
                        ": " + sourceCount + " simple source notes versus " +
                        parsedCount + " parsed TAP/TOUCH objects.");
                }
            }

            foreach (var marker in syntax.Markers)
            {
                var note = buckets[marker.Key][marker.Occurrence];
                if (marker.IsStar && (note.Type != SimaiNoteType.Tap || !note.IsForceStar) ||
                    marker.IsSlide && (note.Type != SimaiNoteType.Slide || note.RawContent != marker.Token) ||
                    !marker.IsStar && !marker.IsSlide && note.Type != SimaiNoteType.Touch)
                {
                    throw new FormatException(
                        "AIRSHIFT hand tag does not match the parsed STAR/TOUCH/SLIDE content: " +
                        marker.Token);
                }
                bindings.Add(note, marker.RequiredHand);
            }
            return bindings;
        }

        private static string? GetGameKey(SimaiNote note)
        {
            if (note.Type == SimaiNoteType.Slide) return "S:A" + note.StartPosition;
            if (note.Type == SimaiNoteType.Tap)
            {
                return "T:A" + note.StartPosition;
            }
            if (note.Type != SimaiNoteType.Touch)
            {
                return null;
            }
            var area = char.ToUpperInvariant(note.TouchArea);
            return area == 'C'
                ? "C:C"
                : "C:" + area + note.StartPosition;
        }

    }
}
