#nullable enable
using Content.Server.GameObjects.Components.NodeContainer;
using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Content.Shared.Atmos;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using System.Linq;

namespace Content.Server.GameObjects.Components.Atmos.Piping
{
    [RegisterComponent]
    public class GasGeneratorComponent : Component
    {
        public override string Name => "GasGenerator";

        [ViewVariables(VVAccess.ReadWrite)]
        public bool GeneratorEnabled { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public Gas GeneratedGas { get; set; }

        /// <summary>
        ///     Molar rate of gas generation.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float GasGenerationRate { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public float GeneratorPressureCap { get; set; }

        [ViewVariables]
        private PipeNode? Pipe { get; set; }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(this, x => x.GeneratorEnabled, "generatorEnabled", true);
            serializer.DataField(this, x => x.GeneratedGas, "generatedGas", Gas.Oxygen);
            serializer.DataField(this, x => x.GasGenerationRate, "gasGenerationRate", 10);
            serializer.DataField(this, x => x.GeneratorPressureCap, "generatorPressureCap", 10);
        }

        public override void Initialize()
        {
            base.Initialize();
            Owner.EnsureComponentWarn<PipeNetDeviceComponent>();
            SetPipes();
        }

        public override void HandleMessage(ComponentMessage message, IComponent? component)
        {
            base.HandleMessage(message, component);
            switch (message)
            {
                case PipeNetUpdateMessage:
                    Update();
                    break;
            }
        }

        private void Update()
        {
            if (!GeneratorEnabled)
                return;

            if (Pipe == null || Pipe.Air.Pressure > GeneratorPressureCap)
                return;

            Pipe.Air.AdjustMoles(GeneratedGas, GasGenerationRate);
        }

        private void SetPipes()
        {
            if (!Owner.TryGetComponent<NodeContainerComponent>(out var container))
            {
                Logger.Error($"{typeof(GasGeneratorComponent)} on {Owner?.Prototype?.ID}, Uid {Owner?.Uid} did not have a {nameof(NodeContainerComponent)}.");
                return;
            }
            Pipe = container.Nodes.OfType<PipeNode>().FirstOrDefault();
            if (Pipe == null)
            {
                Logger.Error($"{typeof(GasGeneratorComponent)} on {Owner?.Prototype?.ID}, Uid {Owner?.Uid} could not find compatible {nameof(PipeNode)}s on its {nameof(NodeContainerComponent)}.");
                return;
            }
        }
    }
}
