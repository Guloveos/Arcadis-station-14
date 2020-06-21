﻿using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Maths;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Content.Client.BodySystem
{
    public class GenericSurgeryWindow : SS14Window
    {
        public delegate void CloseCallback();
        public delegate void OptionSelectedCallback(object selectedOptionData);

        private Control _vSplitContainer;
        private VBoxContainer _optionsBox;
        private OptionSelectedCallback _optionSelectedCallback;


        protected override Vector2? CustomSize => (300, 400);

        public GenericSurgeryWindow(OptionSelectedCallback optionSelectedCallback)
        {
            Title = "Select surgery target...";
            RectClipContent = true;
            _vSplitContainer = new VBoxContainer();
            var listScrollContainer = new ScrollContainer
            {
                SizeFlagsVertical = SizeFlags.FillExpand,
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                HScrollEnabled = true,
                VScrollEnabled = true
            };
            _optionsBox = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.FillExpand
            };
            listScrollContainer.AddChild(_optionsBox);
            _vSplitContainer.AddChild(listScrollContainer);
            Contents.AddChild(_vSplitContainer);

            _optionSelectedCallback = optionSelectedCallback;
        }

        public void BuildDisplay(Dictionary<string, object> data)
        {
            _optionsBox.DisposeAllChildren();
            foreach (var (displayText, callbackData) in data)
            {
                var button = new SurgeryButton(callbackData);
                button.SetOnToggleBehavior(OnButtonPressed);
                button.SetDisplayText(CultureInfo.CurrentCulture.TextInfo.ToTitleCase(displayText));


                _optionsBox.AddChild(button);
            }
        }

        private void OnButtonPressed(BaseButton.ButtonEventArgs args)
        {
            var pressedButton = (SurgeryButton)args.Button.Parent;
            _optionSelectedCallback(pressedButton.CallbackData);
        }
    }

    class SurgeryButton : PanelContainer
    {
        public Button Button { get; }
        private SpriteView SpriteView { get; }
        private Control EntityControl { get; }
        private Label DisplayText { get; }
        public object CallbackData { get; }

        public SurgeryButton(object callbackData)
        {
            CallbackData = callbackData;

            Button = new Button
            {
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                SizeFlagsVertical = SizeFlags.FillExpand,
                ToggleMode = true,
                MouseFilter = MouseFilterMode.Stop
            };
            AddChild(Button);
            var hBoxContainer = new HBoxContainer();
            SpriteView = new SpriteView
            {
                CustomMinimumSize = new Vector2(32.0f, 32.0f)
            };
            DisplayText = new Label
            {
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                Text = "N/A",
            };
            hBoxContainer.AddChild(SpriteView);
            hBoxContainer.AddChild(DisplayText);
            EntityControl = new Control
            {
                SizeFlagsHorizontal = SizeFlags.FillExpand
            };
            hBoxContainer.AddChild(EntityControl);
            AddChild(hBoxContainer);
        }

        public void SetDisplayText(string text)
        {
            DisplayText.Text = text;
        }

        public void SetOnToggleBehavior(Action<BaseButton.ButtonToggledEventArgs> behavior)
        {
            Button.OnToggled += behavior;
        }

        public void SetSprite()
        {
            //button.SpriteView.Sprite = sprite;
        }
    }
}
