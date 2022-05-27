using Content.Server.Damage.Systems;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Popups;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Mousetrap;
using Content.Shared.StepTrigger;
using Robust.Shared.Player;

namespace Content.Server.Mousetrap;

public sealed class MousetrapSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly TriggerSystem _triggerSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<MousetrapComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<MousetrapComponent, BeforeDamageOnTriggerEvent>(BeforeDamageOnTrigger);
        SubscribeLocalEvent<MousetrapComponent, StepTriggerAttemptEvent>(OnStepTriggerAttempt);
        SubscribeLocalEvent<MousetrapComponent, StepTriggeredEvent>(OnStepTrigger);
    }

    private void OnUseInHand(EntityUid uid, MousetrapComponent component, UseInHandEvent args)
    {
        component.IsActive = !component.IsActive;
        _popupSystem.PopupEntity(component.IsActive
            ? Loc.GetString("mousetrap-on-activate")
            : Loc.GetString("mousetrap-on-deactivate"),
            uid,
            Filter.Entities(args.User));

        UpdateVisuals(uid);
    }

    private void OnStepTriggerAttempt(EntityUid uid, MousetrapComponent component, ref StepTriggerAttemptEvent args)
    {
        args.Continue = component.IsActive;
    }

    private void BeforeDamageOnTrigger(EntityUid uid, MousetrapComponent component, BeforeDamageOnTriggerEvent args)
    {
        foreach (var slot in component.IgnoreDamageIfSlotFilled)
        {
            if (!_inventorySystem.TryGetSlotContainer(args.Tripper, slot, out var container, out _))
            {
                continue;
            }

            // This also means that wearing slippers won't
            // hurt the entity.
            if (container.ContainedEntity != null)
            {
                args.Damage *= 0;
                return;
            }
        }

        if (TryComp(args.Tripper, out PhysicsComponent? physics) && float.IsFinite(physics.Mass))
        {
            // The idea here is inverse,
            // Small - big damage,
            // Large - small damage
            // yes i punched numbers into a calculator until the graph looked right
            var scaledDamage = -50 * Math.Atan(physics.Mass - component.MassBalance) + (25 * Math.PI);
            args.Damage *= scaledDamage;
        }
    }

    private void OnStepTrigger(EntityUid uid, MousetrapComponent component, ref StepTriggeredEvent args)
    {
        component.IsActive = false;
        _triggerSystem.Trigger(uid);

        UpdateVisuals(uid);
    }

    private void UpdateVisuals(EntityUid uid, MousetrapComponent? mousetrap = null, AppearanceComponent? appearance = null)
    {
        if (!Resolve(uid, ref mousetrap, ref appearance, false))
        {
            return;
        }

        appearance.SetData(MousetrapVisuals.Visual,
            mousetrap.IsActive ? MousetrapVisuals.Armed : MousetrapVisuals.Unarmed);
    }
}
