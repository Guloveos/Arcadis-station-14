using Content.Client.Items;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.Systems;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Client.Hands.Systems
{
    [UsedImplicitly]
    public sealed class HandVirtualItemSystem : SharedHandVirtualItemSystem
    {
        public override void Initialize()
        {
            base.Initialize();

            Subs.ItemStatus<HandVirtualItemComponent>(_ => new HandVirtualItemStatus());
        }
    }
}
