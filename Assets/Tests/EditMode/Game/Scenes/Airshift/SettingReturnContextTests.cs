using MajdataPlay.Scenes.Setting;
using NUnit.Framework;

namespace MajdataPlay.Tests.Game.Scenes.Airshift
{
    /// <summary>Verifies the source-aware route used by the shared STANDARD Setting scene.</summary>
    public class SettingReturnContextTests
    {
        [SetUp]
        public void SetUp() => SettingReturnContext.Clear();

        [TearDown]
        public void TearDown() => SettingReturnContext.Clear();

        [Test]
        public void NoSource_PreservesLegacyListFallback()
        {
            Assert.That(SettingReturnContext.ConsumeOrDefault(MajScenes.List), Is.EqualTo(MajScenes.List));
        }

        [Test]
        public void AirshiftSource_ReturnsToAirshiftListOnce()
        {
            SettingReturnContext.Begin(MajScenes.AirshiftList);

            Assert.That(SettingReturnContext.ConsumeOrDefault(MajScenes.List), Is.EqualTo(MajScenes.AirshiftList));
            Assert.That(SettingReturnContext.ConsumeOrDefault(MajScenes.List), Is.EqualTo(MajScenes.List));
        }

        [Test]
        public void StandardSource_ClearsStaleAirshiftRoute()
        {
            SettingReturnContext.Begin(MajScenes.AirshiftList);
            SettingReturnContext.Begin(MajScenes.List);

            Assert.That(SettingReturnContext.ConsumeOrDefault(MajScenes.List), Is.EqualTo(MajScenes.List));
        }

        [Test]
        public void Clear_PreservesOtherLegalCallerFallback()
        {
            SettingReturnContext.Begin(MajScenes.AirshiftList);
            SettingReturnContext.Clear();

            Assert.That(SettingReturnContext.ConsumeOrDefault(MajScenes.View), Is.EqualTo(MajScenes.View));
        }
    }
}
