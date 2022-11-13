using System.Runtime.CompilerServices;
using Content.Server.Atmos.Components;
using Content.Shared.Atmos;
using Content.Shared.Maps;
using Robust.Shared.Map;

namespace Content.Server.Atmos.EntitySystems;

public partial class AtmosphereSystem
{
    /// <summary>
    /// Gets the particular price of an air mixture.
    /// </summary>
    public double GetPrice(GasMixture mixture)
    {
        float basePrice = 0; // moles of gas * price/mole
        float totalMoles = 0; // total number of moles in can
        float maxComponent = 0; // moles of the dominant gas
        for (var i = 0; i < Atmospherics.TotalNumberOfGases; i++)
        {
            basePrice += mixture.Moles[i] * GetGas(i).PricePerMole;
            totalMoles += mixture.Moles[i];
            maxComponent = Math.Max(maxComponent, mixture.Moles[i]);
        }

        // Pay more for gas canisters that are more pure
        float purity = 1;
        if (totalMoles > 0) {
            purity = maxComponent / totalMoles;
        }

        return basePrice * purity;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void InvalidateVisuals(EntityUid gridUid, Vector2i tile)
    {
        _gasTileOverlaySystem.Invalidate(gridUid, tile);
    }

    public bool NeedsVacuumFixing(IMapGrid mapGrid, Vector2i indices)
    {
        var value = false;

        var enumerator = GetObstructingComponentsEnumerator(mapGrid, indices);

        while (enumerator.MoveNext(out var airtight))
        {
            value |= airtight.FixVacuum;
        }

        return value;
    }

    /// <summary>
    ///     Gets the volume in liters for a number of tiles, on a specific grid.
    /// </summary>
    /// <param name="mapGrid">The grid in question.</param>
    /// <param name="tiles">The amount of tiles.</param>
    /// <returns>The volume in liters that the tiles occupy.</returns>
    private float GetVolumeForTiles(IMapGrid mapGrid, int tiles = 1)
    {
        return Atmospherics.CellVolume * mapGrid.TileSize * tiles;
    }

    /// <summary>
    ///     Gets all obstructing <see cref="AirtightComponent"/> instances in a specific tile.
    /// </summary>
    /// <param name="mapGrid">The grid where to get the tile.</param>
    /// <param name="tile">The indices of the tile.</param>
    /// <returns>The enumerator for the airtight components.</returns>
    public AtmosObstructionEnumerator GetObstructingComponentsEnumerator(IMapGrid mapGrid, Vector2i tile)
    {
        var ancEnumerator = mapGrid.GetAnchoredEntitiesEnumerator(tile);
        var airQuery = GetEntityQuery<AirtightComponent>();

        var enumerator = new AtmosObstructionEnumerator(ancEnumerator, airQuery);
        return enumerator;
    }

    private AtmosDirection GetBlockedDirections(IMapGrid mapGrid, Vector2i indices)
    {
        var value = AtmosDirection.Invalid;

        var enumerator = GetObstructingComponentsEnumerator(mapGrid, indices);

        while (enumerator.MoveNext(out var airtight))
        {
            if(airtight.AirBlocked)
                value |= airtight.AirBlockedDirection;
        }

        return value;
    }

    /// <summary>
    ///     Pries a tile in a grid.
    /// </summary>
    /// <param name="mapGrid">The grid in question.</param>
    /// <param name="tile">The indices of the tile.</param>
    private void PryTile(IMapGrid mapGrid, Vector2i tile)
    {
        if (!mapGrid.TryGetTileRef(tile, out var tileRef))
            return;

        tileRef.TryDeconstructWithToolQuality("Prying", _mapManager, _tileDefinitionManager, EntityManager, _robustRandom);
    }
}
