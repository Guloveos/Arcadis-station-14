using System;
using Content.Server.GameObjects.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Movement
{
    [RegisterComponent]
    public class PullableComponent: Component
    {
        private const float DistBeforePull = 1.0f;

        private const float DistBeforeStopPull = InteractionSystem.InteractionRange;

        private PhysicsComponent _physicsComponent;

        private IEntity _puller;
        public override string Name => "Pullable";

        [ViewVariables(VVAccess.ReadOnly)]
        public IEntity Puller => _puller;

        public bool GettingPulled => _puller != null;

        public override void Initialize()
        {
            base.Initialize();

            _physicsComponent = Owner.GetComponent<PhysicsComponent>();

            _puller = null;
        }

        public void GetPulled(IEntity puller)
        {
            _puller = puller;
        }

        public void StopPull()
        {
            _physicsComponent.LinearVelocity = Vector2.Zero;
            _puller = null;
        }

        public void MoveTo(GridCoordinates coords)
        {
            var position = new GridCoordinates((float) (Math.Floor(coords.X) + 0.5), (float) (Math.Floor(coords.Y) + 0.5), coords.GridID);
            var dist = _puller.Transform.GridPosition.Position - position.Position;
            if (Math.Sqrt(dist.LengthSquared) > DistBeforeStopPull) return;
            Owner.Transform.GridPosition = position;
        }

        public void Update()
        {
            if (_puller == null) return;

            var pullerPhysics = _puller.GetComponent<PhysicsComponent>();

            // Are we outside of pulling range?
            var dist = _puller.Transform.WorldPosition - Owner.Transform.WorldPosition;
            if (dist.Length > DistBeforeStopPull)
            {
                _puller.GetComponent<HandsComponent>().StopPulling();
            }
            else if (dist.Length > DistBeforePull)
            {
                _physicsComponent.LinearVelocity = dist.Normalized * pullerPhysics.LinearVelocity.Length;
            }
            else
            {
                _physicsComponent.LinearVelocity = Vector2.Zero;
            }
        }
    }
}
