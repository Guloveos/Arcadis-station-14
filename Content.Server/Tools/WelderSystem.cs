﻿using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.Tools.Components;
using Content.Shared.Chemistry;
using Content.Shared.Examine;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;

namespace Content.Server.Tools
{
    /// <summary>
    ///     Despite the name, it's only really used for the welder logic in tools. Go figure.
    /// </summary>
    public class WelderSystem : EntitySystem
    {
        private readonly HashSet<WelderComponent> _activeWelders = new();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<WelderComponent, SolutionChangedEvent>(OnSolutionChange);
            SubscribeLocalEvent<WelderComponent, ExaminedEvent>(OnExamine);
        }

        private void OnExamine(EntityUid uid, WelderComponent component, ExaminedEvent args)
        {
            if (component.WelderLit)
            {
                args.Message.AddMarkup(Loc.GetString("welder-component-on-examine-welder-lit-message") + "\n");
            }
            else
            {
                args.Message.AddText(Loc.GetString("welder-component-on-examine-welder-not-lit-message") + "\n");
            }

            if (args.IsInDetailsRange)
            {
                args.Message.AddMarkup(Loc.GetString("welder-component-on-examine-detailed-message",
                    ("colorName", component.Fuel < component.FuelCapacity / 4f ? "darkorange" : "orange"),
                    ("fuelLeft", Math.Round(component.Fuel)),
                    ("fuelCapacity", component.FuelCapacity)));
            }
        }

        private void OnSolutionChange(EntityUid uid, WelderComponent component, SolutionChangedEvent args)
        {
            component.Dirty();
        }

        public bool Subscribe(WelderComponent welder)
        {
            return _activeWelders.Add(welder);
        }

        public bool Unsubscribe(WelderComponent welder)
        {
            return _activeWelders.Remove(welder);
        }

        public override void Update(float frameTime)
        {
            foreach (var tool in _activeWelders.ToArray())
            {
                tool.OnUpdate(frameTime);
            }
        }
    }
}
