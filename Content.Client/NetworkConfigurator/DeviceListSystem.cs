using System.Linq;
using Content.Shared.DeviceNetwork;
using Robust.Client.Graphics;

namespace Content.Client.NetworkConfigurator;

public sealed class DeviceListSystem : SharedDeviceListSystem
{
    [Dependency] private readonly IOverlayManager _overlay = default!;

    /// <summary>
    /// Toggles the given device lists connection visualisation on and off.
    /// TODO: Implement an overlay that draws a line between the given entity and the entities in the device list
    /// </summary>
    public void ToggleVisualization(EntityUid uid, bool toggle, NetworkConfiguratorComponent? component = null)
    {
        if (!Resolve(uid, ref component) || component.ActiveDeviceList == null)
            return;

        if (!toggle)
        {
            RemComp<NetworkConfiguratorActiveLinkOverlayComponent>(component.ActiveDeviceList.Value);
            if (!EntityQuery<NetworkConfiguratorActiveLinkOverlayComponent>().Any())
            {
                _overlay.RemoveOverlay<NetworkConfiguratorLinkOverlay>();
            }

            return;
        }

        if (!_overlay.HasOverlay<NetworkConfiguratorLinkOverlay>())
        {
            _overlay.AddOverlay(new NetworkConfiguratorLinkOverlay());
        }

        EnsureComp<NetworkConfiguratorActiveLinkOverlayComponent>(component.ActiveDeviceList.Value);
    }

    public void ClearAllOverlays()
    {
        if (!_overlay.HasOverlay<NetworkConfiguratorLinkOverlay>())
        {
            return;
        }

        foreach (var tracker in EntityQuery<NetworkConfiguratorActiveLinkOverlayComponent>())
        {
            RemCompDeferred<NetworkConfiguratorActiveLinkOverlayComponent>(tracker.Owner);
        }

        _overlay.RemoveOverlay<NetworkConfiguratorLinkOverlay>();
    }
}
