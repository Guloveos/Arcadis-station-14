﻿using Content.Server.Administration;
using Content.Server.Eui;
using Content.Server.GameObjects.Components.GUI;
using Content.Shared.Administration;
using Content.Shared.Chemistry;
using Content.Shared.Eui;
using Content.Shared.GameObjects.Components.Chemistry;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Content.Shared.GameObjects.Verbs;
using Robust.Server.Interfaces.GameObjects;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

#nullable enable

namespace Content.Server.GameObjects.Components.Chemistry
{
    /// <summary>
    ///     Transfers solution from the held container to the target container.
    /// </summary>
    [GlobalVerb]
    internal sealed class SolutionFillTargetVerb : GlobalVerb
    {
        public override void GetData(IEntity user, IEntity target, VerbData data)
        {
            if (!target.TryGetComponent(out ISolutionInteractionsComponent? targetSolution) ||
                !ActionBlockerSystem.CanInteract(user) ||
                !user.TryGetComponent<HandsComponent>(out var hands) ||
                hands.GetActiveHand == null ||
                hands.GetActiveHand.Owner == target ||
                !hands.GetActiveHand.Owner.TryGetComponent(out ISolutionInteractionsComponent? sourceSolution) ||
                !sourceSolution.CanDrain ||
                !targetSolution.CanRefill)
            {
                data.Visibility = VerbVisibility.Invisible;
                return;
            }

            var heldEntityName = hands.GetActiveHand.Owner.Prototype?.Name ?? Loc.GetString("<Item>");
            var myName = target.Prototype?.Name ?? Loc.GetString("<Item>");

            var locHeldEntityName = Loc.GetString(heldEntityName);
            var locMyName = Loc.GetString(myName);

            data.Visibility = VerbVisibility.Visible;
            data.Text = Loc.GetString("Transfer liquid from [{0}] to [{1}].", locHeldEntityName, locMyName);
        }

        public override void Activate(IEntity user, IEntity target)
        {
            if (!user.TryGetComponent<HandsComponent>(out var hands) || hands.GetActiveHand == null)
            {
                return;
            }

            if (!hands.GetActiveHand.Owner.TryGetComponent(out ISolutionInteractionsComponent? handSolutionComp) ||
                !handSolutionComp.CanDrain ||
                !target.TryGetComponent(out ISolutionInteractionsComponent? targetComp) ||
                !targetComp.CanRefill)
            {
                return;
            }

            var transferQuantity = ReagentUnit.Min(
                targetComp.RefillSpaceAvailable,
                handSolutionComp.DrainAvailable,
                ReagentUnit.New(10));

            if (transferQuantity <= 0)
            {
                return;
            }

            var transferSolution = handSolutionComp.Drain(transferQuantity);
            targetComp.Refill(transferSolution);
        }
    }

    /// <summary>
    ///     Transfers solution from a target container to the held container.
    /// </summary>
    [GlobalVerb]
    internal sealed class SolutionDrainTargetVerb : GlobalVerb
    {
        public override void GetData(IEntity user, IEntity target, VerbData data)
        {
            if (!target.TryGetComponent(out ISolutionInteractionsComponent? sourceSolution) ||
                !ActionBlockerSystem.CanInteract(user) ||
                !user.TryGetComponent<HandsComponent>(out var hands) ||
                hands.GetActiveHand == null ||
                hands.GetActiveHand.Owner == target ||
                !hands.GetActiveHand.Owner.TryGetComponent(out ISolutionInteractionsComponent? targetSolution) ||
                !sourceSolution.CanDrain ||
                !targetSolution.CanRefill)
            {
                data.Visibility = VerbVisibility.Invisible;
                return;
            }

            var heldEntityName = hands.GetActiveHand.Owner.Prototype?.Name ?? Loc.GetString("<Item>");
            var myName = target.Prototype?.Name ?? Loc.GetString("<Item>");

            var locHeldEntityName = heldEntityName;
            var locMyName = myName;

            data.Visibility = VerbVisibility.Visible;
            data.Text = Loc.GetString("Transfer liquid from [{0}] to [{1}].", locMyName, locHeldEntityName);
        }

