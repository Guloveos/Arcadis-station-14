using System.Linq;
using Content.Shared.Alert;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.Alerts;

[UsedImplicitly]
public sealed class ClientAlertsSystem : AlertsSystem
{
    public AlertOrderPrototype? AlertOrder { get; set; }

    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public event EventHandler? ClearAlerts;
    public event EventHandler<IReadOnlyDictionary<AlertKey, AlertState>>? SyncAlerts;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AlertsComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<AlertsComponent, PlayerDetachedEvent>(OnPlayerDetached);

        SubscribeLocalEvent<AlertsComponent, AfterAutoHandleStateEvent>(ClientAlertsHandleState);
    }
    protected override void LoadPrototypes()
    {
        base.LoadPrototypes();

        AlertOrder = _prototypeManager.EnumeratePrototypes<AlertOrderPrototype>().FirstOrDefault();
        if (AlertOrder == null)
            Log.Error("alert", "no alertOrder prototype found, alerts will be in random order");
    }

    public IReadOnlyDictionary<AlertKey, AlertState>? ActiveAlerts
    {
        get
        {
            var ent = _playerManager.LocalPlayer?.ControlledEntity;
            return ent is not null
                ? GetActiveAlerts(ent.Value)
                : null;
        }
    }

    protected override void AfterShowAlert(EntityUid uid, AlertsComponent alertsComponent)
    {
        UpdateHud(uid, alertsComponent);
    }

    protected override void AfterClearAlert(EntityUid uid, AlertsComponent alertsComponent)
    {
        UpdateHud(uid, alertsComponent);
    }

    private void ClientAlertsHandleState(EntityUid uid, AlertsComponent component, ref AfterAutoHandleStateEvent args)
    {
        UpdateHud(uid, component);
    }

    private void UpdateHud(EntityUid uid, AlertsComponent component)
    {
        if (_playerManager.LocalPlayer?.ControlledEntity == uid && _timing.IsFirstTimePredicted)
            SyncAlerts?.Invoke(this, component.Alerts);
    }

    private void OnPlayerAttached(EntityUid uid, AlertsComponent component, PlayerAttachedEvent args)
    {
        if (_playerManager.LocalPlayer?.ControlledEntity != uid)
            return;

        SyncAlerts?.Invoke(this, component.Alerts);
    }

    protected override void HandleComponentShutdown(EntityUid uid, AlertsComponent component, ComponentShutdown args)
    {
        base.HandleComponentShutdown(uid, component, args);

        if (_playerManager.LocalPlayer?.ControlledEntity != uid)
            return;

        ClearAlerts?.Invoke(this, EventArgs.Empty);
    }

    private void OnPlayerDetached(EntityUid uid, AlertsComponent component, PlayerDetachedEvent args)
    {
        ClearAlerts?.Invoke(this, EventArgs.Empty);
    }

    public void AlertClicked(AlertType alertType)
    {
        RaiseNetworkEvent(new ClickAlertEvent(alertType));
    }
}
