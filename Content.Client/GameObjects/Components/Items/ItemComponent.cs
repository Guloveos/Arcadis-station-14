﻿using Content.Client.GameObjects.Components.Disposal;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Items;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Client.GameObjects.Components.Items
{
    [RegisterComponent]
    [ComponentReference(typeof(IItemComponent))]
    public class ItemComponent : Component, IItemComponent, IDraggable
    {
        [Dependency] private readonly IResourceCache _resourceCache = default!;

        public override string Name => "Item";
        public override uint? NetID => ContentNetIDs.ITEM;

        [ViewVariables]
        [YamlField("sprite")]
        protected ResourcePath RsiPath;

        [ViewVariables(VVAccess.ReadWrite)]
        [YamlField("color")]
        protected Color Color = Color.White;

        [YamlField("HeldPrefix")]
        private string _equippedPrefix;

        [ViewVariables(VVAccess.ReadWrite)]
        public string EquippedPrefix
        {
            get => _equippedPrefix;
            set
            {
                _equippedPrefix = value;
                if (!Owner.TryGetContainer(out IContainer container)) return;
                if(container.Owner.TryGetComponent(out HandsComponent hands))
                    hands.RefreshInHands();
            }
        }

        public (RSI rsi, RSI.StateId stateId, Color color)? GetInHandStateInfo(HandLocation hand)
        {
            if (RsiPath == null)
            {
                return null;
            }

            var handName = hand.ToString().ToLowerInvariant();
            var rsi = GetRSI();
            var stateId = EquippedPrefix != null ? $"{EquippedPrefix}-inhand-{handName}" : $"inhand-{handName}";
            if (rsi.TryGetState(stateId, out _))
            {
                return (rsi, stateId, Color);
            }

            return null;
        }

        protected RSI GetRSI()
        {
            return _resourceCache.GetResource<RSIResource>(SharedSpriteComponent.TextureRoot / RsiPath).RSI;
        }

        public override void HandleComponentState(ComponentState curState, ComponentState nextState)
        {
            if(curState == null)
                return;

            var itemComponentState = (ItemComponentState)curState;
            EquippedPrefix = itemComponentState.EquippedPrefix;
        }

        bool IDraggable.CanDrop(CanDropEventArgs args)
        {
            return args.Target.HasComponent<DisposalUnitComponent>();
        }

        bool IDraggable.Drop(DragDropEventArgs args)
        {
            // TODO: Shared item class
            return false;
        }
    }
}
