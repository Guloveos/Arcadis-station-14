using Content.Client.SubFloor;
using Content.Shared.SubFloor;
using Content.Shared.Wires;
using Robust.Client.GameObjects;

namespace Content.Client.Power.Visualizers;

public sealed partial class CableVisualizerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CableVisualizerComponent, AppearanceChangeEvent>(OnAppearanceChanged, after: new[] { typeof(SubFloorHideSystem) });
    }

    private void OnAppearanceChanged(EntityUid uid, CableVisualizerComponent component, ref AppearanceChangeEvent args)
    {
        if (!TryComp(uid, out SpriteComponent? sprite))
            return;

        if (args.Component.TryGetData(SubFloorVisuals.Covered, out bool isUnderCover)
            && isUnderCover
            && args.Component.TryGetData(SubFloorVisuals.ScannerRevealed, out bool revealed)
            && !revealed)
        {
            // This entity is below a floor and is not even visible to the user -> don't bother updating sprite data.
            // Note that if the subfloor visuals change, then another AppearanceChangeEvent will get triggered.
            return;
        }

        if (!args.Component.TryGetData(WireVisVisuals.ConnectedMask, out WireVisDirFlags mask))
            mask = WireVisDirFlags.None;

        sprite.LayerSetState(0, $"{component.StateBase}{(int) mask}");
    }

}
