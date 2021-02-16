using System.Collections.Generic;
using Content.Shared.Research;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.GameObjects.Components.Research
{
    public partial class SharedProtoLatheDatabaseComponentData
    {
        [DataClassTarget("protolatherecipes")]
        public List<LatheRecipePrototype> ProtolatheRecipes = new();

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataReadWriteFunction(
                "protolatherecipes",
                new List<string>(),
                recipes =>
                {
                    var prototypeManager = IoCManager.Resolve<IPrototypeManager>();

                    foreach (var id in recipes)
                    {
                        if (prototypeManager.TryIndex(id, out LatheRecipePrototype recipe))
                        {
                            ProtolatheRecipes.Add(recipe);
                        }
                    }
                },
                () =>
                {
                    var list = new List<string>();

                    foreach (var recipe in ProtolatheRecipes)
                    {
                        list.Add(recipe.ID);
                    }

                    return list;
                });
        }

    }
}
