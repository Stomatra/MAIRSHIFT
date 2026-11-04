#nullable enable
namespace MajdataPlay.Scenes.Setting
{
    /// <summary>
    /// Remembers the selector that opened the shared Setting scene without carrying gameplay data.
    /// </summary>
    /// <remarks>
    /// The value intentionally survives Setting -&gt; Calibrator -&gt; Setting. It is consumed on the
    /// normal Setting exit path, while all existing callers continue to fall back to STANDARD List.
    /// </remarks>
    internal static class SettingReturnContext
    {
        private static MajScenes? s_returnScene;

        /// <summary>Starts one shared-setting visit from a supported selector.</summary>
        /// <param name="sourceScene">The selector that is opening Setting.</param>
        internal static void Begin(MajScenes sourceScene)
        {
            s_returnScene = sourceScene == MajScenes.AirshiftList
                ? MajScenes.AirshiftList
                : null;
        }

        /// <summary>Resolves and consumes the current return route.</summary>
        /// <param name="fallback">The legacy scene used when no AIRSHIFT source is active.</param>
        /// <returns>The requested AIRSHIFT selector or the legacy fallback.</returns>
        internal static MajScenes ConsumeOrDefault(MajScenes fallback)
        {
            var result = s_returnScene ?? fallback;
            s_returnScene = null;
            return result;
        }

        /// <summary>Clears a stale AIRSHIFT source before a non-AIRSHIFT caller opens Setting.</summary>
        internal static void Clear()
        {
            s_returnScene = null;
        }
    }
}
