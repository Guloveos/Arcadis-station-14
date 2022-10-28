﻿using Content.Server.Atmos.EntitySystems;
using Content.Server.Medical.Components;
using Content.Shared.Medical.Cryogenics;
using Content.Shared.Standing;

namespace Content.Server.Medical
{
    public sealed class InsideCryoPodSystem: EntitySystem
    {

        public override void Initialize()
        {
            base.Initialize();
            // Atmos overrides
            SubscribeLocalEvent<InsideCryoPodComponent, InhaleLocationEvent>(OnInhaleLocation);
            SubscribeLocalEvent<InsideCryoPodComponent, ExhaleLocationEvent>(OnExhaleLocation);
            SubscribeLocalEvent<InsideCryoPodComponent, AtmosExposedGetAirEvent>(OnGetAir);

            SubscribeLocalEvent<InsideCryoPodComponent, DownAttemptEvent>(HandleDown);

        }
        // Must stand in the cryo pod
        private void HandleDown(EntityUid uid, InsideCryoPodComponent component, DownAttemptEvent args)
        {
            args.Cancel();
        }

        #region Atmos handlers

        private void OnGetAir(EntityUid uid, InsideCryoPodComponent component, ref AtmosExposedGetAirEvent args)
        {
            if (TryComp<CryoPodComponent>(component.Holder, out var cryoPodComponent))
            {
                args.Gas = cryoPodComponent.Air;
                args.Handled = true;
            }
        }

        private void OnInhaleLocation(EntityUid uid, InsideCryoPodComponent component, InhaleLocationEvent args)
        {
            if (TryComp<CryoPodComponent>(component.Holder, out var cryoPodComponent))
            {
                args.Gas = cryoPodComponent.Air;
            }
        }

        private void OnExhaleLocation(EntityUid uid, InsideCryoPodComponent component, ExhaleLocationEvent args)
        {
            if (TryComp<CryoPodComponent>(component.Holder, out var cryoPodComponent))
            {
                args.Gas = cryoPodComponent.Air;
            }
        }

        #endregion
    }
}
