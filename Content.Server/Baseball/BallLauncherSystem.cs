using Content.Server.Popups;
using Content.Server.Power.EntitySystems;
using Content.Server.Projectiles.Components;
using Content.Server.Singularity.Components;
using Content.Shared.Interaction;
using Content.Shared.Throwing;
using Robust.Shared.Physics;
using Robust.Shared.Player;

namespace Content.Server.Baseball
{
    /// <summary>
    /// This handles...
    /// </summary>
    public sealed class BallLauncherSystem : EntitySystem
    {
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly ThrowingSystem _throwingSystem = default!;

        /// <inheritdoc/>
        public override void Initialize()
        {
            base.Initialize();
            //SubscribeLocalEvent<BallLauncherComponent, PowerConsumerReceivedChanged>(ReceivedChanged);
            SubscribeLocalEvent<BallLauncherComponent, InteractHandEvent>(OnInteractHand);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            foreach (var ballLauncher in EntityQuery<BallLauncherComponent>())
            {
                if (!ballLauncher.IsOn)
                    return;

                ballLauncher.AccumulatedFrametime += frameTime;

                if (ballLauncher.AccumulatedFrametime < ballLauncher.CurrentLauncherCooldown)
                    continue;

                ballLauncher.AccumulatedFrametime -= ballLauncher.CurrentLauncherCooldown;
                ballLauncher.CurrentLauncherCooldown = ballLauncher.ShootSpeed;


                Fire(ballLauncher.Owner);
            }
        }

        public void OnInteractHand(EntityUid uid, BallLauncherComponent component, InteractHandEvent args)
        {
            args.Handled = true;

            if (EntityManager.TryGetComponent(component.Owner, out PhysicsComponent? phys) && phys.BodyType == BodyType.Static)
            {
                if (!component.IsOn)
                {
                    SwitchOn(component);
                    _popupSystem.PopupEntity("Turned on", component.Owner, Filter.Pvs(component.Owner));
                }
                else
                {
                    SwitchOff(component);
                    _popupSystem.PopupEntity("Turned off", component.Owner, Filter.Pvs(component.Owner));

                }
            }

        }

        private void Fire(EntityUid uid)
        {
            if (!TryComp<BallLauncherComponent>(uid, out var component))
                return;

            _popupSystem.PopupEntity("fire", component.Owner, Filter.Pvs(component.Owner));

            var projectile = EntityManager.SpawnEntity("Football", EntityManager.GetComponent<TransformComponent>(component.Owner).Coordinates);

            /*
            if (!EntityManager.TryGetComponent<PhysicsComponent?>(projectile, out var physicsComponent))
                return;

            physicsComponent.BodyStatus = BodyStatus.InAir;
            */
            var dir = EntityManager.GetComponent<TransformComponent>(component.Owner).WorldRotation.ToWorldVec() * 20f;

            _throwingSystem.TryThrow(projectile, dir, 10f, uid);

        }

        public void SwitchOff(BallLauncherComponent component)
        {
            component.IsOn = false;
            //if (TryComp<PowerConsumerComponent>(component.Owner, out var powerConsumer)) powerConsumer.DrawRate = 0;
            //PowerOff(component);
            //UpdateAppearance(component);
        }

        public void SwitchOn(BallLauncherComponent component)
        {
            component.IsOn = true;
            //if (TryComp<PowerConsumerComponent>(component.Owner, out var powerConsumer)) powerConsumer.DrawRate = component.PowerUseActive;
            // Do not directly PowerOn().
            // OnReceivedPowerChanged will get fired due to DrawRate change which will turn it on.
            //UpdateAppearance(component);
        }

    }
}
