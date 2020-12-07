﻿#nullable enable
using System;
using Content.Server.Administration;
using Content.Server.Commands;
using Content.Shared.Actions;
using Content.Shared.Administration;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Players;

namespace Content.Server.GameObjects.Components.Mobs
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedActionsComponent))]
    public sealed class ServerActionsComponent : SharedActionsComponent
    {
        public override void HandleNetworkMessage(ComponentMessage message, INetChannel netChannel, ICommonSession? session = null)
        {
            base.HandleNetworkMessage(message, netChannel, session);

            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            if (message is PerformActionMessage performMsg)
            {
                HandlePerformActionMessage(performMsg, session);
            }
            else if (message is PerformItemActionMessage performItemActionMsg)
            {
                HandlePerformItemActionMessage(performItemActionMsg, session);
            }
        }

        private void HandlePerformActionMessage(PerformActionMessage performMsg, ICommonSession session)
        {
            var player = session.AttachedEntity;
            if (player != Owner) return;

            if (!TryGetActionState(performMsg.ActionType, out var actionState) || !actionState.Enabled)
            {
                Logger.DebugS("action", "user {0} attempted to use" +
                                        " action {1} which is not granted to them", player.Name,
                    performMsg.ActionType);
                return;
            }

            if (actionState.IsOnCooldown(GameTiming))
            {
                Logger.DebugS("action", "user {0} attempted to use" +
                                        " action {1} which is on cooldown", player.Name,
                    performMsg.ActionType);
                return;
            }

            if (!ActionManager.TryGet(performMsg.ActionType, out var action))
            {
                Logger.DebugS("action", "user {0} attempted to" +
                                        " perform unrecognized action {1}", player.Name,
                    performMsg.ActionType);
                return;
            }

            switch (performMsg)
            {
                case PerformInstantActionMessage msg:
                    if (action.InstantAction == null)
                    {
                        Logger.DebugS("action", "user {0} attempted to" +
                                                " perform action {1} as an instant action, but it isn't one", player.Name,
                            msg.ActionType);
                        return;
                    }

                    action.InstantAction.DoInstantAction(new InstantActionEventArgs(player, action.ActionType));

                    break;
                case PerformToggleActionMessage msg:
                    if (action.ToggleAction == null)
                    {
                        Logger.DebugS("action", "user {0} attempted to" +
                                                " perform action {1} as a toggle action, but it isn't one", player.Name,
                            msg.ActionType);
                        return;
                    }

                    if (msg.ToggleOn == actionState.ToggledOn)
                    {
                        Logger.DebugS("action", "user {0} attempted to" +
                                                " toggle action {1} to {2}, but it is already toggled {2}", player.Name,
                            msg.ActionType, actionState.ToggledOn ? "on" : "off");
                        return;
                    }

                    ToggleAction(action.ActionType, msg.ToggleOn);

                    action.ToggleAction.DoToggleAction(new ToggleActionEventArgs(player, action.ActionType, msg.ToggleOn));
                    break;
                case PerformTargetPointActionMessage msg:
                    if (action.TargetPointAction == null)
                    {
                        Logger.DebugS("action", "user {0} attempted to" +
                                                " perform action {1} as a target point action, but it isn't one", player.Name,
                            msg.ActionType);
                        return;
                    }

                    if (ActionBlockerSystem.CanChangeDirection(player))
                    {
                        var diff = msg.Target.ToMapPos(EntityManager) - player.Transform.MapPosition.Position;
                        if (diff.LengthSquared > 0.01f)
                        {
                            player.Transform.LocalRotation = new Angle(diff);
                        }
                    }

                    action.TargetPointAction.DoTargetPointAction(new TargetPointActionEventArgs(player, msg.Target, action.ActionType));
                    break;
                case PerformTargetEntityActionMessage msg:
                    if (action.TargetEntityAction == null)
                    {
                        Logger.DebugS("action", "user {0} attempted to" +
                                                " perform action {1} as a target entity action, but it isn't one", player.Name,
                            msg.ActionType);
                        return;
                    }

                    if (!EntityManager.TryGetEntity(msg.Target, out var entity))
                    {
                        Logger.DebugS("action", "user {0} attempted to" +
                                                " perform target entity action {1} but could not find entity with " +
                                                "provided uid {2}", player.Name, msg.ActionType, msg.Target);
                        return;
                    }

                    if (ActionBlockerSystem.CanChangeDirection(player))
                    {
                        var diff = entity.Transform.MapPosition.Position - player.Transform.MapPosition.Position;
                        if (diff.LengthSquared > 0.01f)
                        {
                            player.Transform.LocalRotation = new Angle(diff);
                        }
                    }

                    action.TargetEntityAction.DoTargetEntityAction(new TargetEntityActionEventArgs(player, action.ActionType, entity));
                    break;
            }
        }


        private void HandlePerformItemActionMessage(PerformItemActionMessage performMsg, ICommonSession session)
        {
            var player = session.AttachedEntity;
            if (player != Owner) return;

            if (!TryGetItemActionState(performMsg.ActionType, performMsg.Item, out var actionState) || !actionState.Enabled)
            {
                Logger.DebugS("action", "user {0} attempted to use" +
                                        " action {1} which is not granted to them", player.Name,
                    performMsg.ActionType);
                return;
            }

            if (actionState.IsOnCooldown(GameTiming))
            {
                Logger.DebugS("action", "user {0} attempted to use" +
                                        " action {1} which is on cooldown", player.Name,
                    performMsg.ActionType);
                return;
            }

            if (!ActionManager.TryGet(performMsg.ActionType, out var action))
            {
                Logger.DebugS("action", "user {0} attempted to" +
                                        " perform unrecognized action {1}", player.Name,
                    performMsg.ActionType);
                return;
            }

            // item must be in inventory
            if (!IsEquipped(performMsg.Item))
            {
                Logger.DebugS("action", "user {0} attempted to" +
                                        " perform action {1} but the item for this action ({2}) is not in inventory", player.Name,
                    performMsg.ActionType, performMsg.Item);
                // failsafe, ensure it's revoked (it should've been already)
                Revoke(performMsg.Item);
                return;
            }

            if (!EntityManager.TryGetEntity(performMsg.Item, out var item))
            {
                Logger.DebugS("action", "user {0} attempted to" +
                                        " perform action {1} but the item for this action ({2}) could not be found", player.Name,
                    performMsg.ActionType, performMsg.Item);
                // failsafe, ensure it's revoked (it should've been already)
                Revoke(performMsg.Item);
                return;
            }

            switch (performMsg)
            {
                case PerformInstantItemActionMessage msg:
                    if (action.InstantAction == null)
                    {
                        Logger.DebugS("action", "user {0} attempted to" +
                                                " perform action {1} as an instant item action, but it isn't one", player.Name,
                            msg.ActionType);
                        return;
                    }

                    action.InstantAction.DoInstantAction(new InstantItemActionEventArgs(player, item, action.ActionType));

                    break;
                case PerformToggleItemActionMessage msg:
                    if (action.ToggleAction == null)
                    {
                        Logger.DebugS("action", "user {0} attempted to" +
                                                " perform action {1} as a toggle item action, but it isn't one", player.Name,
                            msg.ActionType);
                        return;
                    }

                    if (msg.ToggleOn == actionState.ToggledOn)
                    {
                        Logger.DebugS("action", "user {0} attempted to" +
                                                " toggle item action {1} to {2}, but it is already toggled {2}", player.Name,
                            msg.ActionType, actionState.ToggledOn ? "on" : "off");
                        return;
                    }

                    ToggleAction(action.ActionType, item,  msg.ToggleOn);

                    action.ToggleAction.DoToggleAction(new ToggleItemActionEventArgs(player, msg.ToggleOn, item, action.ActionType));
                    break;
                case PerformTargetPointItemActionMessage msg:
                    if (action.TargetPointAction == null)
                    {
                        Logger.DebugS("action", "user {0} attempted to" +
                                                " perform action {1} as a target point item action, but it isn't one", player.Name,
                            msg.ActionType);
                        return;
                    }

                    if (ActionBlockerSystem.CanChangeDirection(player))
                    {
                        var diff = msg.Target.ToMapPos(EntityManager) - player.Transform.MapPosition.Position;
                        if (diff.LengthSquared > 0.01f)
                        {
                            player.Transform.LocalRotation = new Angle(diff);
                        }
                    }

                    action.TargetPointAction.DoTargetPointAction(
                        new TargetPointItemActionEventArgs(player, msg.Target, item, action.ActionType));
                    break;
                case PerformTargetEntityItemActionMessage msg:
                    if (action.TargetEntityAction == null)
                    {
                        Logger.DebugS("action", "user {0} attempted to" +
                                                " perform action {1} as a target entity action, but it isn't one", player.Name,
                            msg.ActionType);
                        return;
                    }

                    if (!EntityManager.TryGetEntity(msg.Target, out var entity))
                    {
                        Logger.DebugS("action", "user {0} attempted to" +
                                                " perform target entity action {1} but could not find entity with " +
                                                "provided uid {2}", player.Name, msg.ActionType, msg.Target);
                        return;
                    }

                    if (ActionBlockerSystem.CanChangeDirection(player))
                    {
                        var diff = entity.Transform.MapPosition.Position - player.Transform.MapPosition.Position;
                        if (diff.LengthSquared > 0.01f)
                        {
                            player.Transform.LocalRotation = new Angle(diff);
                        }
                    }

                    action.TargetEntityAction.DoTargetEntityAction(
                        new TargetEntityItemActionEventArgs(player, entity, item, action.ActionType));
                    break;
            }
        }
    }

    [AdminCommand(AdminFlags.Debug)]
    public sealed class GrantAction : IClientCommand
    {
        public string Command => "grantaction";
        public string Description => "Grants an action to a player, defaulting to current player";
        public string Help => "grantaction <actionType> <name or userID, omit for current player>";
        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            var attachedEntity = player.AttachedEntity;
            if (args.Length > 1)
            {
                var target = args[1];
                if (!Commands.CommandUtils.TryGetAttachedEntityByUsernameOrId(shell, target, player, out attachedEntity)) return;
            }

            if (!CommandUtils.ValidateAttachedEntity(shell, player, attachedEntity)) return;


            if (!attachedEntity.TryGetComponent(out ServerActionsComponent? actionsComponent))
            {
                shell.SendText(player, "user has no actions component");
                return;
            }

            var actionTypeRaw = args[0];
            if (!Enum.TryParse<ActionType>(actionTypeRaw, out var actionType))
            {
                shell.SendText(player, "unrecognized ActionType enum value, please" +
                                       " ensure you used correct casing: " + actionTypeRaw);
                return;
            }
            var actionMgr = IoCManager.Resolve<ActionManager>();
            if (!actionMgr.TryGet(actionType, out var action))
            {
                shell.SendText(player, "unrecognized actionType " + actionType);
                return;
            }
            actionsComponent.Grant(action.ActionType);
        }
    }

    [AdminCommand(AdminFlags.Debug)]
    public sealed class RevokeAction : IClientCommand
    {
        public string Command => "revokeaction";
        public string Description => "Revokes an action from a player, defaulting to current player";
        public string Help => "revokeaction <actionType> <name or userID, omit for current player>";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            var attachedEntity = player.AttachedEntity;
            if (args.Length > 1)
            {
                var target = args[1];
                if (!CommandUtils.TryGetAttachedEntityByUsernameOrId(shell, target, player, out attachedEntity)) return;
            }

            if (!CommandUtils.ValidateAttachedEntity(shell, player, attachedEntity)) return;

            if (!attachedEntity.TryGetComponent(out ServerActionsComponent? actionsComponent))
            {
                shell.SendText(player, "user has no actions component");
                return;
            }

            var actionTypeRaw = args[0];
            if (!Enum.TryParse<ActionType>(actionTypeRaw, out var actionType))
            {
                shell.SendText(player, "unrecognized ActionType enum value, please" +
                                       " ensure you used correct casing: " + actionTypeRaw);
                return;
            }
            var actionMgr = IoCManager.Resolve<ActionManager>();
            if (!actionMgr.TryGet(actionType, out var action))
            {
                shell.SendText(player, "unrecognized actionType " + actionType);
                return;
            }

            actionsComponent.Revoke(action.ActionType);
        }
    }

    [AdminCommand(AdminFlags.Debug)]
    public sealed class CooldownAction : IClientCommand
    {
        public string Command => "coolaction";
        public string Description => "Sets a cooldown on an action for a player, defaulting to current player";
        public string Help => "coolaction <actionType> <seconds> <name or userID, omit for current player>";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            var attachedEntity = player.AttachedEntity;
            if (args.Length > 2)
            {
                var target = args[2];
                if (!CommandUtils.TryGetAttachedEntityByUsernameOrId(shell, target, player, out attachedEntity)) return;
            }

            if (!CommandUtils.ValidateAttachedEntity(shell, player, attachedEntity)) return;

            if (!attachedEntity.TryGetComponent(out ServerActionsComponent? actionsComponent))
            {
                shell.SendText(player, "user has no actions component");
                return;
            }

            var actionTypeRaw = args[0];
            if (!Enum.TryParse<ActionType>(actionTypeRaw, out var actionType))
            {
                shell.SendText(player, "unrecognized ActionType enum value, please" +
                                       " ensure you used correct casing: " + actionTypeRaw);
                return;
            }
            var actionMgr = IoCManager.Resolve<ActionManager>();

            if (!actionMgr.TryGet(actionType, out var action))
            {
                shell.SendText(player, "unrecognized actionType " + actionType);
                return;
            }

            var cooldownStart = IoCManager.Resolve<IGameTiming>().CurTime;
            if (!uint.TryParse(args[1], out var seconds))
            {
                shell.SendText(player, "cannot parse seconds: " + args[1]);
                return;
            }

            var cooldownEnd = cooldownStart.Add(TimeSpan.FromSeconds(seconds));

            actionsComponent.Cooldown(action.ActionType, (cooldownStart, cooldownEnd));
        }
    }
}
