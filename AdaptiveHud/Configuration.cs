using Dalamud.Configuration;
using Dalamud.Plugin;
using System;

namespace AdaptiveHud
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;
        public int LayoutForWindowedMode { get; set; } = 0;
        public int LayoutForFullscreenMode { get; set; } = 1;

        // the below exist just to make saving less cumbersome

        [NonSerialized]
        private DalamudPluginInterface? pluginInterface;

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;
        }

        public void Save()
        {
            this.pluginInterface!.SavePluginConfig(this);
        }
    }
}
