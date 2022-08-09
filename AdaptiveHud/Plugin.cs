using System;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
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
        CancellationTokenSource StopConfigMonitor = new CancellationTokenSource();


        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] CommandManager commandManager)
        {
            this.PluginInterface = pluginInterface;
            this.CommandManager = commandManager;

            this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(this.PluginInterface);
            this.PluginUi = new PluginUI(this.Configuration);

            this.CommandManager.AddHandler(commandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Opens configuration window for Adaptive Hud"
            });

            this.PluginInterface.UiBuilder.Draw += DrawUI;
            this.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;            
            XivCommonBase chatHandler = new XivCommonBase();
            // start the monitor, all work is done in this class. Shouldn't block thread?
            GameSettingsMonitor gsm = new GameSettingsMonitor();
            CancellationToken token;
            token = StopConfigMonitor.Token;
            Task task = new Task(delegate { gsm.Start(chatHandler, Configuration); }, token);
            task.Start();
        }

        public void Dispose()
        {
            this.PluginUi.Dispose();
            this.CommandManager.RemoveHandler(commandName);
            this.StopConfigMonitor.Cancel();
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
    }

    public class GameSettingsMonitor
    {
        private int currentLayout = 69;

        private unsafe ConfigModule* cfg = ConfigModule.Instance();
        private unsafe int GetDisplaySetting()
        {
            try
            {
                return cfg->GetIntValue(20);
            }
            catch (Exception)
            {
                return 0;
            }
        }
        public void Start(XivCommonBase chatHandler, Configuration configuration)
        {
            while (true)
            {
                // got a funky crash when both were set to the same, decided to just do nothing rather
                // than change config behind the user's back. As nothing should happen anyways.
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
                            Console.WriteLine(e);
                            throw;
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
                            Console.WriteLine(e);
                            throw;
                        }
                    }
                }
            }
        }
    }
}
