using Content.Server.Botany.Systems;
using Content.Shared.EntityEffects;

namespace Content.Server.EntityEffects.Effects.PlantMetabolism;

public sealed partial class PlantAdjustHealth : PlantAdjustAttribute
{
    public override void Effect(EntityEffectArgs args)
    {
        if (!CanMetabolize(args.TargetEntity, out var plantHolderComp, args.EntityManager))
            return;

        var plantHolder = args.EntityManager.System<PlantHolderSystem>();

        plantHolderComp.Health += Amount;
        plantHolder.CheckHealth(args.TargetEntity, plantHolderComp);
    }
}

