using Content.Shared.CCVar;
using Content.Shared.HUD;
using Robust.Client.AutoGenerated;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Client.EscapeMenu.UI.Tabs
{
    [GenerateTypedNameReferences]
    public partial class GraphicsTab : Control
    {
        private static readonly float[] UIScaleOptions =
        {
            0f,
            0.75f,
            1f,
            1.25f,
            1.50f,
            1.75f,
            2f
        };

        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public GraphicsTab()
        {
            IoCManager.InjectDependencies(this);
            RobustXamlLoader.Load(this);

            VSyncCheckBox.OnToggled += OnCheckBoxToggled;
            FullscreenCheckBox.OnToggled += OnCheckBoxToggled;

            LightingPresetOption.AddItem(Loc.GetString("ui-options-lighting-very-low"));
            LightingPresetOption.AddItem(Loc.GetString("ui-options-lighting-low"));
            LightingPresetOption.AddItem(Loc.GetString("ui-options-lighting-medium"));
            LightingPresetOption.AddItem(Loc.GetString("ui-options-lighting-high"));
            LightingPresetOption.OnItemSelected += OnLightingQualityChanged;

            UIScaleOption.AddItem(Loc.GetString("ui-options-scale-auto",
                                                ("scale", UserInterfaceManager.DefaultUIScale)));
            UIScaleOption.AddItem(Loc.GetString("ui-options-scale-75"));
            UIScaleOption.AddItem(Loc.GetString("ui-options-scale-100"));
            UIScaleOption.AddItem(Loc.GetString("ui-options-scale-125"));
            UIScaleOption.AddItem(Loc.GetString("ui-options-scale-150"));
            UIScaleOption.AddItem(Loc.GetString("ui-options-scale-175"));
            UIScaleOption.AddItem(Loc.GetString("ui-options-scale-200"));
            UIScaleOption.OnItemSelected += OnUIScaleChanged;

            foreach (var gear in _prototypeManager.EnumeratePrototypes<HudThemePrototype>())
            {
                HudThemeOption.AddItem(Loc.GetString(gear.Name));
            }
            HudThemeOption.OnItemSelected += OnHudThemeChanged;

            ViewportStretchCheckBox.OnToggled += _ =>
            {
                UpdateViewportScale();
                UpdateApplyButton();
            };

            ViewportScaleSlider.OnValueChanged += _ =>
            {
                UpdateApplyButton();
                UpdateViewportScale();
            };

            ShowHeldItemCheckBox.OnToggled += OnCheckBoxToggled;
            IntegerScalingCheckBox.OnToggled += OnCheckBoxToggled;
            ViewportLowResCheckBox.OnToggled += OnCheckBoxToggled;
            FpsCounterCheckBox.OnToggled += OnCheckBoxToggled;
            ApplyButton.OnPressed += OnApplyButtonPressed;
            VSyncCheckBox.Pressed = _cfg.GetCVar(CVars.DisplayVSync);
            FullscreenCheckBox.Pressed = ConfigIsFullscreen;
            LightingPresetOption.SelectId(GetConfigLightingQuality());
            UIScaleOption.SelectId(GetConfigUIScalePreset(ConfigUIScale));
            HudThemeOption.SelectId(_cfg.GetCVar(CCVars.HudTheme));
            ViewportScaleSlider.Value = _cfg.GetCVar(CCVars.ViewportFixedScaleFactor);
            ViewportStretchCheckBox.Pressed = _cfg.GetCVar(CCVars.ViewportStretch);
            IntegerScalingCheckBox.Pressed = _cfg.GetCVar(CCVars.ViewportSnapToleranceMargin) != 0;
            ViewportLowResCheckBox.Pressed = !_cfg.GetCVar(CCVars.ViewportScaleRender);
            FpsCounterCheckBox.Pressed = _cfg.GetCVar(CCVars.HudFpsCounterVisible);
            ShowHeldItemCheckBox.Pressed = _cfg.GetCVar(CCVars.HudHeldItemShow);

            UpdateViewportScale();
            UpdateApplyButton();
        }

        private void OnUIScaleChanged(OptionButton.ItemSelectedEventArgs args)
        {
            UIScaleOption.SelectId(args.Id);
            UpdateApplyButton();
        }

        private void OnHudThemeChanged(OptionButton.ItemSelectedEventArgs args)
        {
            HudThemeOption.SelectId(args.Id);
            UpdateApplyButton();
        }

        private void OnApplyButtonPressed(BaseButton.ButtonEventArgs args)
        {
            _cfg.SetCVar(CVars.DisplayVSync, VSyncCheckBox.Pressed);
            SetConfigLightingQuality(LightingPresetOption.SelectedId);
            if (HudThemeOption.SelectedId != _cfg.GetCVar(CCVars.HudTheme)) // Don't unnecessarily redraw the HUD
            {
                _cfg.SetCVar(CCVars.HudTheme, HudThemeOption.SelectedId);
            }

            _cfg.SetCVar(CVars.DisplayWindowMode,
                         (int) (FullscreenCheckBox.Pressed ? WindowMode.Fullscreen : WindowMode.Windowed));
            _cfg.SetCVar(CVars.DisplayUIScale, UIScaleOptions[UIScaleOption.SelectedId]);
            _cfg.SetCVar(CCVars.ViewportStretch, ViewportStretchCheckBox.Pressed);
            _cfg.SetCVar(CCVars.ViewportFixedScaleFactor, (int) ViewportScaleSlider.Value);
            _cfg.SetCVar(CCVars.ViewportSnapToleranceMargin,
                         IntegerScalingCheckBox.Pressed ? CCVars.ViewportSnapToleranceMargin.DefaultValue : 0);
            _cfg.SetCVar(CCVars.ViewportScaleRender, !ViewportLowResCheckBox.Pressed);
            _cfg.SetCVar(CCVars.HudHeldItemShow, ShowHeldItemCheckBox.Pressed);
            _cfg.SetCVar(CCVars.HudFpsCounterVisible, FpsCounterCheckBox.Pressed);
            _cfg.SaveToFile();
            UpdateApplyButton();
        }

        private void OnCheckBoxToggled(BaseButton.ButtonToggledEventArgs args)
        {
            UpdateApplyButton();
        }

        private void OnLightingQualityChanged(OptionButton.ItemSelectedEventArgs args)
        {
            LightingPresetOption.SelectId(args.Id);
            UpdateApplyButton();
        }

        private void UpdateApplyButton()
        {
            var isVSyncSame = VSyncCheckBox.Pressed == _cfg.GetCVar(CVars.DisplayVSync);
            var isFullscreenSame = FullscreenCheckBox.Pressed == ConfigIsFullscreen;
            var isLightingQualitySame = LightingPresetOption.SelectedId == GetConfigLightingQuality();
            var isHudThemeSame = HudThemeOption.SelectedId == _cfg.GetCVar(CCVars.HudTheme);
            var isUIScaleSame = MathHelper.CloseToPercent(UIScaleOptions[UIScaleOption.SelectedId], ConfigUIScale);
            var isVPStretchSame = ViewportStretchCheckBox.Pressed == _cfg.GetCVar(CCVars.ViewportStretch);
            var isVPScaleSame = (int) ViewportScaleSlider.Value == _cfg.GetCVar(CCVars.ViewportFixedScaleFactor);
            var isIntegerScalingSame = IntegerScalingCheckBox.Pressed == (_cfg.GetCVar(CCVars.ViewportSnapToleranceMargin) != 0);
            var isVPResSame = ViewportLowResCheckBox.Pressed == !_cfg.GetCVar(CCVars.ViewportScaleRender);
            var isShowHeldItemSame = ShowHeldItemCheckBox.Pressed == _cfg.GetCVar(CCVars.HudHeldItemShow);
            var isFpsCounterVisibleSame = FpsCounterCheckBox.Pressed == _cfg.GetCVar(CCVars.HudFpsCounterVisible);

            ApplyButton.Disabled = isVSyncSame &&
                                   isFullscreenSame &&
                                   isLightingQualitySame &&
                                   isUIScaleSame &&
                                   isVPStretchSame &&
                                   isVPScaleSame &&
                                   isIntegerScalingSame &&
                                   isVPResSame &&
                                   isHudThemeSame &&
                                   isShowHeldItemSame &&
                                   isFpsCounterVisibleSame;
        }

        private bool ConfigIsFullscreen =>
            _cfg.GetCVar(CVars.DisplayWindowMode) == (int) WindowMode.Fullscreen;

        private float ConfigUIScale => _cfg.GetCVar(CVars.DisplayUIScale);

        private int GetConfigLightingQuality()
        {
            var val = _cfg.GetCVar(CVars.DisplayLightMapDivider);
            var soft = _cfg.GetCVar(CVars.DisplaySoftShadows);
            if (val >= 8)
            {
                return 0;
            }
            else if ((val >= 2) && !soft)
            {
                return 1;
            }
            else if (val >= 2)
            {
                return 2;
            }
            else
            {
                return 3;
            }
        }

        private void SetConfigLightingQuality(int value)
        {
            switch (value)
            {
                case 0:
                    _cfg.SetCVar(CVars.DisplayLightMapDivider, 8);
                    _cfg.SetCVar(CVars.DisplaySoftShadows, false);
                    _cfg.SetCVar(CVars.DisplayBlurLight, false);
                    break;
                case 1:
                    _cfg.SetCVar(CVars.DisplayLightMapDivider, 2);
                    _cfg.SetCVar(CVars.DisplaySoftShadows, false);
                    _cfg.SetCVar(CVars.DisplayBlurLight, true);
                    break;
                case 2:
                    _cfg.SetCVar(CVars.DisplayLightMapDivider, 2);
                    _cfg.SetCVar(CVars.DisplaySoftShadows, true);
                    _cfg.SetCVar(CVars.DisplayBlurLight, true);
                    break;
                case 3:
                    _cfg.SetCVar(CVars.DisplayLightMapDivider, 1);
                    _cfg.SetCVar(CVars.DisplaySoftShadows, true);
                    _cfg.SetCVar(CVars.DisplayBlurLight, true);
                    break;
            }
        }

        private static int GetConfigUIScalePreset(float value)
        {
            for (var i = 0; i < UIScaleOptions.Length; i++)
            {
                if (MathHelper.CloseToPercent(UIScaleOptions[i], value))
                {
                    return i;
                }
            }

            return 0;
        }

        private void UpdateViewportScale()
        {
            ViewportScaleBox.Visible = !ViewportStretchCheckBox.Pressed;
            IntegerScalingCheckBox.Visible = ViewportStretchCheckBox.Pressed;
            ViewportScaleText.Text = Loc.GetString("ui-options-vp-scale", ("scale", ViewportScaleSlider.Value));
        }
    }
}
