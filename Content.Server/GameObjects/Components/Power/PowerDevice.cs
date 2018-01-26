﻿using SS14.Server.GameObjects;
using SS14.Shared.GameObjects;
using SS14.Shared.Utility;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.RepresentationModel;

namespace Content.Server.GameObjects.Components.Power
{
    /// <summary>
    /// Component that requires power to function
    /// </summary>
    public class PowerDeviceComponent : Component
    {
        public override string Name => "PowerDevice";

        /// <summary>
        /// The method of draw we will try to use to place our load set via component parameter, defaults to not needing power
        /// </summary>
        public virtual DrawTypes Drawtype { get; protected set; } = DrawTypes.None;

        /// <summary>
        /// The power draw method we are currently connected to and using
        /// </summary>
        public DrawTypes Connected { get; protected set; } = DrawTypes.None;

        public bool _powered = false;
        /// <summary>
        /// Status indicator variable for powered
        /// </summary>
        public virtual bool Powered
        {
            get => _powered;
            set => SetPowered(value);
        }

        /// <summary>
        /// Priority for powernet draw, lower will draw first, defined in powernet.cs
        /// </summary>
        public virtual Powernet.Priority Priority { get; protected set; } = Powernet.Priority.Medium;


        private float _load = 100; //arbitrary magic number to start
        /// <summary>
        /// Power load from this entity
        /// </summary>
        public float Load
        {
            get => _load;
            set { UpdateLoad(value); }
        }

        /// <summary>
        /// All the power providers that we are within range of
        /// </summary>
        public List<PowerProviderComponent> AvailableProviders = new List<PowerProviderComponent>();


        private PowerProviderComponent _provider;
        /// <summary>
        /// A power provider that will handle our load, if we are linked to any
        /// </summary>
        public PowerProviderComponent Provider
        {
            get => _provider;
            set {
                Connected = DrawTypes.PowerProvider;
                if (_provider != null)
                {
                    _provider.RemoveDevice(this);
                }

                if(value != null)
                {
                    _provider = value;
                    _provider.AddDevice(this);
                }
                else
                {
                    Connected = DrawTypes.None;
                }
            }
        }

        public override void LoadParameters(YamlMappingNode mapping)
        {
            if (mapping.TryGetNode("Drawtype", out YamlNode node))
            {
                Drawtype = node.AsEnum<DrawTypes>();
            }
            if (mapping.TryGetNode("Load", out node))
            {
                Load = node.AsFloat();
            }
            if (mapping.TryGetNode("Priority", out node))
            {
                Priority = node.AsEnum<Powernet.Priority>();
            }
        }

        public override void Initialize()
        {
            if(Drawtype == DrawTypes.Both || Drawtype == DrawTypes.Node)
            {
                if(Owner.TryGetComponent(out PowerNodeComponent node))
                {
                    node.OnPowernetConnect += PowernetConnect;
                    node.OnPowernetDisconnect += PowernetDisconnect;
                }
            }
        }

        private void UpdateLoad(float value)
        {
            _load = value;
            if(Connected == DrawTypes.Node)
            {
                var node = Owner.GetComponent<PowerNodeComponent>();
                node.Parent.UpdateDevice(this);
            }
            else if(Connected == DrawTypes.PowerProvider)
            {
                Provider.UpdateDevice(this);
            }
        }

        /// <summary>
        /// Changes behavior when receiving a command to become powered or depowered
        /// </summary>
        public virtual void SetPowered(bool value)
        {
            //Let them set us to true
            if (value == true)
            {
                _powered = true;
                return;
            }

            //A powernet has decided we will not be powered this tick, lets try to power ourselves
            if (value == false && Owner.TryGetComponent(out PowerStorageComponent storage))
            {
                if (storage.CanDeductCharge(Load))
                {
                    storage.DeductCharge(Load);
                    _powered = true;
                    return;
                }
            }

            //For some reason above we could not power ourselves, we depower
            _powered = false;
            return;
        }

        /// <summary>
        /// Register a new power provider as a possible connection to this device
        /// </summary>
        public void AddProvider(PowerProviderComponent provider)
        {
            AvailableProviders.Add(provider);

            if(Connected != DrawTypes.Node)
            {
                ConnectToBestProvider();
            }
        }

        /// <summary>
        /// Find the nearest registered power provider and connect to it
        /// </summary>
        private void ConnectToBestProvider()
        {
            //Any values we can connect to or are we already connected to a node, cancel!
            if (!AvailableProviders.Any() || Connected == DrawTypes.Node)
                return;

            //Get the starting value for our loop
            var position = Owner.GetComponent<TransformComponent>().WorldPosition;
            var bestprovider = AvailableProviders[0];

            //If we are already connected to a power provider we need to do a loop to find the nearest one, otherwise skip it and use first entry
            if (Connected == DrawTypes.PowerProvider)
            {
                var bestdistance = (bestprovider.Owner.GetComponent<TransformComponent>().WorldPosition - position).LengthSquared;

                foreach (var availprovider in AvailableProviders)
                {
                    //Find distance to new provider
                    var distance = (availprovider.Owner.GetComponent<TransformComponent>().WorldPosition - position).LengthSquared;

                    //If new provider distance is shorter it becomes new best possible provider
                    if (distance < bestdistance)
                    {
                        bestdistance = distance;
                        bestprovider = availprovider;
                    }
                }
            }

            if(Provider != bestprovider)
                Provider = bestprovider;
        }

        /// <summary>
        /// Remove a power provider from being a possible connection to this device
        /// </summary>
        public void RemoveProvider(PowerProviderComponent provider)
        {
            if (!AvailableProviders.Contains(provider))
                return;
            
            AvailableProviders.Remove(provider);

            if (Connected != DrawTypes.Node)
            {
                ConnectToBestProvider();
            }
        }

        //Node has become anchored to a powernet
        private void PowernetConnect(object sender, PowernetEventArgs eventarg)
        {
            //This sets connected = none so it must be first
            Provider = null;

            eventarg.Powernet.AddDevice(this);
            Connected = DrawTypes.Node;
        }

        //Node has become unanchored from a powernet
        private void PowernetDisconnect(object sender, PowernetEventArgs eventarg)
        {
            eventarg.Powernet.RemoveDevice(this);
            Connected = DrawTypes.None;

            ConnectToBestProvider();
        }
    }

    public enum DrawTypes
    {
        None = 0,
        Node = 1,
        PowerProvider = 2,
        Both = 3
    }
}
