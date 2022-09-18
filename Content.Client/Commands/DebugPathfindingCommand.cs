using Content.Client.NPC;
using Content.Shared.NPC;
using JetBrains.Annotations;
using Robust.Shared.Console;

namespace Content.Client.Commands
{
    [UsedImplicitly]
    public sealed class DebugPathfindingCommand : IConsoleCommand
    {
        // ReSharper disable once StringLiteralTypo
        public string Command => "pathfinder";
        public string Description => "Toggles visibility of pathfinding debuggers.";
        public string Help => "pathfinder [boundary / breadcrumbs / chunks]";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var system = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<PathfindingSystem>();

            if (args.Length == 0)
            {
                system.Modes = PathfindingDebugMode.None;
                return;
            }

            foreach (var arg in args)
            {
                switch (arg)
                {
                    case "boundary":
                        system.Modes ^= PathfindingDebugMode.Boundary;
                        shell.WriteLine($"Toggled {arg} to {system.Modes & PathfindingDebugMode.Boundary}");
                        break;
                    case "breadcrumbs":
                        system.Modes ^= PathfindingDebugMode.Breadcrumbs;
                        shell.WriteLine($"Toggled {arg} to {system.Modes & PathfindingDebugMode.Breadcrumbs}");
                        break;
                    case "chunks":
                        system.Modes ^= PathfindingDebugMode.Chunks;
                        shell.WriteLine($"Toggled {arg} to {system.Modes & PathfindingDebugMode.Chunks}");
                        break;
                    case "crumb":
                        system.Modes ^= PathfindingDebugMode.Crumb;
                        shell.WriteLine($"Toggled {arg} to {system.Modes & PathfindingDebugMode.Crumb}");
                        break;
                    default:
                        shell.WriteError($"Unrecognised pathfinder args {arg}");
                        break;
                }
            }
        }

        public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            if (args.Length > 1)
            {
                return CompletionResult.Empty;
            }

            var options = new CompletionOption[]
            {
                new("boundary"),
                new("breadcrumbs"),
                new("chunks"),

            };

            return CompletionResult.FromOptions(options);
        }
    }
}
