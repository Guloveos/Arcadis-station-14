using Content.Shared.Chemistry.Reagent;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.ReagentEffectConditions
{
    public sealed partial class Satiation : ReagentEffectCondition
    {
        [DataField]
        public float Max = float.PositiveInfinity;

        [DataField]
        public float Min = 0;

        [DataField]
        public ProtoId<SatiationTypePrototype> SatiationType = "hungerSatiation";

        public override bool Condition(ReagentEffectArgs args)
        {
            if (!args.EntityManager.TryGetComponent(args.SolutionEntity, out SatiationComponent? component))
                return false;

            if (!component.Satiations.AsReadOnly().TryGetValue(SatiationType, out var satiation))
                return false;

            var total = satiation.Current;
            return total > Min && total < Max;
        }

        public override string GuidebookExplanation(IPrototypeManager prototype)
        {
            return Loc.GetString("reagent-effect-condition-guidebook-total-satiation",
                ("max", float.IsPositiveInfinity(Max) ? (float) int.MaxValue : Max),
                ("min", Min),
                ("type", Loc.GetString($"satiation-type-{SatiationType}")));
        }
    }
}
