#nullable enable
using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.Components.Medical;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using Content.Server.Mobs;
using Content.Shared.GameTicking;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Preferences;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Robust.Shared.IoC;
using static Content.Shared.GameObjects.Components.Medical.SharedCloningPodComponent;

namespace Content.Server.GameObjects.EntitySystems
{
    internal sealed class CloningSystem : EntitySystem, IResettingEntitySystem
    {
        [Dependency] private readonly IGameTiming _timing = default!;
        public readonly Dictionary<Mind, int> MindToId = new();
        public readonly Dictionary<int, ClonerDNAEntry> IdToDNA = new();
        private int _nextAllocatedMindId = 0;
        private float _quickAndDirtyUserUpdatePreventerTimer = 0.0f;
        public readonly Dictionary<Mind, EntityUid> ClonesWaitingForMind = new();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<CloningPodComponent, ActivateInWorldMessage>(HandleActivate);
            SubscribeLocalEvent<BeingClonedComponent, MindAddedMessage>(HandleMindAdded);
        }

        public override void Shutdown()
        {
            base.Shutdown();

            UnsubscribeLocalEvent<CloningPodComponent, ActivateInWorldMessage>(HandleActivate);
            UnsubscribeLocalEvent<BeingClonedComponent, MindAddedMessage>(HandleMindAdded);
        }

        internal void TransferMindToClone(Mind mind)
        {
            if (!ClonesWaitingForMind.TryGetValue(mind, out var entityUid) ||
                !EntityManager.TryGetEntity(entityUid, out var entity) ||
                !entity.TryGetComponent(out MindComponent? mindComp) ||
                mindComp.Mind != null)
                return;

            mind.TransferTo(entity);
            mind.UnVisit();
            ClonesWaitingForMind.Remove(mind);
        }

        private void HandleActivate(EntityUid uid, CloningPodComponent component, ActivateInWorldMessage args)
        {
            if (!component.Powered ||
                !args.User.TryGetComponent(out ActorComponent? actor))
            {
                return;
            }

            component.UserInterface?.Open(actor.PlayerSession);
        }

        private void HandleMindAdded(EntityUid uid, BeingClonedComponent component, MindAddedMessage message)
        {
            if (component.Parent == EntityUid.Invalid ||
                !EntityManager.TryGetEntity(component.Parent, out var parent) ||
                !parent.TryGetComponent<CloningPodComponent>(out var cloningPodComponent) ||
                component.Owner != cloningPodComponent.BodyContainer?.ContainedEntity)
            {
                component.Owner.RemoveComponent<BeingClonedComponent>();
                return;
            }

            cloningPodComponent.UpdateStatus(CloningPodStatus.Cloning);
        }

        public override void Update(float frameTime)
        {
            foreach (var (cloning, power) in ComponentManager.EntityQuery<CloningPodComponent, PowerReceiverComponent>(true))
            {
                if (cloning.UiKnownPowerState != power.Powered)
                {
                    // Must be *before* update
                    cloning.UiKnownPowerState = power.Powered;
                    UpdateUserInterface(cloning);
                }

                if (!power.Powered)
                    return;

                if (cloning.BodyContainer.ContainedEntity != null)
                {
                    cloning.CloningProgress += frameTime;
                    cloning.CloningProgress = MathHelper.Clamp(cloning.CloningProgress, 0f, cloning.CloningTime);
                }

                if (cloning.CapturedMind?.Session?.AttachedEntity == cloning.BodyContainer.ContainedEntity)
                {
                    cloning.Eject();
                }
            }
        }

        public void UpdateUserInterface(CloningPodComponent comp)
        {
            var idToUser = GetIdToUser();
            comp.UserInterface?.SetState(
                new CloningPodBoundUserInterfaceState(
                    idToUser,
                    // now
                    _timing.CurTime,
                    // progress, time, progressing
                    comp.CloningProgress,
                    comp.CloningTime,
                    // this is duplicate w/ the above check that actually updates progress
                    // better here than on client though
                    comp.UiKnownPowerState && (comp.BodyContainer.ContainedEntity != null),
                    comp.Status == CloningPodStatus.Cloning));
        }

        public void AddToDnaScans(ClonerDNAEntry dna)
        {
            if (!MindToId.ContainsKey(dna.Mind))
            {
                int id = _nextAllocatedMindId++;
                MindToId.Add(dna.Mind, id);
                IdToDNA.Add(id, dna);
            }
            OnChangeMadeToDnaScans();
        }

        public void OnChangeMadeToDnaScans()
        {
            foreach (var cloning in ComponentManager.EntityQuery<CloningPodComponent>(true))
                UpdateUserInterface(cloning);
        }

        public bool HasDnaScan(Mind mind)
        {
            return MindToId.ContainsKey(mind);
        }

        public Dictionary<int, string?> GetIdToUser()
        {
            return IdToDNA.ToDictionary(m => m.Key, m => m.Value.Mind.CharacterName);
        }

        public void Reset()
        {
            MindToId.Clear();
            IdToDNA.Clear();
            ClonesWaitingForMind.Clear();
            _nextAllocatedMindId = 0;
            // We PROBABLY don't need to send out UI interface updates for the dna scan changes during a reset
        }
    }

    // TODO: This needs to be moved to Content.Server.Mobs and made a global point of reference.
    // For example, GameTicker should be using this, and this should be using ICharacterProfile rather than HumanoidCharacterProfile.
    // It should carry a reference or copy of itself with the mobs that it affects.
    // See TODO in MedicalScannerComponent.
    struct ClonerDNAEntry {
        public Mind Mind;
        public HumanoidCharacterProfile Profile;
        public ClonerDNAEntry(Mind m, HumanoidCharacterProfile hcp)
        {
            Mind = m;
            Profile = hcp;
        }
    }
}
