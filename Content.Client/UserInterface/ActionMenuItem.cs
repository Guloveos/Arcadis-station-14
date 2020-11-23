﻿using Content.Client.UserInterface.Stylesheets;
using Content.Shared.Actions;
using Robust.Client.UserInterface.Controls;
using Robust.Client.Utility;

namespace Content.Client.UserInterface
{
    /// <summary>
    /// An individual action visible in the action menu.
    /// </summary>
    public class ActionMenuItem : ContainerButton
    {
        // shorter than default tooltip delay so user can
        // quickly explore what each action is
        private const float CustomTooltipDelay = 0.2f;

        private readonly TextureRect _icon;
        public ActionPrototype Action { get; private set; }

        public ActionMenuItem(ActionPrototype action)
        {
            Action = action;

            CustomMinimumSize = (64, 64);
            SizeFlagsVertical = SizeFlags.None;

            _icon = new TextureRect
            {
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                SizeFlagsVertical = SizeFlags.FillExpand,
                Stretch = TextureRect.StretchMode.Scale,
                Texture = action.Icon.Frame0()
            };
            AddChild(_icon);

            TooltipDelay = CustomTooltipDelay;
        }

        /// <summary>
        /// Change how this is displayed depending on if it is granted or revoked
        /// </summary>
        public void SetActionState(bool granted)
        {
            if (granted)
            {
                if (HasStyleClass(StyleNano.StyleClassActionMenuItemRevoked))
                {
                    RemoveStyleClass(StyleNano.StyleClassActionMenuItemRevoked);
                }
            }
            else
            {
                if (!HasStyleClass(StyleNano.StyleClassActionMenuItemRevoked))
                {
                    AddStyleClass(StyleNano.StyleClassActionMenuItemRevoked);
                }
            }
        }

    }
}
