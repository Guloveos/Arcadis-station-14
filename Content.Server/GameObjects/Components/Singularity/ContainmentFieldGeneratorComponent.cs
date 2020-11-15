#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.GameObjects.Components.Projectiles;
using Content.Server.Utility;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Physics;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Singularity
{
    [RegisterComponent]
    public class ContainmentFieldGeneratorComponent : Component, ICollideBehavior
    {
        [Dependency] private IPhysicsManager _physicsManager = null!;

        public override string Name => "ContainmentFieldGenerator";

        private int _powerBuffer;

        [ViewVariables]
        public int PowerBuffer
        {
            get => _powerBuffer;
            set => _powerBuffer = Math.Clamp(value, 0, 6);
        }

        public void ReceivePower(int power)
        {
            var totalPower = power + PowerBuffer;
            var powerPerConnection = totalPower  / 2;
            var newBuffer = totalPower % 2;
            TryPowerConnection(ref _connection1, ref newBuffer, powerPerConnection);
            TryPowerConnection(ref _connection2, ref newBuffer, powerPerConnection);

            PowerBuffer = newBuffer;
        }

        private void TryPowerConnection(ref Tuple<Direction, ContainmentFieldConnection>? connectionProperty, ref int powerBuffer, int powerPerConnection)
        {
            if (connectionProperty != null)
            {
                connectionProperty.Item2.SharedEnergyPool += powerPerConnection;
            }
            else
            {
                if (TryGenerateFieldConnection(ref connectionProperty))
                {
                    connectionProperty.Item2.SharedEnergyPool += powerPerConnection;
                }
                else
                {
                    powerBuffer += powerPerConnection;
                }
            }
        }

        private PhysicsComponent? _collidableComponent;

        private Tuple<Direction, ContainmentFieldConnection>? _connection1;
        private Tuple<Direction, ContainmentFieldConnection>? _connection2;

        public bool CanRepell(IEntity toRepell) => _connection1?.Item2?.CanRepell(toRepell) == true ||
                                                   _connection2?.Item2?.CanRepell(toRepell) == true;

        public override void Initialize()
        {
            base.Initialize();
            if (!Owner.TryGetComponent(out _collidableComponent))
            {
                Logger.Error("ContainmentFieldGeneratorComponent created with no CollidableComponent");
                return;
            }
            _collidableComponent.AnchoredChanged += OnAnchoredChanged;
        }


        private void OnAnchoredChanged()
        {
            if(_collidableComponent?.Anchored == true)
            {
                Owner.SnapToGrid();
            }
            else
            {
                _connection1?.Item2.Dispose();
                _connection2?.Item2.Dispose();
            }
        }

        private bool IsConnectedWith(ContainmentFieldGeneratorComponent comp)
        {

            return comp == this || _connection1?.Item2.Generator1 == comp || _connection1?.Item2.Generator2 == comp ||
                   _connection2?.Item2.Generator1 == comp || _connection2?.Item2.Generator2 == comp;
        }

        public bool HasFreeConnections()
        {
            return _connection1 == null || _connection2 == null;
        }

        private bool TryGenerateFieldConnection([NotNullWhen(true)] ref Tuple<Direction, ContainmentFieldConnection>? propertyFieldTuple)
        {
            if (propertyFieldTuple != null) return false;
            if(_collidableComponent?.Anchored == false) return false;

            foreach (var direction in new[] {Direction.North, Direction.East, Direction.South, Direction.West})
            {
                if (_connection1?.Item1 == direction || _connection2?.Item1 == direction) continue;

                var dirVec = direction.ToVec();
                var ray = new CollisionRay(Owner.Transform.WorldPosition, dirVec, (int) CollisionGroup.MobMask);
                var rawRayCastResults = _physicsManager.IntersectRay(Owner.Transform.MapID, ray, 4.5f, Owner, false);

                var rayCastResults = rawRayCastResults as RayCastResults[] ?? rawRayCastResults.ToArray();
                if(!rayCastResults.Any()) continue;

                RayCastResults? closestResult = null;
                var smallestDist = 4.5f;
                foreach (var res in rayCastResults)
                {
                    if (res.Distance > smallestDist) continue;

                    smallestDist = res.Distance;
                    closestResult = res;
                }
                if(closestResult == null) continue;
                var ent = closestResult.Value.HitEntity;
                if (!ent.TryGetComponent<ContainmentFieldGeneratorComponent>(out var fieldGeneratorComponent) ||
                    fieldGeneratorComponent.Owner == Owner ||
                    !fieldGeneratorComponent.HasFreeConnections() ||
                    IsConnectedWith(fieldGeneratorComponent) ||
                    !ent.TryGetComponent<PhysicsComponent>(out var collidableComponent) ||
                    !collidableComponent.Anchored)
                {
                    continue;
                }

                var connection = new ContainmentFieldConnection(this, fieldGeneratorComponent);
                propertyFieldTuple = new Tuple<Direction, ContainmentFieldConnection>(direction, connection);
                if (fieldGeneratorComponent._connection1 == null)
                {
                    fieldGeneratorComponent._connection1 = new Tuple<Direction, ContainmentFieldConnection>(direction.GetOpposite(), connection);
                }
                else if (fieldGeneratorComponent._connection2 == null)
                {
                    fieldGeneratorComponent._connection2 = new Tuple<Direction, ContainmentFieldConnection>(direction.GetOpposite(), connection);
                }
                else
                {
                    Logger.Error("When trying to connect two Containmentfieldgenerators, the second one already had two connection but the check didn't catch it");
                }

                return true;
            }

            return false;
        }

        public void RemoveConnection(ContainmentFieldConnection? connection)
        {
            if (_connection1?.Item2 == connection)
            {
                _connection1 = null;
            }else if (_connection2?.Item2 == connection)
            {
                _connection2 = null;
            }
            else if(connection != null)
            {
                Logger.Error("RemoveConnection called on Containmentfieldgenerator with a connection that can't be found in its connections.");
            }
        }

        public void CollideWith(IEntity collidedWith)
        {
            if(collidedWith.HasComponent<EmitterBoltComponent>())
            {
                ReceivePower(4);
            }
        }

        public override void OnRemove()
        {
            _connection1?.Item2.Dispose();
            _connection2?.Item2.Dispose();
            base.OnRemove();
        }
    }
}
