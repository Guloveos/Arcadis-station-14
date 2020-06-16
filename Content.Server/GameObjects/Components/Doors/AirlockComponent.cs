﻿using System;
using System.Threading;
using Content.Server.GameObjects.Components.Interactable;
using Content.Server.GameObjects.Components.Power;
using Content.Server.GameObjects.Components.VendingMachines;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces;
using Content.Shared.GameObjects.Components.Doors;
using Content.Shared.GameObjects.Components.Interactable;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using static Content.Shared.GameObjects.Components.SharedWiresComponent;
using static Content.Shared.GameObjects.Components.SharedWiresComponent.WiresAction;
using Timer = Robust.Shared.Timers.Timer;

namespace Content.Server.GameObjects.Components.Doors
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    [ComponentReference(typeof(ServerDoorComponent))]
    public class AirlockComponent : ServerDoorComponent, IWires, IInteractUsing
    {
        public override string Name => "Airlock";

        /// <summary>
        /// Duration for which power will be disabled after pulsing either power wire.
        /// </summary>
        private static readonly TimeSpan PowerWiresTimeout = TimeSpan.FromSeconds(5.0);

        private PowerDeviceComponent _powerDevice;
        private WiresComponent _wires;

        private CancellationTokenSource _powerWiresPulsedTimerCancel;

        private bool _powerWiresPulsed;

        /// <summary>
        /// True if either power wire was pulsed in the last <see cref="PowerWiresTimeout"/>.
        /// </summary>
        private bool PowerWiresPulsed
        {
            get => _powerWiresPulsed;
            set
            {
                _powerWiresPulsed = value;
                UpdateWiresStatus();
                UpdatePowerCutStatus();
            }
        }

        private bool _boltsDown;
        private bool BoltsDown
        {
            get => _boltsDown;
            set
            {
                _boltsDown = value;
                EntitySystem.Get<AudioSystem>()
                .PlayFromEntity(value ? "/Audio/machines/boltsdown.ogg" : "/Audio/machines/boltsup.ogg",
                    Owner);
                UpdateWiresStatus();
                UpdateBoltLightStatus();
            }
        }

        private bool _boltLights = true;
        private bool BoltLightsOn
        {
            get => _boltLights;
            set
            {
                _boltLights = value;
                UpdateWiresStatus();
                UpdateBoltLightStatus();
            }
        }

        private void UpdateWiresStatus()
        {
            var powerLight = new StatusLightData(Color.Yellow, StatusLightState.On, "POWR");
            if (PowerWiresPulsed)
            {
                powerLight = new StatusLightData(Color.Yellow, StatusLightState.BlinkingFast, "POWR");
            }
            else if (_wires.IsWireCut(Wires.MainPower) &&
                     _wires.IsWireCut(Wires.BackupPower))
            {
                powerLight = new StatusLightData(Color.Red, StatusLightState.On, "POWR");
            }

            var boltStatus = new StatusLightData(Color.Red, BoltsDown ? StatusLightState.On : StatusLightState.Off, "BOLT");
            var boltLightsStatus = new StatusLightData(Color.Lime, BoltLightsOn ? StatusLightState.On : StatusLightState.Off, "BLTL");

            _wires.SetStatus(AirlockWireStatus.PowerIndicator, powerLight);
            _wires.SetStatus(AirlockWireStatus.BoltIndicator, boltStatus);
            _wires.SetStatus(AirlockWireStatus.BoltLightIndicator, boltLightsStatus);
            _wires.SetStatus(3, new StatusLightData(Color.Purple, StatusLightState.BlinkingSlow, "AICT"));
            _wires.SetStatus(4, new StatusLightData(Color.Orange, StatusLightState.Off, "TIME"));
            _wires.SetStatus(5, new StatusLightData(Color.Red, StatusLightState.Off, "SAFE"));
            /*
            _wires.SetStatus(6, powerLight);
            _wires.SetStatus(7, powerLight);
            _wires.SetStatus(8, powerLight);
            _wires.SetStatus(9, powerLight);
            _wires.SetStatus(10, powerLight);
            _wires.SetStatus(11, powerLight);*/
        }

        private void UpdatePowerCutStatus()
        {
            _powerDevice.IsPowerCut = PowerWiresPulsed ||
                                      _wires.IsWireCut(Wires.MainPower) ||
                                      _wires.IsWireCut(Wires.BackupPower);
        }

        private void UpdateBoltLightStatus()
        {
            if (State == DoorState.Closed) //only shown when door is closed
            {
                if (BoltsDown)
                    SetAppearance(BoltLightsOn ? DoorVisualState.Light : DoorVisualState.Closed);
                else
                    SetAppearance(DoorVisualState.Closed);
            }
        }

        protected override DoorState State
        {
            set
            {
                base.State = value;
                // Only show the maintenance panel if the airlock is closed
                _wires.IsPanelVisible = value != DoorState.Open;
            }
        }

        public override void Initialize()
        {
            base.Initialize();
            _powerDevice = Owner.GetComponent<PowerDeviceComponent>();
            _wires = Owner.GetComponent<WiresComponent>();

            _powerDevice.OnPowerStateChanged += PowerDeviceOnOnPowerStateChanged;
        }

        private void PowerDeviceOnOnPowerStateChanged(object sender, PowerStateEventArgs e)
        {
            if (Owner.TryGetComponent(out AppearanceComponent appearance))
            {
                appearance.SetData(DoorVisuals.Powered, e.Powered);
            }
        }

        protected override void ActivateImpl(ActivateEventArgs args)
        {
            if (_wires.IsPanelOpen)
            {
                if (args.User.TryGetComponent(out IActorComponent actor))
                {
                    _wires.OpenInterface(actor.playerSession);
                }
            }
            else
            {
                base.ActivateImpl(args);
            }
        }

        private enum Wires
        {
            /// <summary>
            /// Pulsing turns off power for <see cref="AirlockComponent.PowerWiresTimeout"/>.
            /// Cutting turns off power permanently if <see cref="BackupPower"/> is also cut.
            /// Mending restores power.
            /// </summary>
            MainPower,

            /// <see cref="MainPower"/>
            BackupPower,

            /// <summary>
            /// Pulsing causes for bolts to toggle (but only raise if power is on)
            /// Cutting causes Bolts to drop
            /// Mending does nothing
            /// </summary>
            Bolts,

            /// <summary>
            /// Pulsing causes light to toggle
            /// Cutting causes light to go out
            /// Mending causes them to go on again
            /// </summary>
            BoltLight,
        }

        public void RegisterWires(WiresComponent.WiresBuilder builder)
        {
            builder.CreateWire(Wires.MainPower);
            builder.CreateWire(Wires.BackupPower);
            builder.CreateWire(Wires.Bolts);
            builder.CreateWire(Wires.BoltLight);
            builder.CreateWire(4);
            builder.CreateWire(5);
            /*builder.CreateWire(5);
            builder.CreateWire(6);
            builder.CreateWire(7);
            builder.CreateWire(8);
            builder.CreateWire(9);
            builder.CreateWire(10);
            builder.CreateWire(11);*/
            UpdateWiresStatus();
        }

        public void WiresUpdate(WiresUpdateEventArgs args)
        {
            if (args.Action == Pulse)
            {
                switch (args.Identifier)
                {
                    case Wires.MainPower:
                    case Wires.BackupPower:
                        PowerWiresPulsed = true;
                        _powerWiresPulsedTimerCancel?.Cancel();
                        _powerWiresPulsedTimerCancel = new CancellationTokenSource();
                        Timer.Spawn(PowerWiresTimeout,
                            () => PowerWiresPulsed = false,
                            _powerWiresPulsedTimerCancel.Token);
                        break;
                    case Wires.Bolts:
                        if (!BoltsDown)
                        {
                            BoltsDown = true;
                        }
                        else
                        {
                            if (IsPowered())
                            {
                                BoltsDown = false;
                            }
                        }
                        break;
                    case Wires.BoltLight:
                        BoltLightsOn = !BoltLightsOn;
                        break;
                }
            }

            if (args.Action == Mend)
            {
                switch (args.Identifier)
                {
                    case Wires.MainPower:
                    case Wires.BackupPower:
                        // mending power wires instantly restores power
                        _powerWiresPulsedTimerCancel?.Cancel();
                        PowerWiresPulsed = false;
                        break;
                    case Wires.BoltLight:
                        BoltLightsOn = true;
                        break;
                }
            }

            if (args.Action == Cut)
            {
                switch (args.Identifier)
                {
                    case Wires.Bolts:
                        BoltsDown = true;
                        break;
                    case Wires.BoltLight:
                        BoltLightsOn = false;
                        break;
                }
            }

            UpdateWiresStatus();
            UpdatePowerCutStatus();
        }

        public override bool CanOpen()
        {
            return IsPowered() && !IsBolted();
        }

        public override bool CanClose()
        {
            return IsPowered() && !IsBolted();
        }

        public override void Deny()
        {
            if (!IsPowered() || IsBolted())
            {
                return;
            }

            base.Deny();
        }

        private bool IsBolted()
        {
            return _boltsDown;
        }

        private bool IsPowered()
        {
            return _powerDevice.Powered;
        }

        public bool InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (!eventArgs.Using.TryGetComponent<ToolComponent>(out var tool))
                return false;

            if (tool.HasQuality(ToolQuality.Cutting)
                || tool.HasQuality(ToolQuality.Multitool))
            {
                if (_wires.IsPanelOpen)
                {
                    if (eventArgs.User.TryGetComponent(out IActorComponent actor))
                    {
                        _wires.OpenInterface(actor.playerSession);
                        return true;
                    }
                }
            }

            if (!tool.UseTool(eventArgs.User, Owner, ToolQuality.Prying)) return false;

            if (IsBolted())
            {
                var notify = IoCManager.Resolve<IServerNotifyManager>();
                notify.PopupMessage(Owner, eventArgs.User, "The airlock's bolts prevent it from being forced!");
                return true;
            }

            if (IsPowered())
            {
                var notify = IoCManager.Resolve<IServerNotifyManager>();
                notify.PopupMessage(Owner, eventArgs.User, "The powered motors block your efforts!");
                return true;
            }

            if (State == DoorState.Closed)
                Open();
            else if (State == DoorState.Open)
                Close();

            return true;
        }
    }
}
