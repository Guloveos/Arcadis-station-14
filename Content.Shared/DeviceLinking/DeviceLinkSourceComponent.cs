﻿using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Shared.DeviceLinking;

[RegisterComponent]
[NetworkedComponent] // for interactions. Actual state isn't currently synced.
[Access(typeof(SharedDeviceLinkSystem))]
public sealed partial class DeviceLinkSourceComponent : Component
{
    /// <summary>
    /// The ports the device link source sends signals from
    /// </summary>
    [DataField("ports", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<SourcePortPrototype>))]
    public HashSet<string>? Ports;

    /// <summary>
    /// A list of sink uids that got linked for each port
    /// </summary>
    [ViewVariables]
    public Dictionary<string, HashSet<EntityUid>> Outputs = new();

    /// <summary>
    /// If set to High or Low, the last signal state for a given port.
    /// Used when linking ports of devices that are currently outputting a signal.
    /// Only set by <c>DeviceLinkSystem.SendSignal</c>.
    /// </summary>
    [DataField]
    public Dictionary<string, bool> LastSignals = new();

    /// <summary>
    /// The list of source to sink ports for each linked sink entity for easier managing of links
    /// </summary>
    [DataField("linkedPorts")]
    public Dictionary<EntityUid, HashSet<(string source, string sink)>> LinkedPorts = new();

    /// <summary>
    ///     Limits the range devices can be linked across.
    /// </summary>
    [DataField("range")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float Range = 30f;
}
