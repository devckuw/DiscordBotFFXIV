using System;
using System.Threading.Tasks;
using System.Numerics;
using System.Text;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using ImGuiNET;
using DiscordBotFFXIV.Utils;

namespace DiscordBotFFXIV.Windows;

public class MainWindow : Window, IDisposable
{
    private Plugin Plugin;
    public string t = "not started";
    private byte[] textInput = new byte[128];

    // We give this window a hidden ID using ##
    // So that the user will see "My Amazing Window" as window title,
    // but for ImGui the ID is "My Amazing Window##With a hidden ID"
    public MainWindow(Plugin plugin)
        : base("My Amazing Window##With a hidden ID", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        Plugin = plugin;
        Plugin.Framework.Update += OnUpdate;
    }

    public void Dispose() 
    {
        Plugin.Framework.Update -= OnUpdate;
    }

    private void OnUpdate(IFramework framework)
    {
        foreach (var item in Plugin.discordBot.messages)
        {
            //ChatHelper.Send(ChatMode.Echo, "chatmode : " + item.Item1 + ", content : " + item.Item2);
            ChatHelper.Send(item.Item1, item.Item2);
        }
        Plugin.discordBot.messages.Clear();
    }

    public override void Draw()
    {
        if (ImGui.Button("Show Settings"))
        {
            Plugin.ToggleConfigUI();
        }
        if (Plugin.Configuration.DiscordToken  == string.Empty)
        {
            ImGui.Text("Enter your token in the config pannel then restart the plugin");
        }
        ImGui.Text("try input, should work the same as discord");
        ImGui.Text("exemple : p|party msg");

        ImGui.InputText(" ", textInput, 128);
        ImGui.SameLine();
        if (ImGui.Button("send"))
        {
            var txt = Encoding.UTF8.GetString(textInput).Replace("\u0000", string.Empty);
            textInput = new byte[128];
            ChatMode mode = DiscordBot.ProcessChatMode(txt);
            if (mode != ChatMode.None)
            {
                txt = DiscordBot.ProcessContent(txt);
            }
            Plugin.discordBot.messages.Add((mode, txt));
        }
    }
}
