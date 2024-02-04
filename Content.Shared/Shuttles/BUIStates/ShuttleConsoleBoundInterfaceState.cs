using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.Systems;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Shuttles.BUIStates;

[Serializable, NetSerializable]
public sealed class ShuttleConsoleBoundInterfaceState : RadarConsoleBoundInterfaceState
{
    /// <summary>
    /// The current FTL state.
    /// </summary>
    public readonly FTLState FTLState;

    /// <summary>
    /// How long the FTL state takes.
    /// </summary>
    public float FTLDuration;

    public List<ShuttleBeacon> Destinations;

    public List<ShuttleExclusion> Exclusions;

    public ShuttleConsoleBoundInterfaceState(
        FTLState ftlState,
        float ftlDuration,
        List<ShuttleBeacon> destinations,
        List<ShuttleExclusion> exclusions,
        float maxRange,
        NetCoordinates? coordinates,
        Angle? angle,
        List<DockingInterfaceState> docks) : base(maxRange, coordinates, angle, docks)
    {
        FTLState = ftlState;
        FTLDuration = ftlDuration;
        Destinations = destinations;
        Exclusions = exclusions;
    }
}

[Serializable, NetSerializable]
public readonly record struct ShuttleBeacon(NetEntity Entity, NetCoordinates Coordinates, string Destination);

[Serializable, NetSerializable]
public record struct ShuttleExclusion(NetCoordinates Coordinates, float Range);
