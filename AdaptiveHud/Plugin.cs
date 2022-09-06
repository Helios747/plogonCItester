using System;
using System.Threading.Tasks;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using Dalamud.Game.ClientState;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using XivCommon;
using Framework = Dalamud.Game.Framework;

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
        private Framework Framework { get; init; } = null!;

        private int currentLayout = 69;

        private XivCommonBase chatHandler = new();


        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] CommandManager commandManager,
            [RequiredVersion("1.0")] Framework framework)
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

            framework.Update += Check;
        }

        public void Dispose()
        {
            this.PluginUi.Dispose();
            this.CommandManager.RemoveHandler(commandName);
            chatHandler.Dispose();
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
        private unsafe int GetDisplaySetting()
        {
            return ConfigModule.Instance() == null ? 0 : ConfigModule.Instance()->GetIntValue(20);
        }
        private void Check(object? _)
        {
            if (Configuration.LayoutForWindowedMode != Configuration.LayoutForFullscreenMode)
            {
                // windowed mode
                if (GetDisplaySetting() == 0 && currentLayout != Configuration.LayoutForWindowedMode)
                {
                    try
                    {
                        int adjustedLayoutValue = Configuration.LayoutForWindowedMode + 1;
                        string rawCmd = $"/hudlayout {adjustedLayoutValue}";
                        string cleanCmd = chatHandler.Functions.Chat.SanitiseText(rawCmd);
                        chatHandler.Functions.Chat.SendMessage(cleanCmd);
                        currentLayout = Configuration.LayoutForWindowedMode;
                    }
                    catch (Exception e)
                    {
                        PluginLog.LogError("Error sending hudlayout command.", e);
                    }
                }
                else if (GetDisplaySetting() > 0 && currentLayout != Configuration.LayoutForFullscreenMode)
                {
                    try
                    {
                        int adjustedLayoutValue = Configuration.LayoutForFullscreenMode + 1;
                        string rawCmd = $"/hudlayout {adjustedLayoutValue}";
                        string cleanCmd = chatHandler.Functions.Chat.SanitiseText(rawCmd);
                        chatHandler.Functions.Chat.SendMessage(cleanCmd);
                        currentLayout = Configuration.LayoutForFullscreenMode;

                    }
                    catch (Exception e)
                    {
                        PluginLog.LogError("Error sending hudlayout command.", e);
                    }
                }

            }
        }

    }
}
