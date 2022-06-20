using System.Text;
using Content.Server.Hands.Components;
using Content.Server.Hands.Systems;
using Content.Server.Popups;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Server.Station.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Database;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Shuttles.Events;
using Content.Shared.Shuttles.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.Shuttles.Systems;

public sealed partial class ShuttleSystem
{
    /*
     * Handles the emergency shuttle's console and early launching.
     */

    [Dependency] private readonly AccessReaderSystem _reader = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    /// <summary>
    /// Has the emergency shuttle arrived?
    /// </summary>
    public bool EmergencyShuttleArrived { get; private set; }

    public bool EarlyLaunchAuthorized { get; private set; }

    /// <summary>
    /// How much time remaining until the shuttle consoles for emergency shuttles are unlocked?
    /// </summary>
    private float _consoleAccumulator;

    /// <summary>
    /// If an early launch is authorized how short is it.
    /// </summary>
    private TimeSpan _authorizeTime = TimeSpan.FromSeconds(10);
    private TimeSpan _transitTime = TimeSpan.FromMinutes(3);

    /// <summary>
    /// Have the emergency shuttles been authorised to launch at Centcomm?
    /// </summary>
    private bool _launchedShuttles;

    private void InitializeEmergencyConsole()
    {
        SubscribeLocalEvent<EmergencyShuttleConsoleComponent, ComponentStartup>(OnEmergencyStartup);
        SubscribeLocalEvent<EmergencyShuttleConsoleComponent, EmergencyShuttleAuthorizeMessage>(OnEmergencyAuthorize);
        SubscribeLocalEvent<EmergencyShuttleConsoleComponent, EmergencyShuttleRepealMessage>(OnEmergencyRepeal);
        SubscribeLocalEvent<EmergencyShuttleConsoleComponent, EmergencyShuttleRepealAllMessage>(OnEmergencyRepealAll);
    }

    private void OnEmergencyStartup(EntityUid uid, EmergencyShuttleConsoleComponent component, ComponentStartup args)
    {
        UpdateConsoleState(uid, component);
    }

    private void UpdateEmergencyConsole(float frameTime)
    {
        if (_consoleAccumulator <= 0f) return;

        _consoleAccumulator -= frameTime;

        if (!_launchedShuttles && _consoleAccumulator <= HyperspaceStartupTime)
        {
            _launchedShuttles = true;

            if (_centcommMap != null)
            {
                foreach (var comp in EntityQuery<StationDataComponent>(true))
                {
                    if (!TryComp<ShuttleComponent>(comp.EmergencyShuttle, out var shuttle)) continue;

                    Hyperspace(shuttle, new EntityCoordinates(_mapManager.GetMapEntityId(_centcommMap.Value), Vector2.One * 1000f), _consoleAccumulator);
                }
            }
        }

        if (_consoleAccumulator <= frameTime)
        {
            _launchedShuttles = true;
            _chatSystem.DispatchGlobalStationAnnouncement(
                $"The Emergency Shuttle has left the station. Estimate {_transitTime.Minutes} until the shuttle docks at Central Command.");
        }
    }

    private void OnEmergencyRepealAll(EntityUid uid, EmergencyShuttleConsoleComponent component, EmergencyShuttleRepealAllMessage args)
    {
        var player = args.Session.AttachedEntity;

        if (!TryComp<HandsComponent>(player, out var hands)) return;

        var activeEnt = hands.ActiveHandEntity;

        if (activeEnt == null ||
            !_idCard.TryGetIdCard(activeEnt.Value, out var idCard)) return;

        if (!_reader.FindAccessTags(idCard.Owner).Contains("EmergencyShuttleRepealAll"))
        {
            _popup.PopupCursor("Access denied", Filter.Entities(player.Value));
            return;
        }

        if (component.AuthorizedEntities.Count == 0) return;

        _logger.Add(LogType.EmergencyShuttle, LogImpact.High, $"Emergency shuttle early launch REPEAL ALL by {args.Session:user}");
        component.AuthorizedEntities.Clear();
        UpdateAllConsoles();
    }

