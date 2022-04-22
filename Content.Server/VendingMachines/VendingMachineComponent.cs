using System;
using Content.Server.UserInterface;
using Content.Server.Wires;
using Content.Shared.Acts;
using Content.Shared.Interaction;
using Content.Shared.Sound;
using Content.Shared.VendingMachines;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;
using Content.Server.VendingMachines.systems;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.VendingMachines
{
    [RegisterComponent]
    public sealed class VendingMachineComponent : SharedVendingMachineComponent
    {
        public bool Ejecting;
        public bool Emagged = false;
        public TimeSpan AnimationDuration = TimeSpan.Zero;
        [ViewVariables] [DataField("pack", customTypeSerializer:typeof(PrototypeIdSerializer<VendingMachineInventoryPrototype>))]   public string PackPrototypeId = string.Empty;
        [ViewVariables] [DataField("emagPack", customTypeSerializer:typeof(PrototypeIdSerializer<VendingMachineInventoryPrototype>))]   public string EmagPackPrototypeId = string.Empty;
        public string SpriteName = "";
        public bool Broken;
        /// <summary>
        /// When true, will forcefully throw any object it dispenses
        /// </summary>
        [DataField("speedLimiter")]
        public bool CanShoot = false;
        [DataField("soundVend")]
        // Grabbed from: https://github.com/discordia-space/CEV-Eris/blob/f702afa271136d093ddeb415423240a2ceb212f0/sound/machines/vending_drop.ogg
        public SoundSpecifier SoundVend = new SoundPathSpecifier("/Audio/Machines/machine_vend.ogg");
        [DataField("soundDeny")]
        // Yoinked from: https://github.com/discordia-space/CEV-Eris/blob/35bbad6764b14e15c03a816e3e89aa1751660ba9/sound/machines/Custom_deny.ogg
        public SoundSpecifier SoundDeny = new SoundPathSpecifier("/Audio/Machines/custom_deny.ogg");
        [ViewVariables] public BoundUserInterface? UserInterface => Owner.GetUIOrNull(VendingMachineUiKey.Key);
        public float NonLimitedEjectForce = 7.5f;
        public float NonLimitedEjectRange = 5f;

        /*
        public enum Wires
        {
            /// <summary>
            /// Shoots a random item when pulsed.
            /// </summary>
            Limiter
        }
        public void RegisterWires(WiresComponent.WiresBuilder builder)
        {
            builder.CreateWire(Wires.Limiter);
        }

        public void WiresUpdate(WiresUpdateEventArgs args)
        {
            var identifier = (Wires) args.Identifier;
            if (identifier == Wires.Shoot && args.Action == WiresAction.Pulse)
            {
                EjectRandom();
            }
        }
        */

    /*
    public class WiresUpdateEventArgs : EventArgs
    public sealed class WiresUpdateEventArgs : EventArgs
    {
        public readonly object Identifier;
        public readonly WiresAction Action;

        public WiresUpdateEventArgs(object identifier, WiresAction action)
        {
            Identifier = identifier;
            Action = action;
        }
    }
    */

    // why was this ever here???
    /*
    public interface IWires
    {
        void RegisterWires(WiresComponent.WiresBuilder builder);
        void WiresUpdate(WiresUpdateEventArgs args);
    }
    */
    }
}
