using System;
using System.Threading.Tasks;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using Dalamud.Game.ClientState;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using XivCommon;

namespace AdaptiveHud
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "Adaptive Hud";

        private const string commandName = "/pah";

        private DalamudPluginInterface PluginInterface { get; init; }
        private CommandManager CommandManager { get; init; }
        private Configuration Configuration { get; init; }
        private PluginUI PluginUi { get; init; }

        private ClientState ClientState { get; init; } = null!;


        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] CommandManager commandManager,
            [RequiredVersion("1.0")] ClientState clientState)
        {
            this.PluginInterface = pluginInterface;
            this.CommandManager = commandManager;
            this.ClientState = clientState;
            

            this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(this.PluginInterface);
            this.PluginUi = new PluginUI(this.Configuration);

            this.CommandManager.AddHandler(commandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Opens configuration window for Adaptive Hud"
            });

            this.PluginInterface.UiBuilder.Draw += DrawUI;
            this.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;

            clientState.Login += StartConfigMonitor;
            
        }

        public void Dispose()
        {
            this.PluginUi.Dispose();
            this.CommandManager.RemoveHandler(commandName);
        }

        private void OnCommand(string command, string args)
        {
            // in response to the slash command, just display our main ui
            this.PluginUi.SettingsVisible = true;
        }

        private void DrawUI()
        {
            this.PluginUi.Draw();
        }

        private void DrawConfigUI()
        {
            this.PluginUi.SettingsVisible = true;
        }
        private void StartConfigMonitor(object? _, EventArgs __)
        {
            PluginLog.LogDebug("Login detected, starting config monitor");
            GameSettingsMonitor gsm = new();
            Task task = new(delegate { gsm.Start(Configuration, ClientState); });
            task.Start();
        }
    }

    public class GameSettingsMonitor
    {
        private int currentLayout = 69;
        private unsafe int GetDisplaySetting()
        {
            return ConfigModule.Instance() == null ? 0 : ConfigModule.Instance()->GetIntValue(20);
        }
        public void Start(Configuration configuration, ClientState clientState)
        {
            XivCommonBase chatHandler = new();
            
            while (clientState.IsLoggedIn)
            {
                if (configuration.LayoutForWindowedMode != configuration.LayoutForFullscreenMode)
                {
                    // windowed mode
                    if (GetDisplaySetting() == 0 && currentLayout != configuration.LayoutForWindowedMode)
                    {
                        try
                        {
                            int adjustedLayoutValue = configuration.LayoutForWindowedMode + 1;
                            string rawCmd = $"/hudlayout {adjustedLayoutValue}";
                            string cleanCmd = chatHandler.Functions.Chat.SanitiseText(rawCmd);
                            chatHandler.Functions.Chat.SendMessage(cleanCmd);
                            currentLayout = configuration.LayoutForWindowedMode;
                        }
                        catch (Exception e)
                        {
                            PluginLog.LogError("Error sending hudlayout command.", e);
                        }
                    }
                    // both fullscreen mode types
                    else if (GetDisplaySetting() > 0 && currentLayout != configuration.LayoutForFullscreenMode)
                    {
                        try
                        {
                            int adjustedLayoutValue = configuration.LayoutForFullscreenMode + 1;
                            string rawCmd = $"/hudlayout {adjustedLayoutValue}";
                            string cleanCmd = chatHandler.Functions.Chat.SanitiseText(rawCmd);
                            chatHandler.Functions.Chat.SendMessage(cleanCmd);
                            currentLayout = configuration.LayoutForFullscreenMode;

                        }
                        catch (Exception e)
                        {
                            PluginLog.LogError("Error sending hudlayout command.", e);
                        }
                    }
                }
            }

            chatHandler.Functions.Dispose();
        }
    }
}
