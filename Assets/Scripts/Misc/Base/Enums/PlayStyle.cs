#nullable enable

namespace MajdataPlay
{
    /// <summary>
    /// Identifies the gameplay ruleset selected from the song list.
    /// </summary>
    public enum PlayStyle
    {
        /// <summary>
        /// Uses the original MajdataPlay gameplay, input, judgement, and score pipeline.
        /// </summary>
        Standard = 0,

        /// <summary>
        /// Uses the future dual cheering-stick gameplay pipeline.
        /// </summary>
        Airshift = 1,
    }
}
