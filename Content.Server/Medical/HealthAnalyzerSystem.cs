using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Server.Medical.Components;
using Content.Server.PowerCell;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.MedicalScanner;
using Content.Shared.Mobs.Components;
using Robust.Server.GameObjects;
using Content.Server.Temperature.Components;
using Content.Server.Body.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;

namespace Content.Server.Medical
{
    public sealed class HealthAnalyzerSystem : EntitySystem
    {
        [Dependency] private readonly PowerCellSystem _cell = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<HealthAnalyzerComponent, AfterInteractEvent>(OnAfterInteract);
            SubscribeLocalEvent<HealthAnalyzerComponent, HealthAnalyzerDoAfterEvent>(OnDoAfter);
        }

        private void OnAfterInteract(EntityUid uid, HealthAnalyzerComponent healthAnalyzer, AfterInteractEvent args)
        {
            if (args.Target == null || !args.CanReach || !HasComp<MobStateComponent>(args.Target) || !_cell.HasActivatableCharge(uid, user: args.User))
                return;

            _audio.PlayPvs(healthAnalyzer.ScanningBeginSound, uid);

            _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, healthAnalyzer.ScanDelay, new HealthAnalyzerDoAfterEvent(), uid, target: args.Target, used: uid)
            {
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                NeedHand = true
            });
        }

        private void OnDoAfter(EntityUid uid, HealthAnalyzerComponent component, DoAfterEvent args)
        {
            if (args.Handled || args.Cancelled || args.Args.Target == null || !_cell.TryUseActivatableCharge(uid, user: args.User))
                return;

            _audio.PlayPvs(component.ScanningEndSound, args.Args.User);

            UpdateScannedUser(uid, args.Args.User, args.Args.Target.Value, component);
            args.Handled = true;
        }

        private void OpenUserInterface(EntityUid user, EntityUid analyzer)
        {
            if (!TryComp<ActorComponent>(user, out var actor) || !_uiSystem.TryGetUi(analyzer, HealthAnalyzerUiKey.Key, out var ui))
                return;

            _uiSystem.OpenUi(ui ,actor.PlayerSession);
        }

        public void UpdateScannedUser(EntityUid uid, EntityUid user, EntityUid? target, HealthAnalyzerComponent? healthAnalyzer)
        {
            if (!Resolve(uid, ref healthAnalyzer))
                return;

            if (target == null || !_uiSystem.TryGetUi(uid, HealthAnalyzerUiKey.Key, out var ui))
                return;

            if (!HasComp<DamageableComponent>(target))
                return;

            TryComp<TemperatureComponent>(target, out var temp);
            TryComp<BloodstreamComponent>(target, out var bloodstream);

            OpenUserInterface(user, uid);

            _uiSystem.SendUiMessage(ui, new HealthAnalyzerScannedUserMessage(
                GetNetEntity(target),
                temp != null ? temp.CurrentTemperature : float.NaN,
                (bloodstream != null && _solutionContainerSystem.ResolveSolution(target.Value, bloodstream.BloodSolutionName, ref bloodstream.BloodSolution, out var bloodSolution))
                    ? bloodSolution.FillFraction
                    : float.NaN
            ));
        }
    }
}
