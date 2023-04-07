using Content.Shared.Chemistry.Components;
using Content.Shared.DragDrop;
using Content.Shared.Fluids;
using Content.Shared.Fluids.Components;

namespace Content.Server.Fluids.EntitySystems;

public sealed partial class PuddleSystem
{
    private void InitializeTransfers()
    {
        SubscribeLocalEvent<RefillableSolutionComponent, DragDropDraggedEvent>(OnRefillableDragged);
    }

    private void OnRefillableDragged(EntityUid uid, RefillableSolutionComponent component, ref DragDropDraggedEvent args)
    {
        TryComp<DrainableSolutionComponent>(args.Target, out var drainable);
        _solutionContainerSystem.TryGetSolution(uid, component.Solution, out var solution);
        _solutionContainerSystem.TryGetDrainableSolution(args.Target, out var drainableSolution, drainable);

        // Dump reagents into drain
        if (TryComp<DrainComponent>(args.Target, out var drain) && drainable != null)
        {
            if (drainableSolution == null || solution == null)
                return;

            var split = _solutionContainerSystem.SplitSolution(uid, solution, drainableSolution.AvailableVolume);

            // TODO: Drane refactor
            if (_solutionContainerSystem.TryAddSolution(args.Target, drainableSolution, split))
            {
                _audio.PlayPvs(AbsorbentComponent.DefaultTransferSound, args.Target);
            }
            else
            {
                _popups.PopupEntity(Loc.GetString("mopping-system-full"), args.Target, args.User);
            }

            return;
        }

        // Take reagents from target
        if (drainable != null)
        {
            if (drainableSolution == null || solution == null)
                return;

            var split = _solutionContainerSystem.SplitSolution(args.Target, drainableSolution, solution.AvailableVolume);

            if (_solutionContainerSystem.TryAddSolution(uid, solution, split))
            {
                _audio.PlayPvs(AbsorbentComponent.DefaultTransferSound, uid);
            }
            else
            {
                _popups.PopupEntity(Loc.GetString("mopping-system-full"), uid, args.User);
            }
        }
    }
}
