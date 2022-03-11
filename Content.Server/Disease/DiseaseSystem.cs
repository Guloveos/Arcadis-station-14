using Content.Shared.Disease;
using Content.Server.Disease.Components;
using Content.Server.Clothing.Components;
using Content.Shared.Inventory;
using Content.Shared.Interaction;
using Content.Server.Popups;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager;
using Content.Shared.Inventory.Events;
using Content.Server.Nutrition.EntitySystems;

namespace Content.Server.Disease
{
    public sealed class DiseaseSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly ISerializationManager _serializationManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly EntityLookupSystem _lookup = default!;
        [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;

        [Dependency] private readonly InventorySystem _inventorySystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<DiseaseCarrierComponent, CureDiseaseAttemptEvent>(OnTryCureDisease);
            SubscribeLocalEvent<DiseasedComponent, InteractHandEvent>(OnInteractDiseasedHand);
            SubscribeLocalEvent<DiseasedComponent, InteractUsingEvent>(OnInteractDiseasedUsing);
            SubscribeLocalEvent<DiseaseInteractSourceComponent, AfterInteractEvent>(OnAfterInteractSource);
            SubscribeLocalEvent<DiseaseProtectionComponent, GotEquippedEvent>(OnEquipped);
            SubscribeLocalEvent<DiseaseProtectionComponent, GotUnequippedEvent>(OnUnequipped);
        }

        private Queue<EntityUid> AddQueue = new();
        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            foreach (var entity in AddQueue)
            {
                EnsureComp<DiseasedComponent>(entity);
            }
            AddQueue.Clear();

