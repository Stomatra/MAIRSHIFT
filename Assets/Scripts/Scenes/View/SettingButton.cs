using MajdataPlay.Utils;
using MajdataPlay.Scenes.Setting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MajdataPlay.Scenes.View
{
    internal class SettingButton: MajComponent
    {
        public void OnClick()
        {
            SettingReturnContext.Clear();
            MajInstances.SceneSwitcher.SwitchScene("Setting", false);
        }
    }
}
