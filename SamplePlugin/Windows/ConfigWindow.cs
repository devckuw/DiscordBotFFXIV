using System;
using System.IO;
using System.Numerics;
using System.Text;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Common.Lua;
using ImGuiNET;

namespace DiscordBotFFXIV.Windows;

public class ConfigWindow : Window, IDisposable
{
    private Configuration Configuration;
    private byte[] textInput = new byte[128];

    // We give this window a constant ID using ###
    // This allows for labels being dynamic, like "{FPS Counter}fps###XYZ counter window",
    // and the window ID will always be "###XYZ counter window" for ImGui
    public ConfigWindow(Plugin plugin) : base("A Wonderful Configuration Window###With a constant ID")
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
        ImGui.Text("Enter Discord Token Here");
        ImGui.InputText(" ", textInput, 128);
        ImGui.SameLine();
        if (ImGui.Button("Save"))
        {
            Configuration.DiscordToken = Encoding.UTF8.GetString(textInput).Replace("\u0000", string.Empty);
            textInput = new byte[128];
            Configuration.Save();
        }
    }
}
