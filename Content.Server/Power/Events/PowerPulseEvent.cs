﻿namespace Content.Server.Power.Events;

/// <summary>
///     Invoked on a target entity, when it was pulsed with an energy.
///     For instance, interacted with a multitool or active stun baton.
/// </summary>
public sealed class PowerPulseEvent : EventArgs
{
    public EntityUid? User;
    public EntityUid? Used;
}
