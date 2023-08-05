using System.Collections.Generic;
using System.Reflection;
using Robust.Shared.Reflection;
using Robust.Shared.Utility;

namespace Content.IntegrationTests;

// Partial class for handling the discovering and storing test prototypes.
public static partial class PoolManager
{
    private static List<string> _testPrototypes = new();

    private const BindingFlags Flags = BindingFlags.Static
                                       | BindingFlags.NonPublic
                                       | BindingFlags.Public
                                       | BindingFlags.DeclaredOnly;

    private static void DiscoverTestPrototypes(Assembly? assembly = null)
    {

        assembly ??= typeof(PoolManager).Assembly;
        _testPrototypes.Clear();

        foreach (var type in assembly.GetTypes())
        {
            foreach (var field in type.GetFields(Flags))
            {
                if (!field.HasCustomAttribute<TestPrototypesAttribute>())
                    continue;

                var val = field.GetValue(null);
                if (val is not string str)
                    throw new Exception($"TestPrototypeAttribute is only valid on non-null string fields");

                _testPrototypes.Add(str);
            }
        }
    }
}
