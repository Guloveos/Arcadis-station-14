﻿using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration.Events
{
    [NetSerializable, Serializable]
    public class PlayerInfoChangedEvent : EntityEventArgs
    {
        public PlayerInfo? PlayerInfo;
    }
}
