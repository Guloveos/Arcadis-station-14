﻿using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components
{
    public abstract class SharedExpendableLightComponent: Component
    {
        protected enum LightState
        {
            BrandNew,
            Lit,
            Fading,
            Dead
        }

        public sealed override string Name => "ExpendableLight";
        public sealed override uint? NetID => ContentNetIDs.EXPENDABLE_LIGHT;

        [ViewVariables(VVAccess.ReadOnly)]
        protected LightState CurrentState { get; set; }

        [ViewVariables]
        protected float GlowRadius { get; set; }

        [ViewVariables]
        protected float GlowEnergy { get; set; }

        [ViewVariables]
        protected float GlowDuration { get; set; }

        [ViewVariables]
        protected float FadeOutDuration { get; set; }

        [ViewVariables]
        protected string SpentDesc { get; set; }

        [ViewVariables]
        protected string SpentName { get; set; }

        [ViewVariables]
        protected string IconStateSpent { get; set; }

        [ViewVariables]
        protected string IconStateLit { get; set; }

        [Serializable, NetSerializable]
        protected sealed class ExpendableLightComponentState : ComponentState
        {
            public ExpendableLightComponentState(LightState state, float stateExpiryTime) : base(ContentNetIDs.EXPENDABLE_LIGHT)
            {
                StateExpiryTime = stateExpiryTime;
                State = state;
            }

            public float StateExpiryTime { get; set; }
            public LightState State { get; set; }
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(this, x => GlowRadius, "glowRadius", 3f);
            serializer.DataField(this, x => GlowEnergy, "glowEnergy", 3f);
            serializer.DataField(this, x => GlowDuration, "glowDuration", 60 * 15f);
            serializer.DataField(this, x => FadeOutDuration, "fadeOutDuration", 60 * 5f);
            serializer.DataField(this, x => SpentName, "spentName", string.Empty);
            serializer.DataField(this, x => SpentDesc, "spentDesc", string.Empty);
            serializer.DataField(this, x => IconStateLit, "iconStateOn", string.Empty);
            serializer.DataField(this, x => IconStateSpent, "iconStateSpent", string.Empty);
        }
    }
}
