using Content.Server.Chemistry.EntitySystems;
using Content.Server.Construction.Components;
using Content.Server.Fluids.Components;
using Content.Shared.Examine;
using Content.Shared.Throwing;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Fluids.EntitySystems;

[UsedImplicitly]
public class SpillableSystem : EntitySystem
{
    [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;
    public override void Initialize() {
        base.Initialize();
        SubscribeLocalEvent<SpillableComponent, LandEvent>(SpillOnLand);
    }
    void SpillOnLand(EntityUid uid, SpillableComponent component, LandEvent args) {
        if (args.User != null && Get<SolutionContainerSystem>().TryGetSolution(uid, component.SolutionName, out var solutionComponent))
        {
            Get<SolutionContainerSystem>()
                .Drain(uid, solutionComponent, solutionComponent.DrainAvailable)
                .SpillAt(EntityManager.GetEntity(uid).Transform.Coordinates, "PuddleSmear");
        }
    }

}
