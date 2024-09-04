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
using Dalamud.Interface.Components;
using Dalamud.Utility;

namespace DiscordBotFFXIV.Windows;

public class MainWindow : Window, IDisposable
{
    private Plugin Plugin;
    private string testMessage = string.Empty;
    private string token = string.Empty;
    private string userName = string.Empty;

    // We give this window a hidden ID using ##
    // So that the user will see "My Amazing Window" as window title,
    // but for ImGui the ID is "My Amazing Window##With a hidden ID"
    public MainWindow(Plugin plugin)
        : base("DiscordBotFFXIV##With a hidden ID", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
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
            if (Plugin.Configuration.showDebug)
            {
                ChatHelper.Send(ChatMode.Echo, "chatmode : " + item.Item1 + ", content : " + item.Item2);
            }
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
        ImGui.SameLine();
        var configValue = Plugin.Configuration.showDebug;
        if (ImGui.Checkbox("Show Debug", ref configValue))
        {
            Plugin.Configuration.showDebug = configValue;
            Plugin.Configuration.Save();
        }
        ImGui.SameLine();
        ImGuiComponents.HelpMarker("Show debug in Echo channel.");
        
        if (Plugin.Configuration.DiscordToken  == string.Empty)
        {
            ImGui.Text("Enter your token in the config pannel then restart the plugin");
        }
        if (Plugin.Configuration.discordUser  == string.Empty)
        {
            ImGui.Text("Enter your discord name in pannel config or you wont get any msg");
        }

        ImGui.NewLine();
        if (ImGui.InputTextWithHint("##inputTest", "Try input here, should work the same as on discord.", ref testMessage, 256, ImGuiInputTextFlags.EnterReturnsTrue))
        {
            Plugin.discordBot.AddMessageToQueue(testMessage);
            testMessage = string.Empty;
        }
        ImGui.SameLine();
        if (ImGui.Button("Send##inputTestSend"))
        {
            Plugin.discordBot.AddMessageToQueue(testMessage);
            testMessage = string.Empty;
        }
        ImGui.Text("exemple : chat-mode msg");
        ImGui.SameLine();
        ImGuiComponents.HelpMarker("chat-mode :\nNone,\nEcho | e,\nTell | t,\nParty | p,\nAlliance | a,\nSay | s,\nEmote | em,\nShout | sh," +
            "\nYell | y,\nFreeCompany | fc,\nLinkShell1-8 | l1-8,\n" +
            "CrossLinkShell1-8 | cwl1-8");

        ImGui.NewLine();
        ImGui.Text("How to Setup :");
        ImGui.Text("Go to discord and create a new application.");
        ImGui.SameLine();
        if (ImGui.Button("Go##discordAPP"))
        {
            Util.OpenLink("https://discord.com/developers/applications");
        }
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("https://discord.com/developers/applications");

        ImGui.NewLine();
        ImGui.Text("Select your Application.");

        ImGui.NewLine();
        ImGui.Text("Go to Bot Tab on the left side.");
        ImGui.Text("Here you can change the name of your bot in username input.");
        ImGui.Text("Turn on 'Message Content Intent' or the bot wont be able to read your mesages.");
        ImGui.Text("Dont forget to save modification.");
        ImGui.Text("Clic 'Reset Token' to get your bot token and enter it bellow.");
        ImGui.InputTextWithHint("##token", "Enter Discord Token Here", ref token, 64);
        ImGui.SameLine();
        if (ImGui.Button("Save##savetoken"))
        {
            Plugin.Configuration.DiscordToken = token;
            token = string.Empty;
            Plugin.Configuration.Save();
        }
        ImGui.SameLine();
        ImGuiComponents.HelpMarker("Enter discord token of your bot");

        ImGui.NewLine();
        ImGui.Text("Go to OAuth2 Tab on the left side.");
        ImGui.Text("Select Bot checkbox");
        ImGui.Text("Bot Permissions selection should pop under.");
        ImGui.Text("Select Administrator.");
        ImGui.Text("Copy the link under 'Generated Url' and access it.");
        ImGui.Text("Select the server where you want to add the bot.");

        ImGui.NewLine();
        ImGui.Text("Enter your discord username bellow.");
        ImGui.InputTextWithHint("##username", "Enter Discord User Name Here", ref userName, 64);
        ImGui.SameLine();
        if (ImGui.Button("Save##saveusername"))
        {
            Plugin.Configuration.discordUser = userName;
            userName = string.Empty;
            Plugin.Configuration.Save();
        }
        ImGui.SameLine();
        ImGuiComponents.HelpMarker("Enter discord user name of the person allowed to use that bot");

        ImGui.NewLine();
        ImGui.Text("You are done! Restart the plugin and try.");
    }
}
