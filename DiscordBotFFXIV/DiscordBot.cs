using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;
using DiscordBotFFXIV.Utils;
using Dalamud.Bindings.ImGuizmo;
using Newtonsoft.Json;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel.Sheets;
using Microsoft.Extensions.DependencyInjection;
using NetCord;
using NetCord.Services;
using NetCord.Hosting;
using NetCord.Hosting.Services;
using NetCord.Hosting.AspNetCore;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using NetCord.Gateway;
using static Dalamud.Interface.Utility.Raii.ImRaii;
//using Discord.Interactions.Builders;

namespace DiscordBotFFXIV;

public class DiscordBot : IDisposable
{
    private Plugin plugin;
    public static List<(ChatMode, string)> messages = new List<(ChatMode, string)>();
    public ApplicationCommandService<ApplicationCommandContext, AutocompleteInteractionContext> applicationCommandService;
    public GatewayClient client;
    public static string userName = "";

    public DiscordBot(Plugin p)
    {
        plugin = p;
        Plugin.Logger.Debug("BEFORE DiscordSocketClient");

        if (!buildFriendList())
        {
            Plugin.Logger.Debug("no friends sadge");
        }

        if (!buildLinkShellList())
        {
            Plugin.Logger.Debug("no linkshells sadge");
        }

        if (!buildCrossWorldLinkShellList())
        {
            Plugin.Logger.Debug("no crossworldlinkshells sadge");
        }

        client = new(new BotToken(plugin.Configuration.DiscordToken), new GatewayClientConfiguration()
        {
            Intents = GatewayIntents.GuildMessages | GatewayIntents.DirectMessages | GatewayIntents.MessageContent,
        });

        // Create the application command service
        applicationCommandService = new();

        // Add commands from modules
        applicationCommandService.AddModules(typeof(ComModule).Assembly);

        // Add the handler to handle interactions
        client.InteractionCreate += Client_InteractionCreate;
        client.MessageCreate += OnMessageCreated;

        Plugin.Logger.Debug("AFTER DiscordSocketClient");

        
    }


