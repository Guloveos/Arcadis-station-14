﻿using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Roles;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SubvertedSiliconRoleComponent : Component, IAntagonistRoleComponent
{
    [DataField, AutoNetworkedField]
    public ProtoId<AntagPrototype>? PrototypeId { get; set; }

    [DataField, AutoNetworkedField]
    public LocId? Briefing { get; set; }
}
