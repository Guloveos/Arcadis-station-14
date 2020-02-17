﻿using System;
using System.Collections.Generic;
using Content.Server.GameObjects.Components.Movement;
using Robust.Server.AI;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Server.Interfaces.Timing;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.IoC;

namespace Content.Server.GameObjects.EntitySystems
{
    internal class AiSystem : EntitySystem
    {
#pragma warning disable 649
        [Dependency] private readonly IPauseManager _pauseManager;
        [Dependency] private readonly IDynamicTypeFactory _typeFactory;
        [Dependency] private readonly IReflectionManager _reflectionManager;
#pragma warning restore 649

        private readonly Dictionary<string, Type> _processorTypes = new Dictionary<string, Type>();

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();

            // register entity query
            EntityQuery = new TypeEntityQuery(typeof(AiControllerComponent));

            var processors = _reflectionManager.GetAllChildren<AiLogicProcessor>();
            foreach (var processor in processors)
            {
                var att = (AiLogicProcessorAttribute)Attribute.GetCustomAttribute(processor, typeof(AiLogicProcessorAttribute));
                if (att != null)
                {
                    _processorTypes.Add(att.SerializeName, processor);
                }
            }
        }

        /// <inheritdoc />
        public override void Update(float frameTime)
        {
            var entities = EntityManager.GetEntities(EntityQuery);
            foreach (var entity in entities)
            {
                if (_pauseManager.IsEntityPaused(entity))
                {
                    continue;
                }

                var aiComp = entity.GetComponent<AiControllerComponent>();
                if (aiComp.Processor == null)
                {
                    aiComp.Processor = CreateProcessor(aiComp.LogicName);
                    aiComp.Processor.SelfEntity = entity;
                    aiComp.Processor.VisionRadius = aiComp.VisionRadius;
                }

                var processor = aiComp.Processor;

                processor.Update(frameTime);
            }
        }

        private AiLogicProcessor CreateProcessor(string name)
        {
            if (_processorTypes.TryGetValue(name, out var type))
            {
                return (AiLogicProcessor)_typeFactory.CreateInstance(type);
            }

            // processor needs to inherit AiLogicProcessor, and needs an AiLogicProcessorAttribute to define the YAML name
            throw new ArgumentException($"Processor type {name} could not be found.", nameof(name));
        }

        private class AddAiCommand : IClientCommand
        {
            public string Command => "addai";
            public string Description => "Add an ai component with a given processor to an entity.";
            public string Help => "addai <processorId> <entityId>";
            public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
            {
                if(args.Length != 2)
                {
                    shell.SendText(player, "Wrong number of args.");
                    return;
                }

                var processorId = args[0];
                var entId = new EntityUid(int.Parse(args[1]));
                var ent = IoCManager.Resolve<IEntityManager>().GetEntity(entId);

                if (ent.HasComponent<AiControllerComponent>())
                {
                    shell.SendText(player, "Entity already has an AI component.");
                    return;
                }

                if (ent.HasComponent<MoverComponent>())
                {
                    ent.RemoveComponent<MoverComponent>();
                }

                var comp = ent.AddComponent<AiControllerComponent>();
                comp.LogicName = processorId;
                shell.SendText(player, "AI component added.");
            }
        }
    }
}
