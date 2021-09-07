using System;
using System.Collections.Generic;
using System.Threading;
using Content.Client.ContextMenu.UI;
using Content.Client.Resources;
using Content.Shared.Verbs;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.Utility;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.Controls.BoxContainer;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Client.Verbs
{

    /// <summary>
    ///     This pop-up appears when hovering over a verb category in the context menu.
    /// </summary>
    public sealed class VerbCategoryPopup : ContextMenuPopup
    {
        public VerbCategoryPopup(VerbSystem system, IEnumerable<Verb> verbs, EntityUid target, bool drawOnlyIcons)
            : base()
        {
            // Do any verbs have icons? If not, don't bother leaving space for icons in the pop-up.
            var drawVerbIcons = false;
            foreach (var verb in verbs)
            {
                if (verb.Icon != null)
                {
                    drawVerbIcons = true;
                    break;
                }
            }

            // If no verbs have icons. we cannot draw only icons
            if (drawVerbIcons == false)
                drawOnlyIcons = false;

            // If we are drawing only icons, show them side by side
            if (drawOnlyIcons)
                List.Orientation = LayoutOrientation.Horizontal;

            foreach (var verb in verbs)
            {
                AddToMenu(new VerbButton(system, verb, target, drawVerbIcons));
            }
        }
    }

    public sealed class VerbButton : BaseButton
    {
        public VerbButton(VerbSystem system, Verb verb, EntityUid target, bool drawIcons = true) : base()
        {
            Disabled = verb.IsDisabled;

            var buttonContents = new BoxContainer { Orientation = LayoutOrientation.Horizontal };

            // maybe draw verb icons
            if (drawIcons)
            {
                TextureRect icon = new()
                {
                    MinSize = (32, 32),
                    Stretch = TextureRect.StretchMode.KeepCentered,
                    TextureScale = (0.5f, 0.5f)
                };

                // Even though we are drawing icons, the icon for this specific verb may be null.
                if (verb.Icon != null)
                {
                    icon.Texture = verb.Icon.Frame0();
                }

                buttonContents.AddChild(icon);
            }

            // maybe add a label
            if (verb.Text != string.Empty)
            {
                var label = new RichTextLabel();
                label.SetMessage(FormattedMessage.FromMarkupPermissive(verb.Text));
                buttonContents.AddChild(label);

                // If we added a label, also add some padding
                buttonContents.AddChild(new Control { MinSize = (8, 0) });
            }

            AddChild(buttonContents);

            if (Disabled)
                return;

            // give the button functionality!
            OnPressed += _ =>
            {
                system.CloseVerbMenu();
                try
                {
                    // Try run the verb locally. Else, ask the server to run it.
                    if (!system.TryExecuteVerb(verb))
                    {
                        system.ExecuteServerVerb(target, verb.Key);
                    }
                }
                catch (Exception e)
                {
                    Logger.ErrorS("verb", "Exception in verb {0} on uid {1}:\n{2}", verb.Key, target.ToString(), e);
                }
            };
        }

        protected override void Draw(DrawingHandleScreen handle)
        {
            base.Draw(handle);

            if (Disabled)
            {
                // use transparent-black rectangle to create a darker background.
                handle.DrawRect(PixelSizeBox, new Color(0,0,0,155)); 
            }    
            else if (DrawMode == DrawModeEnum.Hover)
            {
                // Draw a lighter shade of gray when hovered over
                handle.DrawRect(PixelSizeBox, Color.DarkSlateGray);
            }
        }
    }

    public sealed class VerbCategoryButton : Control
    {
        private readonly VerbSystem _system;

        private CancellationTokenSource? _openCancel;

        /// <summary>
        ///     Whether or not to hide member verb text and just show icons.
        /// </summary>
        /// <remarks>
        ///     If no members have icons, this option is ignored and text is shown anyways. Defaults to using <see cref="VerbCategoryData.IconsOnly"/>.
        /// </remarks>
        private readonly bool _drawOnlyIcons;

        /// <summary>
        ///     The pop-up that appears when hovering over this verb group.
        /// </summary>
        private readonly VerbCategoryPopup _popup;

        public VerbCategoryButton(VerbSystem system, VerbCategoryData category, IEnumerable<Verb> verbs, EntityUid target, bool? drawOnlyIcons = null) : base()
        {
            _system = system;
            _drawOnlyIcons = drawOnlyIcons ?? category.IconsOnly;

            MouseFilter = MouseFilterMode.Stop;

            // Contents of the button stored in this box container
            var box = new BoxContainer() { Orientation = LayoutOrientation.Horizontal };

            // First we add the icon for the verb group
            var icon = new TextureRect
            {
                MinSize = (32, 32),
                TextureScale = (0.5f, 0.5f),
                Stretch = TextureRect.StretchMode.KeepCentered,
            };
            if (category.Icon != null)
            {
                icon.Texture = category.Icon.Frame0();
            }
            box.AddChild(icon);

            // Then we add the label
            var label = new RichTextLabel();
            label.SetMessage(FormattedMessage.FromMarkupPermissive(category.Text));
            label.HorizontalExpand = true;
            box.AddChild(label);

            // Add horizontal padding
            box.AddChild(new Control { MinSize = (8, 0) });

            // Then add the little ">" icon that tells you it's a group of verbs
            box.AddChild( new TextureRect
            {
                Texture = IoCManager.Resolve<IResourceCache>()
                            .GetTexture("/Textures/Interface/VerbIcons/group.svg.192dpi.png"),
                TextureScale = (0.5f, 0.5f),
                Stretch = TextureRect.StretchMode.KeepCentered,
            });

            // The pop-up that appears when hovering over the button
            _popup = new VerbCategoryPopup(_system, verbs, target, _drawOnlyIcons);
            UserInterfaceManager.ModalRoot.AddChild(_popup);

            AddChild(box);
        }

        protected override void Draw(DrawingHandleScreen handle)
        {
            base.Draw(handle);

            if (this == UserInterfaceManager.CurrentlyHovered)
            {
                handle.DrawRect(PixelSizeBox, Color.DarkSlateGray);
            }
        }

        /// <summary>
        ///     Open a verb category pop-up after a short delay.
        /// </summary>
        protected override void MouseEntered()
        {
            base.MouseEntered();

            _openCancel = new CancellationTokenSource();

            Timer.Spawn(ContextMenuPresenter.HoverDelay, () =>
            {
                _system.CurrentCategoryPopup?.Close();
                _system.CurrentCategoryPopup = _popup;
                _popup.Open(UIBox2.FromDimensions(GlobalPosition + (Width, 0), (1, 1)), GlobalPosition);
            }, _openCancel.Token);
        }

        /// <summary>
        ///     Cancel the delayed pop-up
        /// </summary>
        protected override void MouseExited()
        {
            base.MouseExited();

            _openCancel?.Cancel();
            _openCancel = null;
        }
    }
}
