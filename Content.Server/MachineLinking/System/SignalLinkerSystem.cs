using System.Linq;
using System.Diagnostics.CodeAnalysis;
using Content.Server.MachineLinking.Components;
using Content.Server.MachineLinking.Events;
using Content.Server.Power.Components;
using Content.Shared.Interaction;
using Content.Shared.MachineLinking;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Shared.Utility;
using Robust.Shared.Player;

namespace Content.Server.MachineLinking.System
{
    public sealed class SignalLinkerSystem : EntitySystem
    {
        [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
        [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

        private static readonly (string, string)[][] _defaultMappings =
        {
            new [] { ("Pressed", "Toggle") },
            new [] { ("On", "On"), ("Off", "Off") },
            new [] { ("On", "Open"), ("Off", "Close") },
            new [] { ("On", "Forward"), ("Off", "Off") },
            new [] { ("Left", "On"), ("Right", "On"), ("Middle", "Off") },
            new [] { ("Left", "Open"), ("Right", "Open"), ("Middle", "Close") },
            new [] { ("Left", "Forward"), ("Right", "Reverse"), ("Middle", "Off") },
        };

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SignalTransmitterComponent, InvokePortEvent>(OnTransmitterInvokePort);

            SubscribeLocalEvent<SignalTransmitterComponent, ComponentStartup>(OnTransmitterStartup);
            SubscribeLocalEvent<SignalTransmitterComponent, ComponentRemove>(OnTransmitterRemoved);
            SubscribeLocalEvent<SignalTransmitterComponent, InteractUsingEvent>(OnTransmitterInteractUsing);

            SubscribeLocalEvent<SignalReceiverComponent, ComponentRemove>(OnReceiverRemoved);
            SubscribeLocalEvent<SignalReceiverComponent, InteractUsingEvent>(OnReceiverInteractUsing);

            SubscribeLocalEvent<SignalLinkerComponent, SignalPortSelected>(OnSignalPortSelected);
            SubscribeLocalEvent<SignalLinkerComponent, LinkerClearSelected>(OnLinkerClearSelected);
            SubscribeLocalEvent<SignalLinkerComponent, LinkerLinkDefaultSelected>(OnLinkerLinkDefaultSelected);
            SubscribeLocalEvent<SignalLinkerComponent, BoundUIClosedEvent>(OnLinkerUIClosed);
        }

        private void OnTransmitterInvokePort(EntityUid uid, SignalTransmitterComponent component, InvokePortEvent args)
        {
            foreach (var receiver in component.Outputs[args.Port])
                RaiseLocalEvent(receiver.Uid, new SignalReceivedEvent(receiver.Port), false);
        }

        private void OnTransmitterStartup(EntityUid uid, SignalTransmitterComponent component, ComponentStartup args)
        {
            // validate links and give receivers a reference to their linked transmitter(s)
            foreach (var (transmitterPort, receivers) in component.Outputs)
                foreach (var receiver in receivers)
                    if (!TryComp(receiver.Uid, out SignalReceiverComponent? receiverComponent) ||
                        !receiverComponent.Inputs.TryGetValue(receiver.Port, out var transmitters))
                        receivers.Remove(receiver); // TODO log error
                    else if (!transmitters.Contains(new() { Uid = uid, Port = transmitterPort }))
                        receivers.Add(new() { Uid = uid, Port = transmitterPort });
        }

        private void OnTransmitterRemoved(EntityUid uid, SignalTransmitterComponent component, ComponentRemove args)
        {
            foreach (var (transmitterPort, receivers) in component.Outputs)
                foreach (var receiver in receivers)
                    if (TryComp(receiver.Uid, out SignalReceiverComponent? receiverComponent) &&
                        receiverComponent.Inputs.TryGetValue(receiver.Port, out var transmitters))
                        transmitters.Remove(new() { Uid = uid, Port = transmitterPort });
        }

        private void OnReceiverRemoved(EntityUid uid, SignalReceiverComponent component, ComponentRemove args)
        {
            foreach (var (receiverPort, transmitters) in component.Inputs)
                foreach (var transmitter in transmitters)
                    if (TryComp(transmitter.Uid, out SignalTransmitterComponent? transmitterComponent) &&
                        transmitterComponent.Outputs.TryGetValue(transmitter.Port, out var receivers))
                        receivers.Remove(new() { Uid = uid, Port = receiverPort });
        }

        private void OnTransmitterInteractUsing(EntityUid uid, SignalTransmitterComponent transmitter, InteractUsingEvent args)
        {
            if (args.Handled) return;

            if (!TryComp(args.Used, out SignalLinkerComponent? linker) ||
                !TryComp(args.User, out ActorComponent? actor))
                return;

            linker.savedTransmitter = uid;

            if (!TryComp(linker.savedReceiver, out SignalReceiverComponent? receiver))
            {
                _popupSystem.PopupCursor(Loc.GetString("signal-linker-component-saved", ("machine", uid)),
                    Filter.Entities(args.User));
                args.Handled = true;
                return;
            }

            if (TryOpenUI(actor, linker, out var bui))
            {
                TryUpdateUI(linker, transmitter, receiver, bui);
                args.Handled = true;
                return;
            }
        }

        private void OnReceiverInteractUsing(EntityUid uid, SignalReceiverComponent receiver, InteractUsingEvent args)
        {
            if (args.Handled) return;

            if (!TryComp(args.Used, out SignalLinkerComponent? linker) ||
                !TryComp(args.User, out ActorComponent? actor))
                return;

            linker.savedReceiver = uid;

            if (!TryComp(linker.savedTransmitter, out SignalTransmitterComponent? transmitter))
            {
                _popupSystem.PopupCursor(Loc.GetString("signal-linker-component-saved", ("machine", uid)),
                    Filter.Entities(args.User));
                args.Handled = true;
                return;
            }

            if (TryOpenUI(actor, linker, out var bui))
            {
                TryUpdateUI(linker, transmitter, receiver, bui);
                args.Handled = true;
                return;
            }
        }

        private bool TryOpenUI(ActorComponent actor, SignalLinkerComponent linker, [NotNullWhen(true)] out BoundUserInterface? bui)
        {
            if (_userInterfaceSystem.TryGetUi(linker.Owner, SignalLinkerUiKey.Key, out bui))
                return bui.Open(actor.PlayerSession);
            return false;
        }

        private bool TryUpdateUI(SignalLinkerComponent linker, SignalTransmitterComponent transmitter, SignalReceiverComponent receiver, BoundUserInterface? bui = null)
        {
            if (bui == null && !_userInterfaceSystem.TryGetUi(linker.Owner, SignalLinkerUiKey.Key, out bui))
                return false;

            var outKeys = transmitter.Outputs.Keys.ToList();
            var inKeys = receiver.Inputs.Keys.ToList();
            // TODO this could probably be rewritten nicely with linq
            List<(int, int)> links = new();
            foreach (var (ok, i) in outKeys.Select((s, i) => (s, i)))
                foreach (var re in transmitter.Outputs[ok])
                    if (re.Uid == receiver.Owner)
                        links.Add((i, inKeys.IndexOf(re.Port)));

            bui.SetState(new SignalPortsState($"{Name(transmitter.Owner)} ({transmitter.Owner})", outKeys,
                $"{Name(receiver.Owner)} ({receiver.Owner})", inKeys, links));
            return true;

        }

        private bool TryLink(SignalTransmitterComponent transmitter, SignalReceiverComponent receiver, SignalPortSelected args, EntityUid? popupUid = null)
        {
            if (!transmitter.Outputs.TryGetValue(args.TransmitterPort, out var receivers) ||
                !receiver.Inputs.TryGetValue(args.ReceiverPort, out var transmitters))
                return false;

            if (!IsInRange(transmitter, receiver))
            {
                if (popupUid.HasValue)
                    _popupSystem.PopupCursor(Loc.GetString("signal-linker-component-out-of-range"),
                        Filter.Entities(popupUid.Value));
                return false;
            }

            // allow other systems to refuse the connection
            var linkAttempt = new LinkAttemptEvent(transmitter, args.TransmitterPort, receiver, args.ReceiverPort);
            RaiseLocalEvent(transmitter.Owner, linkAttempt);
            if (linkAttempt.Cancelled)
            {
                if (popupUid.HasValue)
                    _popupSystem.PopupCursor(Loc.GetString("signal-linker-component-connection-refused", ("machine", transmitter.Owner)),
                        Filter.Entities(popupUid.Value));
                return false;
            }
            RaiseLocalEvent(receiver.Owner, linkAttempt);
            if (linkAttempt.Cancelled)
            {
                if (popupUid.HasValue)
                    _popupSystem.PopupCursor(Loc.GetString("signal-linker-component-connection-refused", ("machine", receiver.Owner)),
                        Filter.Entities(popupUid.Value));
                return false;
            }

            receivers.Add(new() { Uid = receiver.Owner, Port = args.ReceiverPort });
            transmitters.Add(new() { Uid = transmitter.Owner, Port = args.TransmitterPort });
            if (popupUid.HasValue)
                _popupSystem.PopupCursor(Loc.GetString("signal-linker-component-linked-port",
                    ("machine1", transmitter.Owner), ("port1", args.TransmitterPort),
                    ("machine2", receiver.Owner), ("port2", args.ReceiverPort)),
                    Filter.Entities(popupUid.Value));

            return true;
        }

        private void OnSignalPortSelected(EntityUid uid, SignalLinkerComponent linker, SignalPortSelected args)
        {
            if (!TryComp(linker.savedTransmitter, out SignalTransmitterComponent? transmitter) ||
                !TryComp(linker.savedReceiver, out SignalReceiverComponent? receiver) ||
                !transmitter.Outputs.TryGetValue(args.TransmitterPort, out var receivers) ||
                !receiver.Inputs.TryGetValue(args.ReceiverPort, out var transmitters))
                return;

            if (args.Session.AttachedEntity is not EntityUid attached || attached == default ||
                !TryComp(attached, out ActorComponent? actor))
                return;

            if (receivers.Contains(new() { Uid = receiver.Owner, Port = args.ReceiverPort }) ||
                transmitters.Contains(new() { Uid = transmitter.Owner, Port = args.TransmitterPort }))
            { // link already exists, remove it
                if (receivers.Remove(new() { Uid = receiver.Owner, Port = args.ReceiverPort }) &&
                    transmitters.Remove(new() { Uid = transmitter.Owner, Port = args.TransmitterPort }))
                {
                    RaiseLocalEvent(receiver.Owner, new PortDisconnectedEvent(args.ReceiverPort));
                    RaiseLocalEvent(transmitter.Owner, new PortDisconnectedEvent(args.TransmitterPort));
                    _popupSystem.PopupCursor(Loc.GetString("signal-linker-component-unlinked-port",
                        ("machine1", transmitter.Owner), ("port1", args.TransmitterPort),
                        ("machine2", receiver.Owner), ("port2", args.ReceiverPort)),
                        Filter.Entities(attached));
                }
                else
                { // something weird happened
                  // TODO log error
                }
            }
            else
            {
                TryLink(transmitter, receiver, args, attached);
            }

            TryUpdateUI(linker, transmitter, receiver);
        }

        private void OnLinkerClearSelected(EntityUid uid, SignalLinkerComponent linker, LinkerClearSelected args)
        {
            if (!TryComp(linker.savedTransmitter, out SignalTransmitterComponent? transmitter) ||
                !TryComp(linker.savedReceiver, out SignalReceiverComponent? receiver))
                return;

            foreach (var (port, receivers) in transmitter.Outputs)
                if (receivers.RemoveAll(id => id.Uid == receiver.Owner) > 0)
                    RaiseLocalEvent(transmitter.Owner, new PortDisconnectedEvent(port));

            foreach (var (port, transmitters) in receiver.Inputs)
                if (transmitters.RemoveAll(id => id.Uid == transmitter.Owner) > 0)
                    RaiseLocalEvent(receiver.Owner, new PortDisconnectedEvent(port));

            TryUpdateUI(linker, transmitter, receiver);
        }

        private void OnLinkerLinkDefaultSelected(EntityUid uid, SignalLinkerComponent linker, LinkerLinkDefaultSelected args)
        {
            if (!TryComp(linker.savedTransmitter, out SignalTransmitterComponent? transmitter) ||
                !TryComp(linker.savedReceiver, out SignalReceiverComponent? receiver) ||
                args.Session.AttachedEntity is not EntityUid attached || attached == default ||
                !TryComp(attached, out ActorComponent? actor))
                return;

            if (_defaultMappings.TryFirstOrDefault(map => !map.ExceptBy(transmitter.Outputs.Keys, item => item.Item1).Any() &&
                !map.ExceptBy(receiver.Inputs.Keys, item => item.Item2).Any(), out var mapping))
            {
                foreach (var (port, receivers) in transmitter.Outputs)
                    if (receivers.RemoveAll(id => id.Uid == receiver.Owner) > 0)
                        RaiseLocalEvent(transmitter.Owner, new PortDisconnectedEvent(port));

                foreach (var (port, transmitters) in receiver.Inputs)
                    if (transmitters.RemoveAll(id => id.Uid == transmitter.Owner) > 0)
                        RaiseLocalEvent(receiver.Owner, new PortDisconnectedEvent(port));

                foreach (var (t, r) in mapping)
                    TryLink(transmitter, receiver, new(t, r));
            }

            TryUpdateUI(linker, transmitter, receiver);
        }

        private void OnLinkerUIClosed(EntityUid uid, SignalLinkerComponent component, BoundUIClosedEvent args)
        {
            component.savedTransmitter = null;
            component.savedReceiver = null;
        }

        private bool IsInRange(SignalTransmitterComponent transmitterComponent, SignalReceiverComponent receiverComponent)
        {
            if (TryComp(transmitterComponent.Owner, out ApcPowerReceiverComponent? transmitterPowerReceiverComponent) &&
                TryComp(receiverComponent.Owner, out ApcPowerReceiverComponent? receiverPowerReceiverComponent)
                ) // TODO && are they on the same powernet?
                return true;

            return Comp<TransformComponent>(transmitterComponent.Owner).MapPosition.InRange(
                   Comp<TransformComponent>(receiverComponent.Owner).MapPosition, 30f); // TODO should this be a constant?
        }
    }
}
