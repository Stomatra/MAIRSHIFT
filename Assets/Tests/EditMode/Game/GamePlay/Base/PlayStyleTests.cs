using System;
using MajdataPlay.Collections;
using MajdataPlay.Scenes.Game;
using MajdataPlay.Scenes.Game.Notes;
using MajdataPlay.Settings;
using MajdataPlay.Tests.Game.GamePlay.PlayMode.Support;
using NUnit.Framework;

#nullable enable
namespace MajdataPlay.Tests.Game.GamePlay.Base
{
    /// <summary>
    /// Checks the session-only gameplay style selection and its standard-game launch boundary.
    /// </summary>
    public class PlayStyleTests
    {
        /// <summary>Checks the stable values reserved for persisted or interop-facing style identifiers.</summary>
        [Test]
        public void PlayStyle_UsesStableValues()
        {
            Assert.That((int)PlayStyle.Standard, Is.Zero);
            Assert.That((int)PlayStyle.Airshift, Is.EqualTo(1));
        }

        /// <summary>
        /// AIRSHIFT should be disabled by default.
        /// </summary>
        [Test]
        public void AirshiftMod_DefaultsToOff()
        {
            var mod = new ModOptions();

            Assert.That(mod.AirshiftEnabled, Is.False);
        }

        /// <summary>
        /// AIRSHIFT can be enabled and disabled through ModOptions.
        /// </summary>
        [Test]
        public void AirshiftMod_CanBeSwitched()
        {
            var mod = new ModOptions();

            mod.AirshiftEnabled = true;
            Assert.That(mod.AirshiftEnabled, Is.True);

            mod.AirshiftEnabled = false;
            Assert.That(mod.AirshiftEnabled, Is.False);
        }

        /// <summary>
        /// AIRSHIFT state should be copied into GameModInfo.
        /// </summary>
        [Test]
        public void AirshiftMod_IsPassedToGameRuntime()
        {
            var options = new ModOptions
            {
                AirshiftEnabled = true
            };

            var runtime = new GameModInfo(options);

            Assert.That(runtime.AirshiftEnabled, Is.True);

            var copy = new GameModInfo(runtime);

            Assert.That(copy.AirshiftEnabled, Is.True);
        }

        /// <summary>Checks that standard score creation remains independent from the new list-session selection.</summary>
        [Test]
        public void StandardScoreCreation_RemainsUnchanged()
        {
            var song = new GamePlayFakeSongDetail();
            var result = new GameResult
            {
                Acc = new Accurate { DX = 100.5, Classic = 99.5 },
                SongDetail = song,
                Level = ChartLevel.Master,
                JudgeRecord = JudgeDetail.Empty,
                DXScore = 900,
                TotalDXScore = 1000,
                Fast = 2,
                Late = 3,
                ComboState = ComboState.FC,
            };

            var score = MaiScore.CreateFromResult(in result, ChartLevel.Master);

            Assert.That(score.Hash, Is.EqualTo(song.Hash));
            Assert.That(score.ChartLevel, Is.EqualTo(ChartLevel.Master));
            Assert.That(score.Acc == result.Acc, Is.True);
            Assert.That(score.DXScore, Is.EqualTo(900));
            Assert.That(score.TotalDXScore, Is.EqualTo(1000));
            Assert.That(score.PlayCount, Is.Zero);
            Assert.That(score.ComboState, Is.EqualTo(ComboState.FC));
        }

        /// <summary>Checks that AIRSHIFT remains orthogonal to the existing normal, dan, and practice session modes.</summary>
        [Test]
        public void ExistingGameModeContract_IsUnchanged()
        {
            Assert.That(Enum.GetValues(typeof(GameMode)), Is.EqualTo(new[]
            {
                GameMode.Normal,
                GameMode.Dan,
                GameMode.Practice,
            }));
        }

        [Test]
        public void AirshiftMod_IsConsideredActiveForScoreGuard()
        {
            var mod = new ModOptions();

            Assert.That(mod.IsAnyModActive(), Is.False);

            mod.AirshiftEnabled = true;

            Assert.That(mod.IsAnyModActive(), Is.True);
        }
    }
}
