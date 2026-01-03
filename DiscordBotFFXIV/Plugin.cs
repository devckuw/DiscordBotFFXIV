using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using System.Threading.Tasks;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using DiscordBotFFXIV.Windows;
using Dalamud.Game;
using DiscordBotFFXIV.Utils;

namespace DiscordBotFFXIV;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IPluginLog Logger { get; private set; } = null!;
    [PluginService] public static ISigScanner SigScanner { get; private set; } = null!;
    [PluginService] public static IGameGui GameGui { get; private set; } = null!;
    [PluginService] public static IFramework Framework { get; private set; } = null!;
    [PluginService] public static IDataManager DataManager { get; private set; } = null!;
    [PluginService] public static IChatGui ChatGui { get; private set; } = null!;

    private const string CommandName = "/discordbot";

    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("DiscordBotFFXIV");
    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }
    public DiscordBot? discordBot { get; set; }

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        // you might normally want to embed resources and load them from the manifest stream

        ConfigWindow = new ConfigWindow(this);
        MainWindow = new MainWindow(this);

        ChatHelper.Initialize();

        DiscordBot.userName = Configuration.discordUser;

        StartDiscordBot();

        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Open main interface."
        });

        PluginInterface.UiBuilder.Draw += DrawUI;

        // This adds a button to the plugin installer entry of this plugin which allows
        // to toggle the display status of the configuration ui
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;

        // Adds another button that is doing the same but for the main ui of the plugin
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUI;
    }

    public void StartDiscordBot()
    {
        if (Configuration.DiscordToken == null || Configuration.DiscordToken == string.Empty)
        {
            Plugin.Logger.Debug("Discord Token not set.");
            return;
        }
        discordBot = new DiscordBot(this);
        Task.Run(async () =>
        {
            await discordBot.Start();
        });
    }

    public void Dispose()
    {
        WindowSystem.RemoveAllWindows();
        discordBot?.Dispose();
        ChatHelper.Instance?.Dispose();

        ConfigWindow.Dispose();
        MainWindow.Dispose();

        CommandManager.RemoveHandler(CommandName);
    }

    private void OnCommand(string command, string args)
    {
        // in response to the slash command, just toggle the display status of our main ui
        ToggleMainUI();
    }

    private void DrawUI() => WindowSystem.Draw();

    public void ToggleConfigUI() => ConfigWindow.Toggle();
    public void ToggleMainUI() => MainWindow.Toggle();
}
