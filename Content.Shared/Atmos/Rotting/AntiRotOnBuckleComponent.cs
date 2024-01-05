using Robust.Shared.GameStates;

namespace Content.Shared.Atmos.Rotting;

/// <summary>
/// Perishable entities buckled to an entity with this component will stop rotting.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class AntiRotOnBuckleComponent : Component
{
    /// <summary>
    /// Does this component require power to function
    /// </summary>
    [DataField("requiresPower"), ViewVariables(VVAccess.ReadWrite)]
    public bool RequiresPower = true;

    [ViewVariables(VVAccess.ReadWrite)]
    public bool Enabled { get; set; } = true;
}
