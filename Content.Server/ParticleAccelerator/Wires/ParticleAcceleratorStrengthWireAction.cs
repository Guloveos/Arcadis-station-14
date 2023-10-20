using Content.Server.ParticleAccelerator.Components;
using Content.Server.ParticleAccelerator.EntitySystems;
using Content.Server.Wires;
using Content.Shared.Singularity.Components;
using Content.Shared.Wires;
using Robust.Server.GameObjects;

namespace Content.Server.ParticleAccelerator.Wires;

public sealed partial class ParticleAcceleratorStrengthWireAction : ComponentWireAction<ParticleAcceleratorControlBoxComponent>
{
    public override string Name { get; set; } = "wire-name-pa-strength";
    public override Color Color { get; set; } = Color.Blue;
    public override object StatusKey { get; } = ParticleAcceleratorWireStatus.Strength;

    public override StatusLightState? GetLightState(Wire wire, ParticleAcceleratorControlBoxComponent component)
    {
        return component.StrengthLocked ? StatusLightState.BlinkingSlow : StatusLightState.On;
    }

    public override bool Cut(EntityUid user, Wire wire, Entity<ParticleAcceleratorControlBoxComponent> controller)
    {
        controller.Comp.StrengthLocked = true;
        return true;
    }

    public override bool Mend(EntityUid user, Wire wire, Entity<ParticleAcceleratorControlBoxComponent> controller)
    {
        controller.Comp.StrengthLocked = false;
        return true;
    }

    public override void Pulse(EntityUid user, Wire wire, Entity<ParticleAcceleratorControlBoxComponent> controller)
    {
        var paSystem = EntityManager.System<ParticleAcceleratorSystem>();
        var userSession = EntityManager.TryGetComponent<ActorComponent>(user, out var actor) ? actor.PlayerSession : null;
        paSystem.SetStrength(wire.Owner, (ParticleAcceleratorPowerState) ((int) controller.Comp.SelectedStrength + 1), userSession, controller);
    }
}
