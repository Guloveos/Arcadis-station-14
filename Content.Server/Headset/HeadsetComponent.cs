using Content.Server.Radio.Components;
using Content.Server.Radio.EntitySystems;
using Content.Shared.Chat;
using Content.Shared.Radio;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Server.Headset
{
    [RegisterComponent]
    [ComponentReference(typeof(IRadio))]
    [ComponentReference(typeof(IListen))]
#pragma warning disable 618
    public sealed class HeadsetComponent : Component, IListen, IRadio
#pragma warning restore 618
    {
        [Dependency] private readonly IEntityManager _entMan = default!;
        [Dependency] private readonly IServerNetManager _netManager = default!;

        private RadioSystem _radioSystem = default!;

        [DataField("channels", customTypeSerializer:typeof(PrototypeIdHashSetSerializer<RadioChannelPrototype>))]
        public HashSet<string> Channels = new()
        {
            "Common"
        };

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("listenRange")]
        public int ListenRange { get; private set; }

        public bool RadioRequested { get; set; }

        protected override void Initialize()
        {
            base.Initialize();

            _radioSystem = EntitySystem.Get<RadioSystem>();
        }

        public bool CanListen(string message, EntityUid source, RadioChannelPrototype prototype)
        {
            return Channels.Contains(prototype.ID) && RadioRequested;
        }

        public void Receive(string message, RadioChannelPrototype channel, EntityUid source, bool announcement = false)
        {
            if (!Channels.Contains(channel.ID) || !Owner.TryGetContainer(out var container))
                return;

            if (!_entMan.TryGetComponent(container.Owner, out ActorComponent? actor))
                return;

            var playerChannel = actor.PlayerSession.ConnectedClient;

            var msg = new MsgChatMessage
            {
                Channel = ChatChannel.Radio,
                Message = message,
                //Square brackets are added here to avoid issues with escaping
                MessageWrap = Loc.GetString("chat-radio-message-wrap", ("color", channel.Color), ("channel", $"\\[{channel.LocalizedName}\\]"), ("name", _entMan.GetComponent<MetaDataComponent>(source).EntityName))
            };

            // If the source isn't a player, we set the chat message accordingly
            if (announcement)
            {
                msg = new MsgChatMessage
                {
                    Channel = ChatChannel.Radio,
                    Message = message,
                    //Square brackets are added here to avoid issues with escaping
                    MessageWrap = Loc.GetString("announcement-radio-message-wrap", ("color", channel.Color), ("channel", $"\\[{channel.LocalizedName}\\]"))
                };
            }

            _netManager.ServerSendMessage(msg, playerChannel);
        }

        public void Listen(string message, EntityUid speaker, RadioChannelPrototype channel)
        {
            Broadcast(message, speaker, channel);
        }

        public void Broadcast(string message, EntityUid speaker, RadioChannelPrototype channel)
        {
            if (!Channels.Contains(channel.ID)) return;

            _radioSystem.SpreadMessage(this, speaker, message, channel);
            RadioRequested = false;
        }
    }
}
