using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MajdataPlay
{
    public enum MajScenes
    {
        Init,
        Title,
        Login,
        Calibrator,
        List,
        Game,
        Result,
        Setting,
        SortFind,
        TotalResult,
        Parctice,
        View,
        Test,
        Empty,
        /// <summary>The independent AIRSHIFT local-song selector.</summary>
        AirshiftList,
        /// <summary>The isolated AIRSHIFT gameplay and local-input test scene.</summary>
        AirshiftGame,
        /// <summary>The isolated AIRSHIFT placeholder result scene.</summary>
        AirshiftResult,
    }
}
