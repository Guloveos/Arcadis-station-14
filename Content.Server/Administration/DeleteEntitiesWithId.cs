﻿#nullable enable
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Administration
{
    public class DeleteEntitiesWithId : IClientCommand
    {
        public string Command => "deleteewi";
        public string Description => "Deletes entities with the specified prototype ID.";
        public string Help => $"Usage: {Command} <prototypeID>";

        public void Execute(IConsoleShell shell, IPlayerSession? player, string[] args)
        {
            if (args.Length != 1)
            {
                shell.SendText(player, Help);
                return;
            }

            var id = args[0];
            var entityManager = IoCManager.Resolve<IEntityManager>();
            var query = new PredicateEntityQuery(e => e.Prototype?.ID == id);
            var entities = entityManager.GetEntities(query);

            foreach (var entity in entities)
            {
                entity.Delete();
            }
        }
    }
}
