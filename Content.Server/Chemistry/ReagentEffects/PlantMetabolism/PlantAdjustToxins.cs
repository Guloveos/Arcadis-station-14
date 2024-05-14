﻿using Content.Shared.Chemistry.Reagent;
using JetBrains.Annotations;

namespace Content.Server.Chemistry.ReagentEffects.PlantMetabolism
{
    [UsedImplicitly]
    public sealed partial class PlantAdjustToxins : PlantAdjustAttribute
    {
        public PlantAdjustToxins()
        {
            Attribute = Loc.GetString("plant-attribute-toxins");
        }

        public override void Effect(ReagentEffectArgs args)
        {
            if (!CanMetabolize(args.SolutionEntity, out var plantHolderComp, args.EntityManager))
                return;

            plantHolderComp.Toxins += Amount;
        }
    }
}
