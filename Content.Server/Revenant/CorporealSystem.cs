using Content.Server.Visible;
using Content.Shared.Physics;
using Content.Shared.Revenant;
using Robust.Server.GameObjects;
using Robust.Server.GameStates;
using Robust.Shared.Physics;
using System.Linq;

namespace Content.Server.Revenant;

/// <summary>
/// Makes the revenant solid when the component is applied.
/// Additionally applies a few visual effects.
/// Used for status effect.
/// </summary>
public sealed class CorporealSystem : EntitySystem
{
    [Dependency] private readonly VisibilitySystem _visibilitySystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CorporealComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<CorporealComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnStartup(EntityUid uid, CorporealComponent component, ComponentStartup args)
    {
        if (TryComp<AppearanceComponent>(uid, out var app))
            app.SetData(RevenantVisuals.Corporeal, true);

        var light = EnsureComp<PointLightComponent>(uid);
        light.Color = Color.MediumPurple;
        light.Radius = 1.5f;
        light.Softness = 0.75f;

        if (TryComp<FixturesComponent>(uid, out var fixtures) && fixtures.FixtureCount < 1)
        {
            var fixture = fixtures.Fixtures.Values.First();

            fixture.CollisionMask = (int) (CollisionGroup.SmallMobMask | CollisionGroup.GhostImpassable);
            fixture.CollisionLayer = (int) CollisionGroup.SmallMobLayer;
        }

        if (TryComp<VisibilityComponent>(uid, out var visibility))
        {
            _visibilitySystem.RemoveLayer(visibility, (int) VisibilityFlags.Ghost, false);
            _visibilitySystem.AddLayer(visibility, (int) VisibilityFlags.Normal, false);
            _visibilitySystem.RefreshVisibility(visibility);
        }
        if (TryComp<EyeComponent>(uid, out var eye))
        {
            eye.DrawFov = true;
        }

        Dirty(MetaData(uid));
    }

    private void OnShutdown(EntityUid uid, CorporealComponent component, ComponentShutdown args)
    {
        if (TryComp<AppearanceComponent>(uid, out var app))
            app.SetData(RevenantVisuals.Corporeal, false);

        RemComp<PointLightComponent>(uid);

        if (TryComp<FixturesComponent>(uid, out var fixtures) && fixtures.FixtureCount < 1)
        {
            var fixture = fixtures.Fixtures.Values.First();

            fixture.CollisionMask = (int) CollisionGroup.GhostImpassable;
            fixture.CollisionLayer = 0;
        }

        if (TryComp<VisibilityComponent>(uid, out var visibility))
        {
            _visibilitySystem.AddLayer(visibility, (int) VisibilityFlags.Ghost, false);
            _visibilitySystem.RemoveLayer(visibility, (int) VisibilityFlags.Normal, false);
            _visibilitySystem.RefreshVisibility(visibility);
        }
        if (TryComp<EyeComponent>(uid, out var eye))
        {
            eye.DrawFov = false;
        }

        Dirty(MetaData(uid));
    }
}
