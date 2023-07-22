﻿using System.Linq;
using Content.Server.NewCon.TypeParsers;

namespace Content.Server.NewCon.Commands.Entities;

[ConsoleCommand]
public sealed class CompCommand : ConsoleCommand
{
    [Dependency] private readonly IEntityManager _entity = default!;

    public override Type[] TypeParameterParsers => new[] {typeof(ComponentType)};

    [CommandImplementation]
    public IEnumerable<T> CompEnumerable<T>([PipedArgument] IEnumerable<EntityUid> input)
        where T: IComponent
    {
        return input.Where(x => _entity.HasComponent<T>(x)).Select(x => _entity.GetComponent<T>(x));
    }

    /*[CommandImplementation]
    public T? CompDirect<T>([PipedArgument] EntityUid input)
        where T : Component
    {
        _entity.TryGetComponent(input, out T? res);
        return res;
    }*/
}
