﻿using Content.Shared.Medical.Cryogenics;
using Robust.Client.GameObjects;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Client.Medical.Cryogenics;

public sealed class CryoPodSystem: VisualizerSystem<CryoPodVisualsComponent>
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InsideCryoPodComponent, ComponentAdd>(OnCryoPodInsertion);
        SubscribeLocalEvent<InsideCryoPodComponent, ComponentRemove>(OnCryoPodRemoval);
    }

    private void OnCryoPodInsertion(EntityUid uid, InsideCryoPodComponent component, ComponentAdd args)
    {
        if (!TryComp<SpriteComponent>(uid, out var spriteComponent))
        {
            return;
        }

        component.PreviousOffset = spriteComponent.Offset;
        spriteComponent.Offset = new Vector2(0, 1);
    }

    private void OnCryoPodRemoval(EntityUid uid, InsideCryoPodComponent component, ComponentRemove args)
    {
        if (!TryComp<SpriteComponent>(uid, out var spriteComponent))
        {
            return;
        }

        spriteComponent.Offset = component.PreviousOffset;
    }

    protected override void OnAppearanceChange(EntityUid uid, CryoPodVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
        {
            return;
        }

        if (!args.Component.TryGetData(SharedCryoPodComponent.CryoPodVisuals.ContainsEntity, out bool isOpen)
            || !args.Component.TryGetData(SharedCryoPodComponent.CryoPodVisuals.IsOn, out bool isOn))
        {
            return;
        }

        if (isOpen)
        {
            args.Sprite.LayerSetState(CryoPodVisualLayers.Base, "pod-open");
            args.Sprite.LayerSetVisible(CryoPodVisualLayers.Cover, false);
            args.Sprite.DrawDepth = (int) DrawDepth.Objects;
        }
        else
        {
            args.Sprite.DrawDepth = (int) DrawDepth.Mobs;
            args.Sprite.LayerSetState(CryoPodVisualLayers.Base, isOn ? "pod-on" : "pod-off");
            args.Sprite.LayerSetState(CryoPodVisualLayers.Cover, isOn ? "cover-on" : "cover-off");
            args.Sprite.LayerSetVisible(CryoPodVisualLayers.Cover, true);
        }
    }
}

public enum CryoPodVisualLayers : byte
{
    Base,
    Cover,
}
