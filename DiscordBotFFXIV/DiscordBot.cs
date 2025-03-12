using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;
using DiscordBotFFXIV.Utils;
using ImGuizmoNET;
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

    public static IPluginLog Logger;
    private Plugin plugin;
    public static List<(ChatMode, string)> messages = new List<(ChatMode, string)>();
    public ApplicationCommandService<ApplicationCommandContext, AutocompleteInteractionContext> applicationCommandService;
    public GatewayClient client;
    public static string userName = "";

    public DiscordBot(Plugin p)
    {
        plugin = p;
        Plugin.Logger.Debug("BEFORE DiscordSocketClient");

        if (!buildOption())
        {
            Plugin.Logger.Debug("no friends sadge");
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
        client.CloseAsync().Wait();
        client.Dispose();
        Plugin.Logger.Debug("Unload Discord Bot");
    }

    private ValueTask OnMessageCreated(Message message)
    {
        if (message.Author.GlobalName == plugin.Configuration.discordUser && !message.Author.IsBot)
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
        await applicationCommandService.CreateCommandsAsync(client.Rest, client.Id);

        client.Log += message =>
        {
            Console.WriteLine(message);
            return default;
        };

        await client.StartAsync();
        //await Task.Delay(-1);
    }

    private unsafe bool buildOption()
    {

        var agent = AgentFriendlist.Instance();
        if (agent == null) return false;
        if (agent->InfoProxy == null) return false;

        AddMessageToQueue($"e {agent->InfoProxy->EntryCount}");

        for (var i = 0U; i < agent->InfoProxy->EntryCount; i++)
        {
            var friend = agent->InfoProxy->GetEntry(i);
            if (friend == null) continue;
            Plugin.DataManager.GetExcelSheet<World>().TryGetRow(friend->HomeWorld, out var world);
            var name = friend->NameString;
            ComModule.names.Add($"{name}@{world.Name}");
        }
        AddMessageToQueue("e 3");
        return true;
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

    public void AddMessageToQueue(string message)
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

    public static IEnumerable<string> FindSamples(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return names
            .Select(s => s)
            .Where(n => n.Contains(value, StringComparison.OrdinalIgnoreCase))
            .OrderBy(n => n.IndexOf(value, StringComparison.OrdinalIgnoreCase))
            .ThenBy(n => n);
    }

    public class SearchSamplesAutocompleteProvider : IAutocompleteProvider<AutocompleteInteractionContext>
    {
        public ValueTask<IEnumerable<ApplicationCommandOptionChoiceProperties>?> GetChoicesAsync(
            ApplicationCommandInteractionDataOption option,
            AutocompleteInteractionContext context)
        {
            var sampleNames = FindSamples(option.Value!).Take(25);
            Console.WriteLine(sampleNames);
            var choices = sampleNames.Select(name =>
            {
                string displayName = name.Length > 90 ? name[..90] : name;
                return new ApplicationCommandOptionChoiceProperties(displayName, name);
            });

            return new(choices);
        }
    }

    [SlashCommand("dm", "Send dm to a person.")]
    public void TestDmsDynList([SlashCommandParameter(AutocompleteProviderType = typeof(SearchSamplesAutocompleteProvider))] string name, [SlashCommandParameter]string content)
    {
        if (Context.User.GlobalName == DiscordBot.userName)
        {
            DiscordBot.messages.Add((ChatMode.Tell, $"{name} {content}"));
        }
    }

}
