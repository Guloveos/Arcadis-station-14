using Content.Server.Gatherable.Components;
using Content.Server.Projectiles;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Physics.Events;

namespace Content.Server.Gatherable;

public sealed partial class GatherableSystem
{
    private void InitializeProjectile()
    {
        SubscribeLocalEvent<GatheringProjectileComponent, StartCollideEvent>(OnProjectileCollide);
    }

    private void OnProjectileCollide(EntityUid uid, GatheringProjectileComponent component, ref StartCollideEvent args)
    {
        if (!args.OtherFixture.Hard ||
            args.OurFixture.ID != SharedProjectileSystem.ProjectileFixture ||
            !TryComp<GatherableComponent>(args.OtherEntity, out var gatherable))
        {
            return;
        }

        Gather(args.OtherEntity, gatherable);
        QueueDel(uid);
    }
}