        public override void Activate(IEntity user, IEntity target)
        {
            if (!user.TryGetComponent<HandsComponent>(out var hands) || hands.GetActiveHand == null)
            {
                return;
            }

            if (!hands.GetActiveHand.Owner.TryGetComponent(out ISolutionInteractionsComponent? targetComp) ||
                !targetComp.CanRefill ||
                !target.TryGetComponent(out ISolutionInteractionsComponent? sourceComp) ||
                !sourceComp.CanDrain)
            {
                return;
            }

            var transferQuantity = ReagentUnit.Min(
                targetComp.RefillSpaceAvailable,
                sourceComp.DrainAvailable,
                ReagentUnit.New(10));

            if (transferQuantity <= 0)
            {
                return;
            }

            var transferSolution = sourceComp.Drain(transferQuantity);
            targetComp.Refill(transferSolution);
        }
    }

    [GlobalVerb]
    internal sealed class AdminAddReagentVerb : GlobalVerb
    {
        public override bool RequireInteractionRange => false;
        public override bool BlockedByContainers => false;

        private const AdminFlags ReqFlags = AdminFlags.Fun;

        private static void OpenAddReagentMenu(IPlayerSession player, IEntity target)
        {
            var euiMgr = IoCManager.Resolve<EuiManager>();
            euiMgr.OpenEui(new AdminAddReagentEui(target), player);
        }

        public override void GetData(IEntity user, IEntity target, VerbData data)
        {
            // ISolutionInteractionsComponent doesn't exactly have an interface for "admin tries to refill this", so...
            // Still have a path for SolutionContainerComponent in case it doesn't allow direct refilling.
            if (!target.HasComponent<SolutionContainerComponent>()
                && !(target.TryGetComponent(out ISolutionInteractionsComponent? interactions)
                     && interactions.CanInject))
            {
                data.Visibility = VerbVisibility.Invisible;
                return;
            }

            data.Text = Loc.GetString("Add Reagent...");
            data.CategoryData = VerbCategories.Debug;
            data.Visibility = VerbVisibility.Invisible;

            var adminManager = IoCManager.Resolve<IAdminManager>();

            if (user.TryGetComponent<IActorComponent>(out var player))
            {
                if (adminManager.HasAdminFlag(player.playerSession, ReqFlags))
                {
                    data.Visibility = VerbVisibility.Visible;
                }
            }
        }

        public override void Activate(IEntity user, IEntity target)
        {
            var groupController = IoCManager.Resolve<IAdminManager>();
            if (user.TryGetComponent<IActorComponent>(out var player))
            {
                if (groupController.HasAdminFlag(player.playerSession, ReqFlags))
                    OpenAddReagentMenu(player.playerSession, target);
            }
        }

        private sealed class AdminAddReagentEui : BaseEui
        {
            private readonly IEntity _target;
            [Dependency] private readonly IAdminManager _adminManager = default!;

            public AdminAddReagentEui(IEntity target)
            {
                _target = target;

                IoCManager.InjectDependencies(this);
            }

            public override void Opened()
            {
                StateDirty();
            }

            public override EuiStateBase GetNewState()
            {
                if (_target.TryGetComponent(out SolutionContainerComponent? container))
                {
                    return new AdminAddReagentEuiState
                    {
                        CurVolume = container.CurrentVolume,
                        MaxVolume = container.MaxVolume
                    };
                }

                if (_target.TryGetComponent(out ISolutionInteractionsComponent? interactions))
                {
                    return new AdminAddReagentEuiState
                    {
                        // We don't exactly have an absolute total volume so good enough.
                        CurVolume = ReagentUnit.Zero,
                        MaxVolume = interactions.InjectSpaceAvailable
                    };
                }

                return new AdminAddReagentEuiState
                {
                    CurVolume = ReagentUnit.Zero,
                    MaxVolume = ReagentUnit.Zero
                };
            }

            public override void HandleMessage(EuiMessageBase msg)
            {
                switch (msg)
                {
                    case AdminAddReagentEuiMsg.Close:
                        Close();
                        break;
                    case AdminAddReagentEuiMsg.DoAdd doAdd:
                        // Double check that user wasn't de-adminned in the mean time...
                        // Or the target was deleted.
                        if (!_adminManager.HasAdminFlag(Player, ReqFlags) || _target.Deleted)
                        {
                            Close();
                            return;
                        }

                        var id = doAdd.ReagentId;
                        var amount = doAdd.Amount;

                        if (_target.TryGetComponent(out SolutionContainerComponent? container))
                        {
                            container.TryAddReagent(id, amount, out _);
                        }
                        else if (_target.TryGetComponent(out ISolutionInteractionsComponent? interactions))
                        {
                            var solution = new Solution(id, amount);
                            interactions.Inject(solution);
                        }

                        StateDirty();

                        if (doAdd.CloseAfter)
                            Close();

                        break;
                }
            }
        }
    }
}
