using System;
using System.IO;
using System.Numerics;
using System.Text;
using Dalamud.Interface.Components;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Common.Lua;
using Dalamud.Bindings.ImGui;

namespace DiscordBotFFXIV.Windows;

public class ConfigWindow : Window, IDisposable
{
    private Configuration Configuration;
    private string token = string.Empty;
    private ulong userNameInput = 0;

    public ConfigWindow(Plugin plugin) : base("DiscordBotFFXIV Config###With a constant ID")
    {
        Flags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoScrollWithMouse;

        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(232, 90),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        Configuration = plugin.Configuration;
    }

    public void Dispose() { }

    public override void Draw()
    {
        var configValue = Configuration.showDebug;
        if (ImGui.Checkbox("Show Debug", ref configValue))
        {
            Configuration.showDebug = configValue;
            Configuration.Save();
        }
        ImGui.SameLine();
        ImGuiComponents.HelpMarker("Show debug in Echo channel.");
        var showValue = Configuration.showValues;
        if (ImGui.Checkbox("Show Values", ref showValue))
        {
            Configuration.showValues = showValue;
            Configuration.Save();
        }

        ImGui.InputULong("##userID", ref userNameInput);
        //ImGui.input
        //ImGui.InputTextWithHint("##username", "Enter Discord User Name Here", ref userName, 64);
        ImGui.SameLine();
        if (ImGui.Button("Save##saveusername"))
        {
            Configuration.userID = userNameInput;
            userNameInput = 0;
            Configuration.Save();
            DiscordBot.userID = Configuration.userID;
        }
        ImGui.SameLine();
        ImGuiComponents.HelpMarker("Enter discord user ID of the person allowed to use that bot");
        if (Configuration.showValues)
        {
            ImGui.Text($"{Configuration.userID}");
        }

        ImGui.InputTextWithHint("##token", "Enter Discord Token Here", ref token, 128);
        ImGui.SameLine();
        if (ImGui.Button("Save##savetoken"))
        {
            Configuration.DiscordToken = token;
            token = string.Empty;
            Configuration.Save();
        }
        ImGui.SameLine();
        ImGuiComponents.HelpMarker("Enter discord token of your bot");
        if (Configuration.showValues)
        {
            ImGui.Text(Configuration.DiscordToken);
        }
    }
}
