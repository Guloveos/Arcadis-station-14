﻿namespace Content.Shared.Chemistry.Components.SolutionManager;

[RegisterComponent]
[Obsolete]
public sealed partial class ExaminableSolutionComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("solution")]
    public string Solution { get; set; } = "default";
}
