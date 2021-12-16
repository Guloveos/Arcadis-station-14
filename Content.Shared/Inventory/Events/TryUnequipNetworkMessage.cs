﻿using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Inventory.Events;

[NetSerializable, Serializable]
public class TryUnequipNetworkMessage : EntityEventArgs
{
    public readonly EntityUid Uid;
    public readonly string Slot;
    public readonly bool Silent;
    public readonly bool Force;

    public TryUnequipNetworkMessage(EntityUid uid, string slot, bool silent, bool force)
    {
        Uid = uid;
        Slot = slot;
        Silent = silent;
        Force = force;
    }
}
