using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Prototypes;
using Content.Shared.Actions.ActionTypes;
using Robust.Shared.Utility;

namespace Content.Shared.Abilities.Mime
{
    /// <summary>
    /// Lets its owner entity use mime powers, like placing invisible walls.
    /// </summary>
    [RegisterComponent]
    public sealed class MimePowersComponent : Component
    {
        /// <summary>
        /// Whether this component is active or not.
        /// </summarY>
        [ViewVariables]
        [DataField("enabled")]
        public bool Enabled = true;

        [DataField("invisibleWallAction")]
        public InstantAction InvisibleWallAction = new()
        {
            Name = "mime-invisible-wall",
            Description = "mime-invisible-wall-desc",
            Event = new InvisibleWallActionEvent(),
        };
    }
}
