﻿using Content.Shared.Chemistry.Reagent;
using JetBrains.Annotations;

namespace Content.Server.Chemistry.ReagentEffects.PlantMetabolism
{
    [UsedImplicitly]
    public sealed partial class PlantAdjustPests : PlantAdjustAttribute
    {
        public PlantAdjustPests()
        {
            Attribute = Loc.GetString("plant-attribute-pests");
        }

        public override void Effect(ReagentEffectArgs args)
        {
            if (!CanMetabolize(args.SolutionEntity, out var plantHolderComp, args.EntityManager))
                return;

            plantHolderComp.PestLevel += Amount;
        }
    }
}
