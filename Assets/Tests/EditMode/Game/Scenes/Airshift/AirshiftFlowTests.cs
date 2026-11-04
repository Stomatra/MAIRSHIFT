using MajdataPlay.Scenes.Airshift;
using MajdataPlay.Scenes.Game;
using NUnit.Framework;
using System;
using System.Linq;
using System.Reflection;

#nullable enable
namespace MajdataPlay.Tests.Game.Scenes.Airshift
{
    /// <summary>
    /// Tests original scene classification and legacy AIRSHIFT
    /// session behavior during the Mod architecture migration.
    /// </summary>
    public class AirshiftFlowTests
    {
        /// <summary>
        /// Clears static AIRSHIFT test data after every assertion.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            AirshiftSessionStore.Clear();
        }


        /// <summary>
        /// Verifies that the original gameplay and result scenes
        /// retain their expected classification.
        /// </summary>
        [Test]
        public void OriginalGameplayScenes_HaveCorrectClassification()
        {
            Assert.That(
                SceneSwitcher.IsGameplayScene(MajScenes.Game),
                Is.True
            );

            Assert.That(
                SceneSwitcher.IsGameplayScene(MajScenes.Result),
                Is.False
            );
        }

        /// <summary>
        /// Checks that a local AIRSHIFT session completes without replacing the STANDARD game session.
        /// </summary>
        [Test]
        public void LocalTestSession_DoesNotReplaceStandardGameInfo()
        {
            var previous = Majdata<GameInfo>.Instance;
            var standardInfo = new GameInfo(GameMode.Normal, null!, null!);
            try
            {
                Majdata<GameInfo>.Instance = standardInfo;

                var session = AirshiftSessionStore.StartLocalTest();
                session.Complete();

                Assert.That(session.Kind, Is.EqualTo(AirshiftSessionKind.LocalTest));
                Assert.That(session.IsCompleted, Is.True);
                Assert.That(Majdata<GameInfo>.Instance, Is.SameAs(standardInfo));
            }
            finally
            {
                Majdata<GameInfo>.Instance = previous;
            }
        }

        /// <summary>
        /// Checks that AIRSHIFT session state exists only for the independent scene chain.
        /// </summary>
        [Test]
        public void SessionStore_ClearEndsTheAirshiftSession()
        {
            var session = AirshiftSessionStore.StartLocalTest();

            Assert.That(AirshiftSessionStore.Current, Is.SameAs(session));
            AirshiftSessionStore.Clear();
            Assert.That(AirshiftSessionStore.Current, Is.Null);
        }

        /// <summary>
        /// Checks that AIRSHIFT sessions carry local chart selection without STANDARD game, result, or score objects.
        /// </summary>
        [Test]
        public void AirshiftSessionModel_HasNoStandardDataDependencies()
        {
            var forbiddenTypes = new[]
            {
                typeof(GameInfo),
                typeof(GameResult),
                typeof(MaiScore),
            };
            var exposedTypes = typeof(AirshiftSession)
                .GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Select(member => member switch
                {
                    FieldInfo field => field.FieldType,
                    PropertyInfo property => property.PropertyType,
                    _ => null,
                })
                .Where(type => type is not null)
                .Cast<Type>();

            Assert.That(exposedTypes.Any(type => forbiddenTypes.Any(forbidden => forbidden.IsAssignableFrom(type))), Is.False);
        }
    }
}
