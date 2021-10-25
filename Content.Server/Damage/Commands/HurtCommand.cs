using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.Server.Damage.Commands
{
    [AdminCommand(AdminFlags.Fun)]
    class HurtCommand : IConsoleCommand
    {
        public string Command => "hurt";
        public string Description => "Ouch";
        public string Help => $"Usage: {Command} <type/?> <amount> (<entity uid/_>) (<ignoreResistances>)";

        private readonly IPrototypeManager _prototypeManager = default!;
        public HurtCommand() {
            _prototypeManager = IoCManager.Resolve<IPrototypeManager>();
        }

        private string DamageTypes()
        {
            var msg = new StringBuilder();

            foreach (var damageGroup in _prototypeManager.EnumeratePrototypes<DamageGroupPrototype>())
            {
                msg.Append($"\n{damageGroup.ID}");
                if (damageGroup.DamageTypes.Any())
                {
                    msg.Append(": ");
                    msg.AppendJoin('|', damageGroup.DamageTypes);
                }
            }
            return $"Damage Types:{msg}";
        }

        private delegate void Damage(IEntity entity, bool ignoreResistances);

        private bool TryParseEntity(IConsoleShell shell, IPlayerSession? player, string arg,
            [NotNullWhen(true)] out IEntity? entity)
        {
            entity = null;

            if (arg == "_")
            {
                var playerEntity = player?.AttachedEntity;

                if (playerEntity == null)
                {
                    shell.WriteLine($"You must have a player entity to use this command without specifying an entity.\n{Help}");
                    return false;
                }

                entity = playerEntity;
                return true;
            }

            if (!EntityUid.TryParse(arg, out var entityUid))
            {
                shell.WriteLine($"{arg} is not a valid entity uid.\n{Help}");

                return false;
            }

            var entityManager = IoCManager.Resolve<IEntityManager>();

            if (!entityManager.TryGetEntity(entityUid, out var parsedEntity))
            {
                shell.WriteLine($"No entity found with uid {entityUid}");

                return false;
            }

            entity = parsedEntity;
            return true;
        }

        private bool TryParseDamageArgs(
            IConsoleShell shell,
            IEntity target,
            string[] args,
            [NotNullWhen(true)] out Damage? func)
        {


            if (!int.TryParse(args[1], out var amount))
            {
                shell.WriteLine($"{args[1]} is not a valid damage integer.");

                func = null;
                return false;
            }

            if (_prototypeManager.TryIndex<DamageGroupPrototype>(args[0], out var damageGroup))
            {
                func = (entity, ignoreResistances) =>
                {
                    var damage = new DamageSpecifier(damageGroup, amount);
                    EntitySystem.Get<SharedDamageableSystem>().TryChangeDamage(entity.Uid, damage, ignoreResistances);

                    shell.WriteLine($"Damaged entity {entity.Name} with id {entity.Uid} for {amount} {damageGroup} damage{(ignoreResistances ? ", ignoring resistances." : ".")}");
                };

                return true;
            }
            // Fall back to DamageType
            else if (_prototypeManager.TryIndex<DamageTypePrototype>(args[0], out var damageType))
            {
                func = (entity, ignoreResistances) =>
                {
                    var damage = new DamageSpecifier(damageType, amount);
                    EntitySystem.Get<SharedDamageableSystem>().TryChangeDamage(entity.Uid, damage, ignoreResistances);

                    shell.WriteLine($"Damaged entity {entity.Name} with id {entity.Uid} for {amount} {damageType} damage{(ignoreResistances ? ", ignoring resistances." : ".")}");

                };
                return true;

            }
            else
            {
                shell.WriteLine($"{args[0]} is not a valid damage class or type.");

                var types = DamageTypes();
                shell.WriteLine(types);

                func = null;
                return false;
            }
        }

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player as IPlayerSession;
            bool ignoreResistances;
            IEntity entity;
            Damage? damageFunc;

            switch (args.Length)
            {
                // Check if we have enough for the dmg types to show
                case var n when n > 0 && (args[0] == "?" || args[0] == "¿"):
                    var types = DamageTypes();

                    if (args[0] == "¿")
                    {
                        types = types.Replace('e', 'é');
                    }

                    shell.WriteLine(types);

                    return;
                // Not enough args
                case var n when n < 2:
                    shell.WriteLine($"Invalid number of arguments ({args.Length}).\n{Help}");
                    return;
                case var n when n >= 2 && n <= 4:

                    var entityUid = n == 2 ? "_" : args[2];

                    if (!TryParseEntity(shell, player, entityUid, out var parsedEntity))
                    {
                        return;
                    }

                    entity = parsedEntity;

                    if (!TryParseDamageArgs(shell, entity, args, out damageFunc))
                    {
                        return;
                    }

                    if (n == 4)
                    {
                        if (!bool.TryParse(args[3], out ignoreResistances))
                        {
                            shell.WriteLine($"{args[3]} is not a valid boolean value for ignoreResistances.\n{Help}");
                            return;
                        }
                    }
                    else
                    {
                        ignoreResistances = false;
                    }

                    break;
                default:
                    shell.WriteLine($"Invalid amount of arguments ({args.Length}).\n{Help}");
                    return;
            }

            if (!entity.TryGetComponent(out DamageableComponent? damageable))
            {
                shell.WriteLine($"Entity {entity.Name} with id {entity.Uid} does not have a {nameof(DamageableComponent)}.");
                return;
            }

            damageFunc(entity, ignoreResistances);
        }
    }
}
