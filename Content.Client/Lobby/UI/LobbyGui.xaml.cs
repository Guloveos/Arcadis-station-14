using Content.Client.Chat.UI;
using Content.Client.Info;
using Content.Client.Preferences;
using Content.Client.Preferences.UI;
using Content.Client.UserInterface.Screens;
using Content.Client.UserInterface.Systems.Chat.Widgets;
using Content.Client.UserInterface.Systems.EscapeMenu;
using Robust.Client.AutoGenerated;
using Robust.Client.Console;
using Robust.Client.Graphics;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.Lobby.UI
{
    [GenerateTypedNameReferences]
    internal sealed partial class LobbyGui : InGameScreen
    {
        [Dependency] private readonly IClientConsoleHost _consoleHost = default!;
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;

        public LobbyGui()
        {
            RobustXamlLoader.Load(this);
            SetAnchorPreset(MainContainer, LayoutPreset.Wide);
            SetAnchorPreset(Background, LayoutPreset.Wide);

            LeaveButton.OnPressed += _ => _consoleHost.ExecuteCommand("disconnect");
            OptionsButton.OnPressed += _ => _userInterfaceManager.GetUIController<OptionsUIController>().ToggleWindow();
        }

        public void SwitchState(LobbyGuiState state)
        {
            DefaultState.Visible = false;
            CharacterSetupState.Visible = false;

            switch (state)
            {
                case LobbyGuiState.Default:
                    DefaultState.Visible = true;
                    RightSide.Visible = true;
                    break;
                case LobbyGuiState.CharacterSetup:
                    CharacterSetupState.Visible = true;

                    var actualWidth = (float) _userInterfaceManager.RootControl.PixelWidth;
                    var setupWidth = (float) LeftSide.PixelWidth;

                    if (1 - (setupWidth / actualWidth) > 0.30)
                    {
                        RightSide.Visible = false;
                    }

                    break;
            }
        }

        public enum LobbyGuiState : byte
        {
            /// <summary>
            ///  The default state, i.e., what's seen on launch.
            /// </summary>
            Default,
            /// <summary>
            ///  The character setup state.
            /// </summary>
            CharacterSetup
        }

        public override ChatBox ChatBox => GetWidget<ChatBox>()!;

        public override void SetChatSize(Vector2 size)
        {
        }
    }
}
