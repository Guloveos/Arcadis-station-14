using Content.Server.Destructible;
using Content.Server.Gatherable.Components;
using Content.Shared.DoAfter;
using Content.Shared.EntityList;
using Content.Shared.Gatherable;
using Content.Shared.Interaction;
using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Gatherable;

public sealed partial class GatherableSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly DestructibleSystem _destructible = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GatherableComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<GatherableComponent, GatherableDoAfterEvent>(OnDoAfter);
        InitializeProjectile();
    }

    private void OnInteractUsing(EntityUid uid, GatherableComponent component, InteractUsingEvent args)
    {
        if (!TryComp<GatheringToolComponent>(args.Used, out var tool) || component.ToolWhitelist?.IsValid(args.Used) == false)
            return;

        // Can't gather too many entities at once.
        if (tool.MaxGatheringEntities < tool.GatheringEntities.Count + 1)
            return;

        var damageRequired = _destructible.DestroyedAt(uid);
        var damageTime = (damageRequired / tool.Damage.Total).Float();
        damageTime = Math.Max(1f, damageTime);

        var doAfter = new DoAfterArgs(args.User, damageTime, new GatherableDoAfterEvent(), uid, target: uid, used: args.Used)
        {
            BreakOnDamage = true,
            BreakOnTargetMove = true,
            BreakOnUserMove = true,
            MovementThreshold = 0.25f,
        };

        _doAfterSystem.TryStartDoAfter(doAfter);
    }

    private void OnDoAfter(EntityUid uid, GatherableComponent component, GatherableDoAfterEvent args)
    {
        if(!TryComp<GatheringToolComponent>(args.Args.Used, out var tool) || args.Args.Target == null)
            return;

        tool.GatheringEntities.Remove(args.Args.Target.Value);
        if (args.Handled || args.Cancelled)
            return;

        Gather(args.Args.Target.Value, component, tool.GatheringSound);
        args.Handled = true;
    }

    public void Gather(EntityUid uid, GatherableComponent? component = null, SoundSpecifier? sound = null)
    {
        if (!Resolve(uid, ref component))
            return;

        // Complete the gathering process
        _destructible.DestroyEntity(uid);
        _audio.PlayPvs(sound, uid);

        // Spawn the loot!
        if (component.MappedLoot == null)
            return;

        var pos = Transform(uid).MapPosition;

        foreach (var (tag, table) in component.MappedLoot)
        {
            if (tag != "All")
            {
                if (!_tagSystem.HasTag(uid, tag))
                    continue;
            }
            var getLoot = _prototypeManager.Index<EntityLootTablePrototype>(table);
            var spawnLoot = getLoot.GetSpawns();
            var spawnPos = pos.Offset(_random.NextVector2(0.3f));
            Spawn(spawnLoot[0], spawnPos);
        }
    }
}



