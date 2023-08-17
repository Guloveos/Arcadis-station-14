using Content.Shared.Access.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Shared.Access.Components
{
    /// <summary>
    ///     Simple mutable access provider found on ID cards and such.
    /// </summary>
    [RegisterComponent, NetworkedComponent]
    [Access(typeof(SharedAccessSystem))]
    public sealed partial class AccessComponent : Component
    {
        [DataField("tags", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<AccessLevelPrototype>))]
        [Access(typeof(SharedAccessSystem), Other = AccessPermissions.ReadExecute)] // FIXME Friends
        public HashSet<string> Tags = new();

        /// <summary>
        ///     Access Groups. These are added to the tags during map init. After map init this will have no effect.
        /// </summary>
        [DataField("groups", readOnly: true, customTypeSerializer: typeof(PrototypeIdHashSetSerializer<AccessGroupPrototype>))]
        public HashSet<string> Groups { get; private set; } = new();
    }

    /// <summary>
    /// Event raised on an entity to find additional entities which provide access.
    /// </summary>
    [ByRefEvent]
    public struct GetAdditionalAccessEvent
    {
        public HashSet<EntityUid> Entities = new();

        public GetAdditionalAccessEvent()
        {
        }
    }
}
