using System;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Buckle
{
    public partial class BuckleComponentData
    {
        [CustomYamlField("delay")]
        public TimeSpan? UnbuckleDelay;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            float? seconds = 0.25f;
            serializer.DataField(ref seconds, "cooldown", null);

            UnbuckleDelay = seconds != null ? TimeSpan.FromSeconds((float)seconds) : null;
        }
    }
}