            foreach (var (diseasedComp, carrierComp) in EntityQuery<DiseasedComponent, DiseaseCarrierComponent>(false))
            {

                foreach(var disease in carrierComp.Diseases)
                {
                    var args = new DiseaseEffectArgs(carrierComp.Owner, disease, EntityManager);
                    disease.Accumulator += frameTime;
                    if (disease.Accumulator >= 1f)
                    {
                        disease.Accumulator -= 1f;
                        foreach (var cure in disease.Cures)
                            if (cure.Cure(args))
                            {
                                CureDisease(carrierComp, disease);
                                return; //Prevent any effects or additional cure attempts, it can mess with some of the maths
                            }
                        foreach (var effect in disease.Effects)
                            if (_random.Prob(effect.Probability))
                                effect.Effect(args);
                    }
                }
                if (carrierComp.Diseases.Count == 0)
                  RemComp<DiseasedComponent>(diseasedComp.Owner);
            }
        }

        private void OnAfterInteractSource(EntityUid uid, DiseaseInteractSourceComponent component, AfterInteractEvent args)
        {
            if (!args.CanReach || args.Target == null)
                return;

            if (!_prototypeManager.TryIndex(component.Disease, out DiseasePrototype? compDisease))
                return;

            if (!TryComp<DiseaseCarrierComponent>(args.Target, out var targetDiseases) || targetDiseases == null)
                return;
            TryAddDisease(targetDiseases, compDisease);
        }

        private void OnTryCureDisease(EntityUid uid, DiseaseCarrierComponent component, CureDiseaseAttemptEvent args)
        {
            foreach (var disease in component.Diseases)
            {
                var cureProb = ((args.CureChance / component.Diseases.Count) - disease.CureResist);
                if (cureProb < 0)
                    return;
                if (cureProb > 1)
                {
                    CureDisease(component, disease);
                    return;
                }
                if (_random.Prob(cureProb))
                {
                    CureDisease(component, disease);
                    return;
                }
            }
        }

        private void OnEquipped(EntityUid uid, DiseaseProtectionComponent component, GotEquippedEvent args)
        {
            if (TryComp<ClothingComponent>(uid, out var clothing))
                if (clothing.SlotFlags != args.SlotFlags)
                    return;

            if(TryComp<DiseaseCarrierComponent>(args.Equipee, out var carrier))
                carrier.DiseaseResist += component.Protection;
        }


        private void OnUnequipped(EntityUid uid, DiseaseProtectionComponent component, GotUnequippedEvent args)
        {
            if(TryComp<DiseaseCarrierComponent>(args.Equipee, out var carrier))
                carrier.DiseaseResist -= component.Protection;
        }
        private void CureDisease(DiseaseCarrierComponent carrier, DiseasePrototype? disease)
        {
            if (disease == null)
                return;
            carrier.PastDiseases.Add(disease);
            carrier.Diseases.Remove(disease);
            _popupSystem.PopupEntity(Loc.GetString("disease-cured"), carrier.Owner, Filter.Pvs(carrier.Owner));
        }

        private void OnInteractDiseasedHand(EntityUid uid, DiseasedComponent component, InteractHandEvent args)
        {
            if (!_interactionSystem.InRangeUnobstructed(args.User, args.Target))
                return;

            InteractWithDiseased (args.Target, args.User);
        }

        private void OnInteractDiseasedUsing(EntityUid uid, DiseasedComponent component, InteractUsingEvent args)
        {
            InteractWithDiseased(args.Target, args.User);
        }

        private void InteractWithDiseased(EntityUid diseased, EntityUid target)
        {
            if (!TryComp<DiseaseCarrierComponent>(target, out var carrier))
                return;

            var disease = _random.Pick(Comp<DiseaseCarrierComponent>(diseased).Diseases);
            if (disease != null)
                TryInfect(carrier, disease, 0.3f);
        }
        public void TryAddDisease(DiseaseCarrierComponent? target, DiseasePrototype? addedDisease, string? diseaseName = null, EntityUid host = default!)
        {
            if (diseaseName != null && _prototypeManager.TryIndex(diseaseName, out DiseasePrototype? diseaseProto))
                addedDisease = diseaseProto;

            if (host != default!)
                target = Comp<DiseaseCarrierComponent>(host);

            if (target != null)
            {
                foreach (var disease in target.AllDiseases)
                {
                    if (disease.Name == addedDisease?.Name)
                        return;
                }
            var freshDisease = _serializationManager.CreateCopy(addedDisease) ?? default!;
            target.Diseases.Add(freshDisease);
            AddQueue.Enqueue(target.Owner);
            }
        }
        public void TryInfect(DiseaseCarrierComponent carrier, DiseasePrototype? disease, float chance = 0.7f)
        {
            if(disease == null || !disease.Infectious)
                return;
            var infectionChance = chance - carrier.DiseaseResist;
            if (infectionChance <= 0)
                return;
            if (_random.Prob(infectionChance))
                TryAddDisease(carrier, disease);
        }
        public void SneezeCough(EntityUid uid, DiseasePrototype? disease, SneezeCoughType Snough)
        {
            IngestionBlockerComponent blocker;

            var xform = Comp<TransformComponent>(uid);

            if (Snough == SneezeCoughType.Sneeze)
                _popupSystem.PopupEntity(Loc.GetString("disease-sneeze", ("person", uid)), uid, Filter.Pvs(uid));
            else if (Snough == SneezeCoughType.Cough)
                _popupSystem.PopupEntity(Loc.GetString("disease-cough", ("person", uid)), uid, Filter.Pvs(uid));


            if (_inventorySystem.TryGetSlotEntity(uid, "mask", out var maskUid) &&
                EntityManager.TryGetComponent(maskUid, out blocker) &&
                blocker.Enabled)
                return;

            foreach (var entity in _lookup.GetEntitiesInRange(xform.MapID, xform.WorldPosition, 1.5f))
            {
                if (TryComp<DiseaseCarrierComponent>(entity, out var carrier))
                    TryInfect(carrier, disease);
            }

        }
    }
        public sealed class CureDiseaseAttemptEvent : EntityEventArgs
        {
            public float CureChance { get; }

            public CureDiseaseAttemptEvent(float cureChance)
            {
                CureChance = cureChance;
            }
        }

        public enum SneezeCoughType
        {
            Sneeze,

            Cough,
            None
        }
}
