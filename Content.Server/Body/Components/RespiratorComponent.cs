using System;
using Content.Server.Body.Systems;
using Content.Shared.Damage;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Body.Components
{
    [RegisterComponent, Friend(typeof(RespiratorSystem))]
    public sealed class RespiratorComponent : Component
    {
        /// <summary>
        ///     Saturation level. Reduced by CycleDelay each tick.
        ///     Can be thought of as 'how many seconds you have until you start suffocating' in this configuration.
        /// </summary>
        [DataField("saturation")]
        public float Saturation = 5.0f;

        /// <summary>
        ///     At what level of saturation will you begin to suffocate?
        /// </summary>
        [DataField("suffocationThreshold")]
        public float SuffocationThreshold;

        [DataField("maxSaturation")]
        public float MaxSaturation = 5.0f;

        [DataField("minSaturation")]
        public float MinSaturation = -2.0f;

        // TODO HYPEROXIA?

        [DataField("damage")]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier Damage = new ();

        [DataField("damageRecovery")]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier DamageRecovery = new ();

        [DataField("gaspPopupCooldown")]
        public TimeSpan GaspPopupCooldown { get; private set; } = TimeSpan.FromSeconds(8);

        [ViewVariables]
        public TimeSpan LastGaspPopupTime;

        /// <summary>
        ///     How many cycles in a row has the mob been under-saturated?
        /// </summary>
        [ViewVariables]
        public int SuffocationCycles = 0;

        /// <summary>
        ///     How many cycles in a row does it take for the suffocation alert to pop up?
        /// </summary>
        [ViewVariables]
        public int SuffocationCycleThreshold = 3;

        [ViewVariables]
        public RespiratorStatus Status = RespiratorStatus.Inhaling;

        [DataField("cycleDelay")]
        public float CycleDelay = 2.0f;

        public float AccumulatedFrametime;
    }
}

public enum RespiratorStatus
{
    Inhaling,
    Exhaling
}
