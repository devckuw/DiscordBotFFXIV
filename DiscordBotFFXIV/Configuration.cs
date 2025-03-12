using Dalamud.Configuration;
using Dalamud.Plugin;
using System;

namespace DiscordBotFFXIV;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public bool showDebug { get; set; } = true;
    public bool showValues { get; set; } = false;
    public string discordUser { get; set; } = String.Empty;
    public string DiscordToken { get; set; } = String.Empty;

    // the below exist just to make saving less cumbersome
    public void Save()
    {
        DiscordBot.userName = discordUser;
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
