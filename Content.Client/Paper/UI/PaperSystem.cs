using Robust.Client.GameObjects;

using static Content.Shared.Paper.SharedPaperComponent;

namespace Content.Client.Paper;

public sealed class PaperSystem : VisualizerSystem<PaperVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, PaperVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (args.Component.TryGetData(PaperVisuals.Status, out PaperStatus writingStatus))
        {
            args.Sprite.LayerSetVisible(PaperVisualLayers.Writing, writingStatus == PaperStatus.Written);
            args.Sprite.LayerSetVisible(PaperVisualLayers.Paper, writingStatus != PaperStatus.Plane);
            args.Sprite.LayerSetVisible(PaperVisualLayers.Plane, writingStatus == PaperStatus.Plane);
        }

        if (args.Component.TryGetData(PaperVisuals.Stamp, out string stampState))
        {
            args.Sprite.LayerSetState(PaperVisualLayers.Stamp, stampState);
            args.Sprite.LayerSetVisible(PaperVisualLayers.Stamp, writingStatus != PaperStatus.Plane);
        }
    }
}

public enum PaperVisualLayers
{
    Paper,
    Stamp,
    Writing,
    Plane
}
