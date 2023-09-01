﻿using Content.Shared.Clothing.Components;
using Content.Shared.Inventory.Events;

namespace Content.Shared.TypingIndicator;

/// <summary>
/// This handles...
/// </summary>
public abstract class SharedTypingIndicatorSystem : EntitySystem
{
    /// <summary>
    ///     Default ID of typing indicator icon <see cref="TypingIndicatorPrototype"/>
    /// </summary>
    [ValidatePrototypeId<TypingIndicatorPrototype>]
    public const string InitialIndicatorId = "Default";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TypingIndicatorClothingComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<TypingIndicatorClothingComponent, GotUnequippedEvent>(OnGotUnequipped);
    }

    private void OnGotEquipped(EntityUid uid, TypingIndicatorClothingComponent component, GotEquippedEvent args)
    {
        if (!TryComp<ClothingComponent>(uid, out var clothing) ||
            !TryComp<TypingIndicatorComponent>(args.Equipee, out var indicator))
            return;

        var isCorrectSlot = clothing.Slots.HasFlag(args.SlotFlags);
        if (!isCorrectSlot)
            return;

        indicator.Prototype = component.Prototype;
        Dirty(uid, indicator);
    }

    private void OnGotUnequipped(EntityUid uid, TypingIndicatorClothingComponent component, GotUnequippedEvent args)
    {
        if (!TryComp<TypingIndicatorComponent>(args.Equipee, out var indicator))
            return;

        indicator.Prototype = InitialIndicatorId;
        Dirty(uid, indicator);
    }
}