    ~DiscordBot()
    {
        Dispose(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected void Dispose(bool disposing)
    {
        if (!disposing)
        {
            return;
        }
        client.MessageCreate -= OnMessageCreated;
        client.InteractionCreate -= Client_InteractionCreate;
        //client.CloseAsync().Wait();
        client.CloseAsync();
        client.Dispose();
        Plugin.Logger.Debug("Unload Discord Bot");
        ComModule.names = new List<string>();
        ComModule.linkShells = new List<string>();
    }

    private ValueTask OnMessageCreated(Message message)
    {
        if (message.Author.Username == plugin.Configuration.discordUser && !message.Author.IsBot)
        {
            AddMessageToQueue(message.Content);
        }
        return ValueTask.CompletedTask;
    }

    private async ValueTask Client_InteractionCreate(Interaction interaction)
    {
        var result = await (interaction switch
        {
            SlashCommandInteraction slashCommandInteraction => applicationCommandService.ExecuteAsync(new(slashCommandInteraction, client)),
            //MessageCommandInteraction messageCommandInteraction => _messageCommandService.ExecuteAsync(new(messageCommandInteraction, _client), _serviceProvider),
            //UserCommandInteraction userCommandInteraction => _userCommandService.ExecuteAsync(new(userCommandInteraction, _client), _serviceProvider),
            //StringMenuInteraction stringMenuInteraction => _stringMenuInteractionService.ExecuteAsync(new(stringMenuInteraction, _client), _serviceProvider),
            //UserMenuInteraction userMenuInteraction => _userMenuInteractionService.ExecuteAsync(new(userMenuInteraction, _client), _serviceProvider),
            //RoleMenuInteraction roleMenuInteraction => _roleMenuInteractionService.ExecuteAsync(new(roleMenuInteraction, _client), _serviceProvider),
            //MentionableMenuInteraction mentionableMenuInteraction => _mentionableMenuInteractionService.ExecuteAsync(new(mentionableMenuInteraction, _client), _serviceProvider),
            //ChannelMenuInteraction channelMenuInteraction => _channelMenuInteractionService.ExecuteAsync(new(channelMenuInteraction, _client), _serviceProvider),
            //ButtonInteraction buttonInteraction => _buttonInteractionService.ExecuteAsync(new(buttonInteraction, _client), _serviceProvider),
            AutocompleteInteraction autocompleteInteraction => applicationCommandService.ExecuteAutocompleteAsync(new(autocompleteInteraction, client)),
            //ModalInteraction modalInteraction => _modalInteractionService.ExecuteAsync(new(modalInteraction, _client), _serviceProvider),
            _ => throw new("Invalid interaction."),
        });
        if (result is IFailResult failResult)
        {
            InteractionMessageProperties message = new()
            {
                Content = failResult.Message,
                Flags = MessageFlags.Ephemeral,
            };
            try
            {
                await interaction.SendResponseAsync(InteractionCallback.Message(message));
            }
            catch
            {
                try
                {
                    await interaction.SendFollowupMessageAsync(message);
                }
                catch
                {
                }
            }
        }
    }

    public async Task Start()
    {
        // Create the commands so that you can use them in the Discord client
        await applicationCommandService.RegisterCommandsAsync(client.Rest, client.Id);

        /*client.Log += message =>
        {
            Console.WriteLine(message);
            return default;
        };*/

        await client.StartAsync();
        //await Task.Delay(-1);
    }

    public static unsafe bool buildFriendList()
    {

        var agent = AgentFriendlist.Instance();
        if (agent == null) return false;
        if (agent->InfoProxy == null) return false;

        for (var i = 0U; i < agent->InfoProxy->EntryCount; i++)
        {
            var friend = agent->InfoProxy->GetEntry(i);
            if (friend == null) continue;
            Plugin.DataManager.GetExcelSheet<World>().TryGetRow(friend->HomeWorld, out var world);
            var name = friend->NameString;
            ComModule.names.Add($"{name}@{world.Name}");
        }

        return true;
    }

    public static unsafe bool buildLinkShellList()
    {
        ChatMode[] ls = { ChatMode.l1, ChatMode.l2, ChatMode.l3, ChatMode.l4, ChatMode.l5, ChatMode.l6, ChatMode.l7, ChatMode.l8 };
        var array = FFXIVClientStructs.FFXIV.Component.GUI.AtkStage.Instance()->GetStringArrayData(FFXIVClientStructs.FFXIV.Component.GUI.StringArrayType.LinkShell);
        if (array == null) return false;

        for (int i = 0; i < 8; i++)
        {
            string name = array->ManagedStringArray[640 + i] == null ? string.Empty : array->ManagedStringArray[640 + i];
            if (name != string.Empty)
            {
                //Plugin.Logger.Debug(name);
                ComModule.linkShells.Add($"{ls[i]} {name}");
            }
        }
        return true;
    }

    public static unsafe bool buildCrossWorldLinkShellList()
    {
        ChatMode[] cwls = { ChatMode.cwl1, ChatMode.cwl2, ChatMode.cwl3, ChatMode.cwl4, ChatMode.cwl5, ChatMode.cwl6, ChatMode.cwl7, ChatMode.cwl8 };
        var infoProxy = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->GetUIModule()->GetInfoModule()->GetInfoProxyCrossWorldLinkshell(); ;
        if (infoProxy == null) return false;

        for (uint i = 0; i < 8; i++)
        {
            string name = infoProxy->GetCrossworldLinkshellName(i)->ToString();
            if (name != string.Empty)
            {
                //Plugin.Logger.Debug(name);
                ComModule.linkShells.Add($"{cwls[i]} {name}");
            }
        }
        return true;
    }

    public static void resetFriendList()
    {
        ComModule.names = new List<string>();
    }

    public static void resetLinkShellList()
    {
        List<string> newLinkShells = new List<string>();
        // > 41 => remove all l# values
        foreach (var item in ComModule.linkShells)
        {
            if ((int)ChatHelper.GetChatMode(item.Split(" ")[0]) > 41)
            {
                newLinkShells.Add(item);
            }
        }

        ComModule.linkShells = newLinkShells;
    }
    
    public static void resetCrossWorldLinkShellList()
    {
        List<string> newLinkShells = new List<string>();
        // < 41 => remove all cwl# values
        foreach (var item in ComModule.linkShells)
        {
            if ((int)ChatHelper.GetChatMode(item.Split(" ")[0]) < 41)
            {
                newLinkShells.Add(item);
            }
        }

        ComModule.linkShells = newLinkShells;
    }

    public static ChatMode ProcessChatMode(string message)
    {
        string chatType = message.Split(" ")[0];
        foreach (ChatMode mode in Enum.GetValues(typeof(ChatMode)))
        {
            if (mode.ToString().ToLower() == chatType.ToLower())
            {
                return mode;
            }
        }
        return ChatMode.None;
    }

    public static string ProcessContent(string msg)
    {
        return string.Join(" ", msg.Split(" ")[1..]);
    }

    public static void AddMessageToQueue(string message)
    {
        ChatMode mode = DiscordBot.ProcessChatMode(message);
        if (mode != ChatMode.None)
        {
            message = DiscordBot.ProcessContent(message);
        }
        messages.Add((mode, message));
    }

}

public class ComModule : ApplicationCommandModule<ApplicationCommandContext>
{

    public static List<string> names = new List<string>();
    //public static Dictionary<ChatMode, string> linkShells = new Dictionary<ChatMode, string>();
    public static List<string> linkShells = new List<string>();

    public static IEnumerable<string> FindSamplesFriends(string value)
    {

        if (names.Count == 0)
            DiscordBot.buildFriendList();

        ArgumentNullException.ThrowIfNull(value);

        return names
            .Select(s => s)
            .Where(n => n.Contains(value, StringComparison.OrdinalIgnoreCase))
            .OrderBy(n => n.IndexOf(value, StringComparison.OrdinalIgnoreCase))
            .ThenBy(n => n);
    }

    public class FriendsAutocompleteProvider : IAutocompleteProvider<AutocompleteInteractionContext>
    {
        public ValueTask<IEnumerable<ApplicationCommandOptionChoiceProperties>?> GetChoicesAsync(
            ApplicationCommandInteractionDataOption option,
            AutocompleteInteractionContext context)
        {
            var sampleNames = FindSamplesFriends(option.Value!).Take(25);
            Console.WriteLine(sampleNames);
            var choices = sampleNames.Select(name =>
            {
                string displayName = name.Length > 90 ? name[..90] : name;
                return new ApplicationCommandOptionChoiceProperties(displayName, name);
            });

            return new(choices);
        }
    }

    public static IEnumerable<string> FindSamplesLinkShells(string value)
    {

        if (linkShells.Count == 0)
        {
            DiscordBot.buildCrossWorldLinkShellList();
            DiscordBot.buildLinkShellList();
        }

        ArgumentNullException.ThrowIfNull(value);

        return linkShells
            .Select(s => s)
            .Where(n => n.Contains(value, StringComparison.OrdinalIgnoreCase))
            .OrderBy(n => n.IndexOf(value, StringComparison.OrdinalIgnoreCase))
            .ThenBy(n => n);
    }

    public class LinkShellAutocompleteProvider : IAutocompleteProvider<AutocompleteInteractionContext>
    {
        public ValueTask<IEnumerable<ApplicationCommandOptionChoiceProperties>?> GetChoicesAsync(
            ApplicationCommandInteractionDataOption option,
            AutocompleteInteractionContext context)
        {
            //var sampleNames = FindSamplesLinkShells(option.Value!).Take(25);
            var sampleNames = FindSamplesLinkShells(option.Value!);
            Console.WriteLine(sampleNames);
            var choices = sampleNames.Select(displayName =>
            {
                string mode = displayName.Split(" ")[0];
                return new ApplicationCommandOptionChoiceProperties(displayName, mode);
            });

            return new(choices);
        }
    }

    [SlashCommand("dm", "Sends dm to someone.")]
    public string CommandDirectMessage([SlashCommandParameter(AutocompleteProviderType = typeof(FriendsAutocompleteProvider))] string name, [SlashCommandParameter] string content)
    {
        if (Context.User.Username == DiscordBot.userName)
        {
            DiscordBot.messages.Add((ChatMode.Tell, $"{name} {content}"));
            return $"/tell {name} {content}";
        }

        return "No right for it.";

    }

    [SlashCommand("p", "Talk in party.")]
    public string CommandPartyChat([SlashCommandParameter] string content)
    {
        if (Context.User.Username == DiscordBot.userName)
        {
            DiscordBot.messages.Add((ChatMode.Party, content));
            return $"/p {content}";
        }
        return "No right for it.";
    }

    [SlashCommand("fc", "Talk in free company.")]
    public string CommandFreeCompanyChat([SlashCommandParameter] string content)
    {
        if (Context.User.Username == DiscordBot.userName)
        {
            DiscordBot.messages.Add((ChatMode.FreeCompany, content));
            return $"/fc {content}";
        }
        return "No right for it.";
    }

    [SlashCommand("l", "Talk in cwls/ls.")]
    public string CommandLinkShellMessage([SlashCommandParameter(AutocompleteProviderType = typeof(LinkShellAutocompleteProvider))] string ls, [SlashCommandParameter] string content)
    {
        if (Context.User.Username == DiscordBot.userName)
        {
            DiscordBot.messages.Add((ChatHelper.GetChatMode(ls), $"{content}"));
            return $"/{ls} {content}";
        }

        return "No right for it.";

    }

    [SlashCommand("reload", "Reload friends and cwls/ls.")]
    public string CommandReloadList()
    {
        if (Context.User.Username == DiscordBot.userName)
        {
            DiscordBot.resetCrossWorldLinkShellList();
            DiscordBot.resetLinkShellList();
            DiscordBot.resetFriendList();

            DiscordBot.buildCrossWorldLinkShellList();
            DiscordBot.buildLinkShellList();
            DiscordBot.buildFriendList();

            return $"Reloaded";
        }

        return "No right for it.";

    }

}
