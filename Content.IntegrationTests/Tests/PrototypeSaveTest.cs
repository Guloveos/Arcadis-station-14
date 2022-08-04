#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.Shared.Coordinates;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Content.IntegrationTests.Tests;

/// <summary>
///     This test ensure that when an entity prototype is spawned into an un-initialized map, its component data is not
///     modified during init. I.e., when the entity is saved to the map, its data is simply the default prototype data (ignoring transform component).
/// </summary>
[TestFixture]
public sealed class PrototypeSaveTest
{
    private readonly HashSet<string> _ignoredPrototypes = new()
    {
        "Singularity", // physics collision uses "AllMask" (-1). The flag serializer currently fails to save this.
    };

    [Test]
    public async Task UninitializedSaveTest()
    {
        // Log the problem comps
        
        // ah need a custom context to ignore entity uid
        // actually..... why do mobs even have an entityuiid to save??

        // Apparently SpawnTest fails to clean  up properly. Due to the similarities, I'll assume this also fails.
        await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings { NoClient = true, Dirty = true, Destructive = true });
        var server = pairTracker.Pair.Server;

        var mapManager = server.ResolveDependency<IMapManager>();
        var entityMan = server.ResolveDependency<IEntityManager>();
        var prototypeMan = server.ResolveDependency<IPrototypeManager>();
        var tileDefinitionManager = server.ResolveDependency<ITileDefinitionManager>();
        var seriMan = server.ResolveDependency<ISerializationManager>();
        var compFact = server.ResolveDependency<IComponentFactory>();

        var prototypes = new List<EntityPrototype>();
        IMapGrid grid = default!;
        EntityUid uid;
        MapId mapId = default;

        //Build up test environment
        await server.WaitPost(() =>
        {
            // Create a one tile grid to stave off the grid 0 monsters
            mapId = mapManager.CreateMap();

            mapManager.AddUninitializedMap(mapId);

            grid = mapManager.CreateGrid(mapId);

            var tileDefinition = tileDefinitionManager["underplating"];
            var tile = new Tile(tileDefinition.TileId);
            var coordinates = grid.ToCoordinates();

            grid.SetTile(coordinates, tile);
        });

        await server.WaitRunTicks(5);

        //Generate list of non-abstract prototypes to test
        foreach (var prototype in prototypeMan.EnumeratePrototypes<EntityPrototype>())
        {
            if (prototype.Abstract)
                continue;

            // Currently mobs and such can't be serialized, but they aren't flagged as serializable anyways.
            if (!prototype.MapSavable)
                continue;

            if (_ignoredPrototypes.Contains(prototype.ID))
                continue;

            if (prototype.SetSuffix == "DEBUG")
                continue;

            prototypes.Add(prototype);
        }

        var context = new TestEntityUidContext();

        await server.WaitAssertion(() =>
        {
            Assert.That(!mapManager.IsMapInitialized(mapId));
            var testLocation = grid.ToCoordinates();

            Assert.Multiple(() =>
            {
                //Iterate list of prototypes to spawn
                foreach (var prototype in prototypes)
                {
                    uid = entityMan.SpawnEntity(prototype.ID, testLocation);
                    server.RunTicks(1);
                    
                    // get default prototype data
                    Dictionary<string, MappingDataNode> protoData = new();
                    try
                    {
                        foreach (var (compType, comp) in prototype.Components)
                        {
                            protoData.Add(compType, seriMan.WriteValueAs<MappingDataNode>(comp.Component.GetType(), comp.Component, context: context));
                        }
                    }
                    catch (Exception e)
                    {
                        Assert.Fail($"Failed to convert prototype {prototype.ID} into yaml. Exception: {e.Message}");
                        continue;
                    }

                    var comps = new HashSet<IComponent>(entityMan.GetComponents(uid));
                    var compNames = new HashSet<string>(comps.Count);
                    foreach (var component in comps)
                    {
                        var compType = component.GetType();
                        var compName = compFact.GetComponentName(compType);
                        compNames.Add(compName);

                        if (compType == typeof(MetaDataComponent) || compType == typeof(TransformComponent))
                            continue;

                        MappingDataNode compMapping;
                        try
                        {
                            compMapping = seriMan.WriteValueAs<MappingDataNode>(compType, component, context: context);
                        }
                        catch (Exception e)
                        {
                            Assert.Fail($"Failed to serialize {compName} component of entity prototype {prototype.ID}. Exception: {e.Message}");
                            continue;
                        }

                        if (protoData.TryGetValue(compName, out var protoMapping))
                        {
                            var diff = compMapping.Except(protoMapping);

                            if (diff != null && diff.Children.Count != 0)
                            {
                                var modComps = string.Join(",", diff.Keys.Select(x => x.ToString()));
                                Assert.Fail($"Prototype {prototype.ID} modifies component on spawn: {compName}. Modified fields: {modComps}");
                            }
                        }
                        else
                        {
                            Assert.Fail($"Prototype {prototype.ID} gains a component on spawn: {compName}");
                        }
                    }

                    // An entity may also remove components on init -> check no components are missing.
                    foreach (var (compType, comp) in prototype.Components)
                    {
                        Assert.That(compNames.Contains(compType), $"Prototype {prototype.ID} removes component {compType} on spawn.");
                    }

                    if (!entityMan.Deleted(uid))
                        entityMan.DeleteEntity(uid);
                }
            });
        });
        await pairTracker.CleanReturnAsync();
    }

    private sealed class TestEntityUidContext : ISerializationContext,
        ITypeSerializer<EntityUid, ValueDataNode>,
        ITypeReaderWriter<EntityUid, ValueDataNode>
    {
        public Dictionary<(Type, Type), object> TypeReaders { get; }
        public Dictionary<Type, object> TypeWriters { get; }
        public Dictionary<Type, object> TypeCopiers => TypeWriters;
        public Dictionary<(Type, Type), object> TypeValidators => TypeReaders;

        public TestEntityUidContext()
        {
            TypeReaders = new() { { (typeof(EntityUid), typeof(ValueDataNode)), this } };
            TypeWriters = new() { { typeof(EntityUid), this } };
        }

        ValidationNode ITypeValidator<EntityUid, ValueDataNode>.Validate(ISerializationManager serializationManager,
                ValueDataNode node, IDependencyCollection dependencies, ISerializationContext? context)
        {
            return new ValidatedValueNode(node);
        }

        public DataNode Write(ISerializationManager serializationManager, EntityUid value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            // EntityUids should be nullable and have no initial value.
            throw new InvalidOperationException("Serializing prototypes should not attempt to write entity Uids");
        }

        EntityUid ITypeReader<EntityUid, ValueDataNode>.Read(ISerializationManager serializationManager,
            ValueDataNode node,
            IDependencyCollection dependencies,
            bool skipHook,
            ISerializationContext? context, EntityUid _)
        {
            return EntityUid.Invalid;
        }

        public EntityUid Copy(ISerializationManager serializationManager, EntityUid source, EntityUid target,
            bool skipHook,
            ISerializationContext? context = null)
        {
            return new((int) source);
        }
    }
}
