﻿using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using Content.Shared.BodySystem;
using Robust.Shared.ViewVariables;
using System.Globalization;
using Robust.Server.GameObjects;
using Content.Server.GameObjects.EntitySystems;
using Robust.Shared.Log;
using Content.Shared.Interfaces;

namespace Content.Server.BodySystem {

    /// <summary>
    ///    Component containing the data for a dropped Mechanism entity.
    /// </summary>
    [RegisterComponent]
    public class DroppedMechanismComponent : Component, IAfterInteract
    {

#pragma warning disable 649
        [Dependency] private readonly ISharedNotifyManager _sharedNotifyManager;
#pragma warning restore 649

        public sealed override string Name => "DroppedMechanism";

        [ViewVariables]
        public Mechanism ContainedMechanism => _containedMechanism;

        private Mechanism _containedMechanism;

        public void InitializeDroppedMechanism(Mechanism data)
        {
            _containedMechanism = data;
            Owner.Name = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(_containedMechanism.Name);
            if (Owner.TryGetComponent<SpriteComponent>(out SpriteComponent component))
            {
                component.LayerSetRSI(0, data.RSIPath);
                component.LayerSetState(0, data.RSIState);
            }
        }

        void IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            if (eventArgs.Target == null)
                return;
            if (eventArgs.Target.TryGetComponent<BodyManagerComponent>(out BodyManagerComponent bodyManager))
            {
                //Popup UI to possibly install mechanism on some limb.
            }
            else if (eventArgs.Target.TryGetComponent<DroppedBodyPartComponent>(out DroppedBodyPartComponent droppedBodyPart))
            {
                if (droppedBodyPart.ContainedBodyPart == null)
                {
                    Logger.Debug("Installing a mechanism was attempted on an IEntity with a DroppedBodyPartComponent that doesn't have a BodyPart in it!");
                    throw new InvalidOperationException("A DroppedBodyPartComponent exists without a BodyPart in it!");
                }
                if (!droppedBodyPart.ContainedBodyPart.TryInstallDroppedMechanism(this))
                {
                    _sharedNotifyManager.PopupMessage(eventArgs.Target, eventArgs.User, "You can't fit it in!");
                }
            }
        }
    }
}

