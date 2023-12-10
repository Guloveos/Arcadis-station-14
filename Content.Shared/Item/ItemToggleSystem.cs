using Content.Shared.Interaction.Events;
using Content.Shared.Toggleable;
using Content.Shared.Weapons.Melee;
using Content.Shared.Temperature;
using Content.Shared.Wieldable;
using Content.Shared.Wieldable.Components;
using Robust.Shared.Random;
using Robust.Shared.Audio;

namespace Content.Shared.Item;
/// <summary>
/// Handles generic item toggles, like a welder turning on and off, or an e-sword.
/// </summary>
/// <remarks>
/// If you need extended functionality (e.g. requiring power) then add a new component and use events.
/// </remarks>
public sealed class ItemToggleSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ItemToggleComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<ItemToggleComponent, IsHotEvent>(OnIsHotEvent);
        SubscribeLocalEvent<ItemToggleComponent, ItemUnwieldedEvent>(TurnOffonUnwielded);
        SubscribeLocalEvent<ItemToggleComponent, ItemWieldedEvent>(TurnOnonWielded);
    }


    public void Toggle(EntityUid uid, ItemToggleComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.Activated)
        {
            TryDeactivate(uid, component: component);
        }
        else
        {
            TryActivate(uid, component: component);
        }
    }

    public bool TryActivate(EntityUid uid, EntityUid? user = null, ItemToggleComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (component.Activated)
            return true;

        var attempt = new ItemToggleActivateAttemptEvent();

        RaiseLocalEvent(uid, ref attempt);

        if (attempt.Cancelled)
            return false;

        component.Activated = true;
        var ev = new ItemToggleActivatedEvent();
        RaiseLocalEvent(uid, ref ev);

        UpdateItemComponent(uid, component);
        UpdateWeaponComponent(uid, component);
        UpdateAppearance(uid, component);
        Dirty(uid, component);

        //Added in order to supress the client side item from making noise.
        if (uid.Id == GetNetEntity(uid).Id)
        {
            _audio.PlayPredicted(component.ActivateSound, uid, user);
            //Starts the active sound (like humming).
            component.Stream = _audio.PlayPredicted(component.ActiveSound, uid, user, AudioParams.Default.WithLoop(true));
        }
        return true;
    }

    public bool TryDeactivate(EntityUid uid, EntityUid? user = null, ItemToggleComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (!component.Activated)
            return true;

        var attempt = new ItemToggleDeactivateAttemptEvent();

        RaiseLocalEvent(uid, ref attempt);

        if (attempt.Cancelled)
            return false;

        component.Activated = false;
        var ev = new ItemToggleDeactivatedEvent();
        RaiseLocalEvent(uid, ref ev);

        UpdateItemComponent(uid, component);
        UpdateWeaponComponent(uid, component);
        UpdateAppearance(uid, component);
        Dirty(uid, component);

        //Added in order to supress the client side item from making noise.
        if (uid.Id == GetNetEntity(uid).Id)
        {
            _audio.PlayPredicted(component.DeactivateSound, uid, user);
            //Stops the active sound (like humming).
            component.Stream?.Stop();
            component.Stream = null;
        }
        return true;
    }

    private void OnUseInHand(EntityUid uid, ItemToggleComponent component, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (TryComp<WieldableComponent>(uid, out var wieldableComp))
            return;

        Toggle(uid, component);
    }

    private void TurnOffonUnwielded(EntityUid uid, ItemToggleComponent component, ItemUnwieldedEvent args)
    {
        if (component.Activated)
            TryDeactivate(uid, component: component);
    }

    private void TurnOnonWielded(EntityUid uid, ItemToggleComponent component, ref ItemWieldedEvent args)
    {
        if (!component.Activated)
            TryActivate(uid, component: component);
    }


    /// <summary>
    /// Used to update item appearance.
    /// </summary>
    private void UpdateAppearance(EntityUid uid, ItemToggleComponent component)
    {
        if (!TryComp(uid, out AppearanceComponent? appearanceComponent))
            return;

        _appearance.SetData(uid, ToggleableLightVisuals.Enabled, component.Activated, appearanceComponent);
        _appearance.SetData(uid, ToggleVisuals.Toggled, component.Activated, appearanceComponent);
    }

    /// <summary>
    /// Used to update weapon component aspects, like hit sounds and damage values.
    /// </summary>
    private void UpdateWeaponComponent(EntityUid uid, ItemToggleComponent component)
    {
        if (!TryComp(uid, out MeleeWeaponComponent? weaponComp))
            return;

        if (component.Activated)
        {
            weaponComp.HitSound = component.ActivatedSoundOnHit;
            weaponComp.SwingSound = component.ActivatedSoundOnSwing;
            weaponComp.Damage = component.ActivatedDamage;

            if (component.DeactivatedSecret)
                weaponComp.Hidden = false;
        }
        else
        {
            weaponComp.HitSound = component.DeactivatedSoundOnHit;
            weaponComp.SwingSound = component.DeactivatedSoundOnSwing;
            weaponComp.Damage = component.DeactivatedDamage;

            if (component.DeactivatedSecret)
                weaponComp.Hidden = true;
        }
        Dirty(uid, weaponComp);
    }

    /// <summary>
    /// Used to update item component aspects, like size values for items that expand when activated (heh).
    /// </summary>
    private void UpdateItemComponent(EntityUid uid, ItemToggleComponent component)
    {
        if (!TryComp(uid, out ItemComponent? item))
            return;

        if (component.Activated)
            _item.SetSize(uid, item.Size + component.ActivatedSizeModifier, item);
        else
            _item.SetSize(uid, item.Size - component.ActivatedSizeModifier, item);
    }

    /// <summary>
    /// Used to make the item hot when activated.
    /// </summary>
    private void OnIsHotEvent(EntityUid uid, ItemToggleComponent component, IsHotEvent args)
    {
        if (component.IsHotWhenActivated)
            args.IsHot = component.Activated;
    }
}
