using Dalamud.Configuration;
using Dalamud.Plugin;
using System;

namespace DiscordBotFFXIV;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;
    public string DiscordToken { get; set; } = String.Empty;

    // the below exist just to make saving less cumbersome
    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
