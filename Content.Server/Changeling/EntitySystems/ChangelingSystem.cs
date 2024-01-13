using Content.Server.Actions;
using Content.Shared.DoAfter;
using Content.Server.Store.Components;
using Content.Server.Store.Systems;
using Content.Shared.Changeling;
using Content.Shared.Changeling.Components;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;
using Content.Shared.Store;
using Content.Server.Traitor.Uplink;
using Content.Shared.Damage;
using Content.Server.Body.Systems;
using Content.Shared.Mobs.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Server.Polymorph.Systems;
using System.Linq;
using Content.Shared.Polymorph;
using Content.Server.Forensics;
using Content.Shared.Interaction.Components;
using Content.Shared.Actions;
using Robust.Shared.Serialization.Manager;
using Content.Shared.Alert;
using Content.Shared.Stealth.Components;

namespace Content.Server.Changeling.EntitySystems;

public sealed partial class ChangelingSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly StoreSystem _store = default!;
    [Dependency] private readonly ActionsSystem _action = default!;
    [Dependency] private readonly UplinkSystem _uplink = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly PolymorphSystem _polymorph = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly ISerializationManager _serialization = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ChangelingComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<ChangelingComponent, ChangelingEvolutionMenuActionEvent>(OnShop);
        SubscribeLocalEvent<ChangelingComponent, ChangelingCycleDNAActionEvent>(OnCycleDNA);
        SubscribeLocalEvent<ChangelingComponent, ChangelingTransformActionEvent>(OnTransform);

        InitializeLingAbilities();
    }

    private void OnStartup(EntityUid uid, ChangelingComponent component, ComponentStartup args)
    {
        _uplink.AddUplink(uid, FixedPoint2.New(10), ChangelingShopPresetPrototype, uid, EvolutionPointsCurrencyPrototype); // not really an 'uplink', but it's there to add the evolution menu
        StealDNA(uid, uid, component);
    }

    [ValidatePrototypeId<EntityPrototype>]
    private const string ChangelingEvolutionMenuId = "ActionChangelingEvolutionMenu";

    [ValidatePrototypeId<EntityPrototype>]
    private const string ChangelingRegenActionId = "ActionLingRegenerate";

    [ValidatePrototypeId<EntityPrototype>]
    private const string ChangelingAbsorbActionId = "ActionChangelingAbsorb";

    [ValidatePrototypeId<EntityPrototype>]
    private const string ChangelingDNACycleActionId = "ActionChangelingCycleDNA";

    [ValidatePrototypeId<EntityPrototype>]
    private const string ChangelingTransformActionId = "ActionChangelingTransform";

    [ValidatePrototypeId<CurrencyPrototype>]
    public const string EvolutionPointsCurrencyPrototype = "EvolutionPoints";

    [ValidatePrototypeId<StorePresetPrototype>]
    public const string ChangelingShopPresetPrototype = "StorePresetChangeling";

    public bool ChangeChemicalsAmount(EntityUid uid, float amount, ChangelingComponent? component = null, bool regenCap = true)
    {
        if (!Resolve(uid, ref component))
            return false;

        component.Chemicals += amount;

        if (regenCap)
            float.Min(component.Chemicals, component.MaxChemicals);

        _alerts.ShowAlert(uid, AlertType.Chemicals, (short) Math.Clamp(Math.Round(component.Chemicals / 10.7f), 0, 7));

        return true;
    }

    private bool TryUseAbility(EntityUid uid, ChangelingComponent component, float abilityCost, bool activated = true, float regenCost = 0f)
    {
        if (component.Chemicals <= Math.Abs(abilityCost) && activated)
        {
            _popup.PopupEntity(Loc.GetString("changeling-not-enough-chemicals"), uid, uid);
            return false;
        }

        if (activated)
        {
            ChangeChemicalsAmount(uid, abilityCost, component, false);
            component.ChemicalsPerSecond -= regenCost;
        }
        else
        {
            component.ChemicalsPerSecond += regenCost;
        }

        return true;
    }

    private bool TryStingTarget(EntityUid uid, EntityUid target, ChangelingComponent component)
    {
        if (HasComp<ChangelingComponent>(target))
        {
            var selfMessage = Loc.GetString("changeling-sting-fail-self", ("target", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(selfMessage, uid, uid);

            var targetMessage = Loc.GetString("changeling-sting-fail-target");
            _popup.PopupEntity(targetMessage, target, target);
            return false;
        }

        return true;
    }

    private void OnMapInit(EntityUid uid, ChangelingComponent component, MapInitEvent args)
    {
        _action.AddAction(uid, ChangelingEvolutionMenuId);
        _action.AddAction(uid, ChangelingRegenActionId);
        _action.AddAction(uid, ChangelingAbsorbActionId);
        _action.AddAction(uid, ChangelingDNACycleActionId);
        _action.AddAction(uid, ChangelingTransformActionId);
    }

    private void OnShop(EntityUid uid, ChangelingComponent component, ChangelingEvolutionMenuActionEvent args)
    {
        if (!TryComp(uid, out StoreComponent? store))
            return;
        _store.ToggleUi(uid, uid, store);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ChangelingComponent>();
        while (query.MoveNext(out var uid, out var ling))
        {
            ling.Accumulator += frameTime;

            if (ling.Accumulator <= 1)
                continue;
            ling.Accumulator -= 1;

            if (_mobState.IsDead(ling.Owner)) // if ling is dead dont regenerate chemicals
                return;

            if (ling.Chemicals < ling.MaxChemicals)
            {
                ChangeChemicalsAmount(uid, ling.ChemicalsPerSecond, ling, regenCap: true);
            }
        }
    }

    public bool StealDNA(EntityUid uid, EntityUid target, ChangelingComponent component)
    {
        if (!TryComp<MetaDataComponent>(target, out var metaData))
            return false;
        if (!TryComp<HumanoidAppearanceComponent>(target, out var humanoidappearance))
        {
            var selfMessage = Loc.GetString("changeling-dna-nodna", ("target", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(selfMessage, uid, uid);
            return false;
        }

        if (component.StoredDNA.Count >= component.DNAStrandCap)
        {
            var lastHumanoidData = component.StoredDNA.Last();
            component.StoredDNA.Remove(lastHumanoidData);
            component.StoredDNA.Add(target);
        }
        else
        {
            component.StoredDNA.Add(target);
        }

        return true;
    }

    public void OnCycleDNA(EntityUid uid, ChangelingComponent component, ChangelingCycleDNAActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        component.SelectedDNA += 1;

        if (component.StoredDNA.Count >= component.DNAStrandCap || component.SelectedDNA >= component.StoredDNA.Count)
            component.SelectedDNA = 0;

        var selectedHumanoid = component.StoredDNA[component.SelectedDNA];
        var selfMessage = Loc.GetString("changeling-dna-switchdna", ("target", Identity.Entity(selectedHumanoid, EntityManager)));
        _popup.PopupEntity(selfMessage, uid, uid);
    }

    public void OnTransform(EntityUid uid, ChangelingComponent component, ChangelingTransformActionEvent args)
    {
        if (args.Handled)
            return;

        var selectedHumanoid = component.StoredDNA[component.SelectedDNA];
        var dnaComp = EnsureComp<DnaComponent>(uid);
        var dnaCompSelectedHumanoid = EnsureComp<DnaComponent>(selectedHumanoid);

        if (dnaComp.DNA == dnaCompSelectedHumanoid.DNA)
        {
            var selfMessage = Loc.GetString("changeling-transform-fail", ("target", Identity.Entity(selectedHumanoid, EntityManager)));
            _popup.PopupEntity(selfMessage, uid, uid);
        }
        else
        {
            if (!TryUseAbility(uid, component, component.TransformChemicalsCost))
                return;

            args.Handled = true;

            var transformedUid = _polymorph.PolymorphEntity(uid, "ChangelingHumanoidMorph", selectedHumanoid);
            if (transformedUid == null)
                return;

            var selfMessage = Loc.GetString("changeling-transform-activate", ("target", Identity.Entity(selectedHumanoid, EntityManager)));
            _popup.PopupEntity(selfMessage, transformedUid.Value, transformedUid.Value);

            var transformedDnaComp = EnsureComp<DnaComponent>(transformedUid.Value);
            transformedDnaComp.DNA = dnaCompSelectedHumanoid.DNA;

            var newLingComponent = EnsureComp<ChangelingComponent>(transformedUid.Value);
            newLingComponent.Chemicals = component.Chemicals;
            newLingComponent.ChemicalsPerSecond = component.ChemicalsPerSecond;
            newLingComponent.StoredDNA = component.StoredDNA;
            newLingComponent.SelectedDNA = component.SelectedDNA;
            newLingComponent.ArmBladeActive = component.ArmBladeActive;
            newLingComponent.LingArmorActive = component.LingArmorActive;
            newLingComponent.ChameleonSkinActive = component.ChameleonSkinActive;

            if (TryComp<MetaDataComponent>(selectedHumanoid, out var targetHumanoidMeta))
                _metaData.SetEntityName(transformedUid.Value, targetHumanoidMeta.EntityName);

            if (TryComp(uid, out StoreComponent? storeComp))
            {
                var copiedStoreComponent = (Component) _serialization.CreateCopy(storeComp, notNullableOverride: true);
                RemComp<StoreComponent>(transformedUid.Value);
                EntityManager.AddComponent(transformedUid.Value, copiedStoreComponent);
            }

            if (TryComp(uid, out StealthComponent? stealthComp)) // copy over stealth status
            {
                if (TryComp(uid, out StealthOnMoveComponent? stealthOnMoveComp))
                {
                    var copiedStealthComponent = (Component) _serialization.CreateCopy(stealthComp, notNullableOverride: true);
                    EntityManager.AddComponent(transformedUid.Value, copiedStealthComponent);

                    var copiedStealthOnMoveComponent = (Component) _serialization.CreateCopy(stealthOnMoveComp, notNullableOverride: true);
                    EntityManager.AddComponent(transformedUid.Value, copiedStealthOnMoveComponent);
                }
            }

            _actionContainer.TransferAllActions(uid, transformedUid.Value);
        }
    }
}