    private void OnEmergencyRepeal(EntityUid uid, EmergencyShuttleConsoleComponent component, EmergencyShuttleRepealMessage args)
    {
        var player = args.Session.AttachedEntity;

        if (!TryComp<HandsComponent>(player, out var hands)) return;

        var activeEnt = hands.ActiveHandEntity;

        if (activeEnt == null ||
            !_idCard.TryGetIdCard(activeEnt.Value, out var idCard)) return;

        if (!_reader.IsAllowed(idCard.Owner, uid))
        {
            _popup.PopupCursor("Access denied", Filter.Entities(player.Value));
            return;
        }

        // TODO: This is fucking bad
        if (!component.AuthorizedEntities.Remove(idCard.FullName ?? idCard.OriginalOwnerName)) return;

        _logger.Add(LogType.EmergencyShuttle, LogImpact.High, $"Emergency shuttle early launch REPEAL by {args.Session:user}");
        var remaining = component.AuthorizationsRequired - component.AuthorizedEntities.Count;
        _chatSystem.DispatchGlobalStationAnnouncement($"Early launch authorization revoked, {remaining} authorizations needed");
        CheckForLaunch(component);
        UpdateAllConsoles();
    }

    private void OnEmergencyAuthorize(EntityUid uid, EmergencyShuttleConsoleComponent component, EmergencyShuttleAuthorizeMessage args)
    {
        var player = args.Session.AttachedEntity;

        if (!TryComp<HandsComponent>(player, out var hands)) return;

        var activeEnt = hands.ActiveHandEntity;

        if (activeEnt == null ||
            !_idCard.TryGetIdCard(activeEnt.Value, out var idCard)) return;

        if (!_reader.IsAllowed(idCard.Owner, uid))
        {
            _popup.PopupCursor("Access denied", Filter.Entities(player.Value));
            return;
        }

        // TODO: This is fucking bad
        if (!component.AuthorizedEntities.Add(idCard.FullName ?? idCard.OriginalOwnerName)) return;

        _logger.Add(LogType.EmergencyShuttle, LogImpact.High, $"Emergency shuttle early launch AUTH by {args.Session:user}");
        var remaining = component.AuthorizationsRequired - component.AuthorizedEntities.Count;
        _chatSystem.DispatchGlobalStationAnnouncement($"{remaining} authorizations needed until shuttle is launched early", playDefaultSound: false);
        SoundSystem.Play("/Audio/Misc/notice1.ogg", Filter.Broadcast());
        CheckForLaunch(component);
        UpdateAllConsoles();
    }

    private void CleanupEmergencyConsole()
    {
        _launchedShuttles = false;
        _consoleAccumulator = 0f;
        EarlyLaunchAuthorized = false;
        EmergencyShuttleArrived = false;
    }

    private void UpdateAllConsoles()
    {
        foreach (var comp in EntityQuery<EmergencyShuttleConsoleComponent>(true))
        {
            UpdateConsoleState(comp.Owner, comp);
        }
    }

    private void UpdateConsoleState(EntityUid uid, EmergencyShuttleConsoleComponent component)
    {
        var auths = new List<string>();

        foreach (var auth in component.AuthorizedEntities)
        {
            auths.Add(auth);
        }

        _uiSystem.GetUiOrNull(uid, EmergencyShuttleConsoleUiKey.Key)?.SetState(new EmergencyShuttleConsoleBoundUserInterfaceState()
        {
            EarlyLaunchTime = EarlyLaunchAuthorized ? IoCManager.Resolve<IGameTiming>().CurTime + TimeSpan.FromSeconds(_consoleAccumulator) : null,
            Authorizations = auths,
            AuthorizationsRequired = component.AuthorizationsRequired,
        });
    }

    private void CheckForLaunch(EmergencyShuttleConsoleComponent component)
    {
        if (component.AuthorizedEntities.Count < component.AuthorizationsRequired || EarlyLaunchAuthorized)
            return;

        _logger.Add(LogType.EmergencyShuttle, LogImpact.Extreme, $"Emergency shuttle launch authorized");
        _consoleAccumulator = MathF.Min(_consoleAccumulator, (float) _authorizeTime.TotalSeconds);
        EarlyLaunchAuthorized = true;
        RaiseLocalEvent(new EmergencyShuttleAuthorizedEvent());
        _chatSystem.DispatchGlobalStationAnnouncement($"The emergency shuttle will launch in {_consoleAccumulator:0} seconds", playDefaultSound: false);
        UpdateAllConsoles();
    }
}
