using System;
using System.Collections.Generic;
using Content.Shared.ActionBlocker;
using Content.Shared.NetIDs;
using Robust.Shared.GameObjects;
using Robust.Shared.Players;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Ghost
{
    public class SharedGhostComponent : Component, IActionBlocker
    {
        public override string Name => "Ghost";
        public override uint? NetID => ContentNetIDs.GHOST;

        [ViewVariables(VVAccess.ReadWrite)]
        public bool CanGhostInteract
        {
            get => _canGhostInteract;
            set
            {
                if (_canGhostInteract == value) return;
                _canGhostInteract = value;
                Dirty();
            }
        }

        [DataField("canInteract")]
        private bool _canGhostInteract;

        /// <summary>
        ///     Changed by <see cref="GhostChangeCanReturnToBodyEvent"/>
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public bool CanReturnToBody
        {
            get => _canReturnToBody;
            set
            {
                if (_canReturnToBody == value) return;
                _canReturnToBody = value;
                Dirty();
            }
        }

        [DataField("canReturnToBody")]
        private bool _canReturnToBody;

        public override ComponentState GetComponentState(ICommonSession player)
        {
            return new GhostComponentState(CanReturnToBody, CanGhostInteract);
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (curState is not GhostComponentState state)
            {
                return;
            }

            CanReturnToBody = state.CanReturnToBody;
            CanGhostInteract = state.CanGhostInteract;
        }

        public bool CanInteract() => CanGhostInteract;
        public bool CanUse() => CanGhostInteract;
        public bool CanThrow() => false;
        public bool CanDrop() => false;
        public bool CanPickup() => false;
        public bool CanEmote() => false;
        public bool CanAttack() => false;
    }

    [Serializable, NetSerializable]
    public class GhostComponentState : ComponentState
    {
        public bool CanReturnToBody { get; }
        public bool CanGhostInteract { get; }

        public HashSet<string>? LocationWarps { get; }

        public Dictionary<EntityUid, string>? PlayerWarps { get; }

        public GhostComponentState(
            bool canReturnToBody,
            bool canGhostInteract,
            HashSet<string>? locationWarps = null,
            Dictionary<EntityUid, string>? playerWarps = null)
            : base(ContentNetIDs.GHOST)
        {
            CanReturnToBody = canReturnToBody;
            CanGhostInteract = canGhostInteract;
            LocationWarps = locationWarps;
            PlayerWarps = playerWarps;
        }
    }
}


