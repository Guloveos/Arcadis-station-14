namespace Content.Server.AI.HTN.Preconditions;

public sealed class KeyExistsPrecondition : HTNPrecondition
{
    [DataField("key", required: true)] public string Key = string.Empty;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        return blackboard.ContainsKey(Key);
    }
}
