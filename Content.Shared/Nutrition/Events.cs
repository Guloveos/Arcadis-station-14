﻿using Content.Shared.DoAfter;
using Robust.Shared.Serialization;
using Content.Shared.Chemistry.Components;

namespace Content.Shared.Nutrition;

/// <summary>
///     Do after even for food and drink.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class ConsumeDoAfterEvent : DoAfterEvent
{
    [DataField("solution", required: true)]
    public string Solution { get; private set; } = default!;

    [DataField("flavorMessage", required: true)]
    public string FlavorMessage { get; private set; } = default!;

    private ConsumeDoAfterEvent()
    {
    }

    public ConsumeDoAfterEvent(string solution, string flavorMessage)
    {
        Solution = solution;
        FlavorMessage = flavorMessage;
    }

    public override DoAfterEvent Clone() => this;
}

/// <summary>
///     Do after event for vape.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class VapeDoAfterEvent : DoAfterEvent
{
    [DataField("solution", required: true)]
    public Solution Solution { get; private set; } = default!;

    [DataField("forced", required: true)]
    public bool Forced { get; private set; } = default!;

    private VapeDoAfterEvent()
    {
    }

    public VapeDoAfterEvent(Solution solution, bool forced)
    {
            Solution = solution;
            Forced = forced;
    }

    public override DoAfterEvent Clone() => this;
}
