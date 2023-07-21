using System.Threading;
using System.Threading.Tasks;
using Content.Server.NPC.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators.Combat.Melee;

/// <summary>
/// Attacks the specified key in melee combat.
/// </summary>
public sealed class MeleeOperator : HTNOperator, IHtnConditionalShutdown
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    // TODO: JukeOperator
    // Needs to cancel steering when active, needs to check for melee or whatever
    // Add enum for different juke types
    // Test melee with move + melee + juke and only shutting down on plan shutdown.
    // Services are probably bloating NPC times so if possible run that shit in parallel.
    // Make sure ranged is using services.

    /// <summary>
    /// When to shut the task down.
    /// </summary>
    [DataField("shutdownState")]
    public HTNPlanState ShutdownState { get; } = HTNPlanState.Running;

    /// <summary>
    /// Key that contains the target entity.
    /// </summary>
    [DataField("targetKey", required: true)]
    public string TargetKey = default!;

    /// <summary>
    /// Minimum damage state that the target has to be in for us to consider attacking.
    /// </summary>
    [DataField("targetState")]
    public MobState TargetState = MobState.Alive;

    // Like movement we add a component and pass it off to the dedicated system.

    public override void Startup(NPCBlackboard blackboard)
    {
        base.Startup(blackboard);
        var melee = _entManager.EnsureComponent<NPCMeleeCombatComponent>(blackboard.GetValue<EntityUid>(NPCBlackboard.Owner));
        melee.MissChance = blackboard.GetValueOrDefault<float>(NPCBlackboard.MeleeMissChance, _entManager);
        melee.Target = blackboard.GetValue<EntityUid>(TargetKey);
    }

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        // Don't attack if they're already as wounded as we want them.
        if (!blackboard.TryGetValue<EntityUid>(TargetKey, out var target, _entManager))
        {
            return (false, null);
        }

        if (_entManager.TryGetComponent<MobStateComponent>(target, out var mobState) &&
            mobState.CurrentState > TargetState)
        {
            return (false, null);
        }

        return (true, null);
    }

    public void ConditionalShutdown(NPCBlackboard blackboard)
    {
        _entManager.RemoveComponent<NPCMeleeCombatComponent>(blackboard.GetValue<EntityUid>(NPCBlackboard.Owner));
        blackboard.Remove<EntityUid>(TargetKey);
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        base.Update(blackboard, frameTime);
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        HTNOperatorStatus status;

        if (_entManager.TryGetComponent<NPCMeleeCombatComponent>(owner, out var combat) &&
            blackboard.TryGetValue<EntityUid>(TargetKey, out var target, _entManager))
        {
            combat.Target = target;

            // Success
            if (_entManager.TryGetComponent<MobStateComponent>(target, out var mobState) &&
                mobState.CurrentState > TargetState)
            {
                status = HTNOperatorStatus.Finished;
            }
            else
            {
                switch (combat.Status)
                {
                    case CombatStatus.TargetOutOfRange:
                    case CombatStatus.Normal:
                        status = HTNOperatorStatus.Continuing;
                        break;
                    default:
                        status = HTNOperatorStatus.Failed;
                        break;
                }
            }
        }
        else
        {
            status = HTNOperatorStatus.Failed;
        }

        if (status != HTNOperatorStatus.Continuing)
        {
            _entManager.RemoveComponent<NPCMeleeCombatComponent>(owner);
        }

        return status;
    }
}
