using Content.Server.Emp;
using Content.Server.Radio.Components;
using Content.Shared.Chat.V2;
using Content.Shared.Chat.V2.Components;
using Content.Shared.Inventory.Events;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;
using Content.Shared.Radio.EntitySystems;
using Robust.Shared.Player;

namespace Content.Server.Radio.EntitySystems;

/// <summary>
/// Manages the transmission of intrinsic and headset radio messages to listeners.
/// </summary>
public sealed class RadioSystem : SharedHeadsetSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HeadsetComponent, EncryptionChannelsChangedEvent>(OnKeysChanged);
        SubscribeLocalEvent<HeadsetComponent, EmpPulseEvent>(OnEmpPulse);

        SubscribeLocalEvent<HeadsetComponent, EntityRadioedEvent>(OnHeadsetReceive);
        SubscribeLocalEvent<InternalRadioComponent, EntityRadioedEvent>(OnInternalRadioReceive);
    }

    private static void OnEmpPulse(EntityUid uid, HeadsetComponent component, ref EmpPulseEvent args)
    {
        if (!component.Enabled)
            return;

        args.Affected = true;
        args.Disabled = true;
    }

    private void OnHeadsetReceive(EntityUid uid, HeadsetComponent headset, EntityRadioedEvent ev)
    {
        if (!TryComp<ActorComponent>(headset.CurrentlyWornBy, out var actor))
            return;

        RaiseNetworkEvent(ev, actor.PlayerSession);
    }

    private void OnInternalRadioReceive(EntityUid uid, InternalRadioComponent _, ref EntityRadioedEvent ev)
    {
        if (!TryComp<ActorComponent>(uid, out var actor))
            return;

        RaiseNetworkEvent(ev, actor.PlayerSession);
    }

    private void OnKeysChanged(EntityUid uid, HeadsetComponent component, EncryptionChannelsChangedEvent args)
    {
        UpdateHeadsetRadioChannels(uid, component, args.Component);
    }

    protected override void OnGotEquipped(EntityUid uid, HeadsetComponent component, GotEquippedEvent args)
    {
        base.OnGotEquipped(uid, component, args);

        if (component.CurrentlyWornBy == null || !component.Enabled)
            return;

        UpdateHeadsetRadioChannels(uid, component);

        EnsureComp<HeadsetRadioableComponent>(args.Equipee).Channels = component.ChannelNames;
    }

    protected override void OnGotUnequipped(EntityUid uid, HeadsetComponent component, GotUnequippedEvent args)
    {
        base.OnGotUnequipped(uid, component, args);

        RemComp<HeadsetRadioableComponent>(args.Equipee);
    }

    public void SetEnabled(EntityUid uid, bool isEnabled, HeadsetComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.Enabled == isEnabled)
            return;

        if (!isEnabled)
        {
            if (component.CurrentlyWornBy != null)
                RemComp<HeadsetRadioableComponent>(component.CurrentlyWornBy.Value);

            return;
        }

        if (component.CurrentlyWornBy == null)
            return;

        UpdateHeadsetRadioChannels(uid, component);
    }

    private void UpdateHeadsetRadioChannels(EntityUid uid, HeadsetComponent headset, EncryptionKeyHolderComponent? keyHolder = null)
    {
        // make sure to not add Radioable when headset is being deleted
        if (!headset.Enabled || MetaData(uid).EntityLifeStage >= EntityLifeStage.Terminating)
            return;

        if (!Resolve(uid, ref keyHolder))
            return;

        headset.ChannelNames = new HashSet<string>(keyHolder.Channels);
    }
}
