﻿using System;
using Content.Server.GameObjects.Components.Chemistry;
using Content.Server.Interfaces;
using Content.Shared.Atmos;
using Content.Shared.Chemistry;
using Content.Shared.GameObjects.Components.Pointing;
using Content.Shared.Interfaces.GameObjects.Components;
using Microsoft.CodeAnalysis;
using Robust.Server.GameObjects;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;


namespace Content.Server.Atmos
{
    [RegisterComponent]
    public class GasSprayerComponent : Component, IAfterInteract
    {
#pragma warning disable 649
        [Dependency] private readonly IServerNotifyManager _notifyManager = default!;
        [Dependency] private readonly IServerEntityManager _serverEntityManager = default!;
#pragma warning restore 649

        public override string Name => "GasSprayer";


        public void AfterInteract(AfterInteractEventArgs eventArgs)
        {
            if (Owner.TryGetComponent(out SolutionComponent tank) &&
                tank.Solution.GetReagentQuantity("chem.H2O").Float().Equals(0f))
            {
                //TODO: Parameterize to use object prototype's name
                _notifyManager.PopupMessage(Owner, eventArgs.User,
                    Loc.GetString("The Extinguisher is out of water!", Owner));
            }
            else
            {
                tank.TryRemoveReagent("chem.H2O", ReagentUnit.New(50));

                var playerPos = eventArgs.User.Transform.GridPosition;
                var direction = (eventArgs.ClickLocation.Position - playerPos.Position).Normalized;
                playerPos.Offset(direction);

                var spray = _serverEntityManager.SpawnEntity("ExtinguisherSpray", playerPos);

                spray.GetComponent<AppearanceComponent>()
                    .SetData(RoguePointingArrowVisuals.Rotation, direction.ToAngle().Degrees);
                if (spray.TryGetComponent<GasVaporComponent>(out GasVaporComponent air))
                {
                    air.contents = new GasMixture(200){Temperature = Atmospherics.T20C};
                    air.contents.SetMoles(Gas.WaterVapor,20);
                }


                //Todo: Parameterize into prototype
                spray.GetComponent<GasVaporComponent>().StartMove(direction, 5);
            }
        }
    }
}
