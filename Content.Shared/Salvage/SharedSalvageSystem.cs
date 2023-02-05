namespace Content.Shared.Salvage;

public abstract class SharedSalvageSystem : EntitySystem
{
    public static readonly TimeSpan MissionCooldown = TimeSpan.FromSeconds(10);
}
