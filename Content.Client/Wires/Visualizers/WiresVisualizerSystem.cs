using Content.Shared.Wires;
using Robust.Client.GameObjects;

namespace Content.Client.Wires.Visualizers
{
    public sealed class WiresVisualizerSystem : VisualizerSystem<WiresVisualsComponent>
    {
        [Dependency] private readonly AppearanceSystem _appearanceSystem = default!;

        protected override void OnAppearanceChange(EntityUid uid, WiresVisualsComponent component, ref AppearanceChangeEvent args)
        {
            if (args.Sprite == null)
                return;

            var layer = args.Sprite.LayerMapReserveBlank(WiresVisualLayers.MaintenancePanel);

            if (_appearanceSystem.TryGetData(uid, WiresVisuals.MaintenancePanelState, out var panelState, args.Component))
            {
                args.Sprite.LayerSetVisible(layer, (bool) panelState);
            }
            else
            {
                //Mainly for spawn window
                args.Sprite.LayerSetVisible(layer, false);
            }
        }
    }

    public enum WiresVisualLayers : byte
    {
        MaintenancePanel
    }
}
