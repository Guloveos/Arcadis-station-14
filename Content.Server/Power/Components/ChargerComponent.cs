using Content.Shared.Containers.ItemSlots;
using Content.Shared.Power;

namespace Content.Server.Power.Components
{
    [RegisterComponent]
    public sealed class ChargerComponent : Component
    {
        [ViewVariables]
        public BatteryComponent? HeldBattery;

        [ViewVariables]
        public CellChargerStatus Status;

        [DataField("chargeRate")]
        public int ChargeRate = 20;

        [DataField("chargerSlot", required: true)]
        public ItemSlot ChargerSlot = new();
    }
}
