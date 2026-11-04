#nullable enable

namespace MajdataPlay.Scenes.Airshift
{
    /// <summary>
    /// Identifies how an AIRSHIFT session was created.
    /// </summary>
    internal enum AirshiftSessionKind
    {
        /// <summary>
        /// A local lifecycle and input test with no chart or score data.
        /// </summary>
        LocalTest = 0,
        /// <summary>
        /// A playable session created from one local Simai chart.
        /// </summary>
        ChartPlay = 1,
    }

    /// <summary>
    /// Holds the isolated state shared by the AIRSHIFT gameplay and result scenes.
    /// </summary>
    internal sealed class AirshiftSession
    {
        /// <summary>
        /// Gets the source of this AIRSHIFT session.
        /// </summary>
        internal AirshiftSessionKind Kind { get; }
        /// <summary>Gets the selected local song for a playable session.</summary>
        internal ISongDetail? Song { get; }
        /// <summary>Gets the selected chart difficulty.</summary>
        internal ChartLevel Level { get; }
        /// <summary>Gets the completed AIRSHIFT score, if this chart reached its result.</summary>
        internal AirshiftScoreSnapshot? Score { get; private set; }
        /// <summary>Gets the persisted AIRSHIFT best record, if saving succeeded.</summary>
        internal AirshiftStoredRecord? StoredRecord { get; private set; }
        /// <summary>Gets a non-fatal loading or persistence message for the result screen.</summary>
        internal string? ResultMessage { get; private set; }
        /// <summary>Gets whether debug input participated in this play.</summary>
        internal bool UsedDebugInput { get; private set; }

        /// <summary>
        /// Gets whether the local test reached its explicit completion input.
        /// </summary>
        internal bool IsCompleted { get; private set; }

        /// <summary>
        /// Creates an isolated AIRSHIFT session.
        /// </summary>
        /// <param name="kind">The source of the session.</param>
        /// <param name="song">The selected local song, if this is a playable session.</param>
        /// <param name="level">The selected chart difficulty.</param>
        internal AirshiftSession(
            AirshiftSessionKind kind,
            ISongDetail? song = null,
            ChartLevel level = ChartLevel.Easy)
        {
            Kind = kind;
            Song = song;
            Level = level;
        }

        /// <summary>
        /// Marks the local test as completed without creating a STANDARD gameplay result.
        /// </summary>
        internal void Complete()
        {
            IsCompleted = true;
        }

        /// <summary>Attaches an independent chart result without creating STANDARD result data.</summary>
        internal void CompleteChart(
            AirshiftScoreSnapshot score,
            AirshiftStoredRecord? storedRecord,
            bool usedDebugInput,
            string? resultMessage)
        {
            Score = score;
            StoredRecord = storedRecord;
            UsedDebugInput = usedDebugInput;
            ResultMessage = resultMessage;
            IsCompleted = true;
        }
    }

    /// <summary>
    /// Owns the AIRSHIFT-only session while the independent scene chain is active.
    /// </summary>
    internal static class AirshiftSessionStore
    {
        /// <summary>
        /// Gets the current AIRSHIFT session, or <see langword="null"/> outside the AIRSHIFT scene chain.
        /// </summary>
        internal static AirshiftSession? Current { get; private set; }

        /// <summary>
        /// Starts a local AIRSHIFT input test that deliberately contains no song or score data.
        /// </summary>
        /// <returns>The newly created local test session.</returns>
        internal static AirshiftSession StartLocalTest()
        {
            var session = new AirshiftSession(AirshiftSessionKind.LocalTest);
            Current = session;
            return session;
        }

        /// <summary>
        /// Starts a playable AIRSHIFT session backed by one local Simai chart.
        /// </summary>
        /// <param name="song">The selected local song.</param>
        /// <param name="level">The selected chart difficulty.</param>
        /// <returns>The newly created playable session.</returns>
        /// <exception cref="System.ArgumentNullException">The selected song is null.</exception>
        /// <exception cref="System.ArgumentException">The selected song is online.</exception>
        internal static AirshiftSession StartChart(ISongDetail song, ChartLevel level)
        {
            if (song is null)
            {
                throw new System.ArgumentNullException(nameof(song));
            }
            if (song.IsOnline)
            {
                throw new System.ArgumentException("AIRSHIFT currently supports local charts only.", nameof(song));
            }
            var session = new AirshiftSession(AirshiftSessionKind.ChartPlay, song, level);
            Current = session;
            return session;
        }

        /// <summary>
        /// Removes the current AIRSHIFT session after leaving its scene chain.
        /// </summary>
        internal static void Clear()
        {
            Current = null;
        }
    }
}
