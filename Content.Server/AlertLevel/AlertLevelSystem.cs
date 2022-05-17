using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.AlertLevel;

public sealed class AlertLevelSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly AdminLogSystem _adminLogSystem = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;

    // Until stations are a prototype, this is how it's going to have to be.
    public const string DefaultAlertLevelSet = "stationAlerts";

    public override void Initialize()
    {
        SubscribeLocalEvent<StationInitializedEvent>(OnStationInitialize);
    }

    private void OnStationInitialize(StationInitializedEvent args)
    {
        var alertLevelComponent = AddComp<AlertLevelComponent>(args.Station);

        if (!_prototypeManager.TryIndex(DefaultAlertLevelSet, out AlertLevelPrototype? alerts))
        {
            return;
        }

        alertLevelComponent.AlertLevels = alerts;

        var defaultLevel = alertLevelComponent.AlertLevels.DefaultLevel;
        if (string.IsNullOrEmpty(defaultLevel))
        {
            defaultLevel = alertLevelComponent.AlertLevels.Levels.Keys.First();
        }

        SetLevel(args.Station, defaultLevel, false, false);
    }

    // Set the alert level based on the station's entity ID.
    public void SetLevel(EntityUid station, string level, bool playSound, bool announce, bool force = false, MetaDataComponent? dataComponent = null, AlertLevelComponent? component = null)
    {
        if (!Resolve(station, ref component, ref dataComponent)
            || component.AlertLevels == null
            || !component.AlertLevels.Levels.TryGetValue(level, out var detail)
            || (!detail.Selectable && !force))
        {
            return;
        }

        component.CurrentLevel = level;

        var stationName = dataComponent.EntityName;

        var name = Loc.GetString($"alert-level-{level}");

        // Announcement text. Is passed into announcementFull.
        var announcement = Loc.GetString(detail.Announcement);

        // The full announcement to be spat out into chat.
        var announcementFull = Loc.GetString("alert-level-announcement", ("name", name), ("announcement", announcement));

        if (announce)
        {
            _chatManager.DispatchStationAnnouncement(announcementFull, playDefaultSound: false,
                colorOverride: detail.Color, sender: stationName);
        }

        if (playSound && detail.Sound != null)
        {
            SoundSystem.Play(Filter.Broadcast(), detail.Sound.GetSound());
        }
    }
}

