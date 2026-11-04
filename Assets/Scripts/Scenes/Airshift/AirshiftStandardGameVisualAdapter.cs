using Cysharp.Threading.Tasks;
using MajdataPlay.Scenes.Game;
using MajdataPlay.Settings;
using System;
using System.Linq;
using System.Threading;
using TMPro;
using UnityEngine;

#nullable enable
namespace MajdataPlay.Scenes.Airshift
{
    /// <summary>
    /// Drives copied STANDARD gameplay visuals with AIRSHIFT-only session and score data.
    /// </summary>
    /// <remarks>
    /// This adapter does not create or call STANDARD note, judgement, input, object-counter, or score services.
    /// </remarks>
    internal sealed class AirshiftStandardGameVisualAdapter : MonoBehaviour
    {
        private const string BackgroundName = "StandardGameBackground";
        private const string ChartInfoName = "StandardGameChartInfo";
        private const string CenterInfoName = "StandardGameCenterInfo";
        private const string SecondaryInfoName = "StandardGameSecondaryInfo";

        private BGManager? _background;
        private ChartInfoDisplayer? _chartInfo;
        private TMP_Text? _centerInfo;
        private TMP_Text? _centerHeader;
        private TMP_Text? _secondaryInfo;
        private TMP_Text? _secondaryHeader;

        /// <summary>Gets whether the exact STANDARD background and HUD hierarchy is available.</summary>
        internal bool IsAvailable => _background != null && _chartInfo != null;

        private void Awake()
        {
            ResolveVisuals();
        }

        /// <summary>Loads the selected cover into the copied STANDARD background and metadata panel.</summary>
        /// <param name="session">The independent AIRSHIFT session supplying display data.</param>
        /// <param name="cancellationToken">Cancels cover loading when the AIRSHIFT scene leaves.</param>
        internal async UniTask InitializeAsync(AirshiftSession session, CancellationToken cancellationToken)
        {
            ResolveVisuals();
            if (_background != null)
            {
                _background.SetBrightness(1f - MajEnv.Settings.Game.BackgroundDim);
            }
            if (session.Song is null || _chartInfo == null)
            {
                return;
            }

            _chartInfo.SetMetadata(session.Song, session.Level);
            var cover = await session.Song.GetCoverAsync(false, token: cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            _chartInfo.SetCover(cover);
            _background?.SetBackgroundPic(cover);
        }

        /// <summary>Updates the copied text-only STANDARD HUD from AIRSHIFT live score state.</summary>
        /// <param name="player">The AIRSHIFT chart player that owns the isolated score counter.</param>
        internal void UpdateScore(AirshiftChartPlayer player)
        {
            var score = player.CurrentScore;
            if (score == null)
            {
                return;
            }
            UpdateInfo(
                _centerInfo,
                _centerHeader,
                MajEnv.Settings.Game.BGInfo,
                score,
                player.CurrentCombo);
            UpdateInfo(
                _secondaryInfo,
                _secondaryHeader,
                MajEnv.Settings.Game.SecondaryBGInfo,
                score,
                player.CurrentCombo);
        }

        /// <summary>Resolves only the explicitly copied, data-independent STANDARD visual objects.</summary>
        private void ResolveVisuals()
        {
            _background ??= FindNamedComponent<BGManager>(BackgroundName);
            _chartInfo ??= FindNamedComponent<ChartInfoDisplayer>(ChartInfoName);
            ResolveInfoDisplayer(CenterInfoName, ref _centerInfo, ref _centerHeader);
            ResolveInfoDisplayer(SecondaryInfoName, ref _secondaryInfo, ref _secondaryHeader);
        }

        /// <summary>Finds a component below one uniquely named copied visual root.</summary>
        private static T? FindNamedComponent<T>(string objectName) where T : Component
        {
            return FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(component =>
                    component.gameObject.name == objectName ||
                    component.transform.root.name == objectName);
        }

        /// <summary>Finds the numeric and header text retained from a STANDARD information displayer.</summary>
        private static void ResolveInfoDisplayer(
            string objectName,
            ref TMP_Text? value,
            ref TMP_Text? header)
        {
            if (value != null)
            {
                return;
            }
            var root = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(transform => transform.name == objectName);
            if (root == null)
            {
                return;
            }
            value = root.GetComponent<TMP_Text>();
            var valueText = value;
            header = root.GetComponentsInChildren<TMP_Text>(true)
                .FirstOrDefault(text => text != valueText);
        }

        /// <summary>Maps compatible STANDARD information options to AIRSHIFT-owned values.</summary>
        private static void UpdateInfo(
            TMP_Text? value,
            TMP_Text? header,
            BGInfoOption option,
            AirshiftScoreSnapshot score,
            int combo)
        {
            if (value == null)
            {
                return;
            }

            var visible = true;
            switch (option)
            {
                case BGInfoOption.CPCombo:
                    header?.SetText("CPCombo");
                    value.SetText(combo.ToString());
                    visible = combo > 0 && score.Great == 0 && score.Good == 0 && score.Miss == 0;
                    break;
                case BGInfoOption.PCombo:
                    header?.SetText("PCombo");
                    value.SetText(combo.ToString());
                    visible = combo > 0 && score.Good == 0 && score.Miss == 0;
                    break;
                case BGInfoOption.Combo:
                    header?.SetText("Combo");
                    value.SetText(combo.ToString());
                    visible = combo > 0;
                    break;
                case BGInfoOption.Achievement_101:
                case BGInfoOption.Achievement_100:
                case BGInfoOption.Achievement:
                case BGInfoOption.AchievementClassical:
                case BGInfoOption.AchievementClassical_100:
                    header?.SetText("Achievement");
                    value.SetText($"{Math.Floor(score.Achievement * 10000d) / 10000d:F4}%");
                    break;
                case BGInfoOption.DXScore:
                    header?.SetText("AIRSHIFT SCORE");
                    value.SetText(score.Score.ToString());
                    break;
                case BGInfoOption.None:
                    visible = false;
                    break;
                default:
                    visible = false;
                    break;
            }
            value.gameObject.SetActive(visible);
            if (header != null)
            {
                header.gameObject.SetActive(visible);
            }
        }
    }
}
