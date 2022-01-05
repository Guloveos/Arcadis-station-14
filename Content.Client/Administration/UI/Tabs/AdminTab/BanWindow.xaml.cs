﻿using Content.Shared.Administration;
using JetBrains.Annotations;
using Robust.Client.AutoGenerated;
using Robust.Client.Console;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.IoC;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.Controls.LineEdit;

namespace Content.Client.Administration.UI.Tabs.AdminTab
{
    [GenerateTypedNameReferences]
    [UsedImplicitly]
    public partial class BanWindow : SS14Window
    {
        public BanWindow()
        {
            RobustXamlLoader.Load(this);
            PlayerNameLine.OnTextChanged += _ => OnPlayerNameChanged();
            PlayerList.OnSelectionChanged += OnPlayerSelectionChanged;
            SubmitButton.OnPressed += SubmitButtonOnOnPressed;
            MinutesLine.OnTextChanged += UpdateButtonsText;
            HourButton.OnPressed += _ => AddMinutes(60);
            DayButton.OnPressed += _ => AddMinutes(1440);
            WeekButton.OnPressed += _ => AddMinutes(10080);
            MonthButton.OnPressed += _ => AddMinutes(43200);
        }

        private bool TryGetMinutes(string str, out uint minutes)
        {
            if(string.IsNullOrWhiteSpace(str))
            {
                minutes = 0;
                return true;
            }

            return uint.TryParse(str, out minutes);
        }

        private void AddMinutes(uint add)
        {
            if (!TryGetMinutes(MinutesLine.Text, out var minutes))
                return;

            MinutesLine.Text = $"{minutes + add}";
            UpdateButtons(minutes+add);
        }

        private void UpdateButtonsText(LineEditEventArgs obj)
        {
            if (!TryGetMinutes(obj.Text, out var minutes))
                return;
            UpdateButtons(minutes);
        }

        private void UpdateButtons(uint minutes)
        {
            HourButton.Text = $"+1h ({minutes / 60})";
            DayButton.Text = $"+1d ({minutes / 1440})";
            WeekButton.Text = $"+1w ({minutes / 10080})";
            MonthButton.Text = $"+1M ({minutes / 43200})";
        }

        private void OnPlayerNameChanged()
        {
            SubmitButton.Disabled = string.IsNullOrEmpty(PlayerNameLine.Text);
        }

        public void OnPlayerSelectionChanged(PlayerInfo? player)
        {
            PlayerNameLine.Text = player?.Username ?? string.Empty;
            OnPlayerNameChanged();
        }

        private void SubmitButtonOnOnPressed(BaseButton.ButtonEventArgs obj)
        {
            // Small verification if Player Name exists
            IoCManager.Resolve<IClientConsoleHost>().ExecuteCommand(
                $"ban \"{PlayerNameLine.Text}\" \"{CommandParsing.Escape(ReasonLine.Text)}\" {MinutesLine.Text}");
        }
    }
}
