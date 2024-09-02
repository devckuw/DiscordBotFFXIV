using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Dalamud.Plugin.Services;
using DiscordBotFFXIV.Utils;
using ImGuizmoNET;

namespace DiscordBotFFXIV
{
    public class DiscordBot : IDisposable
    {

        public static IPluginLog Logger;
        private DiscordSocketClient client;
        private Plugin plugin;
        public List<(ChatMode, string)> messages = new List<(ChatMode, string)>();

        public DiscordBot(Plugin p)
        {
            plugin = p;
            Plugin.Logger.Verbose("BEFORE DiscordSocketClient");
            client = new DiscordSocketClient(new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.GuildMessages | GatewayIntents.GuildWebhooks | GatewayIntents.MessageContent,
            });
            Plugin.Logger.Verbose("AFTER DiscordSocketClient");
            client.Ready += SocketClientOnReady;
            client.MessageReceived += OnMessageReceived;
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
            client.LogoutAsync().Wait();
            client.Dispose();
        }


        public async Task Start()
        {
            //await client.LoginAsync(TokenType.Bot, token);
            //await client.StartAsync();

            try
            {
                await this.client.LoginAsync(TokenType.Bot, plugin.Configuration.DiscordToken);
                await this.client.StartAsync();
            }
            catch (Exception ex)
            {
                Logger.Verbose(ex, "Token invalid, cannot start bot.");
            }
            //await Task.Delay(-1);
        }

        private Task SocketClientOnReady()
        {

            Logger.Verbose("DiscordHandler READY!!");

            return Task.CompletedTask;
        }

        private Task OnMessageReceived(SocketMessage message)
        {
            if (message.Author.Username == "devckuw")
            {
                string msg = message.Content;
                ChatMode mode = ProcessChatMode(message.Content);
                if (mode != ChatMode.None)
                {
                    msg = ProcessContent(msg);
                }
                messages.Add((mode, msg));
            }
            return Task.CompletedTask;
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

    }
}
