using MajdataPlay.IO.Storage;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

#nullable enable
namespace MajdataPlay.Scenes.Airshift
{
    /// <summary>Immutable score summary for one AIRSHIFT chart play.</summary>
    internal sealed class AirshiftScoreSnapshot
    {
        internal int TotalNotes { get; init; }
        internal int Perfect { get; init; }
        internal int Great { get; init; }
        internal int Good { get; init; }
        internal int Miss { get; init; }
        internal int Fast { get; init; }
        internal int Late { get; init; }
        internal int MaxCombo { get; init; }
        internal int Score { get; init; }
        internal double Achievement { get; init; }
    }

    /// <summary>Counts AIRSHIFT results independently from STANDARD score data.</summary>
    internal sealed class AirshiftScoreCounter
    {
        private readonly double _maximumWeight;
        private double _earnedWeight;
        private int _combo;
        private int _perfect;
        private int _great;
        private int _good;
        private int _miss;
        private int _fast;
        private int _late;
        private int _maxCombo;

        /// <summary>Gets the current uninterrupted AIRSHIFT combo.</summary>
        internal int CurrentCombo => _combo;

        internal AirshiftScoreCounter(IReadOnlyList<AirshiftChartNote> notes)
        {
            _maximumWeight = notes.Sum(Weight);
        }

        internal void Add(AirshiftChartJudgeResult result)
        {
            var multiplier = result.Grade switch
            {
                AirshiftJudgeGrade.Perfect => 1d,
                AirshiftJudgeGrade.Great => 0.8d,
                AirshiftJudgeGrade.Good => 0.5d,
                _ => 0d,
            };
            _earnedWeight += Weight(result.Note) * multiplier;
            switch (result.Grade)
            {
                case AirshiftJudgeGrade.Perfect:
                    _perfect++;
                    _combo++;
                    break;
                case AirshiftJudgeGrade.Great:
                    _great++;
                    _combo++;
                    break;
                case AirshiftJudgeGrade.Good:
                    _good++;
                    _combo++;
                    break;
                default:
                    _miss++;
                    _combo = 0;
                    break;
            }
            if (result.Grade != AirshiftJudgeGrade.Miss)
            {
                if (result.IsFast)
                {
                    _fast++;
                }
                else if (result.TimingOffset > 0d)
                {
                    _late++;
                }
            }
            _maxCombo = Math.Max(_maxCombo, _combo);
        }

        internal AirshiftScoreSnapshot CreateSnapshot(int totalNotes)
        {
            var achievement = _maximumWeight <= 0d ? 0d : _earnedWeight / _maximumWeight * 100d;
            return new AirshiftScoreSnapshot
            {
                TotalNotes = totalNotes,
                Perfect = _perfect,
                Great = _great,
                Good = _good,
                Miss = _miss,
                Fast = _fast,
                Late = _late,
                MaxCombo = _maxCombo,
                Achievement = achievement,
                Score = (int)Math.Round(achievement * 10000d),
            };
        }

        private static double Weight(AirshiftChartNote note)
        {
            return note.IsBreak ? 5d : 1d;
        }
    }

    /// <summary>One versioned local best record, isolated from STANDARD databases.</summary>
    [JsonObject(MemberSerialization.OptIn)]
    internal sealed class AirshiftStoredRecord
    {
        [JsonProperty("chartKey")]
        internal string ChartKey { get; set; } = string.Empty;
        [JsonProperty("playCount")]
        internal int PlayCount { get; set; }
        [JsonProperty("bestScore")]
        internal int BestScore { get; set; }
        [JsonProperty("bestAchievement")]
        internal double BestAchievement { get; set; }
        [JsonProperty("bestMaxCombo")]
        internal int BestMaxCombo { get; set; }
        [JsonProperty("updatedUtc")]
        internal DateTime UpdatedUtc { get; set; }
    }

    /// <summary>Versioned JSON root for AIRSHIFT-only records.</summary>
    [JsonObject(MemberSerialization.OptIn)]
    internal sealed class AirshiftRecordDocument
    {
        [JsonProperty("version")]
        internal int Version { get; set; } = 1;
        [JsonProperty("records")]
        internal List<AirshiftStoredRecord> Records { get; set; } = new();
    }

    /// <summary>Persists AIRSHIFT bests under a dedicated runtime file.</summary>
    internal static class AirshiftRecordStore
    {
        internal const string FileName = "airshift-scores-v1.json";
        internal const string PlayVersion = "airshift-prototype-v1";

        internal static string CreateChartKey(string songHash, ChartLevel level)
        {
            return $"{songHash}|{(int)level}|{PlayVersion}";
        }

        internal static AirshiftStoredRecord Save(
            StorageDirectory directory,
            string chartKey,
            AirshiftScoreSnapshot score,
            bool debugInput)
        {
            if (debugInput)
            {
                throw new InvalidOperationException("Debug-input AIRSHIFT results are not persisted.");
            }
            var document = Load(directory);
            var record = document.Records.FirstOrDefault(item => item.ChartKey == chartKey);
            if (record is null)
            {
                record = new AirshiftStoredRecord { ChartKey = chartKey };
                document.Records.Add(record);
            }
            record.PlayCount++;
            if (score.Score >= record.BestScore)
            {
                record.BestScore = score.Score;
                record.BestAchievement = score.Achievement;
                record.BestMaxCombo = score.MaxCombo;
            }
            record.UpdatedUtc = DateTime.UtcNow;
            WriteSafely(directory, JsonConvert.SerializeObject(document, Formatting.Indented));
            return record;
        }

        internal static AirshiftRecordDocument Load(StorageDirectory directory)
        {
            var file = directory.FindFile(FileName);
            if (file is null)
            {
                return new AirshiftRecordDocument();
            }
            try
            {
                var document = JsonConvert.DeserializeObject<AirshiftRecordDocument>(file.ReadAllText());
                return document is { Version: 1 } ? document : new AirshiftRecordDocument();
            }
            catch (Exception)
            {
                return new AirshiftRecordDocument();
            }
        }

        private static void WriteSafely(StorageDirectory directory, string json)
        {
            const string tempName = FileName + ".tmp";
            const string backupName = FileName + ".bak";
            directory.FindFile(tempName)?.Delete();
            var temp = directory.CreateFile(tempName, "application/json");
            temp.WriteAllText(json);
            directory.FindFile(backupName)?.Delete();
            var current = directory.FindFile(FileName);
            if (current is not null)
            {
                current.Rename(backupName);
            }
            try
            {
                temp.Rename(FileName);
                directory.FindFile(backupName)?.Delete();
            }
            catch
            {
                if (directory.FindFile(FileName) is null)
                {
                    directory.FindFile(backupName)?.Rename(FileName);
                }
                throw;
            }
        }
    }
}
