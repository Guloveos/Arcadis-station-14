using Content.Shared.Containers.ItemSlots;
using Content.Shared.Nuke;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Nuke
{
    /// <summary>
    ///     Nuclear device that can devistate an entire station.
    ///     Basicaly a station self-destruction mechanism.
    ///     To activate it, user needs to insert an authorization disk and enter a secret code.
    /// </summary>
    [RegisterComponent]
    [Friend(typeof(NukeSystem))]
    public class NukeComponent : Component
    {
        public override string Name => "Nuke";

        /// <summary>
        ///     Default bomb timer value in seconds.
        ///     Bomb always reset to this when armed.
        /// </summary>
        [DataField("timer")]
        [ViewVariables(VVAccess.ReadWrite)]
        public int Timer = 5;

        /// <summary>
        ///     Slot name for to store nuclear disk inside bomb.
        ///     See <see cref="SharedItemSlotsComponent"/> for mor info.
        /// </summary>
        [DataField("slot")]
        public string DiskSlotName = "DiskSlot";

        /// <summary>
        ///     Annihilation radius in which  all human players will be gibed
        /// </summary>
        [DataField("blastRadius")]
        [ViewVariables(VVAccess.ReadWrite)]
        public int BlastRadius = 200;

        /// <summary>
        ///     Time until explosion in seconds.
        /// </summary>
        [ViewVariables]
        public float RemainingTime;

        /// <summary>
        ///     Does bomb contains valid entity inside <see cref="DiskSlotName"/>?
        ///     If it is, user can anchor bomb or enter nuclear code to arm it.
        /// </summary>
        [ViewVariables]
        public bool DiskInserted = false;

        /// <summary>
        ///     Curent nuclear code buffer. Entered manually by players.
        ///     If valid it will allow arm/disarm bomb.
        /// </summary>
        [ViewVariables]
        public string EnteredCode = "";

        /// <summary>
        ///     Current status of a nuclear bomb.
        /// </summary>
        [ViewVariables]
        public NukeStatus Status = NukeStatus.AWAIT_DISK;
    }
}
