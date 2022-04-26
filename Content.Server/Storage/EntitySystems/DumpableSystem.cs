using System.Threading;
using Content.Shared.Interaction;
using Content.Server.Storage.Components;
using Content.Shared.Storage.Components;
using Content.Shared.Verbs;
using Content.Server.Disposal.Unit.Components;
using Content.Server.Disposal.Unit.EntitySystems;
using Content.Server.DoAfter;
using Content.Shared.Placeable;
using Content.Server.Hands.Systems;
using Robust.Shared.Containers;

namespace Content.Server.Storage.EntitySystems
{
    public sealed class DumpableSystem : EntitySystem
    {
        [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly DisposalUnitSystem _disposalUnitSystem = default!;

        [Dependency] private readonly HandsSystem _handsSystem = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<DumpableComponent, AfterInteractEvent>(OnAfterInteract);
            SubscribeLocalEvent<DumpableComponent, GetVerbsEvent<AlternativeVerb>>(AddDumpVerb);
            SubscribeLocalEvent<DumpCompletedEvent>(OnDumpCompleted);
            SubscribeLocalEvent<DumpCancelledEvent>(OnDumpCancelled);
        }

        private void OnAfterInteract(EntityUid uid, DumpableComponent component, AfterInteractEvent args)
        {
            if (!args.CanReach)
                return;

            if (!TryComp<ServerStorageComponent>(args.Used, out var storage))
                return;

            if (storage.StoredEntities == null || storage.StoredEntities.Count == 0)
                return;

            if (HasComp<DisposalUnitComponent>(args.Target) || HasComp<PlaceableSurfaceComponent>(args.Target))
            {
                StartDoAfter(uid, args.Target.Value, args.User, component, storage);
                    return;
            }
        }
        private void AddDumpVerb(EntityUid uid, DumpableComponent dumpable, GetVerbsEvent<AlternativeVerb> args)
        {
            if (!args.CanAccess || !args.CanInteract)
                return;

            if (!TryComp<ServerStorageComponent>(uid, out var storage) || storage.StoredEntities == null || storage.StoredEntities.Count == 0)
                return;

            AlternativeVerb verb = new()
            {
                Act = () =>
                {
                    StartDoAfter(uid, null, args.User, dumpable, storage, 0.6f);
                },
                Text = Loc.GetString("dump-verb-name"),
                IconTexture = "/Textures/Interface/VerbIcons/drop.svg.192dpi.png",
                Priority = 2
            };
            args.Verbs.Add(verb);
        }

        private void StartDoAfter(EntityUid storageUid, EntityUid? targetUid, EntityUid userUid, DumpableComponent dumpable, ServerStorageComponent storage, float multiplier = 1)
        {
            if (dumpable.CancelToken != null)
            {
                dumpable.CancelToken.Cancel();
                dumpable.CancelToken = null;
                return;
            }

            if (storage.StoredEntities == null)
                return;

            float delay = storage.StoredEntities.Count * (float) dumpable.DelayPerItem.TotalSeconds * multiplier;

            dumpable.CancelToken = new CancellationTokenSource();
            _doAfterSystem.DoAfter(new DoAfterEventArgs(userUid, delay, dumpable.CancelToken.Token, target: targetUid)
            {
                BroadcastFinishedEvent = new DumpCompletedEvent(userUid, targetUid, dumpable, storage.StoredEntities),
                BroadcastCancelledEvent = new DumpCancelledEvent(dumpable),
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                BreakOnStun = true,
                NeedHand = true
            });
        }


        private void OnDumpCompleted(DumpCompletedEvent args)
        {
            Queue<EntityUid> dumpQueue = new();
            foreach (var entity in args.StoredEntities)
            {
                dumpQueue.Enqueue(entity);
            }

            if (TryComp<DisposalUnitComponent>(args.Target, out var disposal))
            {
                foreach (var entity in dumpQueue)
                {
                    _disposalUnitSystem.DoInsertDisposalUnit(args.Target.Value, entity);
                }
                return;
            }

            foreach (var entity in dumpQueue)
            {
                Transform(entity).AttachParentToContainerOrGrid(EntityManager);
            }

            if (HasComp<PlaceableSurfaceComponent>(args.Target))
            {
                foreach (var entity in dumpQueue)
                {
                    Transform(entity).LocalPosition = Transform(args.Target.Value).LocalPosition;
                }
                return;
            }
        }

        private void OnDumpCancelled(DumpCancelledEvent args)
        {
            args.Dumpable.CancelToken = null;
        }

        private sealed class DumpCancelledEvent : EntityEventArgs
        {
            public readonly DumpableComponent Dumpable;
            public DumpCancelledEvent(DumpableComponent dumpable)
            {
                Dumpable = dumpable;
            }
        }

        private sealed class DumpCompletedEvent : EntityEventArgs
        {
            public EntityUid User { get; }
            public EntityUid? Target { get; }
            public DumpableComponent Dumpable { get; }

            public IReadOnlyList<EntityUid> StoredEntities { get; }

            public DumpCompletedEvent(EntityUid user, EntityUid? target, DumpableComponent dumpable, IReadOnlyList<EntityUid> storedEntities)
            {
                User = user;
                Target = target;
                Dumpable = dumpable;
                StoredEntities = storedEntities;
            }
        }
    }
}
