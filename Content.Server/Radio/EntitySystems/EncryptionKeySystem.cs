using Content.Server.Radio.Components;
using Content.Shared.Examine;
using Content.Shared.Radio;
using Robust.Shared.Prototypes;

namespace Content.Server.Radio.EntitySystems;

public sealed class EncryptionKeySystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _protoManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EncryptionKeyComponent, ExaminedEvent>(OnExamined);
    }
    private void OnExamined(EntityUid uid, EncryptionKeyComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;
        // args.PushMarkup(Loc.GetString("examine-radio-frequency", ("frequency", component.BroadcastFrequency)));
        if(component.Channels.Count > 0)
        {
            args.PushMarkup(Loc.GetString("examine-encryption-key"));
            foreach (var id in component.Channels)
            {
                // if (id == "Common")
                //     continue;
                var proto = _protoManager.Index<RadioChannelPrototype>(id);
                args.PushMarkup(Loc.GetString("examine-encryption-key-channel",
                    ("color", proto.Color),
                    ("key", proto.KeyCode),
                    ("id", proto.LocalizedName),
                    ("freq", proto.Frequency)));
            }
        }
    }
}
