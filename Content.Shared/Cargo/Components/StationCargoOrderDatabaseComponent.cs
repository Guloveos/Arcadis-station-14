using Robust.Shared.GameStates;

namespace Content.Shared.Cargo.Components;

/// <summary>
/// Stores all of cargo orders for a particular station.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed class StationCargoOrderDatabaseComponent : Component
{
    /// <summary>
    /// Maximum amount of orders a station is allowed, approved or not.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("capacity")]
    public int Capacity = 50;

    [ViewVariables(VVAccess.ReadWrite), DataField("orders")]
    public Dictionary<int, CargoOrderData> Orders = new();

    /// <summary>
    /// Tracks the next order index available.
    /// </summary>
    public int Index;
}
