using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

#nullable enable
namespace MajdataPlay.Scenes.List
{
    /// <summary>
    /// Displays the session gameplay style and the availability of AIRSHIFT on the song list.
    /// </summary>
    internal sealed class PlayStyleDisplayer : MonoBehaviour
    {
        /// <summary>The color used by the standard gameplay label.</summary>
        private static readonly Color32 StandardColor = new(79, 87, 99, 255);

        /// <summary>The color used by the AIRSHIFT gameplay label.</summary>
        private static readonly Color32 AirshiftColor = new(255, 130, 130, 255);

        /// <summary>The text component that presents the selected gameplay style.</summary>
        [SerializeField]
        [FormerlySerializedAs("StatusText")]
        private TMP_Text _statusText = null!;

        /// <summary>
        /// Updates the label to show the selected gameplay style.
        /// </summary>
        /// <param name="playStyle">The selected gameplay style.</param>
        /// <exception cref="ArgumentOutOfRangeException">The gameplay style is unknown.</exception>
        internal void SetPlayStyle(PlayStyle playStyle)
        {
            switch (playStyle)
            {
                case PlayStyle.Standard:
                    _statusText.color = StandardColor;
                    _statusText.text = "P2  PLAY STYLE\nSTANDARD";
                    break;
                case PlayStyle.Airshift:
                    _statusText.color = AirshiftColor;
                    _statusText.text = "P2  AIRSHIFT LOCAL TEST\nA4: TEST  |  DAN / PRACTICE: STANDARD";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(playStyle), playStyle, null);
            }
        }

    }
}
